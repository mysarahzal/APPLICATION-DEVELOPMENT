namespace AspnetCoreMvcFull.Models
{
  public class Image
  {
    public Guid Id { get; set; }
    public Guid CollectionRecordId { get; set; }
    public string ImageUrl { get; set; }  // Make non-nullable
    public string ImageType { get; set; }  // Make non-nullable
    public DateTime CapturedAt { get; set; }
    public DateTime CreatedAt { get; set; }
  }
}

