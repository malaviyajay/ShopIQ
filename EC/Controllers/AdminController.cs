using EC.Data;
using EC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
}
