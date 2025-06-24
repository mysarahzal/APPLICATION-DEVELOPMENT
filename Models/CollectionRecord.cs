using System.ComponentModel.DataAnnotations.Schema;
using System;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AspnetCoreMvcFull.Models
{
  [Index(nameof(CollectionPointId))]
  [Index(nameof(BinId))]
  public class CollectionRecord
  {
    [Key]
    public Guid Id { get; set; }

    // Foreign Keys
    [Required]
    public Guid BinId { get; set; }

    [Required]
    public Guid CollectionPointId { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    public int TruckId { get; set; }  // Changed from Guid to int to match Truck.Id

    // Navigation Properties
    [ForeignKey("CollectionPointId")]
    public virtual CollectionPoint CollectionPoint { get; set; }

    [ForeignKey("BinId")]
    public virtual Bin Bin { get; set; }

    [ForeignKey("UserId")]
    public virtual User User { get; set; }

    [ForeignKey("TruckId")]
    public virtual Truck Truck { get; set; }

    [Required]
    public DateTime PickupTimestamp { get; set; }

    [Column(TypeName = "decimal(18,6)")]
    public decimal GpsLatitude { get; set; }

    [Column(TypeName = "decimal(18,6)")]
    public decimal GpsLongitude { get; set; }

    public string BinPlateIdCaptured { get; set; } = string.Empty;

    public bool IssueReported { get; set; }

    public string IssueDescription { get; set; } = string.Empty;

    public virtual ICollection<Image> Images { get; set; }

    // Constructor
    public CollectionRecord()
    {
      Images = new List<Image>();
      BinPlateIdCaptured = string.Empty;
      IssueDescription = string.Empty;
    }
  }
}
