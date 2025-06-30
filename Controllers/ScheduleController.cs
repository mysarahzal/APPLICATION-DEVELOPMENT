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
          .Include(s => s.CollectionPoints)
          .ThenInclude(cp => cp.Bin)
          .OrderByDescending(s => s.CreatedAt)
          .ToListAsync();
      return View(schedules);
    }

    // GET: Schedule/Create
    public async Task<IActionResult> Create()
    {
      try
      {
        await LoadDropdownData();
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
    public async Task<IActionResult> Create([Bind("TruckId,CollectorId,RouteId,ScheduleDay,ScheduleStartTime,ScheduleEndTime,Status,AdminNotes")] Schedule schedule)
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
      ModelState.Remove("RouteCenterLatitude");
      ModelState.Remove("RouteCenterLongitude");

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

            // Get bins from the selected route with their coordinates
            var routeBins = await _context.RouteBins
                .Include(rb => rb.Bin)
                .Where(rb => rb.RouteId == schedule.RouteId)
                .OrderBy(rb => rb.OrderInRoute)
                .ToListAsync();

            // Calculate route center coordinates
            if (routeBins.Any())
            {
              var validCoordinates = routeBins
                  .Where(rb => rb.Bin.Latitude.HasValue && rb.Bin.Longitude.HasValue)
                  .ToList();

              if (validCoordinates.Any())
              {
                schedule.RouteCenterLatitude = validCoordinates.Average(rb => rb.Bin.Latitude.Value);
                schedule.RouteCenterLongitude = validCoordinates.Average(rb => rb.Bin.Longitude.Value);
              }
            }

            _context.Add(schedule);
            await _context.SaveChangesAsync();

            // Create collection points for each bin in the route
            var collectionPoints = new List<CollectionPoint>();
            foreach (var routeBin in routeBins)
            {
              var collectionPoint = new CollectionPoint
              {
                Id = Guid.NewGuid(),
                ScheduleId = schedule.Id,
                BinId = routeBin.BinId,
                OrderInSchedule = routeBin.OrderInRoute,
                IsCollected = false,
                CollectedAt = null,
                // Copy coordinates from bin
                Latitude = routeBin.Bin.Latitude,
                Longitude = routeBin.Bin.Longitude
              };
              collectionPoints.Add(collectionPoint);
            }

            if (collectionPoints.Any())
            {
              _context.CollectionPoints.AddRange(collectionPoints);
              await _context.SaveChangesAsync();
            }

            TempData["Success"] = $"Schedule created successfully with {collectionPoints.Count} collection points!";
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
          .Include(s => s.CollectionPoints)
          .ThenInclude(cp => cp.Bin)
          .ThenInclude(b => b.Client)
          .FirstOrDefaultAsync(s => s.Id == id);

      if (schedule == null)
      {
        return NotFound();
      }

      // Get all bins in the route
      var routeBins = await _context.RouteBins
          .Include(rb => rb.Bin)
          .ThenInclude(b => b.Client)
          .Where(rb => rb.RouteId == schedule.RouteId)
          .OrderBy(rb => rb.OrderInRoute)
          .ToListAsync();

      // Get bins that are already in the schedule
      var scheduledBinIds = schedule.CollectionPoints.Select(cp => cp.BinId).ToList();

      // Get available bins (not yet in schedule)
      var availableBins = routeBins.Where(rb => !scheduledBinIds.Contains(rb.BinId)).ToList();

      ViewBag.RouteBins = routeBins;
      ViewBag.AvailableBins = availableBins;

      await LoadDropdownData();
      return View(schedule);
    }

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
      ModelState.Remove("RouteCenterLatitude");
      ModelState.Remove("RouteCenterLongitude");

      if (ModelState.IsValid)
      {
        try
        {
          var existingSchedule = await _context.Schedules
              .Include(s => s.CollectionPoints)
              .FirstOrDefaultAsync(s => s.Id == id);

          if (existingSchedule == null)
          {
            return NotFound();
          }

          // Check if route changed
          bool routeChanged = existingSchedule.RouteId != schedule.RouteId;

          // Update schedule fields
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

          // If route changed, update collection points and coordinates
          if (routeChanged)
          {
            // Remove existing collection points
            _context.CollectionPoints.RemoveRange(existingSchedule.CollectionPoints);

            // Get new route bins
            var routeBins = await _context.RouteBins
                .Include(rb => rb.Bin)
                .Where(rb => rb.RouteId == schedule.RouteId)
                .OrderBy(rb => rb.OrderInRoute)
                .ToListAsync();

            // Recalculate route center coordinates
            await RecalculateRouteCenterCoordinates(existingSchedule, routeBins);

            // Create new collection points
            await CreateCollectionPointsForBins(existingSchedule.Id, routeBins);
          }

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
          .Include(s => s.CollectionPoints)
          .ThenInclude(cp => cp.Bin)
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
      var schedule = await _context.Schedules
          .Include(s => s.CollectionPoints)
          .FirstOrDefaultAsync(s => s.Id == id);

      if (schedule != null)
      {
        // Remove collection points first
        _context.CollectionPoints.RemoveRange(schedule.CollectionPoints);
        _context.Schedules.Remove(schedule);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Schedule deleted successfully!";
      }

      return RedirectToAction(nameof(Index));
    }

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

        // Get routes that don't have active schedules (not completed or cancelled)
        var activeScheduleRouteIds = await _context.Schedules
            .Where(s => s.Status != "Completed" && s.Status != "Cancelled")
            .Select(s => s.RouteId)
            .Distinct()
            .ToListAsync();

        var availableRoutes = await _context.RoutePlans
            .Where(r => !activeScheduleRouteIds.Contains(r.Id))
            .OrderBy(r => r.Name)
            .ToListAsync();

        var scheduledRoutes = await _context.RoutePlans
            .Where(r => activeScheduleRouteIds.Contains(r.Id))
            .OrderBy(r => r.Name)
            .ToListAsync();

        ViewBag.Routes = availableRoutes;
        ViewBag.ScheduledRoutes = scheduledRoutes;

        Console.WriteLine($"Available routes: {availableRoutes.Count}");
        Console.WriteLine($"Scheduled routes: {scheduledRoutes.Count}");
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Error loading dropdown data: {ex.Message}");
        ViewBag.ErrorMessage = "Error loading form data: " + ex.Message;

        // Set empty lists to prevent null reference errors
        ViewBag.Routes = new List<RoutePlan>();
        ViewBag.ScheduledRoutes = new List<RoutePlan>();
        ViewBag.Trucks = new List<Truck>();
        ViewBag.Collectors = new List<User>();
      }
    }

    private bool ScheduleExists(int id)
    {
      return _context.Schedules.Any(e => e.Id == id);
    }

    // GET: Schedule/Details/5
    public async Task<IActionResult> Details(int? id)
    {
      if (id == null)
      {
        return NotFound();
      }

      var schedule = await _context.Schedules
          .Include(s => s.Collector)
          .Include(s => s.Truck)
          .Include(s => s.Route)
          .Include(s => s.CollectionPoints)
          .ThenInclude(cp => cp.Bin)
          .ThenInclude(b => b.Client)
          .FirstOrDefaultAsync(m => m.Id == id);

      if (schedule == null)
      {
        return NotFound();
      }

      return View(schedule);
    }

    // AJAX method to get route bins
    [HttpGet]
    public async Task<IActionResult> GetRouteBins(Guid routeId)
    {
      try
      {
        var routeBins = await _context.RouteBins
            .Include(rb => rb.Bin)
            .ThenInclude(b => b.Client)
            .Where(rb => rb.RouteId == routeId)
            .OrderBy(rb => rb.OrderInRoute)
            .ToListAsync();

        var result = routeBins.Select(rb => new {
          id = rb.Id,
          binId = rb.BinId,
          binPlateId = rb.Bin.BinPlateId,
          location = rb.Bin.Location,
          zone = rb.Bin.Zone,
          fillLevel = rb.Bin.FillLevel,
          clientName = rb.Bin.Client?.ClientName,
          latitude = rb.Bin.Latitude,
          longitude = rb.Bin.Longitude,
          orderInRoute = rb.OrderInRoute
        }).ToList();

        return Json(result);
      }
      catch (Exception ex)
      {
        return Json(new { error = ex.Message });
      }
    }

    // Add method to check if route is already scheduled
    [HttpGet]
    public async Task<IActionResult> CheckRouteAvailability(Guid routeId)
    {
      try
      {
        var existingSchedule = await _context.Schedules
            .Include(s => s.Route)
            .Include(s => s.Collector)
            .Where(s => s.RouteId == routeId && s.Status != "Completed" && s.Status != "Cancelled")
            .FirstOrDefaultAsync();

        if (existingSchedule != null)
        {
          return Json(new
          {
            isScheduled = true,
            scheduleId = existingSchedule.Id,
            routeName = existingSchedule.Route?.Name,
            collectorName = $"{existingSchedule.Collector?.FirstName} {existingSchedule.Collector?.LastName}",
            status = existingSchedule.Status,
            startTime = existingSchedule.ScheduleStartTime.ToString("dd/MM/yyyy HH:mm")
          });
        }

        return Json(new { isScheduled = false });
      }
      catch (Exception ex)
      {
        return Json(new { error = ex.Message });
      }
    }

    // NEW: Add bin to schedule
    [HttpPost]
    public async Task<IActionResult> AddBinToSchedule(int scheduleId, Guid binId)
    {
      try
      {
        var schedule = await _context.Schedules
            .Include(s => s.CollectionPoints)
            .FirstOrDefaultAsync(s => s.Id == scheduleId);

        if (schedule == null)
        {
          return Json(new { success = false, message = "Schedule not found" });
        }

        // Check if bin is already in schedule
        if (schedule.CollectionPoints.Any(cp => cp.BinId == binId))
        {
          return Json(new { success = false, message = "Bin is already in this schedule" });
        }

        // Get the bin and route bin info
        var routeBin = await _context.RouteBins
            .Include(rb => rb.Bin)
            .FirstOrDefaultAsync(rb => rb.BinId == binId && rb.RouteId == schedule.RouteId);

        if (routeBin == null)
        {
          return Json(new { success = false, message = "Bin not found in this route" });
        }

        // Create new collection point
        var collectionPoint = new CollectionPoint
        {
          Id = Guid.NewGuid(),
          ScheduleId = scheduleId,
          BinId = binId,
          OrderInSchedule = routeBin.OrderInRoute,
          IsCollected = false,
          CollectedAt = null,
          Latitude = routeBin.Bin.Latitude,
          Longitude = routeBin.Bin.Longitude
        };

        _context.CollectionPoints.Add(collectionPoint);

        // Recalculate route center coordinates
        var allRouteBins = await _context.RouteBins
            .Include(rb => rb.Bin)
            .Where(rb => rb.RouteId == schedule.RouteId)
            .ToListAsync();

        await RecalculateRouteCenterCoordinates(schedule, allRouteBins);

        await _context.SaveChangesAsync();

        return Json(new
        {
          success = true,
          message = "Bin added to schedule successfully",
          binPlateId = routeBin.Bin.BinPlateId,
          location = routeBin.Bin.Location
        });
      }
      catch (Exception ex)
      {
        return Json(new { success = false, message = "Error adding bin: " + ex.Message });
      }
    }

    // NEW: Remove bin from schedule
    [HttpPost]
    public async Task<IActionResult> RemoveBinFromSchedule(int scheduleId, Guid binId)
    {
      try
      {
        var collectionPoint = await _context.CollectionPoints
            .FirstOrDefaultAsync(cp => cp.ScheduleId == scheduleId && cp.BinId == binId);

        if (collectionPoint == null)
        {
          return Json(new { success = false, message = "Collection point not found" });
        }

        // Check if bin has been collected
        if (collectionPoint.IsCollected)
        {
          return Json(new { success = false, message = "Cannot remove bin that has already been collected" });
        }

        _context.CollectionPoints.Remove(collectionPoint);

        // Recalculate route center coordinates
        var schedule = await _context.Schedules.FindAsync(scheduleId);
        var allRouteBins = await _context.RouteBins
            .Include(rb => rb.Bin)
            .Where(rb => rb.RouteId == schedule.RouteId)
            .ToListAsync();

        await RecalculateRouteCenterCoordinates(schedule, allRouteBins);

        await _context.SaveChangesAsync();

        return Json(new { success = true, message = "Bin removed from schedule successfully" });
      }
      catch (Exception ex)
      {
        return Json(new { success = false, message = "Error removing bin: " + ex.Message });
      }
    }

    // Helper method to recalculate route center coordinates
    private async Task RecalculateRouteCenterCoordinates(Schedule schedule, List<RouteBins> routeBins)
    {
      // Get current collection points for this schedule
      var currentCollectionPoints = await _context.CollectionPoints
          .Where(cp => cp.ScheduleId == schedule.Id)
          .Select(cp => cp.BinId)
          .ToListAsync();

      // Filter route bins to only those in the schedule
      var scheduledBins = routeBins.Where(rb => currentCollectionPoints.Contains(rb.BinId)).ToList();

      if (scheduledBins.Any())
      {
        var validCoordinates = scheduledBins
            .Where(rb => rb.Bin.Latitude.HasValue && rb.Bin.Longitude.HasValue)
            .ToList();

        if (validCoordinates.Any())
        {
          schedule.RouteCenterLatitude = validCoordinates.Average(rb => rb.Bin.Latitude.Value);
          schedule.RouteCenterLongitude = validCoordinates.Average(rb => rb.Bin.Longitude.Value);
        }
        else
        {
          schedule.RouteCenterLatitude = null;
          schedule.RouteCenterLongitude = null;
        }
      }
      else
      {
        schedule.RouteCenterLatitude = null;
        schedule.RouteCenterLongitude = null;
      }

      schedule.UpdatedAt = DateTime.Now;
    }

    // Helper method to create collection points for bins
    private async Task CreateCollectionPointsForBins(int scheduleId, List<RouteBins> routeBins)
    {
      var collectionPoints = new List<CollectionPoint>();
      foreach (var routeBin in routeBins)
      {
        var collectionPoint = new CollectionPoint
        {
          Id = Guid.NewGuid(),
          ScheduleId = scheduleId,
          BinId = routeBin.BinId,
          OrderInSchedule = routeBin.OrderInRoute,
          IsCollected = false,
          CollectedAt = null,
          Latitude = routeBin.Bin.Latitude,
          Longitude = routeBin.Bin.Longitude
        };
        collectionPoints.Add(collectionPoint);
      }

      if (collectionPoints.Any())
      {
        _context.CollectionPoints.AddRange(collectionPoints);
      }
    }
  }
}
