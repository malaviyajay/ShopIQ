using EC.Data;
using EC.Helpers;
using EC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        // ================= HOME =================
        public IActionResult Index()
        {
            return RedirectToAction("Dashboard");
        }

        // ================= DASHBOARD =================
        public IActionResult Dashboard()
        {
            int sellerId = GetCurrentUserId();
            var model = _db.GetSellerDashboardData(sellerId);
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Add(Product p)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = _db.GetCategories() ?? new List<Category>();
                return View(p);
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

            if (product == null || product.SellerId != GetCurrentUserId())
                return Unauthorized();

            ViewBag.Categories = _db.GetCategories() ?? new List<Category>();
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Product p)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = _db.GetCategories() ?? new List<Category>();
                return View(p);
            }

            var existing = _db.GetProductById(p.Id);
            if (existing == null || existing.SellerId != GetCurrentUserId())
                return Unauthorized();

            _db.UpdateProduct(p);

            TempData["Success"] = "Product updated successfully!";
            return RedirectToAction("Products");
        }

        // ================= DELETE PRODUCT =================
        public IActionResult Delete(int id)
        {
            var product = _db.GetProductById(id);

            if (product == null || product.SellerId != GetCurrentUserId())
                return Unauthorized();

            _db.DeleteProduct(id);

            TempData["Success"] = "Product deleted successfully!";
            return RedirectToAction("Products");
        }

        // ================= ORDERS =================
        public IActionResult Orders()
        {
            int sellerId = GetCurrentUserId();
            var orders = _db.GetOrdersBySeller(sellerId) ?? new List<Order>();
            return View(orders);
        }

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

        // ================= HELPER =================
        private int GetCurrentUserId()
        {
            var claim = User.Claims.FirstOrDefault(c => c.Type == "UserId");

            if (claim == null || !int.TryParse(claim.Value, out int userId))
                throw new Exception("User not logged in");

            return userId;
        }
    }
}