using EC.Controllers;
using EC.Data;
using EC.Helpers;
using EC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace EC.Controllers
{
    // ================= User Orders =================
    [Authorize(Roles = "Customer")]
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

        [HttpGet]
        public IActionResult GetOrderDetails(int id)
        {
            int userId = HttpContext.UserId() ?? 0;
            var items = _db.GetOrderItems(id, userId); // filter by logged-in user
            if (!items.Any())
                return Content("<div>No products found for this order.</div>", "text/html");

            decimal orderTotal = items.Sum(x => x.Price * x.Quantity);

            string html = "<table style='width:100%; border-collapse: collapse;'>" +
                          "<thead><tr style='background:#f3f4f6;'>" +
                          "<th style='padding:8px; border:1px solid #ddd;'>Product</th>" +
                          "<th style='padding:8px; border:1px solid #ddd;'>Qty</th>" +
                          "<th style='padding:8px; border:1px solid #ddd;'>Price</th>" +
                          "<th style='padding:8px; border:1px solid #ddd;'>Total</th>" +
                          "</tr></thead><tbody>";

            foreach (var item in items)
            {
                decimal total = item.Price * item.Quantity;
                html += $"<tr>" +
                        $"<td style='padding:8px; border:1px solid #ddd;'>{item.ProductName}</td>" +
                        $"<td style='padding:8px; border:1px solid #ddd;'>{item.Quantity}</td>" +
                        $"<td style='padding:8px; border:1px solid #ddd;'>₹{item.Price}</td>" +
                        $"<td style='padding:8px; border:1px solid #ddd;'>₹{total}</td>" +
                        $"</tr>";
            }

            html += $"</tbody></table>" +
                    $"<div style='margin-top:15px; font-weight:bold; text-align:right;'>Total: ₹{orderTotal}</div>";

            return Content(html, "text/html");
        }
    }
}
