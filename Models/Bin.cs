using System.ComponentModel.DataAnnotations.Schema;
using System;

namespace AspnetCoreMvcFull.Models
{
  public class Bin
  {
    public Guid Id { get; set; }
    public string BinPlateId { get; set; }
    public string Location { get; set; }

    [Column(TypeName = "decimal(18,2)")]  // Define precision and scale for FillLevel
    public decimal FillLevel { get; set; }  // Percentage filled

    public string Zone { get; set; }

    // Navigation Properties
    public ICollection<CollectionPoint> CollectionPoints { get; set; }
  }
}





