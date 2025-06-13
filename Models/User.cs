using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspnetCoreMvcFull.Models
{
  public class User
  {
    public int Id { get; set; }

    [Required, EmailAddress]
    public string Email { get; set; }

    [Required]
    public string Password { get; set; }

    [Required]
    public string FirstName { get; set; }

    [Required]
    public string LastName { get; set; }

    [Required]
    public string Role { get; set; } // Admin, Driver, Operator

    [Phone]
    public string PhoneNumber { get; set; }

    public DateTime CreatedAt { get; set; }
  }
}





