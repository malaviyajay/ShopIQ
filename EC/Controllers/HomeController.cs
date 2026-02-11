using EC.Data;
using EC.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;

namespace EC.Controllers
{
    [Authorize(Roles = "Customer")]
    public class HomeController : Controller
    {
        private readonly DbHelper _db;

        public HomeController(DbHelper db)
        {
            _db = db;
        }

        // ================= HOME =================
        public IActionResult Index(int? categoryId)
        {
            ViewBag.Categories = _db.GetCategories();
            ViewBag.BackUrl = "/Home/Index";

            var products = categoryId == null
                ? _db.GetProducts()
                : _db.GetProductsByCategory(categoryId.Value);

            return View(products);
        }

        // ================= SEARCH =================
        public IActionResult Search(string query)
        {
            ViewBag.Categories = _db.GetCategories();
            ViewBag.BackUrl = "/Home/Index";

            var products = _db.GetProducts()
                .Where(p => p.Name != null &&
                            p.Name.Contains(query ?? "", StringComparison.OrdinalIgnoreCase))
                .ToList();

            return View("Index", products);
        }

        // ================= DETAILS =================
        public IActionResult Details(int id)
        {
            ViewBag.Categories = _db.GetCategories();
            ViewBag.BackUrl = "/Home/Index";

            var product = _db.GetProduct(id);
            if (product == null)
                return NotFound();

            return View(product);
        }

        // ================= ACCESS DENIED =================
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            ViewData["Title"] = "Access Denied";
            return View();
        }
    }
}
