using EC.Data;
using EC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;



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

        // =================== LIST / INDEX ===================
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

        // =================== DETAILS ===================
        [Authorize(Roles = "Customer")]
        public IActionResult Details(int id)
        {
            var product = _db.GetProducts(id);
            if (product == null) return NotFound();

            SetCartCount();
            return View(product);
        }

        // =================== ADD PRODUCT (GET) ===================
        [Authorize(Roles = "Seller")]
        public IActionResult Add()
        {
            ViewBag.Categories = _db.GetCategories();
            return View();
        }

        // =================== ADD PRODUCT (POST) ===================
        [HttpPost]
        [Authorize(Roles = "Seller")]
        public IActionResult Add(Product model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = _db.GetCategories();
                return View(model);
            }

            int sellerId = Convert.ToInt32(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            model.SellerId = sellerId;

            _db.AddProduct(model);

            return RedirectToAction("Dashboard", "Seller");
        }

        // =================== EDIT PRODUCT (GET) ===================
        [Authorize(Roles = "Seller")]
        public IActionResult Edit(int id)
        {
            var product = _db.GetProducts(id);
            if (product == null) return NotFound();

            ViewBag.Categories = _db.GetCategories();
            return View(product);
        }

        // =================== EDIT PRODUCT (POST) ===================
        [HttpPost]
        [Authorize(Roles = "Seller")]
        public IActionResult Edit(Product model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = _db.GetCategories();
                return View(model);
            }

            _db.UpdateProduct(model);
            return RedirectToAction("Index");
        }

        // =================== SEARCH ===================
        [Authorize(Roles = "Customer")]
        public IActionResult Search(string query)
        {
            return RedirectToAction("Index", new { search = query });
        }

        // =================== HELPER ===================
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