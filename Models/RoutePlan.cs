using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspnetCoreMvcFull.Models
{
  public class RoutePlan
  {
    [Key]
    public Guid Id { get; set; }  // Primary Key

    [Required]
    [StringLength(255, ErrorMessage = "Route name cannot exceed 255 characters.")]
    public string Name { get; set; }  // Name of the route

    public string Description { get; set; }  // Description of the route

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Expected duration must be greater than 0.")]
    public int ExpectedDurationMinutes { get; set; }  // Expected duration of the route in minutes

    [Required]
    public DateTime CreatedAt { get; set; }  // Date when the route was created

    [Required]
    public DateTime UpdatedAt { get; set; }  // Date when the route was last updated

    // Navigation property for the related RouteBins
    public virtual ICollection<RouteBins> RouteBins { get; set; }
  }
}

