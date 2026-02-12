using EC.Data;
using EC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace EC.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly DbHelper _db;

    public AdminController(DbHelper db)
    {
        _db = db;
    }

    public IActionResult Dashboard()
    {
        var model = new DashboardViewModel
        {
            ProductCount = _db.GetProductCount(),
            OrderCount = _db.GetOrderCount(),
            UserCount = _db.GetUserCount(),
            Revenue = _db.GetTotalRevenue()
        };

        return View(model);
    }

    public IActionResult Products()
    {
        var products = _db.GetProductsWithCategoryNames();
        return View(products);
    }

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

    public IActionResult Edit(int id)
    {
        var product = _db.GetProduct(id);
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

    public IActionResult Delete(int id)
    {
        _db.DeleteProduct(id);
        return RedirectToAction("Products");
    }

    public IActionResult Orders()
    {
        var orders = _db.GetAllOrders();
        return View(orders);
    }

    // ✅ SEARCH ADDED
    public IActionResult Search(string query)
    {
        var products = _db.GetProductsWithCategoryNames()
            .Where(p => p.Name.ToLower().Contains(query.ToLower()))
            .ToList();

        return View("Products", products);
    }

    [HttpGet]
    public IActionResult UserList()
    {
        List<User> users = new List<User>();

        using var con = _db.GetConnection();
        con.Open();

        var cmd = new SqlCommand("SELECT Id, Name, Email, IsAdmin FROM Users", con);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            users.Add(new User
            {
                Id = Convert.ToInt32(reader["Id"]),
                Name = Convert.ToString(reader["Name"]),
                Email = Convert.ToString(reader["Email"]),
                IsAdmin = Convert.ToBoolean(reader["IsAdmin"])
            });
        }

        return View(users);
    }

    [HttpPost]
    public IActionResult UserList(int userId, string role)
    {
        bool isAdmin = role == "Admin";

        using var con = _db.GetConnection();
        con.Open();

        var cmd = new SqlCommand("UPDATE Users SET IsAdmin=@isAdmin WHERE Id=@id", con);
        cmd.Parameters.AddWithValue("@isAdmin", isAdmin);
        cmd.Parameters.AddWithValue("@id", userId);

        cmd.ExecuteNonQuery();

        TempData["Success"] = "Role updated successfully!";
        return RedirectToAction("Userlist");
    }


}
