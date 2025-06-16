using Microsoft.AspNetCore.Mvc;
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.Data;
using Microsoft.EntityFrameworkCore;
using System;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;

namespace AspnetCoreMvcFull.Controllers
{
  public class ScheduleController : Controller
  {
    private readonly KUTIPDbContext _context;
    //private readonly UserManager<User> _userManager;

    public ScheduleController(KUTIPDbContext context)
      //UserManager<User> userManager)
    {
      _context = context;
      //_userManager = userManager;
    }

    // UC10 – Admin Views All Job Schedules
    //[Authorize(Roles = "Admin")]
    public async Task<IActionResult> Index()
    {
      var schedules = await _context.Schedules
          .Include(s => s.Collector)
          .Include(s => s.Road)
          .ToListAsync();

      return View("~/Views/Schedule/Index.cshtml", schedules);
    }

    // UC09 – Operator Views Their Own Schedule
    //[Authorize(Roles = "Operator")]
    public async Task<IActionResult> MySchedule()
    {
      //var currentUser = await _userManager.GetUserAsync(User);
      //if (currentUser == null) return Unauthorized();

      // Simulate logged-in user ID (for testing only)
      var userId = 1; // Replace with real logic later

      var mySchedules = await _context.Schedules
        //.Include(s => s.Collector)
        .Include(s => s.Road)
        //.Where(s => s.CollectorId == userId)
        .Where(s => s.CollectorId == userId)
        .ToListAsync();

      return View("~/Views/Schedule/MySchedule.cshtml", MySchedule);
    }

    // UC08 – Admin Creates a New Schedule
    //[Authorize(Roles = "Admin")]
    public IActionResult Create()
    {
      // Filter Collectors (Users with role = "Driver" or "Operator")
      ViewBag.Collectors = _context.Users
          .Where(u => u.Role == "Driver" || u.Role == "Operator")
          .ToList() ?? new List<User>();  // optional filter

      //var routes = _context.Roads.ToList();

      //ViewBag.Collectors = collectors ?? new List<User>();
      //ViewBag.Routes = routes ?? new List<Road>();
      ViewBag.Routes = _context.Roads
        .ToList() ?? new List<Road>();

      //ViewBag.Routes = new List<Road>
      //{
      //  new Road {Id = 1, Name = "Route A" },
      //  new Road {Id = 2, Name = "Route B" },
      //  new Road {Id = 3, Name = "Route C" },
      //};

      return View("~/Views/Schedule/Create.cshtml");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    //[Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(Schedule schedule)
    {
      if (ModelState.IsValid)
      {
        schedule.Status = "Scheduled";
        _context.Schedules.Add(schedule);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
      }

      // If failed, re-populate dropdowns
      ViewBag.Collectors = _context.Users
          .Where(u => u.Role == "Driver" || u.Role == "Operator")
          .ToList();

      //ViewBag.Collectors = _context.Users.Where(u => u.Role == "Operator").ToList();
      ViewBag.Routes = _context.Roads.ToList();
      return View("~/Views/Schedule/Create.cshtml", schedule);
    }

    // UC08 – Admin Edits Schedule
    //[Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int? id)
    {
      if (id == null) return NotFound();

      var schedule = await _context.Schedules.FindAsync(id);
                //.Include(s => s.Collector)
                //.Include(s => s.Road)
                //.FirstOrDefaultAsync(m => m.Id == id);

      if (schedule == null) return NotFound();

      ViewBag.Collectors = _context.Users.Where(u => u.Role == "Operator").ToList();
      ViewBag.Routes = _context.Roads.ToList();

      return View("~/Views/Schedule/Edit.cshtml", schedule);
    }

    [HttpPost]
    //[Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Schedule updatedSchedule)
    {
      if (id != updatedSchedule.Id)
        return NotFound();

      if (ModelState.IsValid)
      {
        try
        {
          _context.Update(updatedSchedule);
          await _context.SaveChangesAsync();
          return RedirectToAction(nameof(Index));
        }
        catch
        {
            throw;
        }
      }

      ViewBag.Collectors = _context.Users.Where(u => u.Role == "Operator").ToList();
      ViewBag.Routes = _context.Roads.ToList();
      return View("~/Views/Schedule/Edit.cshtml", updatedSchedule);
    }

    // UC08 – Admin Deletes a Schedule
    //[Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int? id)
    {
      if (id == null) return NotFound();

      var schedule = await _context.Schedules
          .Include(s => s.Collector)
          .Include(s => s.Road)
          .FirstOrDefaultAsync(m => m.Id == id);

      if (schedule == null) return NotFound();

      return View("~/Views/Schedule/Delete.cshtml", schedule);
    }

    [HttpPost, ActionName("Delete")]
    //[Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
      var schedule = await _context.Schedules.FindAsync(id);
      if (schedule != null)
      {
        _context.Schedules.Remove(schedule);
        await _context.SaveChangesAsync();
      }

      return RedirectToAction(nameof(Index));
    }

    private bool ScheduleExists(int id)
    {
      return _context.Schedules.Any(e => e.Id == id);
    }
  }
}

