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
    public DateTime ScheduleStartTime { get; set; }

    public DateTime ScheduleEndTime { get; set; }
    public DateTime? ActualStartTime { get; set; }
    public DateTime? ActualEndTime { get; set; }

    [StringLength(50)]
    public string Status { get; set; } = "Scheduled";

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

    // Navigation Properties
    [ForeignKey("CollectorId")]
    public virtual User Collector { get; set; }

    [ForeignKey("TruckId")]
    public virtual Truck Truck { get; set; }

    [ForeignKey("RouteId")]
    public virtual RoutePlan Route { get; set; }

    public virtual ICollection<CollectionPoint> CollectionPoints { get; set; }
  }
}
