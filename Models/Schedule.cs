using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspnetCoreMvcFull.Models
{
  public class Schedule
  {
    public int Id { get; set; }
    public int TruckId { get; set; }
    public int CollectorId { get; set; }
    public DateTime ScheduleStartTime { get; set; }
    public DateTime ScheduleEndTime { get; set; }
    public string Status { get; set; }
    public string? AdminNotes { get; set; }
    public DateTime CreatedAt { get; set; }
  }
}
