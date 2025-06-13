using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspnetCoreMvcFull.Models
{
  public class Alert
  {
    public int Id { get; set; }
    public string Type { get; set; }
    public int SourceId { get; set; }
    public string Message { get; set; }
    public DateTime TriggeredAt { get; set; }
    public string Severity { get; set; }
    public string Status { get; set; }
    public string? AcknowledgeByUserId { get; set; }
    public DateTime? AcknowledgeAt { get; set; }
    public DateTime CreatedAt { get; set; }
  }
}
