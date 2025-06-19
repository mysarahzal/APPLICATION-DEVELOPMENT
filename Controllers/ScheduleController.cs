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
      // Get all users with Collector or Driver role
      ViewBag.Collectors = _context.Users
          .Where(u => u.Role == "Collector" || u.Role == "Driver")
          .ToList();

      // Predefined routes - these could come from a database or be hardcoded
      ViewBag.Routes = new List<Road>
    {
        new Road { Id = 1, Name = "Route A - Jalan Perkasa" },
        new Road { Id = 2, Name = "Route B - Jalan Pahlawan" },
        new Road { Id = 3, Name = "Route C - Jalan Bentara" },
        new Road { Id = 4, Name = "Route D - Jalan HuluBalang" },
        new Road { Id = 5, Name = "Route E - Family Mart Caltex " }
    };

      return View(new Schedule { Status = "Scheduled" });
    }

    // POST: Schedule/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("CollectorId,RoadId,ScheduleStartTime,ScheduleEndTime,Status")] Schedule schedule)
    {
      if (ModelState.IsValid)
      {
        schedule.Id = 0; // Ensure ID is set to 0 for new record
        _context.Add(schedule);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
      }

      ViewBag.Collectors = await _context.Users
          .Where(u => u.Role == "Collector")
          .ToListAsync();

      ViewBag.Routes = await _context.Roads.ToListAsync();

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
