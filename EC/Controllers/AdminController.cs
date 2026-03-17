using EC.Data;
using EC.Helpers;
using EC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace EC.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly DbHelper _db;
        private readonly EmailHelper _email;

        public AdminController(DbHelper db, EmailHelper email)
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
            var model = _db.GetDashboardData();
            return View(model);
        }

        // ================= PRODUCTS =================
        public IActionResult Products()
        {
            var products = _db.GetProductsWithCategoryNames();
            return View(products);
        }

        // ================= ADD PRODUCT =================
        public IActionResult Add()
        {
            ViewBag.Categories = _db.GetCategories();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Add(Product p)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = _db.GetCategories();
                return View(p);
            }

            _db.AddProduct(p);
            return RedirectToAction("Products");
        }

        // ================= EDIT PRODUCT =================
        public IActionResult Edit(int id)
        {
            var product = _db.GetProducts(id);
            ViewBag.Categories = _db.GetCategories();
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Product p)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = _db.GetCategories();
                return View(p);
            }

            _db.UpdateProduct(p);
            return RedirectToAction("Products");
        }

        // ================= DELETE PRODUCT =================
        public IActionResult Delete(int id)
        {
            _db.DeleteProduct(id);
            return RedirectToAction("Products");
        }

        // ================= ORDERS =================
        //public IActionResult Orders()
        //{
        //    int sellerId = GetCurrentUserId();
        //    var orders = _db.GetOrdersBySeller(sellerId);
        //    return View(orders);
        //}

        public IActionResult Orders()
        {
            var orders = _db.GetAllOrders();
            return View(orders);
        }

        // ================= ORDER DETAILS =================
        [HttpGet]
        public IActionResult GetOrderDetails(int orderId)
        {
            var order = _db.GetOrderById(orderId);

            if (order == null)
                return NotFound();

            var result = _db.GetOrderItems(orderId, userId: null);

            return Json(result);
        }

        // ================= SEARCH =================
        public IActionResult Search(string query)
        {
            if (string.IsNullOrEmpty(query))
                return RedirectToAction("Products");

            var products = _db.GetProductsWithCategoryNames()
                .Where(p => p.Name.ToLower().Contains(query.ToLower()))
                .ToList();

            return View("Products", products);
        }

        // ================= USERS LIST =================
        [HttpGet]

        public IActionResult UserList()
        {
            List<User> users = new List<User>();

            using var con = _db.GetConnection();
            con.Open();

            var cmd = new SqlCommand(@"
        SELECT U.Id, U.Name, U.Email, R.Name AS RoleName
        FROM Users U
        LEFT JOIN UserRoles UR ON U.Id = UR.UserId
        LEFT JOIN Roles R ON UR.RoleId = R.RoleId
        ORDER BY U.Id", con);

            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                int userId = reader["Id"] != DBNull.Value   
                    ? Convert.ToInt32(reader["Id"])
                    : 0;

                var existingUser = users.FirstOrDefault(u => u.Id == userId);

                if (existingUser == null)
                {
                    existingUser = new User
                    {
                        Id = userId,
                        Name = reader["Name"]?.ToString() ?? "",
                        Email = reader["Email"]?.ToString() ?? "",
                        Roles = new List<UserRole>()
                    };

                    users.Add(existingUser);
                }

                if (reader["RoleName"] != DBNull.Value)
                {
                    existingUser.Roles.Add(new UserRole
                    {
                        UserId = userId,
                        Name = reader["RoleName"]?.ToString() ?? ""
                    });
                }
            }

            return View(users);
        }

        // ================= CHANGE USER ROLE =================
        [HttpPost]
        public IActionResult UpdateUserRole(int userId, int? roleId)
        {
            if (roleId == null)
            {
                TempData["Error"] = "Please select a role.";
                return RedirectToAction("UserList");
            }

            using var con = _db.GetConnection();
            con.Open();

            var deleteCmd = new SqlCommand(
                "DELETE FROM UserRoles WHERE UserId=@userId", con);
            deleteCmd.Parameters.AddWithValue("@userId", userId);
            deleteCmd.ExecuteNonQuery();

            var insertCmd = new SqlCommand(
                "INSERT INTO UserRoles (UserId, RoleId) VALUES (@userId,@roleId)", con);
            insertCmd.Parameters.AddWithValue("@userId", userId);
            insertCmd.Parameters.AddWithValue("@roleId", roleId);
            insertCmd.ExecuteNonQuery();

            TempData["Success"] = "Role updated successfully!";
            return RedirectToAction("UserList");
        }
    }
}