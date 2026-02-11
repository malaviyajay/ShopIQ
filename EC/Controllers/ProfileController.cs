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
    public IActionResult Edit()
    {
        var profile = _db.GetUserProfile(GetUserId());
        if (profile == null)
            profile = new User();

        return View(profile);
    }

    // ================= SAVE EDIT =================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(User model)
    {
        if (!ModelState.IsValid)
            return View(model);

        model.Id = GetUserId();
        _db.SaveUserProfile(model);

        return RedirectToAction("Index");
    }
}
