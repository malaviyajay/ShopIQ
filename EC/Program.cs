using EC.Data;
using EC.Helpers;
using Microsoft.AspNetCore.Authentication.Cookies;
using Stripe;

var builder = WebApplication.CreateBuilder(args);

StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

builder.Services.AddScoped<DbHelper>();
builder.Services.AddScoped<EmailHelper>();

builder.Services.AddControllersWithViews();
builder.Services.AddSession();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.Cookie.Name = "ECAuthCookie";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
    });

builder.Services.AddAuthorization();

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}");

app.Run(); 
