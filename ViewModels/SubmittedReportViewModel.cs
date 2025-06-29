namespace AspnetCoreMvcFull.ViewModels
{
  public class SubmittedReportViewModel
  {
    public Guid CollectionRecordId { get; set; }
    public string BinPlateId { get; set; } = string.Empty;
    public string BinLocation { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public DateTime PickupTimestamp { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsIssueReported { get; set; }
    public string? ImageUrl { get; set; }
    public string? FileName { get; set; }
    public long FileSize { get; set; }

    // NEW: Add Severity and user tracking fields
    public string Severity { get; set; } = string.Empty;
    public string? ReportedBy { get; set; }
    public string? AcknowledgedBy { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
  }
}
