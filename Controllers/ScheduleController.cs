using AspnetCoreMvcFull.Data;
using AspnetCoreMvcFull.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AspnetCoreMvcFull.Controllers
{
  public class ScheduleController : Controller
  {
    private readonly KUTIPDbContext _context;

    public ScheduleController(KUTIPDbContext context)
    {
      _context = context;
    }

    // GET: Schedule
    public async Task<IActionResult> Index()
    {
      var schedules = await _context.Schedules
          .Include(s => s.Collector)
          .Include(s => s.Truck) // Include truck details
          .Include(s => s.Route)
          .OrderByDescending(s => s.CreatedAt)
          .ToListAsync();
      return View(schedules);
    }

    // GET: Schedule/Create
    public async Task<IActionResult> Create()
    {
      try
      {
        // Get ALL trucks from your Truck table that you created via Truck CRUD
        // You can filter by status if you want only available trucks
        ViewBag.Trucks = await _context.Trucks
            .Where(t => t.Status == "Available") // Only show available trucks
            .OrderBy(t => t.LicensePlate) // Sort by license plate
            .ToListAsync();

        // Alternative: Get ALL trucks regardless of status
        // ViewBag.Trucks = await _context.Trucks
        //     .OrderBy(t => t.LicensePlate)
        //     .ToListAsync();

        // Get collectors/drivers
        ViewBag.Collectors = await _context.Users
            .Where(u => u.Role == "Collector" || u.Role == "Driver")
            .OrderBy(u => u.FirstName)
            .ToListAsync();

        // Get routes
        ViewBag.Routes = await _context.Routes
            .OrderBy(r => r.Name)
            .ToListAsync();

        return View();
      }
      catch (Exception ex)
      {
        ViewBag.ErrorMessage = "Error loading form data: " + ex.Message;
        return View();
      }
    }

    // POST: Schedule/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("TruckId,CollectorId,RouteId,ScheduleStartTime,ScheduleEndTime,Status,AdminNotes")] Schedule schedule)
    {
      // Remove validation for auto-generated fields
      ModelState.Remove("ActualStartTime");
      ModelState.Remove("ActualEndTime");
      ModelState.Remove("CreatedAt");
      ModelState.Remove("UpdatedAt");
      ModelState.Remove("ScheduleId");

      if (ModelState.IsValid)
      {
        try
        {
          // Set automatic fields
          schedule.CreatedAt = DateTime.Now;
          schedule.UpdatedAt = DateTime.Now;
          schedule.ActualStartTime = DateTime.MinValue;
          schedule.ActualEndTime = DateTime.MinValue;
          schedule.Id = new Random().Next(1000, 9999);

          _context.Add(schedule);
          await _context.SaveChangesAsync();

          TempData["Success"] = "Schedule created successfully!";
          return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
          ModelState.AddModelError("", "Error creating schedule: " + ex.Message);
        }
      }

      // If validation fails, reload dropdown data including trucks
      await LoadDropdownData();
      return View(schedule);
    }

    // GET: Schedule/Edit/5
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

      await LoadDropdownData();
      return View(schedule);
    }

    // POST: Schedule/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,ScheduleId,TruckId,CollectorId,RouteId,ScheduleStartTime,ScheduleEndTime,Status,AdminNotes,CreatedAt,ActualStartTime,ActualEndTime")] Schedule schedule)
    {
      if (id != schedule.Id)
      {
        return NotFound();
      }

      ModelState.Remove("UpdatedAt");

      if (ModelState.IsValid)
      {
        try
        {
          schedule.UpdatedAt = DateTime.Now;
          _context.Update(schedule);
          await _context.SaveChangesAsync();

          TempData["Success"] = "Schedule updated successfully!";
          return RedirectToAction(nameof(Index));
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
      }

      await LoadDropdownData();
      return View(schedule);
    }

    // GET: Schedule/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
      if (id == null)
      {
        return NotFound();
      }

      var schedule = await _context.Schedules
          .Include(s => s.Collector)
          .Include(s => s.Truck)
          .Include(s => s.Route)
          .FirstOrDefaultAsync(m => m.Id == id);

      if (schedule == null)
      {
        return NotFound();
      }

      return View(schedule);
    }

    // POST: Schedule/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
      var schedule = await _context.Schedules.FindAsync(id);
      if (schedule != null)
      {
        _context.Schedules.Remove(schedule);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Schedule deleted successfully!";
      }

      return RedirectToAction(nameof(Index));
    }

    // Helper method to load all dropdown data
    private async Task LoadDropdownData()
    {
      // Load trucks from your Truck CRUD system
      ViewBag.Trucks = await _context.Trucks
          .Where(t => t.Status == "Available") // Only available trucks
          .OrderBy(t => t.LicensePlate)
          .ToListAsync();

      ViewBag.Collectors = await _context.Users
          .Where(u => u.Role == "Collector" || u.Role == "Driver")
          .OrderBy(u => u.FirstName)
          .ToListAsync();

      ViewBag.Routes = await _context.Routes
          .OrderBy(r => r.Name)
          .ToListAsync();
    }

    private bool ScheduleExists(int id)
    {
      return _context.Schedules.Any(e => e.Id == id);
    }
  }
}
