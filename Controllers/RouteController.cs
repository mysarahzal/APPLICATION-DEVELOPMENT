using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.Data;
using Microsoft.AspNetCore.Authorization;

namespace AspnetCoreMvcFull.Controllers
{
  [Authorize(Roles = "Admin")]
  public class RouteController : Controller
  {
    private readonly KUTIPDbContext _context;

    public RouteController(KUTIPDbContext context)
    {
      _context = context;
    }

    // GET: Route
    public async Task<IActionResult> Index()
    {
      var routes = await _context.RoutePlans
          .Include(r => r.RouteBins)
          .ThenInclude(rb => rb.Bin) // Include Bin to check GPS coordinates
          .ToListAsync();
      return View(routes);
    }

    // GET: Route/Details/5
    public async Task<IActionResult> Details(Guid? id)
    {
      if (id == null)
      {
        return NotFound();
      }

      var route = await _context.RoutePlans
          .Include(r => r.RouteBins)
          .ThenInclude(rb => rb.Bin)
          .FirstOrDefaultAsync(m => m.Id == id);

      if (route == null)
      {
        return NotFound();
      }

      return View(route);
    }

    // GET: Route/Create
    public IActionResult Create()
    {
      return View();
    }

    // POST: Route/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,Description")] RoutePlan route)
    {
      route.RouteBins = new List<RouteBins>();
      ModelState.Remove("RouteBins");
      ModelState.Remove("CreatedAt");
      ModelState.Remove("UpdatedAt");
      ModelState.Remove("ExpectedDurationMinutes"); // Remove validation since it will be calculated

      if (ModelState.IsValid)
      {
        route.Id = Guid.NewGuid();
        route.CreatedAt = DateTime.UtcNow;
        route.UpdatedAt = DateTime.UtcNow;
        route.ExpectedDurationMinutes = 60; // Default value, will be updated when bins are added

        _context.Add(route);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Route created successfully! Add bins to automatically calculate duration.";
        return RedirectToAction("ManageBins", new { id = route.Id });
      }

      return View(route);
    }

    // GET: Route/Edit/5
    public async Task<IActionResult> Edit(Guid? id)
    {
      if (id == null)
      {
        return NotFound();
      }

      var route = await _context.RoutePlans.FindAsync(id);
      if (route == null)
      {
        return NotFound();
      }
      return View(route);
    }

