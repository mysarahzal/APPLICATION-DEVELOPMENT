using Microsoft.AspNetCore.Mvc;
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.Data;
using Microsoft.EntityFrameworkCore;
using System;

namespace AspnetCoreMvcFull.Controllers
{
  public class ScheduleController : Controller
  {
    private readonly KUTIPDbContext _context;

    public ScheduleController(KUTIPDbContext context)
    {
      _context = context;
    }

    public async Task<IActionResult> Index()
    {
      var schedules = await _context.Schedules
          .Include(s => s.CollectorId)
          .Include(s => s.Route)
          .ToListAsync();

      return View("~/Views/Schedule/Index.cshtml", schedules);
    }

    public async Task<IActionResult> MySchedule()
    {
      var Id = 1; // Replace with actual user ID retrieval logic
      var MySchedules = await _context.Schedules
          .Where(s => s.CollectorId == Id)
          .Include(s => s.Route)
          .ToListAsync();

      return View("~/Views/Schedule/MySchedule.cshtml", MySchedule);
    }
    public IActionResult Create()
    {
      ViewBag.Collectors = _context.Users.ToList();
      ViewBag.Routes = _context.Roads.ToList();
      return View("~/Views/Tables/Create.cshtml");
    }

    [HttpPost]
    public async Task<IActionResult> Create(Schedule schedule)
    {
      if (ModelState.IsValid)
      {
        schedule.Status = "Scheduled";
        _context.Schedules.Add(schedule);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
      }

      ViewBag.Collectors = _context.Users.ToList();
      ViewBag.Routes = _context.Roads.ToList();
      return View("~/Views/Tables/Create.cshtml", schedule);
    }

    public async Task<IActionResult> Edit(int? id)
    {
      if (id == null) return NotFound();

      var schedule = await _context.Schedules
          .FindAsync(id);

      if (schedule == null) return NotFound();

      ViewBag.Collectors = _context.Users.ToList();
      ViewBag.Routes = _context.Roads.ToList();

      return View("~/Views/Tables/Edit.cshtml", schedule);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Schedule updatedSchedule)
    {
      if (id != updatedSchedule.Id)
      {
        return NotFound();
      }

      if (ModelState.IsValid)
      {
        try
        {
          _context.Update(updatedSchedule);
          await _context.SaveChangesAsync();
          return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateConcurrencyException)
        {
          if (!ScheduleExists(updatedSchedule.Id))
          {
            return NotFound();
          }
          else
          {
            throw;
          }
        }
      }

      ViewBag.Collectors = _context.Users.ToList();
      ViewBag.Routes = _context.Roads.ToList();
      return View("~/Views/Tables/Edit.cshtml", updatedSchedule);
    }

    // UC08: Delete Pickup Schedule
    public async Task<IActionResult> Delete(int? id)
    {
      if (id == null) return NotFound();

      var schedule = await _context.Schedules
          .Include(s => s.CollectorId)
          .Include(s => s.Route)
          .FirstOrDefaultAsync(m => m.Id == id);

      if (schedule == null) return NotFound();

      return View("~/Views/Tables/Delete.cshtml", schedule);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
      var schedule = await _context.Schedules.FindAsync(id);
      if (schedule != null)
      {
        _context.Schedules.Remove(schedule);
        await _context.SaveChangesAsync();
      }

      return RedirectToAction(nameof(Index));
    }

    private bool ScheduleExists(int id)
    {
      return _context.Schedules.Any(e => e.Id == id);
    }
  }
  }

