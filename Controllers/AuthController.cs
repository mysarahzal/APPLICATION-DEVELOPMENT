using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.ViewModels;
using Microsoft.EntityFrameworkCore;
using AspnetCoreMvcFull.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace AspnetCoreMvcFull.Controllers;

public class AuthController : Controller
{
  private readonly KUTIPDbContext _context;

  public AuthController(KUTIPDbContext context)
  {
    _context = context;
  }

  public IActionResult ForgotPasswordBasic() => View();

  public IActionResult LoginBasic() => View();

  [HttpPost]
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> LoginBasic(LoginViewModel model)
  {
    if (!ModelState.IsValid)
    {
      return View(model);
    }

    var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);

    if (user == null || user.Password != model.Password) // Use proper password hashing in production
    {
      ModelState.AddModelError(string.Empty, "Invalid login attempt.");
      return View(model);
    }

    // Create claims for the user
    var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Email),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("UserId", user.Id.ToString()),
            new Claim("FirstName", user.FirstName ?? ""),
            new Claim("LastName", user.LastName ?? "")
        };

    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    var authProperties = new AuthenticationProperties
    {
      IsPersistent = model.RememberMe,
      ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
    };

    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
        new ClaimsPrincipal(claimsIdentity), authProperties);

    // Redirect based on role
    return user.Role switch
    {
      "Admin" => RedirectToAction("Index", "Dashboards"),
      "Driver" => RedirectToAction("Index", "RouteSchedule"),
      "Collector" => RedirectToAction("Index", "Scan"),
      _ => RedirectToAction("Index", "Dashboards")
    };
  }

  public async Task<IActionResult> Logout()
  {
    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return RedirectToAction("LoginBasic");
  }

  public IActionResult AccessDenied()
  {
    return View();
  }
}
