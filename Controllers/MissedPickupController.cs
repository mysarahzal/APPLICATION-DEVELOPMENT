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

        // Detect missed pickups based on schedules and collection records
        public async Task<IActionResult> Detect()
        {
            try
            {
                var detectedCount = 0;
                var currentTime = DateTime.Now;

                // Get schedules that should have been completed but haven't been marked as such
                var potentialMissedSchedules = await _context.Schedules
                    .Include(s => s.CollectionPoints)
                        .ThenInclude(cp => cp.CollectionRecords)
                    .Include(s => s.Route)
                    .Include(s => s.Collector)
                    .Where(s => s.ScheduleEndTime <= currentTime && 
                               s.Status != "Completed" && 
                               s.Status != "Cancelled")
                    .ToListAsync();

                foreach (var schedule in potentialMissedSchedules)
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
                            Reason = $"System detected {uncollectedPoints.Count} uncollected point(s) after scheduled end time ({schedule.ScheduleEndTime:g})",
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
                    }
                    else if (schedule.CollectionPoints.All(cp => cp.IsCollected))
                    {
                        // All points collected, mark schedule as completed
                        schedule.Status = "Completed";
                        schedule.ActualEndTime = schedule.CollectionPoints
                            .Where(cp => cp.CollectedAt.HasValue)
                            .Max(cp => cp.CollectedAt);
                    }
                }

                await _context.SaveChangesAsync();

                if (detectedCount > 0)
                {
                    TempData["SuccessMessage"] = $"Detection completed. Found {detectedCount} new missed pickup(s).";
                }
                else
                {
                    TempData["InfoMessage"] = "Detection completed. No new missed pickups found.";
                }
            }
            catch (Exception ex)
            {
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
    }
}
