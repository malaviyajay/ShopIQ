using System.Security.Claims;

namespace EC.Helpers;

public static class AuthHelper
{

    public static bool IsLoggedIn(this HttpContext context)
    {
        return context.User.Identity?.IsAuthenticated is true;
    }

    public static bool IsAdmin(this HttpContext context)
    {
        if (!context.IsLoggedIn())
        {
            return false;
        }
        return context.User.IsInRole("Admin");
    }

    public static int? UserId(this HttpContext context)
    {
        if (!context.IsLoggedIn())
        {
            return null;
        }

        if (!int.TryParse(context.User.FindFirst("UserId")?.Value ?? null, out int userId))
        {
            return null;
        }
        return userId;
    }

    public static string? UserName(this HttpContext context)
    {
        if (!context.IsLoggedIn())
        {
            return null;
        }

        return context.User.FindFirst(ClaimTypes.Name)?.Value;
    }

    public static string? UserEmail(this HttpContext context)
    {
        if (!context.IsLoggedIn())
        {
            return null;
        }

        return context.User.FindFirst(ClaimTypes.Email)?.Value;
    }
}
