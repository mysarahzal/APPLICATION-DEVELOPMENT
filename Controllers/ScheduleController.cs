//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using AspnetCoreMvcFull.Models;
//using AspnetCoreMvcFull.Data;
//using Microsoft.EntityFrameworkCore;
//using System.Linq;
//using System.Threading.Tasks;
//using AspnetCoreMvcFull.Models.ViewModels;

//namespace AspnetCoreMvcFull.Controllers
//{
//  //[Authorize]
//  public class ScheduleController : Controller
//  {
//    private readonly KUTIPDbContext _context;

//    public ScheduleController(KUTIPDbContext context)
//    {
//      _context = context;
//    }

//    // GET: Schedule/Index
//    public ActionResult Index()
//    {
//      var schedules = _context.Schedules
//          .Include(s => s.Collector)
//          .Include(s => s.Road)
//          //.Where(s => s.IsActive)
//          .ToList();

//      return View("~/Views/Schedule/Index.cshtml", schedules);
//    }

//    // GET: Schedule/Create
//    public ActionResult Create()
//    {
//      ViewBag.Clients = _context.Clients.ToList();
//      ViewBag.Bins = _context.Bins.ToList();
//      return View();
//    }

//    [HttpPost]
//    [ValidateAntiForgeryToken]
//    public ActionResult Create(Schedule model)
//    {
//      if (ModelState.IsValid)
//      {
//        model.Status = "Pending";
//        _context.Schedules.Add(model);
//        _context.SaveChanges();
//        return RedirectToAction("Index");
//      }

//      ViewBag.Clients = _context.Clients.ToList();
//      ViewBag.Bins = _context.Bins.ToList();
//      return View(model);
//    }

//    // GET: Schedule/Edit/5
//    public async Task<IActionResult> Edit(int? id)
//    {
//      if (id == null) return NotFound();

//      var schedule = await _context.Schedules
//          .FirstOrDefaultAsync(m => m.Id == id);

//      if (schedule == null) return NotFound();

//      ViewBag.Collectors = _context.Users
//          .Where(u => u.Role == "Driver" || u.Role == "Collector")
//          .ToList() ?? new List<User>();

//      ViewBag.Routes = _context.Roads.ToList() ?? new List<Road>();

//      return View("~/Views/Schedule/Edit.cshtml", schedule);
//    }

//    // POST: Schedule/Edit
//    [HttpPost]
//    [ValidateAntiForgeryToken]
//    public async Task<IActionResult> Edit(int id, Schedule schedule)
//    {
//      if (id != schedule.Id)
//        return NotFound();

//      if (ModelState.IsValid)
//      {
//        try
//        {
//          schedule.UpdatedAt = DateTime.Now;
//          _context.Update(schedule);
//          await _context.SaveChangesAsync();
//          return RedirectToAction(nameof(Index));
//        }
//        catch (DbUpdateConcurrencyException)
//        {
//          if (!ScheduleExists(schedule.Id))
//            return NotFound();
//          else
//            throw;
//        }
//      }

//      ViewBag.Collectors = _context.Users
//          .Where(u => u.Role == "Driver" || u.Role == "Collector")
//          .ToList() ?? new List<User>();

//      ViewBag.Routes = _context.Roads.ToList() ?? new List<Road>();

//      return View("~/Views/Schedule/Edit.cshtml", schedule);
//    }

//    // GET: Schedule/Delete/5
//    public async Task<IActionResult> Delete(int? id)
//    {
//      if (id == null) return NotFound();

//      var schedule = await _context.Schedules
//          .Include(s => s.Collector)
//          .Include(s => s.Road)
//          .FirstOrDefaultAsync(m => m.Id == id);

//      if (schedule == null) return NotFound();

//      return View("~/Views/Schedule/Delete.cshtml", schedule);
//    }

//    // POST: Schedule/Delete/5
//    [HttpPost, ActionName("Delete")]
//    [ValidateAntiForgeryToken]
//    public async Task<IActionResult> DeleteConfirmed(int id)
//    {
//      var schedule = await _context.Schedules.FindAsync(id);
//      if (schedule != null)
//      {
//        _context.Schedules.Remove(schedule);
//        await _context.SaveChangesAsync();
//      }

//      return RedirectToAction(nameof(Index));
//    }

//    private bool ScheduleExists(int id)
//    {
//      return _context.Schedules.Any(e => e.Id == id);
//    }
//  }
//}

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

    // UC08: Create
    public IActionResult Create()
    {
      // Get only users with Collector or Driver role
      ViewBag.Collectors = _context.Users
          .Where(u => u.Role == "Collector" || u.Role == "Driver")
          .ToList();

      return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("TruckId,CollectorId,RouteId,ScheduleStartTime,ScheduleEndTime,Status")] Schedule schedule)
    {
      try
      {
        if (ModelState.IsValid)
        {
          // Generate a unique Schedule ID
          //schedule.ScheduleId = $"SCH-{DateTime.Now:yyyyMMddHHmmss}";
          schedule.CreatedAt = DateTime.Now;
          schedule.UpdatedAt = DateTime.Now;

          _context.Add(schedule);
          await _context.SaveChangesAsync();

          return RedirectToAction(nameof(Index));
        }
      }
      catch (DbUpdateException ex)
      {
        // Log the error
        //_logger.LogError(ex, "An error occurred while creating the schedule.");

        // Add a model error
        ModelState.AddModelError("", "Unable to save changes. " +
            "Try again, and if the problem persists " +
            "see your system administrator.");
      }

      // If we get here, something went wrong
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
