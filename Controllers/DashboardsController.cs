using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AspnetCoreMvcFull.Data;
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AspnetCoreMvcFull.Controllers;

[Authorize(Roles = "Admin")]
public class DashboardsController : Controller
{
  private readonly KUTIPDbContext _context;

  public DashboardsController(KUTIPDbContext context)
  {
    _context = context;
  }

  // ───────────────────────────────────────────────────────────────
  // Index  ‑ accepts date filter via query string
  // ───────────────────────────────────────────────────────────────
  public async Task<IActionResult> Index(
      string period = "day",         // "day" | "week" | "month" | "specific" | "custom"
      DateTime? start = null,          // used by specific & custom
      DateTime? end = null)          // used by custom
  {
    // ---------------- 1. determine date window -----------------
    DateTime today = DateTime.Today;
    DateTime from, to;                // inclusive lower‑bound, exclusive upper‑bound

    switch (period)
    {
      case "week":
        from = today.AddDays(-(int)today.DayOfWeek + 1);   // Monday
        to = from.AddDays(7);
        break;

      case "month":
        from = new DateTime(today.Year, today.Month, 1);
        to = from.AddMonths(1);
        break;

      case "specific" when start.HasValue:
        from = start.Value.Date;
        to = from.AddDays(1);
        break;

      case "custom" when start.HasValue && end.HasValue:
        from = start.Value.Date;
        to = end.Value.Date.AddDays(1);                  // include end day
        break;

      default:                                              // "day"
        from = today;
        to = today.AddDays(1);
        period = "day";                                   // normalise
        break;
    }

    // ---------------- 2. create view‑model ---------------------
    var vm = new DashboardViewModel
    {
      SelectedPeriod = period,
      StartDate = from,
      EndDate = to.AddDays(-1)                        // inclusive end for display
};

    // ---------------- 3. filtered queries ----------------------
    // 3a. pickups & missed (main filter everywhere)
    var pickupsQry = _context.CollectionRecords
                     .Where(r => r.PickupTimestamp >= from && r.PickupTimestamp < to);

    var missedQry = _context.MissedPickups
                     .Where(m => m.DetectedAt >= from && m.DetectedAt < to);

    vm.TotalPickups = await pickupsQry.CountAsync();
    vm.MissedPickups = await missedQry.CountAsync();

    // 3b. fleet / driver / collector / client (not date‑bounded)
    vm.FleetCount = await _context.Trucks.CountAsync();
    vm.DriverCount = await _context.Users.CountAsync(u => u.Role == "Driver");
    vm.CollectorCount = await _context.Users.CountAsync(u => u.Role == "Collector");
    vm.ClientCount = await _context.Clients.CountAsync();

    // 3c. alerts (open issues, no date filter)
    var activeAlerts = await _context.BinReports
        .Where(r => r.IsIssueReported && r.Status == "pending" && r.AcknowledgedAt == null)
        .ToListAsync();

    vm.TotalActiveAlerts = activeAlerts.Count;
    vm.CriticalAlerts = activeAlerts.Count(r => r.Severity == "Critical");
    vm.HighAlerts = activeAlerts.Count(r => r.Severity == "High");
    vm.MediumAlerts = activeAlerts.Count(r => r.Severity == "Medium");

    // ---------------- 4. charts (filtered) ---------------------
    // 4a. day‑by‑day arrays for the selected range
    int daysSpan = (int)(to - from).TotalDays;                 // 1‑31
    var dayLabels = Enumerable.Range(0, daysSpan)
                               .Select(d => from.AddDays(d))
                               .ToList();

    var dailyPickups = await pickupsQry
        .GroupBy(r => r.PickupTimestamp.Date)
        .Select(g => new { Day = g.Key, Cnt = g.Count() })
        .ToListAsync();

    var dailyMissed = await missedQry
        .GroupBy(m => m.DetectedAt.Date)
        .Select(g => new { Day = g.Key, Cnt = g.Count() })
        .ToListAsync();

    vm.WeekDays = dayLabels.Select(d => d.ToString("dd/MM")).ToList();
    vm.WeeklyPickupCounts = dayLabels.Select(d => dailyPickups.FirstOrDefault(x => x.Day == d)?.Cnt ?? 0).ToList();
    vm.WeeklyMissedCounts = dayLabels.Select(d => dailyMissed.FirstOrDefault(x => x.Day == d)?.Cnt ?? 0).ToList();

    // 4b. bar ‑ pickups per collector
    var collectors = await _context.Users
        .Where(u => u.Role == "Collector")
        .OrderBy(u => u.FirstName)
        .Select(u => new { u.Id, Name = u.FirstName })
        .ToListAsync();

    var collCollected = await pickupsQry
        .GroupBy(r => r.CollectorId)
        .Select(g => new { g.Key, Cnt = g.Count() })
        .ToDictionaryAsync(g => g.Key, g => g.Cnt);

    var collMissed = await missedQry
        .GroupBy(m => m.Schedule.CollectorId)
        .Select(g => new { g.Key, Cnt = g.Count() })
        .ToDictionaryAsync(g => g.Key, g => g.Cnt);

    foreach (var c in collectors)
    {
      vm.CollectorNames.Add(c.Name);
      vm.CollectedByCollector.Add(collCollected.TryGetValue(c.Id, out var ok) ? ok : 0);
      vm.MissedByCollector.Add(collMissed.TryGetValue(c.Id, out var ms) ? ms : 0);
    }

    // ---------------- 5. active routes (no date filter) --------
    var allRoutes = await _context.Schedules
        .Include(s => s.Truck)
        .Include(s => s.Route)
        .Include(s => s.CollectionPoints).ThenInclude(cp => cp.Bin)
        .Where(s => s.Status == "Scheduled" || s.Status == "In-Progress")
        .ToListAsync();

    string[] routeColors = { "#2ecc71", "#e74c3c", "#3498db", "#f39c12", "#9b59b6",
                                 "#1abc9c", "#e67e22", "#34495e" };

    vm.AllRoutes = allRoutes.Select((schedule, idx) => new DashboardRoute
    {
      ScheduleId = schedule.Id,
      TruckId = schedule.TruckId,
      TruckName = schedule.Truck?.LicensePlate ?? $"Truck {schedule.TruckId}",
      RouteName = schedule.Route?.Name ?? $"Route {schedule.RouteId}",
      Status = schedule.Status,
      ScheduleStartTime = schedule.ScheduleStartTime,
      RouteColor = routeColors[idx % routeColors.Length],

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
            })
            .ToList()
    }).ToList();

    vm.TotalActiveRoutes = vm.AllRoutes.Count;
    vm.TotalCollectionPoints = vm.AllRoutes.Sum(r => r.CollectionPoints.Count);

    return View(vm);
  }


  [HttpGet]
  public async Task<IActionResult> GetAllRoutesData()
  {
    var allRoutes = await _context.Schedules
        .Include(s => s.Truck)
        .Include(s => s.Route)
        .Include(s => s.CollectionPoints).ThenInclude(cp => cp.Bin)
        .Where(s => s.Status == "Scheduled" || s.Status == "In-Progress")
        .ToListAsync();

    string[] routeColors = { "#2ecc71", "#e74c3c", "#3498db", "#f39c12", "#9b59b6",
                                 "#1abc9c", "#e67e22", "#34495e" };

    var result = allRoutes.Select((schedule, idx) => new DashboardRoute
    {
      ScheduleId = schedule.Id,
      TruckId = schedule.TruckId,
      TruckName = schedule.Truck?.LicensePlate ?? $"Truck {schedule.TruckId}",
      RouteName = schedule.Route?.Name ?? $"Route {schedule.RouteId}",
      Status = schedule.Status,
      ScheduleStartTime = schedule.ScheduleStartTime,
      RouteColor = routeColors[idx % routeColors.Length],

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
            })
            .ToList()
    });

    return Json(result);
  }
}
