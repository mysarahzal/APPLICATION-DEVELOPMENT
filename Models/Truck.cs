namespace AspnetCoreMvcFull.Models
{
  public class Truck
  {
    public Guid Id { get; set; }
    public string license_plate { get; set; }
    public decimal capacity_ton { get; set; }
    public string make { get; set; }
    public string model { get; set; }
    public string status { get; set; } // Active / In Maintenance
    public DateTime created_at { get; set; }
    public DateTime updated_at { get; set; }

    // Navigation Property
    public ICollection<CollectionRecord> CollectionRecords { get; set; }
  }
}
