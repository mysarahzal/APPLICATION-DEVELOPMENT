using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspnetCoreMvcFull.Models
{
  public class RoutePlan
  {
    [Key]
    public Guid Id { get; set; }  // Primary Key

    [Required(ErrorMessage = "Route name is required")]
    [StringLength(255, ErrorMessage = "Route name cannot exceed 255 characters.")]
    public string Name { get; set; }  // Name of the route

    public string? Description { get; set; }  // Make nullable - not required

    [Required(ErrorMessage = "Expected duration is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Expected duration must be greater than 0.")]
    public int ExpectedDurationMinutes { get; set; }  // Expected duration of the route in minutes

    public DateTime CreatedAt { get; set; }  // Remove [Required]

    public DateTime UpdatedAt { get; set; }  // Remove [Required]

    // Navigation property for the related RouteBins - Initialize to avoid validation issues
    public virtual ICollection<RouteBins> RouteBins { get; set; } = new List<RouteBins>();
  }
}
