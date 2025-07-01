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

    // Helper method to safely get schedule end time as DateTime
    private DateTime GetScheduleEndDateTime(Schedule schedule)
    {
      try
      {
        // Since the database stores datetime but model expects TimeSpan,
        // we need to handle this conversion carefully

        // Get the raw value and convert to TimeSpan if needed
        var endTimeProperty = typeof(Schedule).GetProperty("ScheduleEndTime");
        var endTimeValue = endTimeProperty.GetValue(schedule);

        if (endTimeValue is DateTime dateTimeValue)
        {
          // If it's loaded as DateTime from database, use its time part
          return DateTime.Today.Add(dateTimeValue.TimeOfDay);
        }
        else if (endTimeValue is TimeSpan timeSpanValue)
        {
          // If it's actually TimeSpan, convert to DateTime
          var scheduledEndDateTime = DateTime.Today.Add(timeSpanValue);

          // Handle schedules that cross midnight
          var startTimeProperty = typeof(Schedule).GetProperty("ScheduleStartTime");
          var startTimeValue = startTimeProperty.GetValue(schedule);

          if (startTimeValue is TimeSpan startTimeSpan && timeSpanValue < startTimeSpan)
          {
            scheduledEndDateTime = DateTime.Today.AddDays(1).Add(timeSpanValue);
          }

          return scheduledEndDateTime;
        }
        else
        {
          // Fallback
          return DateTime.Now.AddHours(8); // Default 8-hour shift
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Error converting schedule time: {ex.Message}");
        return DateTime.Now.AddHours(8); // Default fallback
      }
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

      var missedPickup = await _context.MissedPickups.FindAsync(model.Id);
      if (missedPickup == null) return NotFound();

      // Update the missed pickup
      missedPickup.Reason = model.Reason;
      missedPickup.Resolution = model.Resolution;
      missedPickup.Status = "Resolved";
      missedPickup.ResolvedAt = DateTime.Now;
      missedPickup.ResolvedBy = User.Identity.Name; // Admin who resolved it

      // Acknowledge the related alert
      var alert = await _context.Alerts
          .FirstOrDefaultAsync(a => a.Type == "missed_pickup" && a.SourceId == model.Id);
      if (alert != null)
      {
        alert.Status = "Acknowledged";
        alert.AcknowledgeAt = DateTime.Now;
      }

      await _context.SaveChangesAsync();

      TempData["SuccessMessage"] = "Missed pickup has been resolved successfully.";
      return RedirectToAction(nameof(Index));
    }

    // Enhanced detect method with automatic schedule completion logic
    public async Task<IActionResult> Detect()
    {
      try
      {
        var detectedCount = 0;
        var completedCount = 0;
        var currentTime = DateTime.Now;

        // Get all active schedules (not completed or cancelled)
        var activeSchedules = await _context.Schedules
            .Include(s => s.CollectionPoints)
                .ThenInclude(cp => cp.CollectionRecords)
            .Include(s => s.Route)
            .Include(s => s.Collector)
            .Where(s => s.Status != "Completed" &&
                       s.Status != "Cancelled")
            .ToListAsync();

        foreach (var schedule in activeSchedules)
        {
          bool scheduleUpdated = false;

          // Use helper method to safely get end DateTime
          var scheduledEndDateTime = GetScheduleEndDateTime(schedule);

          // LOGIC 1: Auto-complete if schedule end time has passed
          if (scheduledEndDateTime <= currentTime && schedule.Status != "Completed")
          {
            schedule.Status = "Completed";
            schedule.ActualEndTime = scheduledEndDateTime;
            schedule.UpdatedAt = currentTime;
            scheduleUpdated = true;
            completedCount++;

            Console.WriteLine($"Schedule {schedule.Id} auto-completed due to end time passed");
          }

          // LOGIC 2: Auto-complete if all collection points are collected
          if (schedule.CollectionPoints.Any() &&
              schedule.CollectionPoints.All(cp => cp.IsCollected) &&
              schedule.Status != "Completed")
          {
            schedule.Status = "Completed";
            schedule.UpdatedAt = currentTime;

            // Set actual end time to the latest collection time
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

            scheduleUpdated = true;
            completedCount++;

            Console.WriteLine($"Schedule {schedule.Id} auto-completed due to all bins collected");
          }

          // Save schedule updates before proceeding with missed pickup detection
          if (scheduleUpdated)
          {
            await _context.SaveChangesAsync();
            continue; // Skip missed pickup detection for completed schedules
          }

          // LOGIC 3: Detect missed pickups only for schedules that haven't been auto-completed
          // Only check for missed pickups if the scheduled end time has passed
          if (scheduledEndDateTime <= currentTime)
          {
            // Check if this schedule already has a missed pickup record
            var existingMissedPickup = await _context.MissedPickups
                .FirstOrDefaultAsync(m => m.ScheduleId == schedule.Id);

            if (existingMissedPickup != null)
              continue; // Skip if already detected

            // Check if any collection points in this schedule are uncollected
            var uncollectedPoints = schedule.CollectionPoints
                .Where(cp => !cp.IsCollected && !cp.CollectionRecords.Any())
                .ToList();

            if (uncollectedPoints.Any())
            {
              // Create missed pickup record
              var missedPickup = new MissedPickup
              {
                ScheduleId = schedule.Id,
                DetectedAt = currentTime,
                Status = "Pending",
                Reason = $"System detected {uncollectedPoints.Count} uncollected point(s) after scheduled end time. " +
                          $"Schedule was due to complete at {scheduledEndDateTime:g}",
                CreatedAt = currentTime
              };

              _context.MissedPickups.Add(missedPickup);
              await _context.SaveChangesAsync(); // Save to get the ID

              // Create an alert
              var alert = new Alert
              {
                Type = "missed_pickup",
                SourceId = missedPickup.Id,
                Message = $"Missed pickup detected for Route {schedule.Route?.Name ?? "Unknown"} - {uncollectedPoints.Count} points uncollected",
                TriggeredAt = currentTime,
                Severity = uncollectedPoints.Count > 5 ? "High" : "Medium",
                Status = "Unread",
                CreatedAt = currentTime
              };

              _context.Alerts.Add(alert);
              detectedCount++;

              Console.WriteLine($"Missed pickup detected for Schedule {schedule.Id} - {uncollectedPoints.Count} uncollected points");
            }
          }
        }

        await _context.SaveChangesAsync();

        // Provide comprehensive feedback
        var messages = new List<string>();

        if (completedCount > 0)
        {
          messages.Add($"{completedCount} schedule(s) automatically marked as completed");
        }

        if (detectedCount > 0)
        {
          messages.Add($"{detectedCount} new missed pickup(s) detected");
        }

        if (messages.Any())
        {
          TempData["SuccessMessage"] = $"Detection completed. {string.Join(", ", messages)}.";
        }
        else
        {
          TempData["InfoMessage"] = "Detection completed. No schedule updates or missed pickups found.";
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Error in Detect method: {ex.Message}");
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

    // NEW: Manual method to complete schedules (can be called separately)
    [HttpPost]
    public async Task<IActionResult> AutoCompleteSchedules()
    {
      try
      {
        var completedCount = 0;
        var currentTime = DateTime.Now;

        var activeSchedules = await _context.Schedules
            .Include(s => s.CollectionPoints)
            .Where(s => s.Status != "Completed" && s.Status != "Cancelled")
            .ToListAsync();

        foreach (var schedule in activeSchedules)
        {
          // Use helper method to safely get end DateTime
          var scheduledEndDateTime = GetScheduleEndDateTime(schedule);

          // Auto-complete based on time or collection status
          if ((scheduledEndDateTime <= currentTime) ||
              (schedule.CollectionPoints.Any() && schedule.CollectionPoints.All(cp => cp.IsCollected)))
          {
            schedule.Status = "Completed";
            schedule.UpdatedAt = currentTime;

            var collectedTimes = schedule.CollectionPoints
                .Where(cp => cp.CollectedAt.HasValue)
                .Select(cp => cp.CollectedAt.Value);

            if (collectedTimes.Any())
            {
              schedule.ActualEndTime = collectedTimes.Max();
            }
            else
            {
              schedule.ActualEndTime = scheduledEndDateTime;
            }

            completedCount++;
          }
        }

        await _context.SaveChangesAsync();

        if (completedCount > 0)
        {
          TempData["SuccessMessage"] = $"{completedCount} schedule(s) have been automatically completed.";
        }
        else
        {
          TempData["InfoMessage"] = "No schedules needed to be completed.";
        }
      }
      catch (Exception ex)
      {
        TempData["ErrorMessage"] = $"Error completing schedules: {ex.Message}";
      }

      return RedirectToAction(nameof(Index));
    }
  }
}
