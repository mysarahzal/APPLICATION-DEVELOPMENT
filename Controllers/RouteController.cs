using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.Data;

namespace AspnetCoreMvcFull.Controllers
{
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
      var routes = await _context.RoutePlan
          .Include(r => r.RouteBins)
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

      var route = await _context.RoutePlan
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
    public async Task<IActionResult> Create([Bind("Name,Description,ExpectedDurationMinutes")] AspnetCoreMvcFull.Models.RoutePlan route)
    {
      // Initialize RouteBins collection to avoid validation issues
      route.RouteBins = new List<RouteBins>();

      // Remove RouteBins from ModelState validation
      ModelState.Remove("RouteBins");

      // Debug: Print all validation errors FIRST
      if (!ModelState.IsValid)
      {
        foreach (var error in ModelState)
        {
          System.Diagnostics.Debug.WriteLine($"Field: {error.Key}");
          foreach (var err in error.Value.Errors)
          {
            System.Diagnostics.Debug.WriteLine($"  Error: {err.ErrorMessage}");
          }
        }
        return View(route); // Return early if validation fails
      }

      // Only execute this if ModelState IS valid
      route.Id = Guid.NewGuid();
      route.CreatedAt = DateTime.UtcNow;
      route.UpdatedAt = DateTime.UtcNow;

      _context.Add(route);
      await _context.SaveChangesAsync();
      return RedirectToAction(nameof(Index));
    }

    // GET: Route/Edit/5
    public async Task<IActionResult> Edit(Guid? id)
    {
      if (id == null)
      {
        return NotFound();
      }

      var route = await _context.RoutePlan.FindAsync(id);
      if (route == null)
      {
        return NotFound();
      }
      return View(route);
    }

    // POST: Route/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, [Bind("Id,Name,Description,ExpectedDurationMinutes,CreatedAt")] AspnetCoreMvcFull.Models.RoutePlan route)
    {
      if (id != route.Id)
      {
        return NotFound();
      }

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

      var route = await _context.RoutePlan
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
      var route = await _context.RoutePlan
          .Include(r => r.RouteBins)
          .FirstOrDefaultAsync(r => r.Id == id);

      if (route != null)
      {
        // Remove associated RouteBins first
        _context.RouteBins.RemoveRange(route.RouteBins);
        _context.RoutePlan.Remove(route);
        await _context.SaveChangesAsync();
      }

      return RedirectToAction(nameof(Index));
    }

    private bool RouteExists(Guid id)
    {
      return _context.RoutePlan.Any(e => e.Id == id);
    }

    // GET: Route/ManageBins/5
    public async Task<IActionResult> ManageBins(Guid? id)
    {
      if (id == null)
      {
        return NotFound();
      }

      var route = await _context.RoutePlan
          .Include(r => r.RouteBins)
          .ThenInclude(rb => rb.Bin)
          .ThenInclude(b => b.Client)
          .FirstOrDefaultAsync(r => r.Id == id);

      if (route == null)
      {
        return NotFound();
      }

      // Get available bins (not already in this route)
      var assignedBinIds = route.RouteBins.Select(rb => rb.BinId).ToList();
      var availableBins = await _context.Bins
          .Include(b => b.Client)
          .Where(b => !assignedBinIds.Contains(b.Id))
          .OrderBy(b => b.BinPlateId)
          .ToListAsync();

      ViewBag.AvailableBins = availableBins;
      return View(route);
    }

    // POST: Route/UpdateBinOrder - Simplified without request models
    [HttpPost]
    public async Task<IActionResult> UpdateBinOrder(Guid routeId, string updates)
    {
      try
      {
        // Parse the JSON string manually or use System.Text.Json
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
        return Json(new { success = true });
      }
      catch (Exception ex)
      {
        return Json(new { success = false, message = ex.Message });
      }
    }

    // POST: Route/AddBinToRoute - Simplified
    [HttpPost]
    public async Task<IActionResult> AddBinToRoute(Guid routeId, Guid binId)
    {
      try
      {
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

        _context.RouteBins.Add(routeBin);
        await _context.SaveChangesAsync();
        return Json(new { success = true });
      }
      catch (Exception ex)
      {
        return Json(new { success = false, message = ex.Message });
      }
    }

    // POST: Route/RemoveBinFromRoute - Simplified
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

          // Reorder remaining bins
          await ReorderBinsInRoute(routeId);
        }
        return Json(new { success = true });
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
  }
}
