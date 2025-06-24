using System.ComponentModel.DataAnnotations;

namespace AspnetCoreMvcFull.Models
{
  public class Schedule
  {
    public int Id { get; set; }  // No nullable
    public int ScheduleId { get; set; }
    public int TruckId { get; set; }  // No nullable

    [Required]
    public int CollectorId { get; set; }  // No nullable

    [Required]
    public int RouteId { get; set; }  // No nullable, ensure the type matches the primary key of Route

    [Required]
    public DateTime ScheduleStartTime { get; set; }  // No nullable
    public DateTime ScheduleEndTime { get; set; }  // No nullable
    //public DateTime ScheduledDay { get; set; }
    public DateTime ActualStartTime { get; set; }  // No nullable
    public DateTime ActualEndTime { get; set; }  // No nullable
    public string Status { get; set; }  // No nullable
    public string AdminNotes { get; set; }  // No nullable
    public DateTime CreatedAt { get; set; }  // No nullable
    public DateTime UpdatedAt { get; set; }  // No nullable

    // Navigation Properties
    public virtual User Collector { get; set; }
    //public virtual User User { get; set; }  // No nullable
    public virtual Road Road { get; set; }  // No nullable
    public virtual Truck Truck { get; set; }
    public virtual ICollection<CollectionPoint> CollectionPoints { get; set; }  // No nullable
  }
}
