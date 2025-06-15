using System.ComponentModel.DataAnnotations.Schema;
using System;

namespace AspnetCoreMvcFull.Models
{
  public class CollectionRecord
  {
    public Guid Id { get; set; }

    // Foreign Keys
    public Guid BinId { get; set; }
    public Guid CollectionPointId { get; set; }
    public int UserId { get; set; }
    public Guid TruckId { get; set; }

    public DateTime PickupTimestamp { get; set; }

    [Column(TypeName = "decimal(18,6)")]  // Precision and scale for latitude
    public decimal GpsLatitude { get; set; }

    [Column(TypeName = "decimal(18,6)")]  // Precision and scale for longitude
    public decimal GpsLongitude { get; set; }

    public string BinPlateIdCaptured { get; set; }
    public bool IssueReported { get; set; }
    public string IssueDescription { get; set; }

    // Navigation Properties
    public virtual CollectionPoint CollectionPoint { get; set; }
    public virtual Bin Bin { get; set; }
    public virtual User User { get; set; }
    public virtual Truck Truck { get; set; }
    public virtual ICollection<Image> Images { get; set; }

    // Constructor to initialize navigation properties
    public CollectionRecord()
    {
      CollectionPoint = new CollectionPoint();
      Bin = new Bin();
      User = new User();
      Truck = new Truck();
      Images = new List<Image>();
      BinPlateIdCaptured = string.Empty;
      IssueDescription = string.Empty;
    }
  }
}







