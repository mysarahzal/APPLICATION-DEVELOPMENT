using System.ComponentModel.DataAnnotations.Schema;
using System;

namespace AspnetCoreMvcFull.Models
{
  public class Truck
  {
    public Guid Id { get; set; }
    public string LicensePlate { get; set; }

    [Column(TypeName = "decimal(18,2)")]  // Precision and scale for CapacityTon
    public decimal CapacityTon { get; set; }

    public string Make { get; set; }
    public string Model { get; set; }
    public string Status { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation Property
    public ICollection<CollectionRecord> CollectionRecords { get; set; }
  }
}




