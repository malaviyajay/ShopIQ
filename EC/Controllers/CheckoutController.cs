using EC.Data;
using EC.Helpers;
using EC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;

namespace EC.Controllers;

[Authorize]
public class CheckoutController : Controller
{
    private readonly DbHelper _db;
    private readonly IConfiguration _config;

    public CheckoutController(DbHelper db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    private bool IsLoggedIn() => HttpContext.IsLoggedIn();

    public IActionResult Index()
    {
        var cart = Request.Cookies["Cart"];
        var cartItems = _db.GetCartItemsFromCookie(cart);

        int userId = HttpContext.UserId() ?? 0;
        string userName = HttpContext.UserName() ?? "";

        var model = new CheckoutViewModel
        {
            UserId = userId,
            UserName = userName,
            TotalAmount = cartItems.Sum(x => x. Price * x.Quantity),
            Items = cartItems.Select(x =>
            {
                var product = _db.GetProducts(x.ProductId);
                return new OrderItem
                {
                    ProductId = x.ProductId,
                    ProductName = product?.Name ?? "Unknown",
                    Price = x.Price,
                    Quantity = x.Quantity,
                    SellerId = product?.SellerId ?? 0
                };
            }).ToList()
        };

        return View(model);
    }

    //==============PlaceOrder=========================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult PlaceOrder(CheckoutViewModel model)
    {
        var user = _db.GetUserProfile(model.UserId);
        if (user is null) return BadRequest("User not found");

        var cart = Request.Cookies["Cart"];
        var cartItems = _db.GetCartItemsFromCookie(cart);

        // Map cart items to OrderItem
        var orderItems = cartItems.Select(x =>
        {
            var product = _db.GetProducts(x.ProductId);
            return new OrderItem
            {
                ProductId = x.ProductId,
                ProductName = product?.Name ?? "Unknown",
                Price = x.Price,
                Quantity = x.Quantity,
                SellerId = product?.SellerId ?? 0
            };
        }).ToList();

        // Split items by SellerId
        var sellerGroups = orderItems.GroupBy(x => x.SellerId);

        if (model.PaymentMethod == "COD")
        {
            // Create separate COD orders per seller
            foreach (var sellerGroup in sellerGroups)
            {
                var sellerOrder = new CheckoutViewModel
                {
                    UserId = user.Id,
                    UserName = user.Name,
                    Items = sellerGroup.ToList(),
                    TotalAmount = sellerGroup.Sum(x => x.Price * x.Quantity),
                    PaymentMethod = "COD",
                    Status = "Not Paid"
                };
                _db.PlaceOrder(sellerOrder);
            }

            Response.Cookies.Delete("Cart");
            return RedirectToAction("Success");
        }
        else
        {
            // Online payment: Ensure Stripe customer exists
            if (string.IsNullOrWhiteSpace(user.StripeCustomerId))
            {
                var custService = new CustomerService();
                var stripeCustomer = custService.Create(new CustomerCreateOptions
                {
                    Email = user.Email,
                    Name = user.Name,
                    Metadata = new Dictionary<string, string>
                    {
                        { "UserId", user.Id.ToString() }
                    }
                });
                _db.UpdateStripeCustId(user.Id, stripeCustomer.Id);
                user.StripeCustomerId = stripeCustomer.Id;
            }

            var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
            var successUrl = baseUrl + "/Checkout/PaymentSuccess?sessionId={CHECKOUT_SESSION_ID}&userId=" + user.Id;
            var cancelUrl = baseUrl + "/Checkout/Index";

            // Create separate Stripe session per seller
            var sellerSessions = new List<string>();
            foreach (var sellerGroup in sellerGroups)
            {
                var options = new SessionCreateOptions
                {
                    Customer = user.StripeCustomerId,
                    PaymentMethodTypes = new List<string> { "card" },
                    LineItems = sellerGroup.Select(item => new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = (long)(item.Price * 100),
                            Currency = "inr",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = item.ProductName
                            }
                        },
                        Quantity = item.Quantity
                    }).ToList(),
                    Mode = "payment",
                    Metadata = new Dictionary<string, string>
                    {
                        { "UserId", user.Id.ToString() },
                        { "SellerId", sellerGroup.Key.ToString() }
                    },
                    SuccessUrl = successUrl,
                    CancelUrl = cancelUrl
                };

                var service = new SessionService();
                var session = service.Create(options);
                sellerSessions.Add(session.Url); // store session urls if needed
            }

            // Redirect to first seller's payment session (you can adjust for multiple payments)
            return Redirect(sellerSessions.First());
        }
    }

    public IActionResult Success()
    {
        return View();
    }

    // Stripe success redirect
    public IActionResult PaymentSuccess(int userId, string sessionId)
    {
        var user = _db.GetUserProfile(userId);
        if (user is null) throw new Exception("User does not exist");

        var sessionService = new SessionService();
        var stripeSession = sessionService.Get(sessionId);
        if (stripeSession is null) throw new Exception("Stripe session not found");

        var cart = Request.Cookies["Cart"];
        var cartItems = _db.GetCartItemsFromCookie(cart);

        // Map cart items to OrderItem
        var orderItems = cartItems.Select(x =>
        {
            var product = _db.GetProducts(x.ProductId);
            return new OrderItem
            {
                ProductId = x.ProductId,
                ProductName = product?.Name ?? "Unknown",
                Price = x.Price,
                Quantity = x.Quantity,
                SellerId = product?.SellerId ?? 0
            };
        }).ToList();

        // Place order only for this seller (from session metadata)
        int sellerId = int.Parse(stripeSession.Metadata["SellerId"]);
        var sellerItems = orderItems.Where(x => x.SellerId == sellerId).ToList();

        var sellerOrder = new CheckoutViewModel
        {
            UserId = user.Id,
            UserName = user.Name,
            Items = sellerItems,
            TotalAmount = sellerItems.Sum(x => x.Price * x.Quantity),
            PaymentMethod = "Online",
            Status = "Paid",
            StripeCheckoutSessionId = stripeSession.Id,
            StripePaymentId = stripeSession.PaymentIntentId
        };

        var orderId = _db.PlaceOrder(sellerOrder);

        // Update Stripe session metadata
        sessionService.Update(stripeSession.Id, new SessionUpdateOptions
        {
            Metadata = new Dictionary<string, string>
            {
                { "OrderId", orderId.ToString() },
                { "UserId", user.Id.ToString() },
                { "SellerId", sellerId.ToString() }
            }
        });

        var piService = new PaymentIntentService();
        piService.Update(stripeSession.PaymentIntentId, new PaymentIntentUpdateOptions
        {
            Metadata = new Dictionary<string, string>
            {
                { "OrderId", orderId.ToString() },
                { "UserId", user.Id.ToString() },
                { "SellerId", sellerId.ToString() }
            }
        });

        Response.Cookies.Delete("Cart");
        return RedirectToAction("Success");
    }
}