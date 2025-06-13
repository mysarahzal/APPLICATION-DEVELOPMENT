using Microsoft.AspNetCore.Mvc;
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.Data;
using System;

namespace AspnetCoreMvcFull.Controllers
{
  public class UserController : Controller
  {
    private readonly KUTIPDbContext _context;
    public UserController(KUTIPDbContext context)
    {
      _context = context;
    }
    public IActionResult Index()
    {
      List<User> users;
      users = _context.Users.ToList();
      return View(users);
    }
    public IActionResult Create()
    {
      User user = new User();
      return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(User user)
    {
      _context.Add(user);
      _context.SaveChanges();
      return RedirectToAction("Index");
    }

    // GET: /User/Edit/5
    public IActionResult Edit(int id)
    {
      var user = _context.Users.Find(id);
      if (user == null)
      {
        return NotFound();
      }
      return View(user);
    }

    // POST: /User/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(int id, User user)
    {
      if (id != user.Id)
      {
        return BadRequest();
      }

      if (ModelState.IsValid)
      {
        try
        {
          _context.Update(user);
          _context.SaveChanges();
          TempData["Success"] = "User updated successfully.";
          return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
          ModelState.AddModelError("", "Unable to save changes. " + ex.Message);
        }
      }
      return View(user);
    }

    // GET: /User/Delete/5
    public IActionResult Delete(int id)
    {
      var user = _context.Users
                         .FirstOrDefault(u => u.Id == id);
      if (user == null)
      {
        return NotFound();
      }
      return View(user);
    }

    // POST: /User/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteConfirmed(int id)
    {
      var user = _context.Users.Find(id);
      if (user == null)
      {
        return NotFound();
      }

      try
      {
        _context.Users.Remove(user);
        _context.SaveChanges();
        TempData["Success"] = "User deleted successfully.";
      }
      catch (Exception ex)
      {
        TempData["Error"] = "Error deleting user: " + ex.Message;
      }

      return RedirectToAction(nameof(Index));
    }
  }
}
