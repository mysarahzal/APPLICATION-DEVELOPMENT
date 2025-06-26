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
          .Include(s => s.Truck)
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
        ViewBag.Trucks = await _context.Trucks
            .Where(t => t.Status == "Available")
            .OrderBy(t => t.LicensePlate)
            .ToListAsync();

        ViewBag.Collectors = await _context.Users
            .Where(u => u.Role == "Collector" || u.Role == "Driver")
            .OrderBy(u => u.FirstName)
            .ToListAsync();

        // FIXED: Changed from Routes to RoutePlans
        ViewBag.Routes = await _context.RoutePlans
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
      ModelState.Remove("Id");
      ModelState.Remove("Collector");
      ModelState.Remove("Truck");
      ModelState.Remove("Route");
      ModelState.Remove("CollectionPoints");

      // Debug: Log validation errors
      if (!ModelState.IsValid)
      {
        foreach (var error in ModelState)
        {
          Console.WriteLine($"Validation Error - Key: {error.Key}, Errors: {string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage))}");
        }
      }

      if (ModelState.IsValid)
      {
        try
        {
          // Validate that the selected entities exist
          var truckExists = await _context.Trucks.AnyAsync(t => t.Id == schedule.TruckId);
          var collectorExists = await _context.Users.AnyAsync(u => u.Id == schedule.CollectorId);
          var routeExists = await _context.RoutePlans.AnyAsync(r => r.Id == schedule.RouteId);

          if (!truckExists)
          {
            ModelState.AddModelError("TruckId", "Selected truck does not exist.");
          }
          if (!collectorExists)
          {
            ModelState.AddModelError("CollectorId", "Selected collector does not exist.");
          }
          if (!routeExists)
          {
            ModelState.AddModelError("RouteId", "Selected route does not exist.");
          }

          if (ModelState.IsValid)
          {
            // Set automatic fields
            schedule.CreatedAt = DateTime.Now;
            schedule.UpdatedAt = DateTime.Now;
            schedule.ActualStartTime = null;
            schedule.ActualEndTime = null;

            _context.Add(schedule);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Schedule created successfully!";
            return RedirectToAction(nameof(Index));
          }
        }
        catch (Exception ex)
        {
          ModelState.AddModelError("", "Error creating schedule: " + ex.Message);
          Console.WriteLine($"Exception creating schedule: {ex.Message}");
          Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
      }

      // If validation fails, reload dropdown data
      await LoadDropdownData();
      return View(schedule);
    }
    //[HttpPost]
    //[ValidateAntiForgeryToken]
    //public async Task<IActionResult> Create([Bind("TruckId,CollectorId,RouteId,ScheduleStartTime,ScheduleEndTime,Status,AdminNotes")] Schedule schedule)
    //{
    //  // Remove validation for auto-generated fields
    //  ModelState.Remove("ActualStartTime");
    //  ModelState.Remove("ActualEndTime");
    //  ModelState.Remove("CreatedAt");
    //  ModelState.Remove("UpdatedAt");
    //  ModelState.Remove("Id");  // Remove Id validation since it's auto-generated

    //  if (ModelState.IsValid)
    //  {
    //    try
    //    {
    //      // Set automatic fields
    //      schedule.CreatedAt = DateTime.Now;
    //      schedule.UpdatedAt = DateTime.Now;
    //      schedule.ActualStartTime = null;  // Use null instead of DateTime.MinValue
    //      schedule.ActualEndTime = null;    // Use null instead of DateTime.MinValue
    //                                        // Don't manually set Id - let EF Core auto-generate it

    //      _context.Add(schedule);
    //      await _context.SaveChangesAsync();

    //      TempData["Success"] = "Schedule created successfully!";
    //      return RedirectToAction(nameof(Index));
    //    }
    //    catch (Exception ex)
    //    {
    //      ModelState.AddModelError("", "Error creating schedule: " + ex.Message);
    //    }
    //  }

    //  // If validation fails, reload dropdown data
    //  await LoadDropdownData();
    //  return View(schedule);
    //}

    // GET: Schedule/Edit/5
    // GET: Schedule/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
      if (id == null)
      {
        return NotFound();
      }

      var schedule = await _context.Schedules
          .Include(s => s.Collector)
          .Include(s => s.Truck)
          .Include(s => s.Route)
          .FirstOrDefaultAsync(s => s.Id == id);

      if (schedule == null)
      {
        return NotFound();
      }

      // Load dropdown data
      await LoadDropdownData();

      // Debug logging
      Console.WriteLine($"Editing schedule ID: {id}");
      Console.WriteLine($"Schedule found: {schedule.Id}");
      Console.WriteLine($"Current RouteId: {schedule.RouteId}");
      Console.WriteLine($"Current TruckId: {schedule.TruckId}");
      Console.WriteLine($"Current CollectorId: {schedule.CollectorId}");

      return View(schedule);
    }
    //public async Task<IActionResult> Edit(int? id)
    //{
    //  if (id == null)
    //  {
    //    return NotFound();
    //  }

    //  var schedule = await _context.Schedules.FindAsync(id);
    //  if (schedule == null)
    //  {
    //    return NotFound();
    //  }

    //  await LoadDropdownData();
    //  return View(schedule);
    //}

    // POST: Schedule/Edit/5
    // POST: Schedule/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,TruckId,CollectorId,RouteId,ScheduleStartTime,ScheduleEndTime,Status,AdminNotes,CreatedAt,ActualStartTime,ActualEndTime")] Schedule schedule)
    {
      if (id != schedule.Id)
      {
        return NotFound();
      }

      // Remove validation for navigation properties and auto-updated fields
      ModelState.Remove("UpdatedAt");
      ModelState.Remove("Collector");
      ModelState.Remove("Truck");
      ModelState.Remove("Route");
      ModelState.Remove("CollectionPoints");

      // Debug: Log validation errors
      if (!ModelState.IsValid)
      {
        Console.WriteLine("ModelState is invalid:");
        foreach (var error in ModelState)
        {
          Console.WriteLine($"Key: {error.Key}, Errors: {string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage))}");
        }
      }

      if (ModelState.IsValid)
      {
        try
        {
          // Get the existing schedule from database
          var existingSchedule = await _context.Schedules.FindAsync(id);
          if (existingSchedule == null)
          {
            return NotFound();
          }

          // Update only the fields that can be changed
          existingSchedule.TruckId = schedule.TruckId;
          existingSchedule.CollectorId = schedule.CollectorId;
          existingSchedule.RouteId = schedule.RouteId;
          existingSchedule.ScheduleStartTime = schedule.ScheduleStartTime;
          existingSchedule.ScheduleEndTime = schedule.ScheduleEndTime;
          existingSchedule.Status = schedule.Status;
          existingSchedule.AdminNotes = schedule.AdminNotes;
          existingSchedule.ActualStartTime = schedule.ActualStartTime;
          existingSchedule.ActualEndTime = schedule.ActualEndTime;
          existingSchedule.UpdatedAt = DateTime.Now;

          await _context.SaveChangesAsync();
          TempData["Success"] = "Schedule updated successfully!";
          return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateConcurrencyException ex)
        {
          if (!ScheduleExists(schedule.Id))
          {
            return NotFound();
          }
          else
          {
            Console.WriteLine($"Concurrency exception: {ex.Message}");
            ModelState.AddModelError("", "The schedule was modified by another user. Please reload and try again.");
          }
        }
        catch (Exception ex)
        {
          Console.WriteLine($"Error updating schedule: {ex.Message}");
          Console.WriteLine($"Stack trace: {ex.StackTrace}");
          ModelState.AddModelError("", "Error updating schedule: " + ex.Message);
        }
      }

      await LoadDropdownData();
      return View(schedule);
    }
    //[HttpPost]
    //[ValidateAntiForgeryToken]
    //public async Task<IActionResult> Edit(int id, [Bind("Id,TruckId,CollectorId,RouteId,ScheduleStartTime,ScheduleEndTime,Status,AdminNotes,CreatedAt,ActualStartTime,ActualEndTime")] Schedule schedule)
    //{
    //  if (id != schedule.Id)
    //  {
    //    return NotFound();
    //  }

    //  ModelState.Remove("UpdatedAt");

    //  if (ModelState.IsValid)
    //  {
    //    try
    //    {
    //      schedule.UpdatedAt = DateTime.Now;
    //      _context.Update(schedule);
    //      await _context.SaveChangesAsync();

    //      TempData["Success"] = "Schedule updated successfully!";
    //      return RedirectToAction(nameof(Index));
    //    }
    //    catch (DbUpdateConcurrencyException)
    //    {
    //      if (!ScheduleExists(schedule.Id))
    //      {
    //        return NotFound();
    //      }
    //      else
    //      {
    //        throw;
    //      }
    //    }
    //  }

    //  await LoadDropdownData();
    //  return View(schedule);
    //}

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
    // Helper method to load all dropdown data
    private async Task LoadDropdownData()
    {
      try
      {
        ViewBag.Trucks = await _context.Trucks
            .Where(t => t.Status == "Available")
            .OrderBy(t => t.LicensePlate)
            .ToListAsync();

        ViewBag.Collectors = await _context.Users
            .Where(u => u.Role == "Collector" || u.Role == "Driver")
            .OrderBy(u => u.FirstName)
            .ToListAsync();

        ViewBag.Routes = await _context.RoutePlans
            .OrderBy(r => r.Name)
            .ToListAsync();

        // Debug logging
        Console.WriteLine($"Loaded {((List<Truck>)ViewBag.Trucks).Count} trucks");
        Console.WriteLine($"Loaded {((List<User>)ViewBag.Collectors).Count} collectors");
        Console.WriteLine($"Loaded {((List<RoutePlan>)ViewBag.Routes).Count} routes");
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Error loading dropdown data: {ex.Message}");
        ViewBag.ErrorMessage = "Error loading form data: " + ex.Message;
      }
    }
    //private async Task LoadDropdownData()
    //{
    //  ViewBag.Trucks = await _context.Trucks
    //      .Where(t => t.Status == "Available")
    //      .OrderBy(t => t.LicensePlate)
    //      .ToListAsync();

    //  ViewBag.Collectors = await _context.Users
    //      .Where(u => u.Role == "Collector" || u.Role == "Driver")
    //      .OrderBy(u => u.FirstName)
    //      .ToListAsync();

    //  // FIXED: Changed from Routes to RoutePlans
    //  ViewBag.Routes = await _context.RoutePlans
    //      .OrderBy(r => r.Name)
    //      .ToListAsync();
    //}

    private bool ScheduleExists(int id)
    {
      return _context.Schedules.Any(e => e.Id == id);
    }
  }
}
