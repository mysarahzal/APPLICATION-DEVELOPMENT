using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AspnetCoreMvcFull.Models
{
  public class Client
  {
    public int ClientID { get; set; }

    [Required(ErrorMessage = "Client name is required")]
    [StringLength(100, ErrorMessage = "Client name cannot exceed 100 characters")]
    [Display(Name = "Client Name")]
    public string ClientName { get; set; }

    [Required(ErrorMessage = "Location is required")]
    [StringLength(200, ErrorMessage = "Location cannot exceed 200 characters")]
    public string Location { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Number of bins must be a positive number")]
    [Display(Name = "Number of Bins")]
    public int NumOfBins { get; set; }

    // Navigation property for related bins (One-to-many relationship)
    public virtual ICollection<Bin> Bins { get; set; } = new List<Bin>();
  }
}









