

using AspnetCoreMvcFull.Data;
using AspnetCoreMvcFull.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AspnetCoreMvcFull.Controllers
{
  public class ScheduleController : Controller
  {
    private readonly KUTIPDbContext _context;

    public ScheduleController(KUTIPDbContext context)
    {
      _context = context;
    }

    // UC08: Admin Manage Pickup Schedule (CRUD)
    public async Task<IActionResult> Index()
    {
      var schedules = await _context.Schedules
          .Include(s => s.Collector)
          .Include(s => s.Road)
          .ToListAsync();
      return View(schedules);
    }

    //// GET: Schedule/Create
    //public IActionResult Create()
    //{
    //  // Get only users with Collector or Driver role
    //  ViewBag.Collectors = _context.Users
    //      .Where(u => u.Role == "Collector" || u.Role == "Driver")
    //      .ToList();

    //  return View();
    //}


    // GET: Schedule/Create
    public async Task<IActionResult> Create()
    {
      try
      {
        Console.WriteLine("=== CREATE GET ACTION STARTED ===");

        // Get only users with Collector or Driver role
        var collectors = await _context.Users
            .Where(u => u.Role == "Collector" || u.Role == "Driver")
            .ToListAsync();

        Console.WriteLine($"Found {collectors.Count} collectors/drivers");

        ViewBag.Collectors = collectors;

        Console.WriteLine("=== RETURNING CREATE VIEW ===");
        return View();
      }
      catch (Exception ex)
      {
        Console.WriteLine($"ERROR in Create GET: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");

        ViewBag.ErrorMessage = ex.Message;
        return View("Error"); // Create a simple error view
      }
    }

    //// POST: Schedule/Create
    //[HttpPost]
    //[ValidateAntiForgeryToken]
    //public async Task<IActionResult> Create([Bind("TruckId,CollectorId,RouteId,ScheduleStartTime,ScheduleEndTime,Status,AdminNotes")] Schedule schedule)
    //{
    //  try
    //  {
    //    if (ModelState.IsValid)
    //    {
    //      // Generate a unique Schedule ID
    //      //schedule.ScheduleId = $"SCH-{DateTime.Now:yyyyMMddHHmmss}";
    //      schedule.CreatedAt = DateTime.Now;
    //      schedule.UpdatedAt = DateTime.Now;

    //      _context.Add(schedule);
    //      await _context.SaveChangesAsync();

    //      return RedirectToAction(nameof(Index));
    //    }
    //  }
    //  catch (DbUpdateException ex)
    //  {
    //    // Log the error
    //    //_logger.LogError(ex, "An error occurred while creating the schedule.");

    //    // Add a model error
    //    ModelState.AddModelError("", "Unable to save changes. " +
    //        "Try again, and if the problem persists " +
    //        "see your system administrator.");
    //  }

    //  // If we get here, something went wrong
    //  ViewBag.Collectors = await _context.Users
    //      .Where(u => u.Role == "Collector" || u.Role == "Driver")
    //      .ToListAsync();

    //  return View(schedule);
    //}

    // POST: Schedule/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("TruckId,CollectorId,RouteId,ScheduleStartTime,ScheduleEndTime,Status,AdminNotes")] Schedule schedule)
    {
      try
      {
        if (ModelState.IsValid)
        {
          // Set timestamps
          schedule.CreatedAt = DateTime.Now;
          schedule.UpdatedAt = DateTime.Now;

          // Set default values for required fields if they're not set
          if (schedule.ActualStartTime == DateTime.MinValue)
            schedule.ActualStartTime = DateTime.Now;
          if (schedule.ActualEndTime == DateTime.MinValue)
            schedule.ActualEndTime = DateTime.Now;

          _context.Add(schedule);
          await _context.SaveChangesAsync();

          TempData["SuccessMessage"] = "Schedule created successfully!";
          return RedirectToAction(nameof(Index));
        }
      }
      catch (DbUpdateException ex)
      {
        // Log the error
        ModelState.AddModelError("", "Unable to save changes. " +
            "Try again, and if the problem persists " +
            "see your system administrator. Error: " + ex.InnerException?.Message);
      }

      // If we get here, something went wrong - reload the dropdown data
      ViewBag.Collectors = await _context.Users
          .Where(u => u.Role == "Collector" || u.Role == "Driver")
          .ToListAsync();

      return View(schedule);
    }

    // UC08: Edit
    public async Task<IActionResult> Edit(int? id)
    {
      if (id == null)
      {
        return NotFound();
      }

      var schedule = await _context.Schedules.FindAsync(id);
      if (schedule == null)
      {
        return NotFound();
      }

      ViewBag.Trucks = _context.Trucks.ToList();
      ViewBag.Collectors = _context.Users.Where(u => u.Role == "Collector").ToList();
      ViewBag.Routes = _context.Roads.ToList();
      return View(schedule);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Schedule schedule)
    {
      if (id != schedule.Id)
      {
        return NotFound();
      }

      if (ModelState.IsValid)
      {
        try
        {
          schedule.UpdatedAt = DateTime.Now;
          _context.Update(schedule);
          await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
          if (!ScheduleExists(schedule.Id))
          {
            return NotFound();
          }
          else
          {
            throw;
          }
        }
        return RedirectToAction(nameof(Index));
      }
      return View(schedule);
    }

    // UC08: Delete
    public async Task<IActionResult> Delete(int? id)
    {
      if (id == null)
      {
        return NotFound();
      }

      var schedule = await _context.Schedules
          .Include(s => s.Collector)
          .Include(s => s.Road)
          .FirstOrDefaultAsync(m => m.Id == id);
      if (schedule == null)
      {
        return NotFound();
      }

      return View(schedule);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
      var schedule = await _context.Schedules.FindAsync(id);
      _context.Schedules.Remove(schedule);
      await _context.SaveChangesAsync();
      return RedirectToAction(nameof(Index));
    }

    // UC09: Driver Views Schedule
    public async Task<IActionResult> DriverSchedule()
    {
      // In a real app, you'd filter by the logged-in driver's ID
      var driverSchedules = await _context.Schedules
          .Include(s => s.Collector)
          .Include(s => s.Road)
          .Where(s => s.CollectorId == GetCurrentUserId()) // You'd implement GetCurrentUserId()
          .ToListAsync();

      return View(driverSchedules);
    }

    // UC10: Admin Views All Job Schedules
    public async Task<IActionResult> AllSchedules()
    {
      var allSchedules = await _context.Schedules
          .Include(s => s.Collector)
          .Include(s => s.Road)
          .ToListAsync();

      return View(allSchedules);
    }

    private bool ScheduleExists(int id)
    {
      return _context.Schedules.Any(e => e.Id == id);
    }

    // Helper method - in a real app, you'd get this from authentication
    private int GetCurrentUserId()
    {
      // Implement your logic to get current user ID
      return 1; // Placeholder
    }
  }
}
