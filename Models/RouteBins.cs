using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspnetCoreMvcFull.Models
{
  public class RouteBins
  {
    [Key]
    public Guid Id { get; set; }

    [ForeignKey("RoutePlan")]
    public Guid RouteId { get; set; }

    [ForeignKey("Bin")]
    public Guid BinId { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Order in route must be greater than 0.")]
    public int OrderInRoute { get; set; }

    public virtual RoutePlan RoutePlan { get; set; }
    public virtual Bin Bin { get; set; }
  }
}


