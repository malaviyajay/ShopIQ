using EC.Data;
using EC.Helpers;
using EC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace EC.Controllers
{
    [Authorize(Roles = "Seller")]
    public class SellerController : Controller
    {
        private readonly DbHelper _db;
        private readonly EmailHelper _email;

        public SellerController(DbHelper db, EmailHelper email)
        {
            _db = db;
            _email = email;
        }
        // ================= HELPER =================
        private int GetCurrentUserId()
        {
            var claim = User.Claims.FirstOrDefault(c => c.Type == "UserId");

            if (claim == null || !int.TryParse(claim.Value, out int userId))
                throw new Exception("User not logged in");

            return userId;
        }

        // ================= HOME =================
        public IActionResult Index()
        {
            return RedirectToAction("Dashboard");
        }

        // ================= DASHBOARD =================
        public IActionResult Dashboard()
        {
            int sellerId = GetCurrentUserId();
            SellerDashboardViewModel model = _db.GetSellerDashboardData(sellerId);
            return View(model);
        }

        // ================= PRODUCTS =================
        public IActionResult Products()
        {
            int sellerId = GetCurrentUserId();
            var products = _db.GetProductsBySeller(sellerId) ?? new List<Product>();
            return View(products);
        }

        // ================= ADD PRODUCT =================
        public IActionResult Add()
        {
            ViewBag.Categories = _db.GetCategories() ?? new List<Category>();
            return View();
        }

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public IActionResult Add(Product p)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        ViewBag.Categories = _db.GetCategories() ?? new List<Category>();
        //        return View(p);
        //    }

        //    p.SellerId = GetCurrentUserId();
        //    _db.AddProduct(p);

        //    TempData["Success"] = "Product added successfully!";
        //    return RedirectToAction("Products");
        //}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Add(Product p, IFormFile imageFile)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = _db.GetCategories() ?? new List<Category>();
                return View(p);
            }

            // Handle image upload
            if (imageFile != null && imageFile.Length > 0)
            {
                string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");

                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                string filePath = Path.Combine(folderPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    imageFile.CopyTo(stream);
                }

                p.Image = fileName;
            }

            p.SellerId = GetCurrentUserId();

            _db.AddProduct(p);

            TempData["Success"] = "Product added successfully!";
            return RedirectToAction("Products");
        }

        // ================= EDIT PRODUCT =================
        public IActionResult Edit(int id)
        {
            var product = _db.GetProductById(id);
            int userId = GetCurrentUserId();

            if (product == null)
                return NotFound();

            if (product.SellerId != userId)
            {
                TempData["Error"] = "You cannot edit this product.";
                return RedirectToAction("Products");
            }

            ViewBag.Categories = _db.GetCategories() ?? new List<Category>();
            return View(product);
        }
        [HttpPost]

        public IActionResult Edit(Product p)
        {
            var existing = _db.GetProductById(p.Id);

            int userId = GetCurrentUserId();

            if (existing == null || existing.SellerId != userId)
                return Unauthorized();

            p.SellerId = existing.SellerId;

            _db.UpdateProduct(p);

            TempData["Success"] = "Product updated successfully!";
            return RedirectToAction("Products");
        }
        //==================== Order details =========================
        public IActionResult OrderDetails(int id)
        {
            var items = _db.GetOrderItems(id);
            return View(items);
        }
        // ================= DELETE PRODUCT =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var product = _db.GetProductById(id);

            int userId = GetCurrentUserId();

            if (product == null || product.SellerId != userId)
                return Unauthorized();

            _db.DeleteProduct(id);

            TempData["Success"] = "Product deleted successfully!";
            return RedirectToAction("Products");
        }
        //======Orders============
        public IActionResult Orders()
        {
            int sellerId = GetCurrentUserId();
            var orders = _db.GetOrdersBySeller(sellerId);
            return View(orders);
        }
        //public IActionResult Orders()
        //{
        //    int sellerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        //    var orders = _db.Orders
        //        .Where(o => o.SellerId == sellerId)
        //        .Select(o => new OrderDetailViewModel
        //        {
        //            ProductId = p.Id,
        //            UserName = o.User.Name,
        //            OrderDate = o.OrderDate,
        //            Price = o.TotalAmount,
        //            PaymentMethod = o.PaymentMethod,
        //            Status = o.Status
        //        })
        //        .OrderByDescending(o => o.OrderDate)
        //        .ToList();

        //    return View(orders);
        //}

        // ================= PROFILE =================
        public IActionResult Profile()
        {
            int sellerId = GetCurrentUserId();
            var user = _db.GetUserProfile(sellerId) ?? new User();
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Profile(User u)
        {
            int sellerId = GetCurrentUserId();

            if (u.Id != sellerId)
                return Unauthorized();

            if (!ModelState.IsValid)
                return View(u);

            _db.UpdateUser(u);

            TempData["Success"] = "Profile updated successfully!";
            return RedirectToAction("Profile");
        }

        //=============== seller Monthly Revenue & Profit &&Monthly Orders chart =========
        [HttpGet]
        public IActionResult SellerDashboardChart(DateTime fromDate, DateTime toDate)
        {
            int sellerId = GetCurrentUserId();
            // Generate months in range
            var months = new List<string>();
            var monthNumbers = new List<int>();
            for (var dt = new DateTime(fromDate.Year, fromDate.Month, 1); dt <= toDate; dt = dt.AddMonths(1))
            {
                months.Add(dt.ToString("MMM"));
                monthNumbers.Add(dt.Month);
            }

            decimal[] monthlyRevenue = new decimal[months.Count];
            decimal[] monthlyProfit = new decimal[months.Count];
            int[] monthlyOrders = new int[months.Count];

            using (var conn = _db.GetConnection())
            {
                conn.Open();

                string query = @"
                        SELECT 
                            MONTH(OrderDate) AS MonthNum,
                            YEAR(OrderDate) AS YearNum, 
                            ISNULL(SUM(TotalAmount), 0) AS Revenue,
                            ISNULL(SUM(TotalAmount * 0.2), 0) AS Profit,
                            COUNT(*) AS OrdersCount
                        FROM Orders
                        WHERE SellerId = @SellerId
                          AND OrderDate BETWEEN @FromDate AND @ToDate
                        GROUP BY YEAR(OrderDate), MONTH(OrderDate)
                        ORDER BY YEAR(OrderDate), MONTH(OrderDate)";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@SellerId", sellerId);
                    cmd.Parameters.AddWithValue("@FromDate", fromDate);
                    cmd.Parameters.AddWithValue("@ToDate", toDate);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int year = Convert.ToInt32(reader["YearNum"]);
                            int month = Convert.ToInt32(reader["MonthNum"]);
                            int index = months.FindIndex(m => m == new DateTime(year, month, 1).ToString("MMM"));

                            if (index >= 0)
                            {
                                monthlyRevenue[index] = Convert.ToDecimal(reader["Revenue"]);
                                monthlyProfit[index] = Convert.ToDecimal(reader["Profit"]);
                                monthlyOrders[index] = Convert.ToInt32(reader["OrdersCount"]);
                            }
                        }
                    }
                }
            }

            return Ok(new
            {
                months,
                monthlyRevenue,
                monthlyProfit,
                monthlyOrders
            });
        }


    }

}