using EC.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EC.Controllers
{
    [Authorize]
    public class ProductController : Controller
    {
        private readonly DbHelper _db;

        public ProductController(DbHelper db)
        {
            _db = db;
        }

        [Authorize(Roles = "Customer")]
        public IActionResult Index(int? categoryId, string? search)
        {
            var products = _db.GetProducts();

            if (categoryId.HasValue && categoryId.Value > 0)
                products = products.Where(p => p.CategoryId == categoryId.Value).ToList();

            if (!string.IsNullOrEmpty(search))
                products = products.Where(p => p.Name.ToLower().Contains(search.ToLower())).ToList();

            ViewBag.Categories = _db.GetCategories();
            SetCartCount();
            return View(products);
        }

        [Authorize(Roles = "Customer")]
        public IActionResult Details(int id)
        {
            var product = _db.GetProduct(id);
            if (product == null) return NotFound();

            SetCartCount();
            return View(product);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Add()
        {
            ViewBag.Categories = _db.GetCategories();
            SetCartCount();
            return View();
        }

        // ✅ SEARCH ADDED
        [Authorize(Roles = "Customer")]
        public IActionResult Search(string query)
        {
            return RedirectToAction("Index", new { search = query });
        }

        private void SetCartCount()
        {
            var cart = Request.Cookies["Cart"];
            int count = 0;

            if (!string.IsNullOrEmpty(cart))
                count = cart.Split(',').Length;

            ViewBag.CartCount = count;
        }
    }
}
