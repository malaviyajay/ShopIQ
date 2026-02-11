using EC.Data;
using EC.Helpers;
using EC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EC.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly DbHelper _db;
        public OrderController(DbHelper db) => _db = db;

        public IActionResult Index()
        {
            int userId = HttpContext.UserId() ?? 0;

            var orders = _db.GetOrdersByUser(userId);
            return View(orders);
        }
        public void SetCartCount()
{
    var cart = Request.Cookies["Cart"];
    int count = 0;

    if (!string.IsNullOrEmpty(cart))
        count = cart.Split(',').Length;

    ViewBag.CartCount = count;
}

    }
}
