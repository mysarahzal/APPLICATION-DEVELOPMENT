using AspnetCoreMvcFull.Data;
using AspnetCoreMvcFull.Models;
using Microsoft.EntityFrameworkCore;

namespace AspnetCoreMvcFull.Services
{
  public class MissedPickupDetectionService : BackgroundService
  {
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MissedPickupDetectionService> _logger;
    private readonly TimeSpan _detectionInterval = TimeSpan.FromMinutes(2); // Changed from 30 to 2 minutes

    public MissedPickupDetectionService(
        IServiceProvider serviceProvider,
        ILogger<MissedPickupDetectionService> logger)
    {
      _serviceProvider = serviceProvider;
      _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      _logger.LogInformation("Missed Pickup Detection Service started - Running every {Interval} minutes", _detectionInterval.TotalMinutes);

      while (!stoppingToken.IsCancellationRequested)
      {
        try
        {
          await RunDetection();
          _logger.LogInformation("Next detection check in {Minutes} minutes", _detectionInterval.TotalMinutes);
          await Task.Delay(_detectionInterval, stoppingToken);
        }
        catch (OperationCanceledException)
        {
          // Service is being stopped
          break;
        }
        catch (Exception ex)
        {
          _logger.LogError(ex, "Error occurred during missed pickup detection");
          // Wait a shorter time before retrying on error
          await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
      }

      _logger.LogInformation("Missed Pickup Detection Service stopped");
    }

    private async Task RunDetection()
    {
      using var scope = _serviceProvider.CreateScope();
      var context = scope.ServiceProvider.GetRequiredService<KUTIPDbContext>();

      try
      {
        var detectedCount = 0;
        var updatedScheduleCount = 0;
        var currentTime = DateTime.Now;

        _logger.LogInformation("=== AUTOMATIC MISSED PICKUP DETECTION STARTED ===");
        _logger.LogInformation("Current time: {CurrentTime}", currentTime);

        // Get schedules that should have been completed but haven't been marked as such
        // Since ScheduleEndTime is DateTime, we compare directly
        var potentialMissedSchedules = await context.Schedules
            .Include(s => s.CollectionPoints)
                .ThenInclude(cp => cp.CollectionRecords)
            .Include(s => s.Route)
            .Include(s => s.Collector)
            .Where(s => s.ScheduleEndTime <= currentTime &&
                       s.Status != "Completed" &&
                       s.Status != "Cancelled" &&
                       s.Status != "Missed")
            .ToListAsync();

        _logger.LogInformation("Found {Count} potential missed schedules", potentialMissedSchedules.Count);

        foreach (var schedule in potentialMissedSchedules)
        {
          _logger.LogDebug("Checking Schedule #{ScheduleId} - End time: {EndTime}, Status: {Status}",
              schedule.Id, schedule.ScheduleEndTime, schedule.Status);

          // Check if this schedule already has a missed pickup record
          var existingMissedPickup = await context.MissedPickups
              .FirstOrDefaultAsync(m => m.ScheduleId == schedule.Id);

          if (existingMissedPickup != null)
          {
            _logger.LogDebug("Schedule #{ScheduleId} already has missed pickup record", schedule.Id);
            continue; // Skip if already detected
          }

          // Check if any collection points in this schedule are uncollected
          var uncollectedPoints = schedule.CollectionPoints
              .Where(cp => !cp.IsCollected && !cp.CollectionRecords.Any())
              .ToList();

          var totalPoints = schedule.CollectionPoints.Count();
          var collectedPoints = schedule.CollectionPoints.Count(cp => cp.IsCollected);

          _logger.LogDebug("Schedule #{ScheduleId}: {CollectedPoints}/{TotalPoints} points collected, {UncollectedCount} uncollected",
              schedule.Id, collectedPoints, totalPoints, uncollectedPoints.Count);

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

            _logger.LogInformation("Marking Schedule #{ScheduleId} as MISSED", schedule.Id);

            // Create missed pickup record (this serves as both record and notification)
            var missedPickup = new MissedPickup
            {
              ScheduleId = schedule.Id,
              DetectedAt = currentTime,
              Status = "Pending",
              Reason = $"AUTO-DETECTED: {uncollectedPoints.Count} uncollected point(s) out of {totalPoints} total points after scheduled end time ({schedule.ScheduleEndTime:g}). Route: {schedule.Route?.Name ?? "Unknown"}",
              CreatedAt = currentTime
            };

            context.MissedPickups.Add(missedPickup);
            detectedCount++;
            updatedScheduleCount++;

            _logger.LogInformation("Created missed pickup notification for Schedule #{ScheduleId}", schedule.Id);
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
            _logger.LogInformation("Marking Schedule #{ScheduleId} as COMPLETED (all points collected)", schedule.Id);
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
                Reason = $"AUTO-DETECTED: Schedule overdue by {hoursOverdue:F1} hours. {collectedPoints}/{totalPoints} points collected, {uncollectedPoints.Count} remaining uncollected. Route: {schedule.Route?.Name ?? "Unknown"}",
                CreatedAt = currentTime
              };

              context.MissedPickups.Add(missedPickup);
              detectedCount++;
              updatedScheduleCount++;

              _logger.LogInformation("Marking Schedule #{ScheduleId} as MISSED (partially completed but overdue)", schedule.Id);
            }
            else
            {
              _logger.LogDebug("Schedule #{ScheduleId} is overdue by {Hours:F1}h but within tolerance",
                  schedule.Id, hoursOverdue);
            }
          }
        }

        await context.SaveChangesAsync();

        _logger.LogInformation("=== AUTOMATIC DETECTION COMPLETED ===");
        _logger.LogInformation("Missed pickups detected: {DetectedCount}", detectedCount);
        _logger.LogInformation("Schedules updated: {UpdatedCount}", updatedScheduleCount);

      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error during automatic missed pickup detection");
        throw;
      }
    }
  }
}
