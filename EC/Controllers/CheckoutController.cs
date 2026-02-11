using EC.Data;
using EC.Helpers;
using EC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EC.Controllers;

[Authorize]
public class CheckoutController : Controller
{
    private readonly DbHelper _db;
    public CheckoutController(DbHelper db) => _db = db;

    private bool IsLoggedIn() =>
        HttpContext.IsLoggedIn();

    public IActionResult Index()
    {
        var cart = Request.Cookies["Cart"];
        var cartItems = _db.GetCartItemsFromCookie(cart);

        decimal total = cartItems.Sum(x => x.Price * x.Quantity);

        // Get user data from cookie
        int userId = HttpContext.UserId() ?? 0;
        string userName = HttpContext.UserName() ?? default!;

        var model = new CheckoutViewModel
        {
            UserId = userId,
            UserName = userName,
            TotalAmount = total,
            Items = cartItems.Select(x => new OrderItem
            {
                ProductId = x.ProductId,
                ProductName = _db.GetProduct(x.ProductId)?.Name ?? "Unknown",
                Price = x.Price,
                Quantity = x.Quantity
            }).ToList()
        };

        return View(model);
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult PlaceOrder(CheckoutViewModel model)
    {
        // Recalculate from cart (SECURE)
        var cart = Request.Cookies["Cart"];
        var cartItems = _db.GetCartItemsFromCookie(cart);

        model.Items = cartItems.Select(x => new OrderItem
        {
            ProductId = x.ProductId,
            ProductName = _db.GetProduct(x.ProductId)?.Name ?? "Unknown",
            Price = x.Price,
            Quantity = x.Quantity
        }).ToList();

        model.TotalAmount = model.Items.Sum(x => x.Price * x.Quantity);

        model.UserId = HttpContext.UserId() ?? 0;
        model.UserName = HttpContext.UserName() ?? "";

        _db.PlaceOrder(model);

        Response.Cookies.Delete("Cart");

        return RedirectToAction("Success");
    }


    public IActionResult Success()
    {
        return View();
    }
}
