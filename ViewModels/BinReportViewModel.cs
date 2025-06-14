using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace AspnetCoreMvcFull.ViewModels
{
  public class BinReportViewModel
  {
    [Required]
    public Guid BinId { get; set; }  // Bin ID to identify the bin

    public string? IssueDescription { get; set; }  // Nullable, Description of any issue reported

    [Required]
    public IFormFile ImageFile { get; set; }  // Make ImageFile required (Non-nullable in this case)

    public string? Description { get; set; }  // Nullable, Additional description if needed
  }
}




