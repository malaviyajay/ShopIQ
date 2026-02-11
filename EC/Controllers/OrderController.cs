using EC.Data;
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
            // ✅ Check if UserId claim exists
            var userIdClaim = User.FindFirst("UserId");
            if (userIdClaim == null)
                return RedirectToAction("Login", "Account");

            int userId = int.Parse(userIdClaim.Value);

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
