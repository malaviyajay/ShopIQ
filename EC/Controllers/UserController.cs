using EC.Data;
using EC.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EC.Controllers;


[Authorize]
public class UserController : Controller
{
    private readonly DbHelper _db;

    public UserController(DbHelper db)
    {
        _db = db;
    }

    // ================= LOGIN =================

    [AllowAnonymous]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginVM model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = _db.ValidateUser(model.Email, model.Password);

        if (user == null)
        {
            ViewBag.Error = "Invalid email or password";
            return View(model);
        }

        // Claims for authentication
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim("UserId", user.Id.ToString()),
            new Claim(ClaimTypes.Role, user.IsAdmin ? "Admin": "Customer")
        };

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme
        );

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = true }
        );

        return user.IsAdmin
            ? RedirectToAction("Index", "Admin")
            : RedirectToAction("Index", "Home");
    }

    // ================= REGISTER =================

    [AllowAnonymous]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public IActionResult Register(User model)
    {
        if (!ModelState.IsValid)
            return View(model);

        if (_db.IsEmailExists(model.Email))
        {
            ViewBag.Error = "Email already exists";
            return View(model);
        }

        _db.RegisterUser(model);
        return RedirectToAction("Login");
    }

    // ================= LOGOUT =================

    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme
        );

        return RedirectToAction("Login");
    }

    // ================= CART COUNT =================

    public void SetCartCount()
    {
        var cart = Request.Cookies["Cart"];
        int count = 0;

        if (!string.IsNullOrEmpty(cart))
            count = cart.Split(',').Length;

        ViewBag.CartCount = count;
    }
}
