using System.ComponentModel.DataAnnotations.Schema;
using System;

namespace AspnetCoreMvcFull.Models
{
  public class Truck
  {
    public int Id { get; set; }  // Changed from Guid to int
    public string LicensePlate { get; set; }
    public string Model { get; set; }
    public string Status { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation Property
    public ICollection<CollectionRecord> CollectionRecords { get; set; }
  }
}
