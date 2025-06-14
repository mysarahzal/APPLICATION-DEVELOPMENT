namespace AspnetCoreMvcFull.ViewModels
{
  public class SubmittedReportViewModel
  {
    public Guid CollectionRecordId { get; set; }
    public string? BinPlateId { get; set; }  // Make BinPlateId nullable
    public DateTime PickupTimestamp { get; set; }
    public decimal GpsLatitude { get; set; }
    public decimal GpsLongitude { get; set; }
    public string? CollectorEmail { get; set; }  // Make CollectorEmail nullable
    public string? TruckLicensePlate { get; set; }  // Make TruckLicensePlate nullable
    public bool IssueReported { get; set; }
    public string? IssueDescription { get; set; }  // Make IssueDescription nullable
    public string? ImageUrl { get; set; }  // Make ImageUrl nullable
  }
}



