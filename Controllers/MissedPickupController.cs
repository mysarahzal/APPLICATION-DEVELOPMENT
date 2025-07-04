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
    [HttpGet]
    public async Task<IActionResult> Resolve(int? id)
    {
      if (id == null) return NotFound();

      var missedPickup = await _context.MissedPickups
          .Include(m => m.Schedule)
              .ThenInclude(s => s.Route)
          .Include(m => m.Schedule)
              .ThenInclude(s => s.Collector)
          .Include(m => m.Schedule)
              .ThenInclude(s => s.CollectionPoints)
          .FirstOrDefaultAsync(m => m.Id == id);

      if (missedPickup == null) return NotFound();

      return View(missedPickup);
    }

    // POST: MissedPickup/Resolve
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResolvePost(int id, string reason, string resolution)
    {
      try
      {
        var missedPickup = await _context.MissedPickups
            .Include(m => m.Schedule)
                .ThenInclude(s => s.Route)
            .Include(m => m.Schedule)
                .ThenInclude(s => s.Collector)
            .Include(m => m.Schedule)
                .ThenInclude(s => s.CollectionPoints)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (missedPickup == null)
        {
          TempData["ErrorMessage"] = "Missed pickup not found.";
          return RedirectToAction(nameof(Index));
        }

        // Validate required fields
        if (string.IsNullOrWhiteSpace(resolution))
        {
          TempData["ErrorMessage"] = "Resolution action is required.";
          return View("Resolve", missedPickup);
        }

        // Update the missed pickup
        missedPickup.Reason = !string.IsNullOrWhiteSpace(reason) ? reason : missedPickup.Reason;
        missedPickup.Resolution = resolution;
        missedPickup.Status = "Resolved";
        missedPickup.ResolvedAt = DateTime.Now;
        missedPickup.ResolvedBy = User.Identity?.Name ?? "Admin";

        // Update the related schedule status
        if (missedPickup.Schedule != null)
        {
          missedPickup.Schedule.Status = "Completed";
          missedPickup.Schedule.UpdatedAt = DateTime.Now;

          // Set actual end time if not already set
          if (!missedPickup.Schedule.ActualEndTime.HasValue)
          {
            missedPickup.Schedule.ActualEndTime = DateTime.Now;
          }
        }

        // Acknowledge related alert if it exists
        var alert = await _context.Alerts
            .FirstOrDefaultAsync(a => a.Type == "missed_pickup" && a.SourceId == id);
        if (alert != null)
        {
          alert.Status = "Acknowledged";
          alert.AcknowledgeAt = DateTime.Now;
          alert.AcknowledgedBy = User.Identity?.Name ?? "Admin";
        }

        await _context.SaveChangesAsync();

        // Return the resolved view
        return View("Resolve", missedPickup);
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Error resolving missed pickup: {ex.Message}");
        TempData["ErrorMessage"] = $"Error resolving missed pickup: {ex.Message}";
        return RedirectToAction(nameof(Index));
      }
    }

    // Enhanced detection that works with automatic status updates
    public async Task<IActionResult> Detect()
    {
      try
      {
        var detectedCount = 0;
        var updatedScheduleCount = 0;
        var currentTime = DateTime.Now;

        Console.WriteLine($"=== MANUAL MISSED PICKUP DETECTION STARTED ===");
        Console.WriteLine($"Current time: {currentTime}");

        // Get schedules that are marked as "Missed" but don't have missed pickup records yet
        var missedSchedulesWithoutRecords = await _context.Schedules
            .Include(s => s.CollectionPoints)
            .Include(s => s.Route)
            .Include(s => s.Collector)
            .Where(s => s.Status == "Missed")
            .Where(s => !_context.MissedPickups.Any(m => m.ScheduleId == s.Id))
            .ToListAsync();

        Console.WriteLine($"Found {missedSchedulesWithoutRecords.Count} missed schedules without records");

        foreach (var schedule in missedSchedulesWithoutRecords)
        {
          var totalPoints = schedule.CollectionPoints?.Count() ?? 0;
          var collectedPoints = schedule.CollectionPoints?.Count(cp => cp.IsCollected) ?? 0;
          var uncollectedPoints = totalPoints - collectedPoints;

          var hoursOverdue = schedule.ScheduleEndTime < currentTime
            ? (currentTime - schedule.ScheduleEndTime).TotalHours
            : 0;

          // Create missed pickup record
          var missedPickup = new MissedPickup
          {
            ScheduleId = schedule.Id,
            DetectedAt = currentTime,
            Status = "Pending",
            Reason = totalPoints > 0
              ? $"Manual detection - {uncollectedPoints}/{totalPoints} points uncollected, {hoursOverdue:F1} hours overdue"
              : $"Manual detection - Schedule marked as missed, {hoursOverdue:F1} hours past scheduled end time",
            CreatedAt = currentTime
          };

          _context.MissedPickups.Add(missedPickup);
          await _context.SaveChangesAsync(); // Save to get the ID

          // Create an alert
          var alert = new Alert
          {
            Type = "missed_pickup",
            SourceId = missedPickup.Id,
            Message = $"Missed pickup detected for Route {schedule.Route?.Name ?? "Unknown"} - {uncollectedPoints}/{totalPoints} points uncollected",
            TriggeredAt = currentTime,
            Severity = uncollectedPoints > 5 ? "High" : "Medium",
            Status = "Unread",
            CreatedAt = currentTime
          };

          _context.Alerts.Add(alert);
          detectedCount++;

          Console.WriteLine($"Created missed pickup record for Schedule #{schedule.Id}");
        }

        // Also check for schedules that should be completed but aren't
        var potentialCompletedSchedules = await _context.Schedules
            .Include(s => s.CollectionPoints)
            .Where(s => s.Status == "In Progress" || s.Status == "Scheduled")
            .Where(s => s.CollectionPoints.All(cp => cp.IsCollected))
            .ToListAsync();

        foreach (var schedule in potentialCompletedSchedules)
        {
          schedule.Status = "Completed";
          schedule.UpdatedAt = currentTime;
          schedule.ActualEndTime = schedule.CollectionPoints
              .Where(cp => cp.CollectedAt.HasValue)
              .Max(cp => cp.CollectedAt);

          updatedScheduleCount++;
          Console.WriteLine($"Marking Schedule #{schedule.Id} as COMPLETED (all points collected)");
        }

        await _context.SaveChangesAsync();

        Console.WriteLine($"=== MANUAL DETECTION COMPLETED ===");
        Console.WriteLine($"Missed pickups detected: {detectedCount}");
        Console.WriteLine($"Schedules updated: {updatedScheduleCount}");

        if (detectedCount > 0)
        {
          TempData["SuccessMessage"] = $"Detection completed. Found {detectedCount} new missed pickup(s) and updated {updatedScheduleCount} schedule(s).";
        }
        else if (updatedScheduleCount > 0)
        {
          TempData["InfoMessage"] = $"Detection completed. No new missed pickups found, but {updatedScheduleCount} schedule(s) were updated to completed status.";
        }
        else
        {
          TempData["InfoMessage"] = "Detection completed. No new missed pickups found and all schedules are up to date.";
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Error during detection: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
        TempData["ErrorMessage"] = $"Error during detection: {ex.Message}";
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

    // GET: MissedPickup/Dashboard - Summary view
    public async Task<IActionResult> Dashboard()
    {
      var today = DateTime.Today;
      var thisWeek = today.AddDays(-(int)today.DayOfWeek);
      var thisMonth = new DateTime(today.Year, today.Month, 1);

      var stats = new
      {
        TotalMissed = await _context.MissedPickups.CountAsync(),
        PendingMissed = await _context.MissedPickups.CountAsync(m => m.Status == "Pending"),
        ResolvedMissed = await _context.MissedPickups.CountAsync(m => m.Status == "Resolved"),
        TodayMissed = await _context.MissedPickups.CountAsync(m => m.DetectedAt.Date == today),
        WeekMissed = await _context.MissedPickups.CountAsync(m => m.DetectedAt >= thisWeek),
        MonthMissed = await _context.MissedPickups.CountAsync(m => m.DetectedAt >= thisMonth),
        ResolutionRate = await _context.MissedPickups.AnyAsync()
          ? Math.Round((double)await _context.MissedPickups.CountAsync(m => m.Status == "Resolved") /
                      await _context.MissedPickups.CountAsync() * 100, 1)
          : 0
      };

      ViewBag.Stats = stats;

      var recentMissedPickups = await _context.MissedPickups
          .Include(m => m.Schedule)
              .ThenInclude(s => s.Route)
          .Include(m => m.Schedule)
              .ThenInclude(s => s.Collector)
          .OrderByDescending(m => m.DetectedAt)
          .Take(10)
          .ToListAsync();

      return View(recentMissedPickups);
    }
  }
}
