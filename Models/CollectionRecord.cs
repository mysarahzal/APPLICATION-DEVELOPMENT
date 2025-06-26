using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspnetCoreMvcFull.Models
{
  public class CollectionRecord
  {
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid BinId { get; set; }

    [Required]
    public Guid CollectionPointId { get; set; }

    [Required]
    public int CollectorId { get; set; }

    [Required]
    public int TruckId { get; set; }

    [Required]
    public DateTime PickupTimestamp { get; set; }

    [Column(TypeName = "decimal(18,6)")]
    public decimal GpsLatitude { get; set; }

    [Column(TypeName = "decimal(18,6)")]
    public decimal GpsLongitude { get; set; }

    public string? BinPlateIdCaptured { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    [ForeignKey(nameof(BinId))]
    public virtual Bin Bin { get; set; }

    [ForeignKey(nameof(CollectionPointId))]
    public virtual CollectionPoint CollectionPoint { get; set; }

    [ForeignKey(nameof(CollectorId))]
    public virtual User Collector { get; set; }

    [ForeignKey(nameof(TruckId))]
    public virtual Truck Truck { get; set; }

    public virtual Image Image { get; set; }
  }
}
