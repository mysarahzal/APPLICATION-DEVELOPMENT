using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.ViewModels;
using Microsoft.EntityFrameworkCore;
using AspnetCoreMvcFull.Data;
using Microsoft.AspNetCore.Authorization;

namespace AspnetCoreMvcFull.Controllers;
[Authorize(Roles = "Admin,Driver")]
public class DashboardsController : Controller
{
  private readonly KUTIPDbContext _context;

  public DashboardsController(KUTIPDbContext context)
  {
    _context = context;
  }

  public async Task<IActionResult> Index()
  {
    var viewModel = new DashboardViewModel();

    viewModel.TotalPickups = await _context.CollectionRecords.CountAsync();
    viewModel.MissedPickups = await _context.MissedPickups.CountAsync();
    viewModel.FleetCount = await _context.Trucks.CountAsync();
    viewModel.DriverCount = await _context.Users.Where(u => u.Role == "Driver").CountAsync();
    viewModel.CollectorCount = await _context.Users.Where(u => u.Role == "Collector").CountAsync();
    viewModel.ClientCount = await _context.Clients.CountAsync();

    var activeAlerts = await _context.BinReports
       .Where(r => r.IsIssueReported               
                && r.Status == "pending"            
                && r.AcknowledgedAt == null)       
       .ToListAsync();

    viewModel.TotalActiveAlerts = activeAlerts.Count;
    viewModel.CriticalAlerts = activeAlerts.Count(r => r.Severity == "Critical");
    viewModel.HighAlerts = activeAlerts.Count(r => r.Severity == "High");
    viewModel.MediumAlerts = activeAlerts.Count(r => r.Severity == "Medium");

    // Get weekly pickup counts
    var weekStart = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + 1); // Monday
    var weekEnd = weekStart.AddDays(6); // Sunday (but we will exclude it later)

    var pickups = await _context.CollectionRecords
        .Where(r => r.PickupTimestamp >= weekStart && r.PickupTimestamp < weekEnd)
        .ToListAsync();

    var missed = await _context.MissedPickups
        .Where(r => r.DetectedAt >= weekStart && r.DetectedAt < weekEnd)
        .ToListAsync();

