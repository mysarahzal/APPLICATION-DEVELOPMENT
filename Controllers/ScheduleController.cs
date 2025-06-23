using System;
using System.Linq;
using System.Threading.Tasks;
using AspnetCoreMvcFull.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AspnetCoreMvcFull.Data;
using AspnetCoreMvcFull.Models;

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
          .Include(s => s.Road)
          .ToListAsync();

      return View(schedules);
    }


    // GET: Schedule/Create
    public IActionResult Create()
    {
      // Use consistent route data source (hardcoded in this case)
      var model = new Schedule { Status = "Scheduled" };
      PopulateViewBags(model);
      return View(model);
    }

    // POST: Schedule/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Schedule schedule)
    {
      try
      {
        if (ModelState.IsValid)
        {
          _context.Add(schedule);
          await _context.SaveChangesAsync();
          return RedirectToAction(nameof(Index));
        }

        // If we get here, validation failed
        PopulateViewBags(schedule);
        return View(schedule);
      }
      catch (Exception ex)
      {
        ModelState.AddModelError("", "An error occurred while saving: " + ex.Message);
        PopulateViewBags(schedule);
        return View(schedule);
      }
    }

    private void PopulateViewBags(Schedule model)
    {
      // Consistent route data source (hardcoded)
      ViewBag.Routes = new List<Road>
    {
        new Road { Id = 1, Name = "Route A - Jalan Perkasa" },
        new Road { Id = 2, Name = "Route B - Jalan Pahlawan" },
        new Road { Id = 3, Name = "Route C - Jalan Bentara" },
        new Road { Id = 4, Name = "Route D - Jalan HuluBalang" },
        new Road { Id = 5, Name = "Route E - Family Mart Caltex" }
    };

      // Get collectors from database
      ViewBag.Collectors = _context.Users
          .Where(u => u.Role == "Collector" || u.Role == "Driver")
          .ToList();

      // Preselect values in case of validation failure
      ViewBag.SelectedCollectorId = model.CollectorId;
      ViewBag.SelectedRoadId = model.RouteId;
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

      ViewBag.Collectors = await _context.Users
          .Where(u => u.Role == "Collector")
          .ToListAsync();

      ViewBag.Routes = await _context.Roads.ToListAsync();

      return View(schedule);
    }

    // POST: Schedule/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,CollectorId,RoadId,ScheduleStartTime,ScheduleEndTime,Status")] Schedule schedule)
    {
      if (id != schedule.Id)
      {
        return NotFound();
      }

      if (ModelState.IsValid)
      {
        try
        {
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

      ViewBag.Collectors = await _context.Users
          .Where(u => u.Role == "Collector")
          .ToListAsync();

      ViewBag.Routes = await _context.Roads.ToListAsync();

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
          .Include(s => s.Road)
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
      _context.Schedules.Remove(schedule);
      await _context.SaveChangesAsync();
      return RedirectToAction(nameof(Index));
    }

    private bool ScheduleExists(int id)
    {
      return _context.Schedules.Any(e => e.Id == id);
    }
  }
}
