namespace AspnetCoreMvcFull.Models
{
  public class CollectionPoint
  {
    public Guid Id { get; set; }
    public Guid ScheduleId { get; set; }
    public Guid BinId { get; set; }
    public int OrderInSchedule { get; set; }
    public bool IsCollected { get; set; }
    public DateTime? CollectedAt { get; set; }

    // Navigation Properties
    public virtual Schedule Schedule { get; set; }
    public virtual Bin Bin { get; set; }
  }
}
