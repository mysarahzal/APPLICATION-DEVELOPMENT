using Microsoft.AspNetCore.Mvc;
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace AspnetCoreMvcFull.Controllers
{
  [Authorize(Roles = "Admin")]
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
      var missedPickups = await _context.MissedPickups
          .Include(m => m.Schedule)
              .ThenInclude(s => s.Route)
          .Include(m => m.Schedule)
              .ThenInclude(s => s.Collector)
          .Include(m => m.Schedule)
              .ThenInclude(s => s.Truck)
          .Include(m => m.Schedule)
              .ThenInclude(s => s.CollectionPoints)
          .OrderByDescending(m => m.DetectedAt)
          .ToListAsync();

      return View(missedPickups);
    }

    // GET: MissedPickup/Resolve/5
    public async Task<IActionResult> Resolve(int? id)
    {
      if (id == null) return NotFound();

      var missedPickup = await _context.MissedPickups
          .Include(m => m.Schedule)
              .ThenInclude(s => s.Route)
          .Include(m => m.Schedule)
              .ThenInclude(s => s.Collector)
          .FirstOrDefaultAsync(m => m.Id == id);

      if (missedPickup == null) return NotFound();

      return View(missedPickup);
    }

    // POST: MissedPickup/Resolve
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Resolve(MissedPickup model)
    {
      if (!ModelState.IsValid)
      {
        // Reload the schedule data for the view
        model = await _context.MissedPickups
            .Include(m => m.Schedule)
                .ThenInclude(s => s.Route)
            .Include(m => m.Schedule)
                .ThenInclude(s => s.Collector)
            .FirstOrDefaultAsync(m => m.Id == model.Id);
        return View(model);
      }

      var missedPickup = await _context.MissedPickups
          .Include(m => m.Schedule)
          .FirstOrDefaultAsync(m => m.Id == model.Id);

      if (missedPickup == null) return NotFound();

      // Update the missed pickup
      missedPickup.Reason = model.Reason;
      missedPickup.Resolution = model.Resolution;
      missedPickup.Status = "Resolved";
      missedPickup.ResolvedAt = DateTime.Now;
      missedPickup.ResolvedBy = User.Identity.Name; // Admin who resolved it

      // Update the related schedule status based on resolution
      if (missedPickup.Schedule != null)
      {
        // Mark schedule as completed when resolved
        missedPickup.Schedule.Status = "Completed";
        missedPickup.Schedule.UpdatedAt = DateTime.Now;

        // Set actual end time if not already set
        if (!missedPickup.Schedule.ActualEndTime.HasValue)
        {
          missedPickup.Schedule.ActualEndTime = DateTime.Now;
        }
      }

      await _context.SaveChangesAsync();

      TempData["SuccessMessage"] = "Missed pickup has been resolved successfully and schedule status updated.";
      return RedirectToAction(nameof(Index));
    }

    // Manual detection method (kept as backup)
    public async Task<IActionResult> Detect()
    {
      try
      {
        var detectedCount = 0;
        var updatedScheduleCount = 0;
        var currentTime = DateTime.Now;

        Console.WriteLine($"=== MANUAL MISSED PICKUP DETECTION STARTED ===");
        Console.WriteLine($"Current time: {currentTime}");

        // Get schedules that should have been completed but haven't been marked as such
        // Since ScheduleEndTime is DateTime, we compare directly
        var potentialMissedSchedules = await _context.Schedules
            .Include(s => s.CollectionPoints)
                .ThenInclude(cp => cp.CollectionRecords)
            .Include(s => s.Route)
            .Include(s => s.Collector)
            .Where(s => s.ScheduleEndTime <= currentTime &&
                       s.Status != "Completed" &&
                       s.Status != "Cancelled" &&
                       s.Status != "Missed")
            .ToListAsync();

        Console.WriteLine($"Found {potentialMissedSchedules.Count} potential missed schedules");

        foreach (var schedule in potentialMissedSchedules)
        {
          Console.WriteLine($"Checking Schedule #{schedule.Id} - End time: {schedule.ScheduleEndTime}, Status: {schedule.Status}");

          // Check if this schedule already has a missed pickup record
          var existingMissedPickup = await _context.MissedPickups
              .FirstOrDefaultAsync(m => m.ScheduleId == schedule.Id);

          if (existingMissedPickup != null)
          {
            Console.WriteLine($"Schedule #{schedule.Id} already has missed pickup record");
            continue; // Skip if already detected
          }

          // Check if any collection points in this schedule are uncollected
          var uncollectedPoints = schedule.CollectionPoints
              .Where(cp => !cp.IsCollected && !cp.CollectionRecords.Any())
              .ToList();

          var totalPoints = schedule.CollectionPoints.Count();
          var collectedPoints = schedule.CollectionPoints.Count(cp => cp.IsCollected);

          Console.WriteLine($"Schedule #{schedule.Id}: {collectedPoints}/{totalPoints} points collected, {uncollectedPoints.Count} uncollected");

          if (uncollectedPoints.Any())
          {
            // UPDATE SCHEDULE STATUS TO "MISSED"
            schedule.Status = "Missed";
            schedule.UpdatedAt = currentTime;

            // Set actual end time to the scheduled end time since it wasn't completed
            if (!schedule.ActualEndTime.HasValue)
            {
              schedule.ActualEndTime = schedule.ScheduleEndTime;
            }

            Console.WriteLine($"Marking Schedule #{schedule.Id} as MISSED");

            // Create missed pickup record
            var missedPickup = new MissedPickup
            {
              ScheduleId = schedule.Id,
              DetectedAt = currentTime,
              Status = "Pending",
              Reason = $"MANUAL CHECK: {uncollectedPoints.Count} uncollected point(s) out of {totalPoints} total points after scheduled end time ({schedule.ScheduleEndTime:g}). Route: {schedule.Route?.Name ?? "Unknown"}",
              CreatedAt = currentTime
            };

            _context.MissedPickups.Add(missedPickup);
            detectedCount++;
            updatedScheduleCount++;

            Console.WriteLine($"Created missed pickup record for Schedule #{schedule.Id}");
          }
          else if (schedule.CollectionPoints.All(cp => cp.IsCollected))
          {
            // All points collected, mark schedule as completed
            schedule.Status = "Completed";
            schedule.UpdatedAt = currentTime;

            // Get the latest collection time
            var collectedTimes = schedule.CollectionPoints
                .Where(cp => cp.CollectedAt.HasValue)
                .Select(cp => cp.CollectedAt.Value);

            if (collectedTimes.Any())
            {
              schedule.ActualEndTime = collectedTimes.Max();
            }
            else
            {
              schedule.ActualEndTime = currentTime;
            }

            updatedScheduleCount++;
            Console.WriteLine($"Marking Schedule #{schedule.Id} as COMPLETED (all points collected)");
          }
          else
          {
            // Some points collected but not all - this might be in progress
            // Check if it's significantly past the end time
            var hoursOverdue = (currentTime - schedule.ScheduleEndTime).TotalHours;

            if (hoursOverdue > 2) // More than 2 hours overdue
            {
              schedule.Status = "Missed";
              schedule.UpdatedAt = currentTime;

              if (!schedule.ActualEndTime.HasValue)
              {
                schedule.ActualEndTime = schedule.ScheduleEndTime;
              }

              // Create missed pickup record for partial completion
              var missedPickup = new MissedPickup
              {
                ScheduleId = schedule.Id,
                DetectedAt = currentTime,
                Status = "Pending",
                Reason = $"MANUAL CHECK: Schedule overdue by {hoursOverdue:F1} hours. {collectedPoints}/{totalPoints} points collected, {uncollectedPoints.Count} remaining uncollected. Route: {schedule.Route?.Name ?? "Unknown"}",
                CreatedAt = currentTime
              };

              _context.MissedPickups.Add(missedPickup);
              detectedCount++;
              updatedScheduleCount++;

              Console.WriteLine($"Marking Schedule #{schedule.Id} as MISSED (partially completed but overdue)");
            }
          }
        }

        await _context.SaveChangesAsync();

        Console.WriteLine($"=== MANUAL DETECTION COMPLETED ===");
        Console.WriteLine($"Missed pickups detected: {detectedCount}");
        Console.WriteLine($"Schedules updated: {updatedScheduleCount}");

        if (detectedCount > 0)
        {
          TempData["SuccessMessage"] = $"Manual detection completed. Found {detectedCount} new missed pickup(s) and updated {updatedScheduleCount} schedule(s).";
        }
        else if (updatedScheduleCount > 0)
        {
          TempData["InfoMessage"] = $"Manual detection completed. No new missed pickups found, but {updatedScheduleCount} schedule(s) were updated to completed status.";
        }
        else
        {
          TempData["InfoMessage"] = "Manual detection completed. No new missed pickups found and all schedules are up to date.";
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Error during manual detection: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
        TempData["ErrorMessage"] = $"Error during manual detection: {ex.Message}";
      }

      return RedirectToAction(nameof(Index));
    }

    // GET: MissedPickup/Details/5
    public async Task<IActionResult> Details(int? id)
    {
      if (id == null) return NotFound();

      var missedPickup = await _context.MissedPickups
          .Include(m => m.Schedule)
              .ThenInclude(s => s.Route)
          .Include(m => m.Schedule)
              .ThenInclude(s => s.Collector)
          .Include(m => m.Schedule)
              .ThenInclude(s => s.Truck)
          .Include(m => m.Schedule)
              .ThenInclude(s => s.CollectionPoints)
                  .ThenInclude(cp => cp.Bin)
          .FirstOrDefaultAsync(m => m.Id == id);

      if (missedPickup == null) return NotFound();

      return View(missedPickup);
    }

    // GET: Get notification count for navbar badge
    [HttpGet]
    public async Task<IActionResult> GetNotificationCount()
    {
      var pendingCount = await _context.MissedPickups
          .CountAsync(m => m.Status == "Pending");

      return Json(new { count = pendingCount });
    }

    // GET: Get recent notifications for dropdown
    [HttpGet]
    public async Task<IActionResult> GetRecentNotifications()
    {
      var recentMissedPickups = await _context.MissedPickups
          .Include(m => m.Schedule)
              .ThenInclude(s => s.Route)
          .Where(m => m.Status == "Pending")
          .OrderByDescending(m => m.DetectedAt)
          .Take(5)
          .Select(m => new
          {
            id = m.Id,
            message = $"Route {m.Schedule.Route.Name} - {m.DetectedAt:MMM dd, HH:mm}",
            detectedAt = m.DetectedAt.ToString("MMM dd, yyyy HH:mm"),
            routeName = m.Schedule.Route.Name
          })
          .ToListAsync();

      return Json(recentMissedPickups);
    }
  }
}
