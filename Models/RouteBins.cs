using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspnetCoreMvcFull.Models
{
  public class RouteBins
  {
    [Key]
    public Guid Id { get; set; }  // Primary Key

    [Required]
    public Guid RouteId { get; set; }  // FIXED: Changed from int to Guid

    [Required]
    public Guid BinId { get; set; }  // Foreign Key to Bins

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Order in route must be greater than 0.")]
    public int OrderInRoute { get; set; }  // Order of the bin in the route

    // Navigation properties
    [ForeignKey("RouteId")]
    public virtual RoutePlan RoutePlan { get; set; }  // FIXED: Changed from Route to RoutePlan

    [ForeignKey("BinId")]
    public virtual Bin Bin { get; set; }  // Navigation to Bins
  }
}
