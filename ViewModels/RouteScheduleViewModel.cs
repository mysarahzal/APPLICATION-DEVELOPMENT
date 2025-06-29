namespace AspnetCoreMvcFull.Models.ViewModels
{
  public class RouteScheduleViewModel
  {
    public List<ScheduleOption> AvailableSchedules { get; set; } = new List<ScheduleOption>();
    public int? SelectedScheduleId { get; set; }
  }

  public class ScheduleOption
  {
    public int ScheduleId { get; set; }
    public int TruckId { get; set; }
    public string TruckName { get; set; }
    public string RouteName { get; set; }
    public DateTime ScheduleStartTime { get; set; }
    public string Status { get; set; }
    public int CollectionPointsCount { get; set; }
  }

  public class CollectionPointData
  {
    public int ScheduleId { get; set; }
    public int TruckId { get; set; }
    public string TruckName { get; set; }
    public string RouteName { get; set; }
    public List<CollectionPointInfo> CollectionPoints { get; set; } = new List<CollectionPointInfo>();
  }

  public class CollectionPointInfo
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
  }
}
