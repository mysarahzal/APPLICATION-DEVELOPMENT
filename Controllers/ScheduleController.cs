using AspnetCoreMvcFull.Data;
using AspnetCoreMvcFull.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace AspnetCoreMvcFull.Controllers
{
  public class ScheduleController : Controller
  {
    private readonly KUTIPDbContext _context;

    public ScheduleController(KUTIPDbContext context)
    {
      _context = context;
    }

    // UC08: Manage Pickup Schedule - Admin CRUD
    public async Task<IActionResult> Index()
    {
      var schedules = await _context.Schedules
          .Include(s => s.Collector)
          .Include(s => s.Route)
          .ToListAsync();
      return View(schedules);
    }

    // UC08: Create
    public IActionResult Create()
    {
      ViewBag.Collectors = _context.Users.Where(u => u.Role == "Driver").ToList();
      ViewBag.Routes = _context.Roads.ToList();
      return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Schedule schedule)
    {
      if (ModelState.IsValid)
      {
        schedule.CreatedAt = DateTime.Now;
        schedule.UpdatedAt = DateTime.Now;
        _context.Add(schedule);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
      }
      ViewBag.Collectors = _context.Users.Where(u => u.Role == "Driver").ToList();
      ViewBag.Routes = _context.Roads.ToList();
      return View(schedule);
    }

    // UC08: Edit
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
      ViewBag.Collectors = _context.Users.Where(u => u.Role == "Driver").ToList();
      ViewBag.Routes = _context.Roads.ToList();
      return View(schedule);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Schedule schedule)
    {
      if (id != schedule.Id)
      {
        return NotFound();
      }

      if (ModelState.IsValid)
      {
        try
        {
          schedule.UpdatedAt = DateTime.Now;
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
      ViewBag.Collectors = _context.Users.Where(u => u.Role == "Driver").ToList();
      ViewBag.Routes = _context.Roads.ToList();
      return View(schedule);
    }

    // UC08: Delete
    public async Task<IActionResult> Delete(int? id)
    {
      if (id == null)
      {
        return NotFound();
      }

      var schedule = await _context.Schedules
          .Include(s => s.Collector)
          .Include(s => s.Route)
          .FirstOrDefaultAsync(m => m.Id == id);
      if (schedule == null)
      {
        return NotFound();
      }

      return View(schedule);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
      var schedule = await _context.Schedules.FindAsync(id);
      _context.Schedules.Remove(schedule);
      await _context.SaveChangesAsync();
      return RedirectToAction(nameof(Index));
    }

    // UC09: Driver Views Schedule
    public async Task<IActionResult> DriverSchedule(int driverId)
    {
      var driverSchedules = await _context.Schedules
          .Include(s => s.Route)
          .Include(s => s.CollectionPoints)
          .Where(s => s.CollectorId == driverId)
          .OrderBy(s => s.ScheduleStartTime)
          .ToListAsync();

      return View(driverSchedules);
    }

    // UC10: Admin Views All Job Schedules
    public async Task<IActionResult> AdminScheduleOverview()
    {
      var allSchedules = await _context.Schedules
          .Include(s => s.Collector)
          .Include(s => s.Route)
          .Include(s => s.CollectionPoints)
          .OrderBy(s => s.ScheduleStartTime)
          .ToListAsync();

      return View(allSchedules);
    }

    private bool ScheduleExists(int id)
    {
      return _context.Schedules.Any(e => e.Id == id);
    }
  }
}
