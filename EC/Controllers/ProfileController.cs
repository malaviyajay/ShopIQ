using EC.Data;
using EC.Helpers;
using EC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EC.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly DbHelper _db;

    public ProfileController(DbHelper db)
    {
        _db = db;
    }

    private int GetUserId()
    {

        return HttpContext.UserId() ?? 0;
    }

    // ================= PROFILE VIEW =================
    public IActionResult Index()
    {

        var profile = _db.GetUserProfile(GetUserId());
        if (profile == null)
            profile = new User();

        return View(profile);
    }

    // ================= EDIT PAGE =================
    [HttpGet]
    public IActionResult Edit()
    {
        int userId = GetUserId();
        var user = _db.GetUserProfile(userId);

        if (user == null)
        {
            return NotFound();
        }

        return View(user);
    }


    // ================= SAVE EDIT =================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(User model)
    {
        if (!ModelState.IsValid)
        {
            return View(model); 
        }

        var user = _db.GetUserProfile(model.Id);
        if (user == null)
        {
            return NotFound();
        }

        user.Name = model.Name;
        user.Email = model.Email;
        user.Phone = model.Phone;
        user.Address = model.Address;

        _db.UpdateUser(user); 

        TempData["Success"] = "Profile updated successfully!";
        return RedirectToAction("Profile"); 
    }
}