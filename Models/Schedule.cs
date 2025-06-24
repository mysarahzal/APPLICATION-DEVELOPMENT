using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspnetCoreMvcFull.Models
{
  public class Schedule
  {
    [Key]
    public int Id { get; set; }  // Primary key - remove ScheduleId as it's redundant

    [Required]
    public int TruckId { get; set; }  // Foreign key to Truck - already correct as int

    [Required]
    public int CollectorId { get; set; }  // Foreign key to User

    [Required]
    public int RouteId { get; set; }  // Foreign key to Route

    [Required]
    public DateTime ScheduleStartTime { get; set; }

    public DateTime ScheduleEndTime { get; set; }
    public DateTime? ActualStartTime { get; set; }  // Made nullable since it might not be set initially
    public DateTime? ActualEndTime { get; set; }    // Made nullable since it might not be set initially

    [StringLength(50)]
    public string Status { get; set; } = "Scheduled";

    public string AdminNotes { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; }

    [Required]
    public DateTime UpdatedAt { get; set; }

    // Navigation Properties with proper Foreign Key attributes
    [ForeignKey("CollectorId")]
    public virtual User Collector { get; set; }

    [ForeignKey("TruckId")]
    public virtual Truck Truck { get; set; }

    [ForeignKey("RouteId")]
    public virtual Route Route { get; set; }

    public virtual ICollection<CollectionPoint> CollectionPoints { get; set; }
  }
}
