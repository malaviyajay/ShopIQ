using EC.Data;
using EC.Helpers;
using EC.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EC.Controllers;

[AllowAnonymous]
public class AccountController : Controller
{
    private readonly DbHelper _db;

    public AccountController(DbHelper db)
    {
        _db = db;
    }

    // ================= LOGIN =================
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string email, string password)
    {
        var user = _db.Login(email, password);

        if (user == null)
        {
            ViewBag.Error = "Invalid login";
            return View();
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim("UserId", user.Id.ToString()),
            new Claim(ClaimTypes.Role, user.IsAdmin ? "Admin": "Customer")
        };

        var identity = new ClaimsIdentity(claims,CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,new ClaimsPrincipal(identity),new AuthenticationProperties { IsPersistent = true });

        if (user.IsAdmin)
            return RedirectToAction("Dashboard", "Admin");

        return RedirectToAction("Index", "Home");
    }

    // ================= REGISTER =================
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Register(User model)
    {
        if (!ModelState.IsValid)
            return View(model);

        _db.RegisterUser(model);
        return RedirectToAction("Login");
    }

    // ================= LOGOUT =================
    public async Task<IActionResult> Logout()
    {
        if (HttpContext.IsLoggedIn())
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
        
        return RedirectToAction("Login");
    }
}
