using System.ComponentModel.DataAnnotations.Schema;
using System;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace AspnetCoreMvcFull.Models
{
  [Index(nameof(CollectionPointId))]
  [Index(nameof(BinId))]
  public class CollectionRecord
  {
    public Guid Id { get; set; }

    // Foreign Keys
    public Guid BinId { get; set; }  // Change BinId to Guid (from int to Guid)
    public Guid CollectionPointId { get; set; }  // Ensure CollectionPointId is Guid

    // Navigation Properties
    [DeleteBehavior(DeleteBehavior.Restrict)]
    [ForeignKey("CollectionPointId")]
    public virtual CollectionPoint CollectionPoint { get; set; }

    [DeleteBehavior(DeleteBehavior.Restrict)]
    [ForeignKey("BinId")]
    public virtual Bin Bin { get; set; }

    public int UserId { get; set; }
    [DeleteBehavior(DeleteBehavior.Restrict)]
    [ForeignKey("UserId")]
    public virtual User User { get; set; }

    public Guid TruckId { get; set; }
    [DeleteBehavior(DeleteBehavior.Restrict)]
    [ForeignKey("TruckId")]
    public virtual Truck Truck { get; set; }

    public DateTime PickupTimestamp { get; set; }

    [Column(TypeName = "decimal(18,6)")]
    public decimal GpsLatitude { get; set; }

    [Column(TypeName = "decimal(18,6)")]
    public decimal GpsLongitude { get; set; }

    public string BinPlateIdCaptured { get; set; }
    public bool IssueReported { get; set; }
    public string IssueDescription { get; set; }

    public virtual ICollection<Image> Images { get; set; }

    // Constructor
    public CollectionRecord()
    {
      Bin = new Bin();
      User = new User();
      Truck = new Truck();
      Images = new List<Image>();
      BinPlateIdCaptured = string.Empty;
      IssueDescription = string.Empty;
    }
  }
}