    // Prepare Monday–Saturday chart data
    var weekdays = new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };
    var pickupCounts = new int[6];
    var missedCounts = new int[6];

    foreach (var p in pickups)
    {
      int day = ((int)p.PickupTimestamp.DayOfWeek + 6) % 7; // Monday = 0, Sunday = 6
      if (day < 6) pickupCounts[day]++;
    }

    foreach (var m in missed)
    {
      int day = ((int)m.DetectedAt.DayOfWeek + 6) % 7;
      if (day < 6) missedCounts[day]++;
    }

    viewModel.WeeklyPickupCounts = pickupCounts.ToList();
    viewModel.WeeklyMissedCounts = missedCounts.ToList();
    viewModel.WeekDays = weekdays.ToList();

    // -------------------------------------------
    //  Pickup & missed counts grouped by collector
    // -------------------------------------------

    // 1. Get all collectors (stable ordering for chart)
    var collectors = await _context.Users
        .Where(u => u.Role == "Collector")
        .OrderBy(u => u.FirstName)
        .Select(u => new { u.Id, u.FirstName })
        .ToListAsync();

    // 2. Count total pickups per collector (from CollectionRecord)
    var collectedGroups = await _context.CollectionRecords
        .GroupBy(r => r.CollectorId)
        .Select(g => new { CollectorId = g.Key, Count = g.Count() })
        .ToDictionaryAsync(g => g.CollectorId, g => g.Count);

    // 3. Count missed pickups per collector (direct from Schedule.CollectorId)
    var missedGroups = await _context.MissedPickups
        .GroupBy(mp => mp.Schedule.CollectorId)
        .Select(g => new { CollectorId = g.Key, Count = g.Count() })
        .ToDictionaryAsync(g => g.CollectorId, g => g.Count);

    // 4. Add to view model
    foreach (var c in collectors)
    {
      viewModel.CollectorNames.Add(c.FirstName);
      viewModel.CollectedByCollector.Add(collectedGroups.TryGetValue(c.Id, out var col) ? col : 0);
      viewModel.MissedByCollector.Add(missedGroups.TryGetValue(c.Id, out var mis) ? mis : 0);
    }


    // Get all active routes with collection points
    var allRoutes = await _context.Schedules
        .Include(s => s.Truck)
        .Include(s => s.Route)
        .Include(s => s.CollectionPoints)
            .ThenInclude(cp => cp.Bin)
        .Where(s => s.Status == "Scheduled" || s.Status == "In-Progress")
        .ToListAsync();

    // Define colors for different routes
    var routeColors = new[] { "#2ecc71", "#e74c3c", "#3498db", "#f39c12", "#9b59b6", "#1abc9c", "#e67e22", "#34495e" };

    viewModel.AllRoutes = allRoutes.Select((schedule, index) => new DashboardRoute
    {
      ScheduleId = schedule.Id,
      TruckId = schedule.TruckId,
      TruckName = schedule.Truck?.LicensePlate ?? $"Truck {schedule.TruckId}",
      RouteName = schedule.Route?.Name ?? $"Route {schedule.RouteId}",
      Status = schedule.Status,
      ScheduleStartTime = schedule.ScheduleStartTime,
      RouteColor = routeColors[index % routeColors.Length],
      CollectionPoints = schedule.CollectionPoints
            .OrderBy(cp => cp.OrderInSchedule)
            .Select(cp => new RouteCollectionPoint
            {
              Id = cp.Id,
              BinPlateId = cp.Bin.BinPlateId,
              Location = cp.Bin.Location,
              Latitude = cp.Latitude ?? cp.Bin.Latitude ?? 0,
              Longitude = cp.Longitude ?? cp.Bin.Longitude ?? 0,
              OrderInSchedule = cp.OrderInSchedule,
              IsCollected = cp.IsCollected,
              CollectedAt = cp.CollectedAt,
              FillLevel = cp.Bin.FillLevel
            }).ToList()
    }).ToList();

    viewModel.TotalActiveRoutes = viewModel.AllRoutes.Count;
    viewModel.TotalCollectionPoints = viewModel.AllRoutes.Sum(r => r.CollectionPoints.Count);

    return View(viewModel);
  }

  [HttpGet]
  public async Task<IActionResult> GetAllRoutesData()
  {
    var allRoutes = await _context.Schedules
        .Include(s => s.Truck)
        .Include(s => s.Route)
        .Include(s => s.CollectionPoints)
            .ThenInclude(cp => cp.Bin)
        .Where(s => s.Status == "Scheduled" || s.Status == "In-Progress")
        .ToListAsync();

    var routeColors = new[] { "#2ecc71", "#e74c3c", "#3498db", "#f39c12", "#9b59b6", "#1abc9c", "#e67e22", "#34495e" };

    var result = allRoutes.Select((schedule, index) => new DashboardRoute
    {
      ScheduleId = schedule.Id,
      TruckId = schedule.TruckId,
      TruckName = schedule.Truck?.LicensePlate ?? $"Truck {schedule.TruckId}",
      RouteName = schedule.Route?.Name ?? $"Route {schedule.RouteId}",
      Status = schedule.Status,
      ScheduleStartTime = schedule.ScheduleStartTime,
      RouteColor = routeColors[index % routeColors.Length],
      CollectionPoints = schedule.CollectionPoints
            .OrderBy(cp => cp.OrderInSchedule)
            .Select(cp => new RouteCollectionPoint
            {
              Id = cp.Id,
              BinPlateId = cp.Bin.BinPlateId,
              Location = cp.Bin.Location,
              Latitude = cp.Latitude ?? cp.Bin.Latitude ?? 0,
              Longitude = cp.Longitude ?? cp.Bin.Longitude ?? 0,
              OrderInSchedule = cp.OrderInSchedule,
              IsCollected = cp.IsCollected,
              CollectedAt = cp.CollectedAt,
              FillLevel = cp.Bin.FillLevel
            }).ToList()
    }).ToList();

    return Json(result);
  }
}

