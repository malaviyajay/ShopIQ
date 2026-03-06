using EC.Data;
using EC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;

namespace EC.Controllers
{
    [Authorize]
    public class PaymentController : Controller
    {
        private readonly DbHelper _db;

        public PaymentController(DbHelper db, IConfiguration config)
        {
            _db = db;
        }

        [HttpPost]
        public IActionResult CreateCheckoutSession(int orderId, decimal amount)
        {
            // Create Stripe Checkout Session
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = (long)(amount * 100), // amount in paise
                            Currency = "inr",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = $"Order #{orderId}"
                            },
                        },
                        Quantity = 1
                    }
                },
                Mode = "payment",
                SuccessUrl = Url.Action("Success", "Payment", new { orderId = orderId }, Request.Scheme),
                CancelUrl = Url.Action("Cancel", "Payment", new { orderId = orderId }, Request.Scheme)
            };

            var service = new SessionService();
            Session session = service.Create(options);

            return Redirect(session.Url);
        }

        public IActionResult Success(int orderId, string sessionId)
        {
            Order order = _db.GetAllOrders().FirstOrDefault(o => o.Id == orderId);
            if (order != null && order.Status != "Paid")
            {
                order.Status = "Paid";
                order.PaymentMethod = "Online";

                // Get Stripe session details
                var service = new SessionService();
                var session = service.Get(sessionId);
                order.PaymentId = session.PaymentIntentId; // Save the Payment ID

                _db.PlaceOrder(new CheckoutViewModel
                {
                    UserId = order.UserId,
                    UserName = order.UserName,
                    TotalAmount = order.TotalAmount,
                    PaymentMethod = order.PaymentMethod,
                    Items = order.Items
                });
            }

            return View();
        }

        public IActionResult Cancel(int orderId)
        {
            // Payment failed or cancelled
            return View();
        }
    }
}
