using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspnetCoreMvcFull.Models
{
  public class BinReport
  {
    public int Id { get; set; }
    public int BinId { get; set; }  // Foreign Key to Bin
    public DateTime ReportedAt { get; set; }

    public string Status { get; set; }  // Not nullable anymore
    public string Description { get; set; }  // Not nullable anymore
    public bool IsIssueReported { get; set; }
    public string IssueDescription { get; set; }  // Not nullable anymore

    // Navigation Property to link to Bin
    public Bin Bin { get; set; }  // Not nullable anymore

    // Property for file upload (not saved to DB, only used in the controller)
    [NotMapped]  // Ignore this property during migration
    public IFormFile ImageFile { get; set; }  // Not nullable anymore
  }
}











