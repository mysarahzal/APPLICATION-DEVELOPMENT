using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspnetCoreMvcFull.Models
{
  public class PickupReport
  {
    public int Id { get; set; }
    public int ScheduleId { get; set; }
    public DateTime PickupTime { get; set; }
    public string Status { get; set; }
    public string? Notes { get; set; }
  }
}
