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

    // Your existing counts
    viewModel.FleetCount = await _context.Trucks.CountAsync();
    viewModel.DriverCount = await _context.Users.Where(u => u.Role == "Driver").CountAsync();
    viewModel.CollectorCount = await _context.Users.Where(u => u.Role == "Collector").CountAsync();
    viewModel.ClientCount = await _context.Clients.CountAsync();

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