    // POST: Route/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, [Bind("Id,Name,Description,ExpectedDurationMinutes,CreatedAt")] RoutePlan route)
    {
      if (id != route.Id)
      {
        return NotFound();
      }

      ModelState.Remove("UpdatedAt");
      ModelState.Remove("RouteBins");

      if (ModelState.IsValid)
      {
        try
        {
          route.UpdatedAt = DateTime.UtcNow;
          _context.Update(route);
          await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
          if (!RouteExists(route.Id))
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
      return View(route);
    }

    // GET: Route/Delete/5
    public async Task<IActionResult> Delete(Guid? id)
    {
      if (id == null)
      {
        return NotFound();
      }

      var route = await _context.RoutePlans
          .Include(r => r.RouteBins)
          .FirstOrDefaultAsync(m => m.Id == id);

      if (route == null)
      {
        return NotFound();
      }

      return View(route);
    }

    // POST: Route/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
      var route = await _context.RoutePlans
          .Include(r => r.RouteBins)
          .FirstOrDefaultAsync(r => r.Id == id);

      if (route != null)
      {
        _context.RouteBins.RemoveRange(route.RouteBins);
        _context.RoutePlans.Remove(route);
        await _context.SaveChangesAsync();
      }

      return RedirectToAction(nameof(Index));
    }

    private bool RouteExists(Guid id)
    {
      return _context.RoutePlans.Any(e => e.Id == id);
    }

    // GET: Route/ManageBins/5
    public async Task<IActionResult> ManageBins(Guid? id)
    {
      if (id == null)
      {
        return NotFound();
      }

      var route = await _context.RoutePlans
          .Include(r => r.RouteBins)
          .ThenInclude(rb => rb.Bin)
          .ThenInclude(b => b.Client)
          .FirstOrDefaultAsync(r => r.Id == id);

      if (route == null)
      {
        return NotFound();
      }

      var assignedBinIds = route.RouteBins.Select(rb => rb.BinId).ToList();
      var availableBins = await _context.Bins
          .Include(b => b.Client)
          .Where(b => !assignedBinIds.Contains(b.Id))
          .OrderBy(b => b.BinPlateId)
          .ToListAsync();

      ViewBag.AvailableBins = availableBins;
      return View(route);
    }

    // POST: Route/UpdateBinOrder
    [HttpPost]
    public async Task<IActionResult> UpdateBinOrder(Guid routeId, string updates)
    {
      try
      {
        var updateList = System.Text.Json.JsonSerializer.Deserialize<List<dynamic>>(updates);

        foreach (var update in updateList)
        {
          var updateDict = (System.Text.Json.JsonElement)update;
          var routeBinId = Guid.Parse(updateDict.GetProperty("Id").GetString());
          var orderInRoute = updateDict.GetProperty("OrderInRoute").GetInt32();

          var routeBin = await _context.RouteBins.FindAsync(routeBinId);
          if (routeBin != null)
          {
            routeBin.OrderInRoute = orderInRoute;
          }
        }
        await _context.SaveChangesAsync();

        // Recalculate duration after reordering
        await RecalculateRouteDuration(routeId);

        return Json(new { success = true });
      }
      catch (Exception ex)
      {
        return Json(new { success = false, message = ex.Message });
      }
    }

    // POST: Route/AddBinToRoute - ENHANCED with automatic duration calculation
    [HttpPost]
    public async Task<IActionResult> AddBinToRoute(Guid routeId, Guid binId)
    {
      try
      {
        // DEBUG: Check if route exists
        var routeExists = await _context.RoutePlans.AnyAsync(r => r.Id == routeId);
        if (!routeExists)
        {
          return Json(new { success = false, message = $"Route with ID {routeId} does not exist" });
        }

        // DEBUG: Check if bin exists
        var binExists = await _context.Bins.AnyAsync(b => b.Id == binId);
        if (!binExists)
        {
          return Json(new { success = false, message = $"Bin with ID {binId} does not exist" });
        }

        // Check if bin is already in route
        var existingRouteBin = await _context.RouteBins
            .FirstOrDefaultAsync(rb => rb.RouteId == routeId && rb.BinId == binId);

        if (existingRouteBin != null)
        {
          return Json(new { success = false, message = "Bin is already in this route" });
        }

        // Get next order number
        var maxOrder = await _context.RouteBins
            .Where(rb => rb.RouteId == routeId)
            .MaxAsync(rb => (int?)rb.OrderInRoute) ?? 0;

        var routeBin = new RouteBins
        {
          Id = Guid.NewGuid(),
          RouteId = routeId,
          BinId = binId,
          OrderInRoute = maxOrder + 1
        };

        // DEBUG: Log the values being inserted
        System.Diagnostics.Debug.WriteLine($"Inserting RouteBin: Id={routeBin.Id}, RouteId={routeBin.RouteId}, BinId={routeBin.BinId}, Order={routeBin.OrderInRoute}");

        _context.RouteBins.Add(routeBin);
        await _context.SaveChangesAsync();

        // Automatically recalculate route duration
        var newDuration = await RecalculateRouteDuration(routeId);

        return Json(new
        {
          success = true,
          newDuration = newDuration,
          message = $"Bin added successfully! Route duration updated to {newDuration} minutes."
        });
      }
      catch (Exception ex)
      {
        // Get the full error details including inner exception
        var fullError = ex.InnerException?.Message ?? ex.Message;
        System.Diagnostics.Debug.WriteLine($"AddBinToRoute Error: {fullError}");
        return Json(new { success = false, message = $"Database error: {fullError}" });
      }
    }

    // POST: Route/RemoveBinFromRoute
    [HttpPost]
    public async Task<IActionResult> RemoveBinFromRoute(Guid routeBinId)
    {
      try
      {
        var routeBin = await _context.RouteBins.FindAsync(routeBinId);
        if (routeBin != null)
        {
          var routeId = routeBin.RouteId;
          _context.RouteBins.Remove(routeBin);
          await _context.SaveChangesAsync();

          await ReorderBinsInRoute(routeId);

          // Recalculate duration after removing bin
          var newDuration = await RecalculateRouteDuration(routeId);

          return Json(new
          {
            success = true,
            newDuration = newDuration,
            message = $"Bin removed successfully! Route duration updated to {newDuration} minutes."
          });
        }
        return Json(new { success = false, message = "Route bin not found" });
      }
      catch (Exception ex)
      {
        return Json(new { success = false, message = ex.Message });
      }
    }

    private async Task ReorderBinsInRoute(Guid routeId)
    {
      var routeBins = await _context.RouteBins
          .Where(rb => rb.RouteId == routeId)
          .OrderBy(rb => rb.OrderInRoute)
          .ToListAsync();

      for (int i = 0; i < routeBins.Count; i++)
      {
        routeBins[i].OrderInRoute = i + 1;
      }

      await _context.SaveChangesAsync();
    }

    // ENHANCED: Method to calculate route duration based on bin coordinates
    private async Task<int> RecalculateRouteDuration(Guid routeId)
    {
      try
      {
        var route = await _context.RoutePlans
            .Include(r => r.RouteBins)
            .ThenInclude(rb => rb.Bin)
            .FirstOrDefaultAsync(r => r.Id == routeId);

        if (route == null || !route.RouteBins.Any())
        {
          System.Diagnostics.Debug.WriteLine($"Route {routeId}: No bins found, using default 60 minutes");
          return 60; // Default 1 hour for empty routes
        }

        var totalBins = route.RouteBins.Count;
        System.Diagnostics.Debug.WriteLine($"Route {routeId}: Processing {totalBins} bins");

        // Get bins with coordinates ordered by their position in route
        var binsWithCoords = route.RouteBins
            .Where(rb => rb.Bin != null && rb.Bin.Latitude.HasValue && rb.Bin.Longitude.HasValue)
            .OrderBy(rb => rb.OrderInRoute)
            .Select(rb => new {
              Lat = rb.Bin.Latitude.Value,
              Lng = rb.Bin.Longitude.Value,
              Order = rb.OrderInRoute,
              BinId = rb.Bin.BinPlateId
            })
            .ToList();

        System.Diagnostics.Debug.WriteLine($"Route {routeId}: {binsWithCoords.Count} bins have GPS coordinates");

        // Collection time: 5 minutes per bin (minimum)
        int collectionTimeMinutes = Math.Max(totalBins * 5, 15);

        int travelTimeMinutes = 0;
        string calculationMethod = "";

        if (binsWithCoords.Count >= 2)
        {
          // GPS-based calculation
          double totalDistanceKm = 0;

          for (int i = 0; i < binsWithCoords.Count - 1; i++)
          {
            var distance = CalculateHaversineDistance(
                binsWithCoords[i].Lat, binsWithCoords[i].Lng,
                binsWithCoords[i + 1].Lat, binsWithCoords[i + 1].Lng);
            totalDistanceKm += distance;

            System.Diagnostics.Debug.WriteLine($"Route {routeId}: Distance from bin {binsWithCoords[i].BinId} to {binsWithCoords[i + 1].BinId}: {distance:F2} km");
          }

          // Estimate travel time (assuming average speed of 20 km/h in urban areas with stops)
          travelTimeMinutes = (int)Math.Ceiling(totalDistanceKm / 20.0 * 60);
          calculationMethod = $"GPS-based ({totalDistanceKm:F2}km)";

          System.Diagnostics.Debug.WriteLine($"Route {routeId}: Total distance: {totalDistanceKm:F2}km, Travel time: {travelTimeMinutes} minutes");
        }
        else if (binsWithCoords.Count == 1)
        {
          // Single bin with GPS - estimate based on typical urban route density
          travelTimeMinutes = Math.Max(totalBins * 3, 10); // 3 minutes travel per bin
          calculationMethod = "Single GPS point estimation";

          System.Diagnostics.Debug.WriteLine($"Route {routeId}: Single GPS point, estimated travel time: {travelTimeMinutes} minutes");
        }
        else
        {
          // No GPS coordinates - estimate based on bin count and typical urban density
          // Assume bins are spread across urban area with average 2km between bins
          double estimatedDistanceKm = Math.Max(totalBins * 1.5, 2.0); // 1.5km average between bins
          travelTimeMinutes = (int)Math.Ceiling(estimatedDistanceKm / 20.0 * 60);
          calculationMethod = "No GPS - density estimation";

          System.Diagnostics.Debug.WriteLine($"Route {routeId}: No GPS coordinates, estimated distance: {estimatedDistanceKm:F2}km, travel time: {travelTimeMinutes} minutes");
        }

        // Add buffer time (25% of total time, minimum 10 minutes)
        int bufferTimeMinutes = Math.Max((int)Math.Ceiling((travelTimeMinutes + collectionTimeMinutes) * 0.25), 10);

        int totalDuration = travelTimeMinutes + collectionTimeMinutes + bufferTimeMinutes;

        // Ensure reasonable minimum and maximum durations
        totalDuration = Math.Max(30, Math.Min(totalDuration, 480)); // 30 minutes to 8 hours

        // Update the route with calculated duration
        route.ExpectedDurationMinutes = totalDuration;
        route.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        System.Diagnostics.Debug.WriteLine($"Route {routeId} duration calculated: {totalDuration} minutes " +
            $"(Travel: {travelTimeMinutes}, Collection: {collectionTimeMinutes}, Buffer: {bufferTimeMinutes}) " +
            $"Method: {calculationMethod}");

        return totalDuration;
      }
      catch (Exception ex)
      {
        System.Diagnostics.Debug.WriteLine($"Error calculating route duration for {routeId}: {ex.Message}");
        return 60; // Default fallback
      }
    }

    // Haversine formula to calculate distance between two GPS coordinates
    private double CalculateHaversineDistance(decimal lat1, decimal lng1, decimal lat2, decimal lng2)
    {
      const double R = 6371; // Earth's radius in kilometers

      double dLat = DegreesToRadians((double)(lat2 - lat1));
      double dLng = DegreesToRadians((double)(lng2 - lng1));

      double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                 Math.Cos(DegreesToRadians((double)lat1)) * Math.Cos(DegreesToRadians((double)lat2)) *
                 Math.Sin(dLng / 2) * Math.Sin(dLng / 2);

      double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
      double distance = R * c;

      return distance;
    }

    private double DegreesToRadians(double degrees)
    {
      return degrees * (Math.PI / 180);
    }

    // GET: Route/RecalculateAllDurations - Admin utility
    [HttpGet]
    public async Task<IActionResult> RecalculateAllDurations()
    {
      try
      {
        var routes = await _context.RoutePlans
            .Include(r => r.RouteBins)
            .ThenInclude(rb => rb.Bin)
            .ToListAsync();

        int updatedCount = 0;
        foreach (var route in routes)
        {
          if (route.RouteBins.Any())
          {
            await RecalculateRouteDuration(route.Id);
            updatedCount++;
          }
        }

        TempData["Success"] = $"Successfully recalculated durations for {updatedCount} routes.";
        return RedirectToAction(nameof(Index));
      }
      catch (Exception ex)
      {
        TempData["Error"] = $"Error recalculating durations: {ex.Message}";
        return RedirectToAction(nameof(Index));
      }
    }

    // GET: Route/DebugGPS - Debug utility to check GPS data
    [HttpGet]
    public async Task<IActionResult> DebugGPS()
    {
      try
      {
        var routes = await _context.RoutePlans
            .Include(r => r.RouteBins)
            .ThenInclude(rb => rb.Bin)
            .ToListAsync();

        var debugInfo = new List<object>();

        foreach (var route in routes)
        {
          var bins = route.RouteBins.Select(rb => new {
            BinId = rb.Bin?.BinPlateId ?? "Unknown",
            HasGPS = rb.Bin?.Latitude.HasValue == true && rb.Bin?.Longitude.HasValue == true,
            Latitude = rb.Bin?.Latitude,
            Longitude = rb.Bin?.Longitude,
            Order = rb.OrderInRoute
          }).ToList();

          debugInfo.Add(new
          {
            RouteId = route.Id,
            RouteName = route.Name,
            TotalBins = route.RouteBins.Count,
            BinsWithGPS = bins.Count(b => b.HasGPS),
            Duration = route.ExpectedDurationMinutes,
            Bins = bins
          });
        }

        return Json(debugInfo);
      }
      catch (Exception ex)
      {
        return Json(new { error = ex.Message });
      }
    }
  }
}
