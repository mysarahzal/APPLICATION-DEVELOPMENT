using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspnetCoreMvcFull.Models
{
  public class Route
  {
    [Key]
    public int Id { get; set; }  // Changed from Guid to int

    [Required]
    [StringLength(255, ErrorMessage = "Route name cannot exceed 255 characters.")]
    public string Name { get; set; }

    public string Description { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Expected duration must be greater than 0.")]
    public int ExpectedDurationMinutes { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; }

    [Required]
    public DateTime UpdatedAt { get; set; }

    // Navigation property for the related RouteBins
    public virtual ICollection<RouteBins> RouteBins { get; set; }
  }
}
