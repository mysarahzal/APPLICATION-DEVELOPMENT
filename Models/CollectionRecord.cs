using System.Diagnostics;

namespace AspnetCoreMvcFull.Models
{
  public class CollectionRecord
  {
    public Guid Id { get; set; }
    public Guid CollectionPointId { get; set; }
    public string BinPlateId { get; set; }
    public Guid UserId { get; set; }
    public Guid TruckId { get; set; }
    public DateTime PickupTimestamp { get; set; }
    public string BinPlateIdCaptured { get; set; }
    public bool IssueReported { get; set; }
    public string IssueDescription { get; set; }

    // Navigation Properties
    public virtual CollectionPoint CollectionPoint { get; set; }
    public virtual User User { get; set; }
    public virtual Truck Truck { get; set; }
  }
}
