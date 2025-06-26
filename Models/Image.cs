using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspnetCoreMvcFull.Models
{
  public class Image
  {
      [Key]
      public Guid Id { get; set; }

      [Required]
      public Guid CollectionRecordId { get; set; }

      [Required]
      public byte[] ImageData { get; set; }

      [Required]
      public DateTime CapturedAt { get; set; }

      [Required]
      public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

      [ForeignKey(nameof(CollectionRecordId))]
      public virtual CollectionRecord CollectionRecord { get; set; }
    }

  }

