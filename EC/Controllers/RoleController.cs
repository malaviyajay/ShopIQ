using EC.Data;
using EC.Helpers;
using EC.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

namespace EC.Controllers;

[Authorize]
public class RoleController : Controller
{
    private readonly DbHelper _db;

    public RoleController(DbHelper db)
    {
        _db = db;
    }

    public async Task<IActionResult> SelectRole()
    {

        var userId = HttpContext.UserId() ?? 0;

        var userRoles = _db.GetUserRole(userId);

        if(userRoles is null || userRoles.Count == 0)
        {
            if (HttpContext.IsLoggedIn())
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }

            return RedirectToAction("Login", "Account");
        }
        else if(userRoles.Count == 1)
        {
            return RedirectToAction("ChangeRole", "Role", new { RoleId = userRoles[0].RoleId });
        }

        var Roles = new SelectList(userRoles, nameof(UserRole.RoleId), nameof(UserRole.Name));

        return View(new RoleSelectionViewModel { Roles = Roles });
    }

  //==============select role=======================
    [HttpPost]
    public async Task<IActionResult> SelectRole(RoleSelectionViewModel model)
    {
        if (model.SelectedRole == 0)
        {
            return View(model);
        }

        var userId = HttpContext.UserId() ?? 0;
        var userRoles = _db.GetUserRole(userId)?.FirstOrDefault(m => m.RoleId == model.SelectedRole);

        if (userRoles is null)
        {
            ViewBag.Error = "User do not have any role. Please contact administrator.";
            return RedirectToAction("Login", "Account");
        }

        return await ChangeRole(model.SelectedRole);
    }

    public async Task<IActionResult> ChangeRole([FromQuery(Name = "RoleId")]int RoleId)
    {
        var userId = HttpContext.UserId() ?? 0;
        var userName = HttpContext.UserName() ?? "Unknown";
        var email = HttpContext.UserEmail() ?? "Unknown";

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        var userRoles = _db.GetUserRole(userId)?.FirstOrDefault(m => m.RoleId == RoleId);

        if (userRoles is null)
        {
            ViewBag.Error = "User do not have any role. Please contact administrator.";
            return RedirectToAction("Login", "Account");
        }

        var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, userName ?? ""),
                new Claim(ClaimTypes.Email, email ?? ""),
                new Claim("UserId", userId.ToString()),
                new Claim("RoleId", RoleId.ToString()),
                new Claim(ClaimTypes.Role, userRoles.Name)
            };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = true });

        // Redirect based on selected role
        return RedirectToRoleDashboard(RoleId);
    }

    private IActionResult RedirectToRoleDashboard(int roleId)
    {
        return roleId switch
        {
            1 => RedirectToAction("Dashboard", "Admin"),
            2 => RedirectToAction("Index", "Seller"),
            3 => RedirectToAction("Index", "Home"),
            _ => RedirectToAction("Login", "Account")
        };
    }

}