using Microsoft.AspNetCore.Mvc;
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.Data;
using Microsoft.EntityFrameworkCore;
using System;

namespace AspnetCoreMvcFull.Controllers
{
  public class MissedPickupController : Controller
  {
    private readonly KUTIPDbContext _context;

    public MissedPickupController(KUTIPDbContext context)
    {
      _context = context;
    }

    public async Task<IActionResult> Index()
    {
      var missed = await _context.MissedPickups
          .Include(m => m.Schedule)
          .Where(m => m.Status == "Pending")
          .ToListAsync();

      return View(missed);
    }

    public async Task<IActionResult> Resolve(int? id)
    {
      if (id == null) return NotFound();
      var missed = await _context.MissedPickups.FindAsync(id);
      if (missed == null) return NotFound();

      return View(missed);
    }

    [HttpPost]
    public async Task<IActionResult> Resolve(int id, string reason, string resolution)
    {
      var missed = await _context.MissedPickups.FindAsync(id);
      if (missed == null) return NotFound();

      missed.Reason = reason;
      missed.Resolution = resolution;
      missed.Status = "Resolved";

      var alert = await _context.Alerts.FirstOrDefaultAsync(a => a.Type == "missed_pickup" && a.SourceId == id);
      if (alert != null)
      {
        alert.Status = "Acknowledged";
        alert.AcknowledgeAt = DateTime.Now;
      }

      await _context.SaveChangesAsync();
      return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Detect()
    {
      var scheduled = await _context.Schedules
          .Where(s => s.ScheduleEndTime <= DateTime.Now && s.Status != "Completed")
          .ToListAsync();

      foreach (var s in scheduled)
      {
        var reportExists = _context.PickupReports.Any(p => p.ScheduleId == s.Id);
        if (!reportExists)
        {
          bool alreadyDetected = _context.MissedPickups.Any(m => m.ScheduleId == s.Id);
          if (!alreadyDetected)
          {
            var missed = new MissedPickup
            {
              ScheduleId = s.Id,
              DetectedAt = DateTime.Now,
              Status = "Pending",
              CreatedAt = DateTime.Now
            };
            _context.MissedPickups.Add(missed);
            await _context.SaveChangesAsync();

            var alert = new Alert
            {
              Type = "missed_pickup",
              SourceId = missed.Id,
              Message = $"Missed pickup for schedule #{s.Id}",
              TriggeredAt = DateTime.Now,
              Severity = "Medium",
              Status = "Unread",
              CreatedAt = DateTime.Now
            };
            _context.Alerts.Add(alert);
            await _context.SaveChangesAsync();
          }
        }
      }

      return RedirectToAction(nameof(Index));
    }
  }
}
