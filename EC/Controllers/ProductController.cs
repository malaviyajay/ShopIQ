using EC.Data;
using EC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Linq;
using System.Collections.Generic;

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

            // ====== Ratings Integration (all reviews now included) ======
            var reviews = _db.GetAllReviews(); // Get all reviews
            ViewBag.ProductRatings = products.ToDictionary(
                p => p.Id,
                p => new
                {
                    Avg = reviews.Where(r => r.ProductId == p.Id)
                                 .DefaultIfEmpty().Average(r => r?.Rating ?? 0),
                    Total = reviews.Count(r => r.ProductId == p.Id)
                }
            );

            return View(products);
        }

        // =================== DETAILS ===================
        [Authorize(Roles = "Customer")]
        public IActionResult Details(int id)
        {
            var product = _db.GetProducts(id);
            if (product == null) return NotFound();

            SetCartCount();

            var reviews = _db.GetReviews(id) ?? new List<Review>();

            // ✅ Fill Name (Amazon logic)
            foreach (var r in reviews)
            {
                if (string.IsNullOrEmpty(r.Name))
                {
                    var user = _db.GetUser(r.UserId);
                    r.Name = user != null ? user.Name : "Deleted User";
                }
            }

            // ✅ AVG + TOTAL
            ViewBag.AvgRating = reviews.Any() ? reviews.Average(r => (double)r.Rating) : 0;
            ViewBag.TotalRatings = reviews.Count;

            // ✅ AMAZON POLL (MAIN FIX)
            Dictionary<int, int> starCounts = new Dictionary<int, int>();

            for (int star = 1; star <= 5; star++)
            {
                int count = reviews.Count(r => r.Rating == star);
                starCounts[star] = count; // ✅ store COUNT not %
            }

            ViewBag.StarCounts = starCounts;

            ViewBag.Reviews = reviews;

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

            int sellerId = Convert.ToInt32(/*User.FindFirst(ClaimTypes.NameIdentifier)?.Value*/User.FindFirst("UserId"));
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

        // =================== Submit Rating ===================
        [HttpPost]
        [Authorize(Roles = "Customer")]
        public IActionResult SubmitRating(int productId, int rating, string comment)
        {
            int userId = Convert.ToInt32(User.FindFirst("UserId")?.Value);

            var user = _db.GetUser(userId);

            // ✅ AMAZON LOGIC: store name at time of review
            var review = new Review
            {
                ProductId = productId,
                UserId = userId,
                Name = user != null ? user.Name : "Deleted User", // IMPORTANT
                Rating = rating,
                Comment = comment,
                Status = "Approved",
                CreatedAt = DateTime.Now
            };

            _db.AddReview(review);

            return RedirectToAction("Details", new { id = productId });
        }
        //==================================LoadMoreReviews=====================
        [HttpGet]
        public IActionResult LoadMoreReviews(int productId, int page = 1, int pageSize = 5)
        {
            var reviews = _db.GetReviews(productId)
                   .OrderByDescending(r => r.CreatedAt)
                   .Skip((page - 1) * pageSize)
                   .Take(pageSize)
                   .ToList();

            // Fill Name from User table or fallback
            var reviewsDto = reviews.Select(r => {
                var user = _db.GetUser(r.UserId);
                string name = user != null ? user.Name : "Deleted User";
                return new
                {
                    Name = name,
                    r.Rating,
                    CreatedAt = r.CreatedAt.ToString("dd MMM yyyy"),
                    r.Comment
                };
            }).ToList();

            var allReviews = _db.GetReviews(productId).ToList();
            int totalReviews = allReviews.Count;

            return Json(new
            {
                reviews = reviewsDto,
                totalReviews
            });
        }
    }
}