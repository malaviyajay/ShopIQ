using Microsoft.AspNetCore.Mvc;

using System.Security.Claims;

public class RoleSelectionMiddleware
{
    private readonly RequestDelegate _next;

    public RoleSelectionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity.IsAuthenticated)
        {
            var roles = context.User.Claims
                        .Where(c => c.Type == ClaimTypes.Role)
                        .Select(c => c.Value)
                        .ToList();

            var activeRole = context.Session.GetString("ActiveRole");

            if (roles.Count > 1 && string.IsNullOrEmpty(activeRole)
                && !context.Request.Path.StartsWithSegments("/Role/SelectRole"))
            {
                context.Response.Redirect("/Role/SelectRole");
                return;
            }
        }

        await _next(context);
    }
}