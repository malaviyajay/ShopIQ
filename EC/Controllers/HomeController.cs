using EC.Data;
using EC.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EC.Controllers
{
    [Authorize]
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
            var selectedRole = HttpContext.RoleId() ?? 0;
            if (selectedRole == 1)
            {
                return RedirectToAction("Dashboard", "Admin");
            }
            else if (selectedRole == 2)
            {
                return RedirectToAction("Index", "Seller");
            }


            ViewBag.Categories = _db.GetCategories();
            ViewBag.BackUrl = "/Home/Index";

            var products = categoryId == null
                ? _db.GetProducts()
                  .GroupBy(p => p.CategoryId)
            .SelectMany(g => g
                .OrderBy(x => Guid.NewGuid()) // 🔥 RANDOM ORDER
                .Take(2))
            .ToList()
                : _db.GetProductsByCategory(categoryId.Value);

            return View(products);
        }

        // ================= SEARCH =================
       
        public IActionResult Search(string query)
        {
            ViewBag.Categories = _db.GetCategories();
            ViewBag.BackUrl = "/Home/Index";

            var products = _db.GetProducts()
                .Where(p => !string.IsNullOrEmpty(p.Name) &&
                            p.Name.Contains(query ?? "", StringComparison.OrdinalIgnoreCase))
                .ToList();

            return View("Index", products);
        }

        // ================= DETAILS =================
        
        public IActionResult Details(int id)
        {
            ViewBag.Categories = _db.GetCategories();
            ViewBag.BackUrl = "/Home/Index";

            var product = _db.GetProducts(id);
            if (product == null)
                return NotFound();

            return View(product);
        }

        // ================= ACCESS DENIED =================    
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}