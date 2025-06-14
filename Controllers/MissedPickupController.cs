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

    // GET: MissedPickup/Index
    public async Task<IActionResult> Index()
    {
      var missed = await _context.MissedPickups
          .Include(m => m.Schedule)  // Include Schedule to avoid lazy loading errors
          .Where(m => m.Status == "Pending")
          .ToListAsync();

      return View(missed);
    }

    // GET: MissedPickup/Resolve/5
    public async Task<IActionResult> Resolve(int? id)
    {
      if (id == null) return NotFound();

      var missed = await _context.MissedPickups.FindAsync(id);
      if (missed == null) return NotFound();

      return View(missed);
    }

    // POST: MissedPickup/Resolve/5
    [HttpPost]
    public async Task<IActionResult> Resolve(int id, string reason, string resolution)
    {
      var missed = await _context.MissedPickups.FindAsync(id);
      if (missed == null) return NotFound();

      missed.Reason = reason;
      missed.Resolution = resolution;
      missed.Status = "Resolved";

      // Acknowledge the related alert
      var alert = await _context.Alerts.FirstOrDefaultAsync(a => a.Type == "missed_pickup" && a.SourceId == id);
      if (alert != null)
      {
        alert.Status = "Acknowledged";
        alert.AcknowledgeAt = DateTime.Now;
      }

      await _context.SaveChangesAsync();
      return RedirectToAction(nameof(Index));
    }

    // Detect missed pickups based on schedules
    public async Task<IActionResult> Detect()
    {
      // Get schedules that are not completed but have ended
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
              ScheduleId = s.Id,  // Set ScheduleId (foreign key)
              Schedule = s,       // Set the actual Schedule object (navigation property)
              DetectedAt = DateTime.Now,
              Status = "Pending",
              CreatedAt = DateTime.Now
            };
            _context.MissedPickups.Add(missed);
            await _context.SaveChangesAsync();  // Save MissedPickup entity

            // Create an alert for the missed pickup
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
            await _context.SaveChangesAsync();  // Save Alert entity
          }
        }
      }

      return RedirectToAction(nameof(Index));
    }
  }
}

