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

    public static bool IsSeller(this HttpContext context)
    {
        if (!context.IsLoggedIn())
        {
            return false;
        }
        return context.User.IsInRole("Seller");
    }

    public static bool IsCustomer(this HttpContext context)
    {
        if (!context.IsLoggedIn())
        {
            return false;
        }
        return context.User.IsInRole("Customer");
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

    public static int? RoleId(this HttpContext context)
    {
        if (!context.IsLoggedIn())
        {
            return null;
        }

        if (!int.TryParse(context.User.FindFirst("RoleId")?.Value ?? null, out int RoleId))
        {
            return null;
        }
        return RoleId;
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
