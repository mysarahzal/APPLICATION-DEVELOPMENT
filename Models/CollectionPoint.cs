namespace AspnetCoreMvcFull.Models
{
  public class CollectionPoint
  {
    public int Id { get; set; }
    public int ScheduleId { get; set; }  // No nullable, matching the primary key type of Schedule
    public int BinId { get; set; }     // No nullable
    public int OrderInSchedule { get; set; }  // No nullable
    public bool IsCollected { get; set; }    // No nullable
    public DateTime CollectedAt { get; set; }  // No nullable

    // Navigation Properties
    public virtual Schedule Schedule { get; set; }  // No nullable
    public virtual Bin Bin { get; set; }           // No nullable
    public virtual ICollection<CollectionRecord> CollectionRecords { get; set; }
  }
}










