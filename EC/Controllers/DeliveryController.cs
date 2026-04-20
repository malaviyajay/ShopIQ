using Microsoft.AspNetCore.Mvc;
using EC.Data;

namespace EC.Controllers
{
    public class DeliveryController : Controller
    {
        private readonly DbHelper _db;
        public DeliveryController(DbHelper db)
        {
            _db = db;
        }

        // Delivery boy updates location
        [HttpPost]
        public IActionResult UpdateLocation(int orderId, double lat, double lng)
        {
            _db.InsertDeliveryLocation(orderId, lat, lng);
            return Ok();
        }

        // Website fetch live location
        [HttpGet]
        public IActionResult GetLocation(int orderId)
        {
            var loc = _db.GetLatestLocation(orderId);
            return Json(loc);
        }
    }
}