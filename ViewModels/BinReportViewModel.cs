using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace AspnetCoreMvcFull.ViewModels
{
  public class BinReportViewModel
  {
    [Required(ErrorMessage = "Bin Plate ID is required")]
    [Display(Name = "Bin Plate ID")]
    public string BinPlateId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please provide a description")]
    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    [Display(Name = "Description")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select an image file")]
    [Display(Name = "Upload Image")]
    public IFormFile ImageFile { get; set; }

    [Display(Name = "Mark as Issue")]
    public bool IsIssueReported { get; set; } = false;

    // NEW: Add Severity field
    [Required]
    [Display(Name = "Severity Level")]
    public string Severity { get; set; } = "Medium";
  }
}
