using System.ComponentModel.DataAnnotations;

namespace AspnetCoreMvcFull.Models
{
  public class User
  {
    public int Id { get; set; }

    [Required, EmailAddress]
    public string Email { get; set; }  // No nullable

    [Required]
    public string Password { get; set; }  // No nullable

    [Required]
    public string FirstName { get; set; }  // No nullable

    [Required]
    public string LastName { get; set; }  // No nullable

    [Required]
    public string Role { get; set; } // Admin, Driver, Operator

    [Phone]
    public string PhoneNumber { get; set; }  // No nullable

    public DateTime CreatedAt { get; set; }  // No nullable

    public virtual ICollection<Truck> Trucks { get; set; } // For drivers
    public virtual ICollection<Schedule> DriverSchedules { get; set; }
    public virtual ICollection<CollectionRecord> CollectionRecords { get; set; }
  }
}





