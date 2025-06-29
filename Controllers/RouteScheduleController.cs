using AspnetCoreMvcFull.Data;
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AspnetCoreMvcFull.Controllers
{
  [Authorize(Roles = "Admin,Driver")]
  public class RouteScheduleController : Controller
  {
    private readonly KUTIPDbContext _context;

    public RouteScheduleController(KUTIPDbContext context)
    {
      _context = context;
    }

    public async Task<IActionResult> Index()
    {
      var viewModel = new RouteScheduleViewModel();

      // Get all available schedules
      viewModel.AvailableSchedules = await _context.Schedules
          .Include(s => s.Truck)
          .Include(s => s.Route)
          .Include(s => s.CollectionPoints)
          .Select(s => new ScheduleOption
          {
            ScheduleId = s.Id,
            TruckId = s.TruckId,
            TruckName = s.Truck.LicensePlate ?? $"Truck {s.TruckId}",
            RouteName = s.Route.Name ?? $"Route {s.RouteId}",
            ScheduleStartTime = s.ScheduleStartTime,
            Status = s.Status,
            CollectionPointsCount = s.CollectionPoints.Count()
          })
          .OrderByDescending(s => s.ScheduleStartTime)
          .ToListAsync();

      return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> GetCollectionPoints(int scheduleId)
    {
      var schedule = await _context.Schedules
          .Include(s => s.Truck)
          .Include(s => s.Route)
          .Include(s => s.CollectionPoints)
              .ThenInclude(cp => cp.Bin)
          .FirstOrDefaultAsync(s => s.Id == scheduleId);

      if (schedule == null)
      {
        return NotFound();
      }

      var result = new CollectionPointData
      {
        ScheduleId = schedule.Id,
        TruckId = schedule.TruckId,
        TruckName = schedule.Truck?.LicensePlate ?? $"Truck {schedule.TruckId}",
        RouteName = schedule.Route?.Name ?? $"Route {schedule.RouteId}",
        CollectionPoints = schedule.CollectionPoints
              .OrderBy(cp => cp.OrderInSchedule)
              .Select(cp => new CollectionPointInfo
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
      };

      return Json(result);
    }
  }
}
