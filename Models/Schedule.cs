using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspnetCoreMvcFull.Models
{
  public class Schedule
  {
    public int Id { get; set; }
    public int TruckId { get; set; }
    public int CollectorId { get; set; }
    public int RouteId { get; set; }
    public DateTime ScheduleStartTime { get; set; }
    public DateTime ScheduleEndTime { get; set; }
    public DateTime? actual_start_time { get; set; }
    public DateTime? actual_end_time { get; set; }
    public string Status { get; set; } // "Scheduled", "Completed", "Missed"
    public string? AdminNotes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? updated_at { get; set; }
    public DateTime? actual_start_time { get; set; }
    public DateTime? actual_end_time { get; set; }

    // Navigation Properties
    public virtual User User { get; set; }
    public virtual Road Route { get; set; }
    public virtual ICollection<CollectionPoint> CollectionPoints { get; set; }
  }
}
