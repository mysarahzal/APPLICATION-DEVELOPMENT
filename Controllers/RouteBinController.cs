using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.Data;

namespace AspnetCoreMvcFull.Controllers
{
  public class RouteBinController : Controller
  {
    private readonly KUTIPDbContext _context;

    public RouteBinController(KUTIPDbContext context)
    {
      _context = context;
    }

    // GET: RouteBin
    public async Task<IActionResult> Index()
    {
      var routeBins = await _context.RouteBins
          .Include(rb => rb.RoutePlan)
          .Include(rb => rb.Bin)
          .ThenInclude(b => b.Client)
          .OrderBy(rb => rb.RoutePlan.Name)
          .ThenBy(rb => rb.OrderInRoute)
          .ToListAsync();
      return View(routeBins);
    }

    // GET: RouteBin/Details/5
    public async Task<IActionResult> Details(Guid? id)
    {
      if (id == null)
      {
        return NotFound();
      }

      var routeBin = await _context.RouteBins
          .Include(rb => rb.RoutePlan)
          .Include(rb => rb.Bin)
          .ThenInclude(b => b.Client)
          .FirstOrDefaultAsync(m => m.Id == id);

      if (routeBin == null)
      {
        return NotFound();
      }

      return View(routeBin);
    }

    // GET: RouteBin/Create
    public async Task<IActionResult> Create()
    {
      await PopulateDropdowns();
      return View();
    }

    // POST: RouteBin/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("RouteId,BinId,OrderInRoute")] RouteBins routeBin)
    {
      if (ModelState.IsValid)
      {
        var existingRouteBin = await _context.RouteBins
            .FirstOrDefaultAsync(rb => rb.RouteId == routeBin.RouteId && rb.BinId == routeBin.BinId);

        if (existingRouteBin != null)
        {
          ModelState.AddModelError("", "This bin is already assigned to this route.");
          await PopulateDropdowns(routeBin.RouteId, routeBin.BinId);
          return View(routeBin);
        }

        if (routeBin.OrderInRoute <= 0)
        {
          var maxOrder = await _context.RouteBins
              .Where(rb => rb.RouteId == routeBin.RouteId)
              .MaxAsync(rb => (int?)rb.OrderInRoute) ?? 0;
          routeBin.OrderInRoute = maxOrder + 1;
        }

        routeBin.Id = Guid.NewGuid();
        _context.Add(routeBin);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
      }

      await PopulateDropdowns(routeBin.RouteId, routeBin.BinId);
      return View(routeBin);
    }

    // GET: RouteBin/Edit/5
    public async Task<IActionResult> Edit(Guid? id)
    {
      if (id == null)
      {
        return NotFound();
      }

      var routeBin = await _context.RouteBins.FindAsync(id);
      if (routeBin == null)
      {
        return NotFound();
      }

      await PopulateDropdowns(routeBin.RouteId, routeBin.BinId);
      return View(routeBin);
    }

    // POST: RouteBin/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, [Bind("Id,RouteId,BinId,OrderInRoute")] RouteBins routeBin)
    {
      if (id != routeBin.Id)
      {
        return NotFound();
      }

      if (ModelState.IsValid)
      {
        try
        {
          var existingRouteBin = await _context.RouteBins
              .FirstOrDefaultAsync(rb => rb.RouteId == routeBin.RouteId &&
                                 rb.BinId == routeBin.BinId &&
                                 rb.Id != routeBin.Id);

          if (existingRouteBin != null)
          {
            ModelState.AddModelError("", "This bin is already assigned to this route.");
            await PopulateDropdowns(routeBin.RouteId, routeBin.BinId);
            return View(routeBin);
          }

          _context.Update(routeBin);
          await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
          if (!RouteBinExists(routeBin.Id))
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

      await PopulateDropdowns(routeBin.RouteId, routeBin.BinId);
      return View(routeBin);
    }

    // GET: RouteBin/Delete/5
    public async Task<IActionResult> Delete(Guid? id)
    {
      if (id == null)
      {
        return NotFound();
      }

      var routeBin = await _context.RouteBins
          .Include(rb => rb.RoutePlan)
          .Include(rb => rb.Bin)
          .ThenInclude(b => b.Client)
          .FirstOrDefaultAsync(m => m.Id == id);

      if (routeBin == null)
      {
        return NotFound();
      }

      return View(routeBin);
    }

    // POST: RouteBin/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
      var routeBin = await _context.RouteBins.FindAsync(id);
      if (routeBin != null)
      {
        _context.RouteBins.Remove(routeBin);
        await _context.SaveChangesAsync();

        await ReorderBinsInRoute(routeBin.RouteId);
      }

      return RedirectToAction(nameof(Index));
    }

    // GET: RouteBin/ByRoute/5
    public async Task<IActionResult> ByRoute(Guid routeId)
    {
      // FIXED: Changed from RoutePlan to RoutePlans
      var route = await _context.RoutePlans.FindAsync(routeId);
      if (route == null)
      {
        return NotFound();
      }

      var routeBins = await _context.RouteBins
          .Include(rb => rb.Bin)
          .ThenInclude(b => b.Client)
          .Where(rb => rb.RouteId == routeId)
          .OrderBy(rb => rb.OrderInRoute)
          .ToListAsync();

      ViewBag.RouteName = route.Name;
      ViewBag.RouteId = routeId;
      return View(routeBins);
    }

    private bool RouteBinExists(Guid id)
    {
      return _context.RouteBins.Any(e => e.Id == id);
    }

    private async Task PopulateDropdowns(Guid? selectedRouteId = null, Guid? selectedBinId = null)
    {
      // FIXED: Changed from RoutePlan to RoutePlans
      ViewBag.RouteId = new SelectList(
          await _context.RoutePlans.OrderBy(r => r.Name).ToListAsync(),
          "Id", "Name", selectedRouteId);

      ViewBag.BinId = new SelectList(
          await _context.Bins
              .Include(b => b.Client)
              .OrderBy(b => b.BinPlateId)
              .Select(b => new {
                b.Id,
                DisplayText = b.BinPlateId + " - " + b.Location + " (" + b.Client.ClientName + ")"
              })
              .ToListAsync(),
          "Id", "DisplayText", selectedBinId);
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
