namespace AspnetCoreMvcFull.Models
{
  public class Road
  {
    public Guid Id { get; set; }

    // Make Name and Description non-nullable
    public string Name { get; set; }  // No nullable
    public string Description { get; set; }  // No nullable
  }
}


