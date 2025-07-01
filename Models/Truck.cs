using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;

namespace AspnetCoreMvcFull.Models
{
  public class Truck
  {
    public int Id { get; set; }

    [Required]
    [Display(Name = "License Plate")]
    public string LicensePlate { get; set; }

    [Required]
    public string Model { get; set; }

    public string Status { get; set; }

    [Required]
    [Display(Name = "Assigned Driver")]
    public int DriverId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation Properties
    [ForeignKey("DriverId")]
    public virtual User Driver { get; set; }

    public ICollection<CollectionRecord> CollectionRecords { get; set; }
  }
}
