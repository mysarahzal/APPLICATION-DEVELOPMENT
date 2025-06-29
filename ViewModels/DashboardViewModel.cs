using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
namespace AspnetCoreMvcFull.ViewModels
{
  public class DashboardViewModel
  {
    public int FleetCount { get; set; }
    public int CollectorCount { get; set; }
    public int DriverCount { get; set; }
    public int ClientCount { get; set; }

    public int TotalPickups { get; set; }
    public int MissedPickups { get; set; }

    public double SuccessRate => TotalPickups + MissedPickups > 0
        ? Math.Round(((double)TotalPickups / (TotalPickups + MissedPickups)) * 100, 1)
        : 0;
    // New properties for all routes
    public List<DashboardRoute> AllRoutes { get; set; } = new List<DashboardRoute>();
    public int TotalActiveRoutes { get; set; }
    public int TotalCollectionPoints { get; set; }
  }

  public class DashboardRoute
  {
    public int ScheduleId { get; set; }
    public int TruckId { get; set; }
    public string TruckName { get; set; }
    public string RouteName { get; set; }
    public string Status { get; set; }
    public DateTime ScheduleStartTime { get; set; }
    public string RouteColor { get; set; } // For different colored routes
    public List<RouteCollectionPoint> CollectionPoints { get; set; } = new List<RouteCollectionPoint>();
  }

  public class RouteCollectionPoint
  {
    public Guid Id { get; set; }
    public string BinPlateId { get; set; }
    public string Location { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public int OrderInSchedule { get; set; }
    public bool IsCollected { get; set; }
    public DateTime? CollectedAt { get; set; }
    public decimal FillLevel { get; set; }
    public string Status => IsCollected ? "collected" : "pending";
  }
}

