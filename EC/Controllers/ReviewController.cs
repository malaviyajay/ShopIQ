using EC.Data;
using EC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EC.Controllers
{
    [Authorize]
    public class ReviewController : Controller
    {
        private readonly DbHelper _db;

        public ReviewController(DbHelper db)
        {
            _db = db;
        }

        // =================== Submit review via rating form ===================
        [HttpPost]
        [Authorize(Roles = "Customer")]
        public IActionResult SubmitRating(int productId, int rating, string comment)
        {
            int userId = Convert.ToInt32(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var review = new Review
            {
                ProductId = productId,
                UserId = userId,
                Rating = rating,
                Comment = comment,
                Status = "Approved" // Auto-approved
            };

            _db.AddReview(review);

            TempData["Message"] = "Thank you! Your review has been submitted.";
            return RedirectToAction("Details", "Product", new { id = productId });
        }

        // =================== Add review via Review model ===================
        [HttpPost]
        [Authorize(Roles = "Customer")]
        public IActionResult AddReview(Review model)
        {
            if (ModelState.IsValid)
            {
                model.Status = "Approved"; // Auto-approved
                _db.AddReview(model);
                return RedirectToAction("Details", "Product", new { id = model.ProductId });
            }
            return View(model);
        }

        // =================== View all reviews (read-only) ===================
        public IActionResult ManageReviews()
        {
            var reviews = _db.GetAllReviews(); // Show all reviews
            return View(reviews);
        }

        
    }
}