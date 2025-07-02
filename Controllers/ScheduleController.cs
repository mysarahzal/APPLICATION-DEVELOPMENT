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
using System.Security.Claims;

namespace AspnetCoreMvcFull.Controllers
{
  public class ScheduleController : Controller
  {
    private readonly KUTIPDbContext _context;

    public ScheduleController(KUTIPDbContext context)
    {
      _context = context;
    }

    // GET: Schedule/MySchedule - For drivers to view their schedules
    [Authorize(Roles = "Driver")]
    public async Task<IActionResult> MySchedule()
    {
      try
      {
        // Try multiple ways to get current user ID
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                     User.FindFirst("sub")?.Value ??
                     User.FindFirst("id")?.Value ??
                     User.Identity?.Name;

        Console.WriteLine($"User claims debug:");
        foreach (var claim in User.Claims)
        {
          Console.WriteLine($"  {claim.Type}: {claim.Value}");
        }

        int currentUserId = 0;

        // Try to parse the user ID
        if (!string.IsNullOrEmpty(userIdClaim))
        {
          if (!int.TryParse(userIdClaim, out currentUserId))
          {
            // If parsing fails, try to find user by email or username
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? User.Identity?.Name;
            if (!string.IsNullOrEmpty(userEmail))
            {
              var userByEmail = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
              if (userByEmail != null)
              {
                currentUserId = userByEmail.Id;
                Console.WriteLine($"Found user by email: {userEmail}, ID: {currentUserId}");
              }
            }
          }
          else
          {
            Console.WriteLine($"Successfully parsed user ID: {currentUserId}");
          }
        }

        if (currentUserId == 0)
        {
          Console.WriteLine("Failed to identify user, trying alternative approach...");
          // Last resort - get user by identity name
          var identityName = User.Identity?.Name;
          if (!string.IsNullOrEmpty(identityName))
          {
            var userByName = await _context.Users.FirstOrDefaultAsync(u => u.Email == identityName || u.FirstName + " " + u.LastName == identityName);
            if (userByName != null)
            {
              currentUserId = userByName.Id;
              Console.WriteLine($"Found user by identity name: {identityName}, ID: {currentUserId}");
            }
          }
        }

        if (currentUserId == 0)
        {
          ViewBag.ErrorMessage = $"Unable to identify current user. Identity: {User.Identity?.Name}, Claims: {User.Claims.Count()}";
          return View(new List<Schedule>());
        }

        // Get current user details
        var currentUser = await _context.Users.FindAsync(currentUserId);
        if (currentUser == null)
        {
          ViewBag.ErrorMessage = "User account not found. Please contact your administrator.";
          return View(new List<Schedule>());
        }

        if (currentUser.Role != "Driver")
        {
          ViewBag.AccessDenied = "Access denied. Driver role required to view this page.";
          return View(new List<Schedule>());
        }

        Console.WriteLine($"Loading schedules for driver: {currentUser.FirstName} {currentUser.LastName} (ID: {currentUserId})");

        // Get ALL schedules with full details (same as admin view)
        var allSchedules = await _context.Schedules
            .Include(s => s.Collector)
            .Include(s => s.Truck)
            .ThenInclude(t => t.Driver)
            .Include(s => s.Route)
            .Include(s => s.CollectionPoints)
            .ThenInclude(cp => cp.Bin)
            .ThenInclude(b => b.Client)
            .OrderByDescending(s => s.ScheduleStartTime)
            .ToListAsync();

        Console.WriteLine($"Total schedules found: {allSchedules.Count}");

        // Get all trucks for filtering
        var allTrucks = await _context.Trucks
            .Include(t => t.Driver)
            .OrderBy(t => t.LicensePlate)
            .ToListAsync();

        Console.WriteLine($"Total trucks found: {allTrucks.Count}");

        // Get all collectors for filtering (both Collector and Driver roles)
        var allCollectors = await _context.Users
            .Where(u => u.Role == "Collector" || u.Role == "Driver")
            .OrderBy(u => u.FirstName)
            .ToListAsync();

        Console.WriteLine($"Total collectors found: {allCollectors.Count}");

        // Pass data to view
        ViewBag.CurrentUser = currentUser;
        ViewBag.AllTrucks = allTrucks;
        ViewBag.AllCollectors = allCollectors;

        return View(allSchedules);
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Error in MySchedule: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
        ViewBag.ErrorMessage = $"An error occurred while loading schedules: {ex.Message}";
        return View(new List<Schedule>());
      }
    }

    // GET: Schedule/MyScheduleDetails/5 - For drivers to view specific schedule details
    [Authorize(Roles = "Driver")]
    public async Task<IActionResult> MyScheduleDetails(int? id)
    {
      if (id == null)
      {
        TempData["Error"] = "Schedule ID is required.";
        return RedirectToAction("MySchedule");
      }

      try
      {
        // Get current user ID
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out int currentUserId))
        {
          TempData["Error"] = "Unable to identify current user. Please log in again.";
          return RedirectToAction("MySchedule");
        }

