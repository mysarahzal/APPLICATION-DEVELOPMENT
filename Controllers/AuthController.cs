using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.ViewModels;
using Microsoft.EntityFrameworkCore;
using AspnetCoreMvcFull.Data;

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

    // TODO: Replace with actual authentication logic (e.g., check database or use Identity)

    var user = _context.Users.FirstOrDefault(u => u.Email == model.Email);

    if (user == null || user.Password != model.Password) // Not secure! Use hashing in production
    {
      ModelState.AddModelError(string.Empty, "Invalid login attempt.");
      return View(model);
    }

    // TODO: Set up authentication cookie or session here
    // Example: SignInManager.PasswordSignInAsync(...)

    return RedirectToAction("Index", "User"); // Or redirect to dashboard
  }

  public IActionResult Logout()
  {
    // Optional: Clear session or cookie
    //HttpContext.Session.Clear();

    // Optional: If using Identity
    // await _signInManager.SignOutAsync();

    // Redirect to login page
    return RedirectToAction("LoginBasic");
  }

}
