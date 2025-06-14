namespace AspnetCoreMvcFull.Models
{
  public class MissedPickup
  {
    public int Id { get; set; }
    public int ScheduleId { get; set; }
    public string ReportedBy { get; set; }  // Make non-nullable
    public DateTime DetectedAt { get; set; }
    public string Reason { get; set; }  // Make non-nullable
    public string Resolution { get; set; }  // Make non-nullable
    public string Status { get; set; } // No nullable (Pending, Resolved)
    public DateTime CreatedAt { get; set; }

    // Navigation Property
    public virtual Schedule Schedule { get; set; }  // No nullable
  }
}





