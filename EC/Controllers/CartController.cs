using EC.Data;
using EC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Security.Claims;
using EC.Helpers;

namespace EC.Controllers;

[Authorize(Roles = "Customer")]
public class CartController : Controller
{
    private readonly DbHelper _db;

    public CartController(DbHelper db)
    {
        _db = db;
    }

    // ================= USER ID HELPER =================
    private string GetUserId()
    {
        //// 🔥 FIX: Your system stores UserId inside Name claim
        //return User.FindFirstValue(ClaimTypes.Name)
        //       ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
        //       ?? "guest";
        int? userId = HttpContext.UserId();

        if (userId == null)
            return "guest";

        return userId.Value.ToString();
    }

    // =================== CART PAGE ===================
    public IActionResult Index()
    {
        List<Product> items = new();
        var cartItems = GetCartItems();

        if (!cartItems.Any())
        {
            ViewBag.CartCount = 0;
            return View(items);
        }

        using var con = _db.GetConnection();
        con.Open();

        foreach (var kv in cartItems)
        {
            int productId = kv.Key;
            int qty = kv.Value;

            using var cmd = new SqlCommand(
                "SELECT * FROM Products WHERE Id=@id", con);

            cmd.Parameters.AddWithValue("@id", productId);

            using var rd = cmd.ExecuteReader();

            if (rd.Read())
            {
                int stockQty = rd["Quantity"] == DBNull.Value
                    ? 0
                    : Convert.ToInt32(rd["Quantity"]);

                if (stockQty <= 0)
                {
                    rd.Close();
                    continue;
                }

                if (qty > stockQty)
                    qty = stockQty;

                items.Add(new Product
                {
                    Id = Convert.ToInt32(rd["Id"]),
                    Name = rd["Name"]?.ToString() ?? "",
                    Price = Convert.ToDecimal(rd["Price"]),
                    Image = rd["Image"]?.ToString() ?? "",
                    CategoryId = Convert.ToInt32(rd["CategoryId"]),
                    Quantity = qty
                });
            }

            rd.Close();
        }

        ViewBag.CartCount = items.Sum(x => x.Quantity);
        return View(items);
    }

    // =================== ADD TO CART ===================
    [HttpGet]
    public IActionResult AddToCart(int id, int quantity = 1)
    {
        if (quantity <= 0)
            quantity = 1;

        using var con = _db.GetConnection();
        con.Open();

        using var cmd = new SqlCommand(
            "SELECT Quantity FROM Products WHERE Id=@id", con);

        cmd.Parameters.AddWithValue("@id", id);

        var result = cmd.ExecuteScalar();

        if (result == null)
        {
            TempData["Error"] = "Product not found!";
            return RedirectToAction("Index", "Home");
        }

        int stockQty = Convert.ToInt32(result);

        if (stockQty <= 0)
        {
            TempData["Error"] = "Product is out of stock!";
            return RedirectToAction("Index", "Home");
        }

        var items = GetCartItems();
        int existingQty = items.ContainsKey(id) ? items[id] : 0;

        if (existingQty + quantity > stockQty)
        {
            TempData["Error"] = $"Only {stockQty} items available!";
            return RedirectToAction("Index");
        }

        if (items.ContainsKey(id))
            items[id] += quantity;
        else
            items[id] = quantity;

        SaveCartItems(items);

        TempData["Success"] = "Product added to cart!";
        return RedirectToAction("Index");
    }

    // =================== INCREASE ===================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult IncreaseQty(int id)
    {
        var items = GetCartItems();

        if (!items.ContainsKey(id))
            return RedirectToAction("Index");

        items[id]++;
        SaveCartItems(items);

        return RedirectToAction("Index");
    }

    // =================== DECREASE ===================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DecreaseQty(int id)
    {
        var items = GetCartItems();

        if (items.ContainsKey(id))
        {
            if (items[id] > 1)
                items[id]--;
            else
                items.Remove(id);
        }

        SaveCartItems(items);
        return RedirectToAction("Index");
    }

    // =================== REMOVE ===================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult RemoveFromCart(int id)
    {
        var items = GetCartItems();

        if (items.ContainsKey(id))
            items.Remove(id);

        SaveCartItems(items);
        return RedirectToAction("Index");
    }

    // =================== GET CART ===================
    private Dictionary<int, int> GetCartItems()
    {
        string cookieName = $"Cart_{GetUserId()}";

        string cart = Request.Cookies[cookieName] ?? "";

        Dictionary<int, int> items = new();

        if (!string.IsNullOrEmpty(cart))
        {
            foreach (var c in cart.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = c.Split(':');

                if (parts.Length == 2 &&
                    int.TryParse(parts[0], out int pid) &&
                    int.TryParse(parts[1], out int qty))
                {
                    items[pid] = qty;
                }
            }
        }

        return items;
    }

    // =================== SAVE CART ===================
    private void SaveCartItems(Dictionary<int, int> items)
    {
        string cookieName = $"Cart_{GetUserId()}";

        string cart = string.Join(",", items.Select(x => $"{x.Key}:{x.Value}"));

        var options = new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddDays(7),
            Path = "/",
            HttpOnly = true,
            IsEssential = true
        };

        Response.Cookies.Append(cookieName, cart, options);
    }

    // =================== CART COUNT ===================
    [HttpGet]
    [AllowAnonymous]
    public IActionResult CartCount()
    {
        var items = GetCartItems();
        return Json(items.Sum(x => x.Value));
    }
}