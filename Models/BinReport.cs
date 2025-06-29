using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace AspnetCoreMvcFull.Models
{
  public class BinReport
  {
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid BinId { get; set; }  // Foreign Key to Bin

    [Required]
    public DateTime ReportedAt { get; set; }

    [Required]
    [StringLength(50)]
    public string Status { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please provide a description")]
    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string Description { get; set; } = string.Empty;

    // Optional: Keep this if you want to manually flag issues
    public bool IsIssueReported { get; set; } = false;

    // NEW: Add Severity field
    [Required]
    [StringLength(20)]
    [Display(Name = "Severity")]
    public string Severity { get; set; } = "Medium"; // Critical, High, Medium, Low

    // NEW: Add user tracking fields
    [StringLength(100)]
    [Display(Name = "Reported By")]
    public string? ReportedBy { get; set; }

    [StringLength(100)]
    [Display(Name = "Acknowledged By")]
    public string? AcknowledgedBy { get; set; }

    [Display(Name = "Acknowledged At")]
    public DateTime? AcknowledgedAt { get; set; }

    // Navigation Properties
    public virtual Bin Bin { get; set; }
    public virtual ICollection<BinReportImage> BinReportImages { get; set; } = new List<BinReportImage>();

    // Property for file upload (not saved to DB)
    [NotMapped]
    public IFormFile? ImageFile { get; set; }
  }
}
