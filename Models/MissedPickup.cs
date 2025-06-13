using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspnetCoreMvcFull.Models
{
  public class MissedPickup
  {
    public int Id { get; set; }
    public int ScheduleId { get; set; }
    public string? ReportedBy { get; set; }
    public DateTime DetectedAt { get; set; }
    public string? Reason { get; set; }
    public string? Resolution { get; set; }
    public string Status { get; set; } // Pending, Resolved
    public DateTime CreatedAt { get; set; }

    public virtual Schedule Schedule { get; set; }
  }
}
