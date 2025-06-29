using Microsoft.AspNetCore.Mvc;
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.Data;
using System;
using AspnetCoreMvcFull.ViewModels;
using Microsoft.AspNetCore.Authorization;

namespace AspnetCoreMvcFull.Controllers
{
  [Authorize(Roles = "Admin")]
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

    // GET: /User/Edit/5 - REPLACE this method
    public IActionResult Edit(int id)
    {
      var user = _context.Users.Find(id);
      if (user == null)
      {
        return NotFound();
      }

      // Map User model to EditUserViewModel
      var viewModel = new EditUserViewModel
      {
        Id = user.Id,
        Email = user.Email,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Role = user.Role,
        PhoneNumber = user.PhoneNumber
      };

      return View(viewModel);
    }

    // POST: /User/Edit/5 - REPLACE this method
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(int id, EditUserViewModel viewModel)
    {
      if (id != viewModel.Id)
      {
        return BadRequest();
      }

      if (ModelState.IsValid)
      {
        try
        {
          // Get the existing user from database
          var user = _context.Users.Find(id);
          if (user == null)
          {
            return NotFound();
          }

          // Update only the editable fields (keep password and other fields unchanged)
          user.Email = viewModel.Email;
          user.FirstName = viewModel.FirstName;
          user.LastName = viewModel.LastName;
          user.Role = viewModel.Role;
          user.PhoneNumber = viewModel.PhoneNumber;

          _context.Update(user);
          _context.SaveChanges();
          TempData["Success"] = "User updated successfully.";
          return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
          ModelState.AddModelError("", "Unable to save changes. " + ex.Message);
          TempData["Error"] = "Unable to save changes: " + ex.Message;
        }
      }

      // If validation failed, return the view with errors
      return View(viewModel);
    }

    // GET: /User/Edit/5
    //public IActionResult Edit(int id)
    //{
    //  var user = _context.Users.Find(id);
    //  if (user == null)
    //  {
    //    return NotFound();
    //  }
    //  return View(user);
    //}

    // POST: /User/Edit/5
    //[HttpPost]
    //[ValidateAntiForgeryToken]
    //public IActionResult Edit(int id, User user)
    //{
    //  if (id != user.Id)
    //  {
    //    return BadRequest();
    //  }

    //  if (ModelState.IsValid)
    //  {
    //    try
    //    {
    //      _context.Update(user);
    //      _context.SaveChanges();
    //      TempData["Success"] = "User updated successfully.";
    //      return RedirectToAction("Index");
    //    }
    //    catch (Exception ex)
    //    {
    //      ModelState.AddModelError("", "Unable to save changes. " + ex.Message);
    //    }
    //  }
    //  return View(user);
    //}

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
