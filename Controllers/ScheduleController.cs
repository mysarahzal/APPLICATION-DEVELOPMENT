using AspnetCoreMvcFull.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AspnetCoreMvcFull.Data;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AspnetCoreMvcFull.Controllers
{
  [Authorize(Roles = "Admin")]
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
          .ThenInclude(t => t.Driver)
          .Include(s => s.Route)
          .Include(s => s.CollectionPoints)
          .ThenInclude(cp => cp.Bin)
          .OrderByDescending(s => s.CreatedAt)
          .ToListAsync();
      return View(schedules);
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
          .ThenInclude(t => t.Driver)
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

    // Helper method to load all dropdown data - UPDATED VERSION
    private async Task LoadDropdownData()
    {
      try
      {
        // Get drivers who have trucks assigned to them and trucks are available
        var driversWithTrucks = await _context.Trucks
            .Include(t => t.Driver)
            .Where(t => t.Status == "Available" && t.Driver != null && t.Driver.Role == "Driver")
            .Select(t => new { Driver = t.Driver, Truck = t })
            .OrderBy(dt => dt.Driver.FirstName)
            .ToListAsync();

        ViewBag.DriversWithTrucks = driversWithTrucks;

        // Get routes from Route Management - include bin count for better display
        var allRoutes = await _context.RoutePlans
            .Include(r => r.RouteBins)
            .OrderBy(r => r.Name)
            .ToListAsync();

        // Get routes that don't have active schedules (not completed or cancelled)
        var activeScheduleRouteIds = await _context.Schedules
            .Where(s => s.Status != "Completed" && s.Status != "Cancelled")
            .Select(s => s.RouteId)
            .Distinct()
            .ToListAsync();

        // Separate available and scheduled routes
        var availableRoutes = allRoutes
            .Where(r => !activeScheduleRouteIds.Contains(r.Id))
            .ToList();

        var scheduledRoutes = allRoutes
            .Where(r => activeScheduleRouteIds.Contains(r.Id))
            .ToList();

        ViewBag.Routes = availableRoutes;
        ViewBag.ScheduledRoutes = scheduledRoutes;

        Console.WriteLine($"Total routes: {allRoutes.Count}");
        Console.WriteLine($"Available routes: {availableRoutes.Count}");
        Console.WriteLine($"Scheduled routes: {scheduledRoutes.Count}");
        Console.WriteLine($"Drivers with trucks: {driversWithTrucks.Count()}");
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Error loading dropdown data: {ex.Message}");
        ViewBag.ErrorMessage = "Error loading form data: " + ex.Message;

        // Set empty lists to prevent null reference errors
        ViewBag.Routes = new List<RoutePlan>();
        ViewBag.ScheduledRoutes = new List<RoutePlan>();
        ViewBag.DriversWithTrucks = new List<object>();
      }
    }

    // POST: Schedule/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("CollectorId,RouteId,ScheduleStartTime,ScheduleEndTime,Status,AdminNotes")] Schedule schedule)
    {
      // Log the incoming data
      Console.WriteLine($"=== SCHEDULE CREATE DEBUG ===");
      Console.WriteLine($"CollectorId: {schedule.CollectorId}");
      Console.WriteLine($"RouteId: {schedule.RouteId}");
      Console.WriteLine($"ScheduleStartTime: {schedule.ScheduleStartTime}");
      Console.WriteLine($"ScheduleEndTime: {schedule.ScheduleEndTime}");
      Console.WriteLine($"Status: {schedule.Status}");
      Console.WriteLine($"AdminNotes: {schedule.AdminNotes}");

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
      ModelState.Remove("TruckId"); // Remove TruckId validation since it's auto-assigned
      ModelState.Remove("DayOfWeek"); // Remove DayOfWeek validation since it's auto-generated
      ModelState.Remove("MissedPickups"); // Remove MissedPickups validation

      // Log ModelState errors
      if (!ModelState.IsValid)
      {
        Console.WriteLine("=== MODEL STATE ERRORS ===");
        foreach (var modelState in ModelState)
        {
          foreach (var error in modelState.Value.Errors)
          {
            Console.WriteLine($"Field: {modelState.Key}, Error: {error.ErrorMessage}");
          }
        }
      }

      if (ModelState.IsValid)
      {
        try
        {
          Console.WriteLine("ModelState is valid, proceeding with creation...");

          // Get the driver and their assigned truck
          var driverTruck = await _context.Trucks
              .Include(t => t.Driver)
              .FirstOrDefaultAsync(t => t.DriverId == schedule.CollectorId && t.Status == "Available");

          Console.WriteLine($"Driver truck found: {driverTruck != null}");
          if (driverTruck != null)
          {
            Console.WriteLine($"Driver: {driverTruck.Driver?.FirstName} {driverTruck.Driver?.LastName}");
            Console.WriteLine($"Truck: {driverTruck.LicensePlate} ({driverTruck.Model})");
          }

          if (driverTruck == null)
          {
            Console.WriteLine("ERROR: Driver truck not found");
            ModelState.AddModelError("CollectorId", "Selected driver does not have an available truck assigned.");
          }
          else
          {
            // Auto-assign the truck based on the selected driver
            schedule.TruckId = driverTruck.Id;
            Console.WriteLine($"Assigned TruckId: {schedule.TruckId}");
          }

          // Validate that the selected entities exist
          var collectorExists = await _context.Users.AnyAsync(u => u.Id == schedule.CollectorId && u.Role == "Driver");
          var routeExists = await _context.RoutePlans.AnyAsync(r => r.Id == schedule.RouteId);

          Console.WriteLine($"Collector exists: {collectorExists}");
          Console.WriteLine($"Route exists: {routeExists}");

          if (!collectorExists)
          {
            Console.WriteLine("ERROR: Collector does not exist");
            ModelState.AddModelError("CollectorId", "Selected driver does not exist.");
          }
          if (!routeExists)
          {
            Console.WriteLine("ERROR: Route does not exist");
            ModelState.AddModelError("RouteId", "Selected route does not exist.");
          }

          if (ModelState.IsValid)
          {
            Console.WriteLine("All validations passed, creating schedule...");

            // Set automatic fields
            schedule.CreatedAt = DateTime.Now;
            schedule.UpdatedAt = DateTime.Now;
            schedule.ActualStartTime = null;
            schedule.ActualEndTime = null;
            // Auto-generate DayOfWeek from ScheduleStartTime
            schedule.DayOfWeek = schedule.ScheduleStartTime.DayOfWeek.ToString();

            Console.WriteLine($"DayOfWeek set to: {schedule.DayOfWeek}");

            // Get bins from the selected route with their coordinates
            var routeBins = await _context.RouteBins
                .Include(rb => rb.Bin)
                .Where(rb => rb.RouteId == schedule.RouteId)
                .OrderBy(rb => rb.OrderInRoute)
                .ToListAsync();

            Console.WriteLine($"Route bins found: {routeBins.Count}");

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
                Console.WriteLine($"Route center: {schedule.RouteCenterLatitude}, {schedule.RouteCenterLongitude}");
              }
            }

            Console.WriteLine("Adding schedule to context...");
            _context.Add(schedule);
            await _context.SaveChangesAsync();
            Console.WriteLine($"Schedule saved with ID: {schedule.Id}");

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
              Console.WriteLine($"Adding {collectionPoints.Count} collection points...");
              _context.CollectionPoints.AddRange(collectionPoints);
              await _context.SaveChangesAsync();
              Console.WriteLine("Collection points saved successfully");
            }

            TempData["Success"] = $"Schedule created successfully! Driver: {driverTruck.Driver.FirstName} {driverTruck.Driver.LastName}, Truck: {driverTruck.LicensePlate}, Route with {collectionPoints.Count} collection points.";
            Console.WriteLine("=== SCHEDULE CREATION SUCCESSFUL ===");
            return RedirectToAction(nameof(Index));
          }
          else
          {
            Console.WriteLine("=== VALIDATION FAILED AFTER ENTITY CHECKS ===");
          }
        }
        catch (Exception ex)
        {
          Console.WriteLine($"=== EXCEPTION DURING SCHEDULE CREATION ===");
          Console.WriteLine($"Exception: {ex.Message}");
          Console.WriteLine($"Stack trace: {ex.StackTrace}");
          if (ex.InnerException != null)
          {
            Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
          }
          ModelState.AddModelError("", "Error creating schedule: " + ex.Message);
        }
      }
      else
      {
        Console.WriteLine("=== MODEL STATE IS INVALID ===");
      }

      Console.WriteLine("=== RELOADING DROPDOWN DATA AND RETURNING VIEW ===");
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
          .FirstOrDefaultAsync(s => s.Id == id);

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
      ModelState.Remove("DayOfWeek");
      ModelState.Remove("MissedPickups");

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
          existingSchedule.DayOfWeek = schedule.ScheduleStartTime.DayOfWeek.ToString();

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
            if (routeBins.Any())
            {
              var validCoordinates = routeBins
                  .Where(rb => rb.Bin.Latitude.HasValue && rb.Bin.Longitude.HasValue)
                  .ToList();

              if (validCoordinates.Any())
              {
                existingSchedule.RouteCenterLatitude = validCoordinates.Average(rb => rb.Bin.Latitude.Value);
                existingSchedule.RouteCenterLongitude = validCoordinates.Average(rb => rb.Bin.Longitude.Value);
              }
            }

            // Create new collection points
            var newCollectionPoints = new List<CollectionPoint>();
            foreach (var routeBin in routeBins)
            {
              var collectionPoint = new CollectionPoint
              {
                Id = Guid.NewGuid(),
                ScheduleId = existingSchedule.Id,
                BinId = routeBin.BinId,
                OrderInSchedule = routeBin.OrderInRoute,
                IsCollected = false,
                CollectedAt = null,
                Latitude = routeBin.Bin.Latitude,
                Longitude = routeBin.Bin.Longitude
              };
              newCollectionPoints.Add(collectionPoint);
            }

            if (newCollectionPoints.Any())
            {
              _context.CollectionPoints.AddRange(newCollectionPoints);
            }
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

    private bool ScheduleExists(int id)
    {
      return _context.Schedules.Any(e => e.Id == id);
    }

    // AJAX method to get route bins - ADDED
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

    // Add method to check if route is already scheduled - ADDED
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

    // AJAX method to get route information - UPDATED
    [HttpGet]
    public async Task<IActionResult> GetRouteInfo(Guid routeId)
    {
      try
      {
        var route = await _context.RoutePlans
            .Include(r => r.RouteBins)
            .ThenInclude(rb => rb.Bin)
            .FirstOrDefaultAsync(r => r.Id == routeId);

        if (route == null)
        {
          return Json(new { error = "Route not found" });
        }

        var result = new
        {
          id = route.Id,
          name = route.Name,
          description = route.Description,
          expectedDurationMinutes = route.ExpectedDurationMinutes,
          binCount = route.RouteBins.Count,
          createdAt = route.CreatedAt.ToString("dd/MM/yyyy"),
          updatedAt = route.UpdatedAt.ToString("dd/MM/yyyy")
        };

        return Json(result);
      }
      catch (Exception ex)
      {
        return Json(new { error = ex.Message });
      }
    }
  }
}
