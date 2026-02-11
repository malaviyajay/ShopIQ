using EC.Data;
using EC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace EC.Controllers;

[Authorize(Roles = "Customer")]
public class CartController : Controller
{
    private readonly DbHelper _db;
    public CartController(DbHelper db) => _db = db;

    // =================== CART PAGE ===================
    public IActionResult Index()
    {
        List<Product> items = new();
        var cartItems = GetCartItems();

        if (!cartItems.Any())
        {
            ViewBag.Message = "Your cart is empty.";
            ViewBag.CartCount = 0;
            return View(items);
        }

        using var con = _db.GetConnection();
        con.Open();

        foreach (var kv in cartItems)
        {
            int productId = kv.Key;
            int qty = kv.Value;

            var cmd = new SqlCommand("SELECT * FROM Products WHERE Id=@id", con);
            cmd.Parameters.AddWithValue("@id", productId);

            using var rd = cmd.ExecuteReader();
            if (rd.Read())
            {
                items.Add(new Product
                {
                    Id = (int)rd["Id"],
                    Name = rd["Name"].ToString()!,
                    Price = (decimal)rd["Price"],
                    Image = rd["Image"].ToString()!,
                    CategoryId = (int)rd["CategoryId"],
                    Quantity = qty
                });
            }
        }

        ViewBag.CartCount = cartItems.Sum(x => x.Value);
        return View(items);
    }

    // =================== ADD TO CART ===================
    public IActionResult AddToCart(int id)
    {
        var items = GetCartItems();

        if (items.ContainsKey(id))
            items[id]++;
        else
            items[id] = 1;

        SaveCartItems(items);
        return RedirectToAction("Index");
    }

    // =================== INCREASE QTY ===================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult IncreaseQty(int id)
    {
        var items = GetCartItems();
        if (items.ContainsKey(id))
            items[id]++;

        SaveCartItems(items);
        return RedirectToAction("Index");
    }

    // =================== DECREASE QTY ===================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DecreaseQty(int id)
    {
        var items = GetCartItems();
        if (items.ContainsKey(id) && items[id] > 1)
            items[id]--;

        SaveCartItems(items);
        return RedirectToAction("Index");
    }

    // =================== REMOVE ITEM ===================
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
        string cart = Request.Cookies["Cart"] ?? "";
        Dictionary<int, int> items = new();

        if (!string.IsNullOrEmpty(cart))
        {
            foreach (var c in cart.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = c.Split(':');
                items[int.Parse(parts[0])] = int.Parse(parts[1]);
            }
        }

        return items;
    }

    // =================== SAVE CART ===================
    private void SaveCartItems(Dictionary<int, int> items)
    {
        string cart = string.Join(",", items.Select(x => $"{x.Key}:{x.Value}"));

        var options = new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddDays(7), // persists 7 days
            Path = "/"
        };

        Response.Cookies.Append("Cart", cart, options);
        ViewBag.CartCount = items.Sum(x => x.Value);
    }
}
