using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspnetCoreMvcFull.Models
{
  public class Schedule
  {
    [Key]
    public int Id { get; set; }

    [Required]
    public int TruckId { get; set; }

    [Required]
    public int CollectorId { get; set; }

    [Required]
    public Guid RouteId { get; set; }  // Guid to match RoutePlan.Id

    [Required]
    [Display(Name = "Start Time")]
    public DateTime ScheduleStartTime { get; set; }

    [Required]
    [Display(Name = "End Time")]
    public DateTime ScheduleEndTime { get; set; }

    [Required]
    [StringLength(20)]
    [Display(Name = "Day of Week")]
    public string DayOfWeek { get; set; } // Sunday, Monday, Tuesday, etc.

    [Display(Name = "Actual Start Time")]
    public DateTime? ActualStartTime { get; set; }

    [Display(Name = "Actual End Time")]
    public DateTime? ActualEndTime { get; set; }

    [StringLength(50)]
    public string Status { get; set; } = "Scheduled"; // Manual status for completed/cancelled

    [Display(Name = "Admin Notes")]
    public string AdminNotes { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; }

    [Required]
    public DateTime UpdatedAt { get; set; }

    // Add route center coordinates (calculated from bins in route)
    [Column(TypeName = "decimal(10,8)")]
    [Display(Name = "Route Center Latitude")]
    public decimal? RouteCenterLatitude { get; set; }

    [Column(TypeName = "decimal(11,8)")]
    [Display(Name = "Route Center Longitude")]
    public decimal? RouteCenterLongitude { get; set; }

    // Computed property for automatic status based on time
    [NotMapped]
    public string AutomaticStatus
    {
      get
      {
        var now = DateTime.Now;

        // If manually set to Completed or Cancelled, keep that status
        if (Status == "Completed" || Status == "Cancelled")
        {
          return Status;
        }

        // Auto-calculate based on time
        if (now < ScheduleStartTime)
        {
          return "Scheduled";
        }
        else if (now >= ScheduleStartTime && now <= ScheduleEndTime)
        {
          return "In Progress";
        }
        else // now > ScheduleEndTime
        {
          return "Missed";
        }
      }
    }

    // Helper method to get status badge class
    [NotMapped]
    public string StatusBadgeClass
    {
      get
      {
        return AutomaticStatus switch
        {
          "Completed" => "bg-success",
          "In Progress" => "bg-warning",
          "Cancelled" => "bg-danger",
          "Missed" => "bg-danger",
          "Scheduled" => "bg-primary",
          _ => "bg-secondary"
        };
      }
    }

    // Navigation Properties
    [ForeignKey("CollectorId")]
    public virtual User Collector { get; set; }

    [ForeignKey("TruckId")]
    public virtual Truck Truck { get; set; }

    [ForeignKey("RouteId")]
    public virtual RoutePlan Route { get; set; }

    public virtual ICollection<CollectionPoint> CollectionPoints { get; set; }
    public virtual ICollection<MissedPickup> MissedPickups { get; set; }
  }
}
