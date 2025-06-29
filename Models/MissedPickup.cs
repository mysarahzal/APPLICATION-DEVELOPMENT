using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspnetCoreMvcFull.Models
{
    public class MissedPickup
    {
        public int Id { get; set; }

        [Required]
        public int ScheduleId { get; set; }

        [Required]
        [Display(Name = "Detected At")]
        public DateTime DetectedAt { get; set; }

        [StringLength(500)]
        [Display(Name = "Reason")]
        public string? Reason { get; set; }

        [StringLength(500)]
        [Display(Name = "Resolution")]
        public string? Resolution { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending"; // Pending, Resolved, Cancelled

        [Display(Name = "Resolved At")]
        public DateTime? ResolvedAt { get; set; }

        [StringLength(100)]
        [Display(Name = "Resolved By")]
        public string? ResolvedBy { get; set; }

        [Required]
        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation Property
        [ForeignKey(nameof(ScheduleId))]
        public virtual Schedule? Schedule { get; set; }
    }
}
