using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace AspnetCoreMvcFull.Models
{
  public class CollectionPoint
  {
    public Guid Id { get; set; }

    public int ScheduleId { get; set; }

    public Guid BinId { get; set; }

    public int OrderInSchedule { get; set; }

    public bool IsCollected { get; set; }

    public DateTime? CollectedAt { get; set; }

    // Add latitude and longitude from bin
    [Column(TypeName = "decimal(10,8)")]
    [Display(Name = "Latitude")]
    public decimal? Latitude { get; set; }

    [Column(TypeName = "decimal(11,8)")]
    [Display(Name = "Longitude")]
    public decimal? Longitude { get; set; }

    [ForeignKey(nameof(ScheduleId))]
    public virtual Schedule Schedule { get; set; }

    [ForeignKey(nameof(BinId))]
    public virtual Bin Bin { get; set; }

    public virtual ICollection<CollectionRecord> CollectionRecords { get; set; }
  }
}
