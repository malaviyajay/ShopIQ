using EC.Data;
using EC.Helpers;
using EC.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Security.Claims;

namespace EC.Controllers;

[AllowAnonymous]
public class AccountController : Controller
{
    private readonly DbHelper _db;
    private readonly EmailHelper _email;

    public AccountController(DbHelper db, EmailHelper email)
    {
        _db = db;
        _email = email;
    }

    // ================= REGISTER ================
    public IActionResult Register() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Register(User model)
    {
        if (!ModelState.IsValid)
            return View(model);

        // Default Role = Customer (RoleId = 3)
        _db.RegisterUser(model, new List<int> { 3 });

        return RedirectToAction("Login");
    }

    // ================= LOGIN =================
    public IActionResult Login() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string email, string password)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ViewBag.Error = "Email and Password are required.";
            return View();
        }

        var user = _db.Login(email, password);
        if (user is null)
        {
            ViewBag.Error = "Invalid login credentials.";
            return View();
        }

        if (user.Roles is null || user.Roles.Count == 0)
        {
            ViewBag.Error = "User does not have any role. Contact admin.";
            return View();
        }

        var claims = new List<Claim>
            {
                new Claim("UserId", user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Name ?? ""),
                new Claim(ClaimTypes.Email, user.Email ?? "")
            };


        var roleClaims = user.Roles
         .Where(r => !string.IsNullOrEmpty(r.Name))
         .Select(r => new Claim(ClaimTypes.Role, r.Name))
         .ToList();

        claims.AddRange(roleClaims);

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = true }
        );

        return RedirectToAction("SelectRole", "Role");
    }

    // ================= LOGOUT =================
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        return RedirectToAction("Login");
    }

    // ================= ACCESS DENIED =================
    public IActionResult AccessDenied()
    {
        return View();
    }

    // ================= FORGOT PASSWORD =================
    [HttpGet]
    public IActionResult ForgotPassword() => View(new ForgotPasswordView());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordView model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var token = Guid.NewGuid().ToString();
        var expiry = DateTime.Now.AddMinutes(30);

        using var conn = _db.GetConnection();
        conn.Open();

        var cmd = new SqlCommand(
            @"INSERT INTO ResetPassword (Email, Token, Expiry)
              VALUES (@email,@token,@exp)", conn);

        cmd.Parameters.AddWithValue("@email", model.Email);
        cmd.Parameters.AddWithValue("@token", token);
        cmd.Parameters.AddWithValue("@exp", expiry);

        cmd.ExecuteNonQuery();

        string resetUrl = Url.Action(
            "ResetPassword",
            "Account",
            new { token, email = model.Email },
            Request.Scheme)!;

        await _email.SendResetEmail(model.Email, resetUrl);

        ViewBag.Msg = "Check your email for reset link.";
        return View(model);
    }

    // ================= RESET PASSWORD (GET) =================
    [HttpGet]
    public IActionResult ResetPassword(string email, string token)
    {
        return View(new ResetPasswordView
        {
            Email = email,
            Token = token
        });
    }

    // ================= RESET PASSWORD (POST) =================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ResetPassword(ResetPasswordView model)
    {
        if (!ModelState.IsValid)
            return View(model);

        if (model.NewPassword != model.ConfirmPassword)
        {
            ModelState.AddModelError("", "Passwords do not match.");
            return View(model);
        }

        using var conn = _db.GetConnection();
        conn.Open();

        var check = new SqlCommand(
            @"SELECT COUNT(*) FROM ResetPassword
              WHERE Email=@email AND Token=@token AND Expiry > GETDATE()", conn);

        check.Parameters.AddWithValue("@email", model.Email);
        check.Parameters.AddWithValue("@token", model.Token);

        int valid = (int)check.ExecuteScalar();

        if (valid == 0)
        {
            ModelState.AddModelError("", "Invalid or expired token.");
            return View(model);
        }

        var update = new SqlCommand(
            @"UPDATE Users SET Password=@pass WHERE Email=@email", conn);

        update.Parameters.AddWithValue("@pass", model.NewPassword);
        update.Parameters.AddWithValue("@email", model.Email);
        update.ExecuteNonQuery();

        var delete = new SqlCommand(
            @"DELETE FROM ResetPassword WHERE Email=@email AND Token=@token", conn);

        delete.Parameters.AddWithValue("@email", model.Email);
        delete.Parameters.AddWithValue("@token", model.Token);
        delete.ExecuteNonQuery();

        return RedirectToAction("Login");
    }
}