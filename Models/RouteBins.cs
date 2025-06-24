using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspnetCoreMvcFull.Models
{
  public class RouteBins
  {
    [Key]
    public Guid Id { get; set; }  // Primary Key

    [ForeignKey("Bin")]
    public Guid BinId { get; set; }  // Foreign Key to Bins

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Order in route must be greater than 0.")]
    public int OrderInRoute { get; set; }  // Order of the bin in the route

    // Navigation properties
    public virtual Route Route { get; set; }  // Navigation to Road
    public virtual Bin Bin { get; set; }  // Navigation to Bins
  }
}
