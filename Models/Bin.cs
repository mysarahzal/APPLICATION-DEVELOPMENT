using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspnetCoreMvcFull.Models
{
  public class Bin
  {
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required(ErrorMessage = "Bin Plate ID is required")]
    [StringLength(50, ErrorMessage = "Bin Plate ID cannot exceed 50 characters")]
    [Display(Name = "Bin Plate ID")]
    public string BinPlateId { get; set; }

    [Required(ErrorMessage = "Location is required")]
    [StringLength(200, ErrorMessage = "Location cannot exceed 200 characters")]
    public string Location { get; set; }

    [Range(0, 100, ErrorMessage = "Fill level must be between 0 and 100")]
    [Column(TypeName = "decimal(5,2)")]
    [Display(Name = "Fill Level (%)")]
    public decimal FillLevel { get; set; }

    [Required(ErrorMessage = "Zone is required")]
    [StringLength(50, ErrorMessage = "Zone cannot exceed 50 characters")]
    public string Zone { get; set; }

    [Column(TypeName = "decimal(10,8)")]
    [Display(Name = "Latitude")]
    public decimal? Latitude { get; set; }

    [Column(TypeName = "decimal(11,8)")]
    [Display(Name = "Longitude")]
    public decimal? Longitude { get; set; }

    // Foreign Key and Navigation Property for the related Client
    [Required(ErrorMessage = "Please select a client")]
    [Display(Name = "Client")]
    public int ClientID { get; set; }

    public virtual Client Client { get; set; }

    // Navigation Properties (if you have these tables)
    public virtual ICollection<CollectionPoint> CollectionPoints { get; set; } = new List<CollectionPoint>();
    public virtual ICollection<CollectionRecord> CollectionRecords { get; set; } = new List<CollectionRecord>();

    // ADD THIS: Missing navigation property for RouteBins
    public virtual ICollection<RouteBins> RouteBins { get; set; } = new List<RouteBins>();
  }
}
