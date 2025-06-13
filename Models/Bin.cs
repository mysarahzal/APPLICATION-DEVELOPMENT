namespace AspnetCoreMvcFull.Models
{
  public class Bin
  {
    public Guid Id { get; set; }
    public string BinPlateId { get; set; } // BIN plate number for validation
    public string Location { get; set; }
    public decimal FillLevel { get; set; } // Percentage filled
    public string Zone { get; set; }

    // Navigation Properties (optional for now)
    public ICollection<CollectionPoint> CollectionPoints { get; set; }
  }
}
