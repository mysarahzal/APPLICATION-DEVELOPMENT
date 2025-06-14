namespace AspnetCoreMvcFull.Models
{
  public class PickupReport
  {
    public int Id { get; set; }
    public int ScheduleId { get; set; }  // No nullable
    public DateTime PickupTime { get; set; }  // No nullable
    public string Status { get; set; }  // No nullable
    public string Notes { get; set; }  // No nullable
  }
}