        var schedule = await _context.Schedules
            .Include(s => s.Collector)
            .Include(s => s.Truck)
            .Include(s => s.Route)
            .Include(s => s.CollectionPoints)
            .ThenInclude(cp => cp.Bin)
            .ThenInclude(b => b.Client)
            .FirstOrDefaultAsync(m => m.Id == id && m.CollectorId == currentUserId);

        if (schedule == null)
        {
          TempData["Error"] = "Schedule not found or you don't have permission to view it.";
          return RedirectToAction("MySchedule");
        }

        return View(schedule);
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Error in MyScheduleDetails: {ex.Message}");
        TempData["Error"] = "An error occurred while loading the schedule details.";
        return RedirectToAction("MySchedule");
      }
    }

    // POST: Schedule/UpdateStatus - For drivers to update schedule status
    [HttpPost]
    [Authorize(Roles = "Driver")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int scheduleId, string newStatus)
    {
      try
      {
        // Get current user ID
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out int currentUserId))
        {
          return Json(new { success = false, message = "Unable to identify current user." });
        }

        var schedule = await _context.Schedules
            .FirstOrDefaultAsync(s => s.Id == scheduleId && s.CollectorId == currentUserId);

        if (schedule == null)
        {
          return Json(new { success = false, message = "Schedule not found or access denied." });
        }

        // Update status and actual times
        schedule.Status = newStatus;
        schedule.UpdatedAt = DateTime.Now;

        if (newStatus == "In Progress" && !schedule.ActualStartTime.HasValue)
        {
          schedule.ActualStartTime = DateTime.Now;
        }
        else if (newStatus == "Completed" && !schedule.ActualEndTime.HasValue)
        {
          schedule.ActualEndTime = DateTime.Now;
          if (!schedule.ActualStartTime.HasValue)
          {
            schedule.ActualStartTime = schedule.ScheduleStartTime;
          }
        }

        await _context.SaveChangesAsync();

        return Json(new
        {
          success = true,
          message = $"Schedule status updated to {newStatus}",
          actualStartTime = schedule.ActualStartTime?.ToString("dd/MM/yyyy HH:mm"),
          actualEndTime = schedule.ActualEndTime?.ToString("dd/MM/yyyy HH:mm")
        });
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Error updating schedule status: {ex.Message}");
        return Json(new { success = false, message = "An error occurred while updating the schedule." });
      }
    }

    // GET: Schedule
    [Authorize(Roles = "Admin")]
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
    [Authorize(Roles = "Admin")]
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
    [Authorize(Roles = "Admin")]
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
        // Get all collectors (users with role "Collector" only)
        var collectors = await _context.Users
            .Where(u => u.Role == "Collector")
            .OrderBy(u => u.FirstName)
            .ToListAsync();

        ViewBag.Collectors = collectors;

        // Get all available trucks
        var availableTrucks = await _context.Trucks
            .Include(t => t.Driver)
            .Where(t => t.Status == "Available")
            .OrderBy(t => t.LicensePlate)
            .ToListAsync();

        ViewBag.AvailableTrucks = availableTrucks;

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
        Console.WriteLine($"Collectors: {collectors.Count}");
        Console.WriteLine($"Available trucks: {availableTrucks.Count}");
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Error loading dropdown data: {ex.Message}");
        ViewBag.ErrorMessage = "Error loading form data: " + ex.Message;

        // Set empty lists to prevent null reference errors
        ViewBag.Routes = new List<RoutePlan>();
        ViewBag.ScheduledRoutes = new List<RoutePlan>();
        ViewBag.Collectors = new List<User>();
        ViewBag.AvailableTrucks = new List<Truck>();
      }
    }

    // POST: Schedule/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([Bind("CollectorId,TruckId,RouteId,ScheduleStartTime,ScheduleEndTime,Status,AdminNotes")] Schedule schedule)
    {
      // Log the incoming data
      Console.WriteLine($"=== SCHEDULE CREATE DEBUG ===");
      Console.WriteLine($"CollectorId: {schedule.CollectorId}");
      Console.WriteLine($"TruckId: {schedule.TruckId}");
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
      ModelState.Remove("DayOfWeek");
      ModelState.Remove("MissedPickups");
      ModelState.Remove("AdminNotes");

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

          // Validate that the selected entities exist
          var collectorExists = await _context.Users.AnyAsync(u => u.Id == schedule.CollectorId && u.Role == "Collector");
          var truckExists = await _context.Trucks.AnyAsync(t => t.Id == schedule.TruckId && t.Status == "Available");
          var routeExists = await _context.RoutePlans.AnyAsync(r => r.Id == schedule.RouteId);

          Console.WriteLine($"Collector exists: {collectorExists}");
          Console.WriteLine($"Truck exists: {truckExists}");
          Console.WriteLine($"Route exists: {routeExists}");

          if (!collectorExists)
          {
            Console.WriteLine("ERROR: Collector does not exist");
            ModelState.AddModelError("CollectorId", "Selected collector does not exist or is not a valid collector.");
          }
          if (!truckExists)
          {
            Console.WriteLine("ERROR: Truck does not exist or is not available");
            ModelState.AddModelError("TruckId", "Selected truck does not exist or is not available.");
          }
          if (!routeExists)
          {
            Console.WriteLine("ERROR: Route does not exist");
            ModelState.AddModelError("RouteId", "Selected route does not exist.");
          }

          // Check if truck is already assigned to another active schedule
          var truckInUse = await _context.Schedules
              .AnyAsync(s => s.TruckId == schedule.TruckId &&
                       s.Status != "Completed" && s.Status != "Cancelled" &&
                       s.ScheduleStartTime.Date == schedule.ScheduleStartTime.Date);

          if (truckInUse)
          {
            Console.WriteLine("ERROR: Truck is already scheduled");
            ModelState.AddModelError("TruckId", "This truck is already scheduled for another route on the same date.");
          }

          if (ModelState.IsValid)
          {
            Console.WriteLine("All validations passed, creating schedule...");

            // Get truck and collector details for success message
            var truck = await _context.Trucks.Include(t => t.Driver).FirstOrDefaultAsync(t => t.Id == schedule.TruckId);
            var collector = await _context.Users.FirstOrDefaultAsync(u => u.Id == schedule.CollectorId);

            // Set automatic fields
            schedule.CreatedAt = DateTime.Now;
            schedule.UpdatedAt = DateTime.Now;
            schedule.ActualStartTime = null;
            schedule.ActualEndTime = null;
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

            // Update truck status to "Assigned"
            if (truck != null)
            {
              truck.Status = "Assigned";
              truck.UpdatedAt = DateTime.Now;
              _context.Update(truck);
              await _context.SaveChangesAsync();
              Console.WriteLine($"Truck {truck.LicensePlate} status updated to Assigned");
            }

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

            TempData["Success"] = $"Schedule created successfully! Collector: {collector?.FirstName} {collector?.LastName}, Truck: {truck?.LicensePlate} ({truck?.Model}), Route with {collectionPoints.Count} collection points.";
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
    [Authorize(Roles = "Admin")]
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
    [Authorize(Roles = "Admin")]
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
      ModelState.Remove("AdminNotes"); // Remove AdminNotes validation since it's optional

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

          // Update truck status based on current schedules
          await UpdateTruckStatusBasedOnSchedules(schedule.TruckId);
          if (routeChanged && existingSchedule.TruckId != schedule.TruckId)
          {
            // Also update the old truck's status if truck was changed
            await UpdateTruckStatusBasedOnSchedules(existingSchedule.TruckId);
          }

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
    [Authorize(Roles = "Admin")]
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
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
      var schedule = await _context.Schedules
          .Include(s => s.CollectionPoints)
          .FirstOrDefaultAsync(s => s.Id == id);

      if (schedule != null)
      {
        var truckId = schedule.TruckId;

        // Remove collection points first
        _context.CollectionPoints.RemoveRange(schedule.CollectionPoints);
        _context.Schedules.Remove(schedule);
        await _context.SaveChangesAsync();

        // Update truck status after schedule deletion
        await UpdateTruckStatusBasedOnSchedules(truckId);

        TempData["Success"] = "Schedule deleted successfully!";
      }

      return RedirectToAction(nameof(Index));
    }

    // Add method to update truck status based on active schedules
    private async Task UpdateTruckStatusBasedOnSchedules(int truckId)
    {
      try
      {
        var truck = await _context.Trucks.FindAsync(truckId);
        if (truck == null) return;

        // Check if truck has any active schedules
        var hasActiveSchedules = await _context.Schedules
            .AnyAsync(s => s.TruckId == truckId &&
                     s.Status != "Completed" &&
                     s.Status != "Cancelled");

        // If no active schedules and truck is currently assigned, make it available
        if (!hasActiveSchedules && truck.Status == "Assigned")
        {
          truck.Status = "Available";
          truck.UpdatedAt = DateTime.Now;
          _context.Update(truck);
          await _context.SaveChangesAsync();
        }
        // If has active schedules and truck is available, make it assigned
        else if (hasActiveSchedules && truck.Status == "Available")
        {
          truck.Status = "Assigned";
          truck.UpdatedAt = DateTime.Now;
          _context.Update(truck);
          await _context.SaveChangesAsync();
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Error updating truck status: {ex.Message}");
      }
    }

    private bool ScheduleExists(int id)
    {
      return _context.Schedules.Any(e => e.Id == id);
    }

    // AJAX method to get route bins - ADDED
    [Authorize(Roles = "Admin,Driver")]
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
    [Authorize(Roles = "Admin,Driver")]
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
    [Authorize(Roles = "Admin,Driver")]
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
