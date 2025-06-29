using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspnetCoreMvcFull.Models
{
  public class BinReportImage
  {
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid BinReportId { get; set; }  // Foreign Key to BinReport

    [StringLength(500)]
    public string? ImagePath { get; set; }  // For file system storage

    public byte[]? ImageData { get; set; }  // For database storage (alternative)

    [Required]
    public DateTime CapturedAt { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; }

    [StringLength(100)]
    public string? FileName { get; set; }  // Original file name

    [StringLength(50)]
    public string? ContentType { get; set; }  // MIME type

    public long FileSize { get; set; }  // File size in bytes

    // Navigation Property
    public virtual BinReport BinReport { get; set; }
  }
}
