using System.ComponentModel.DataAnnotations;

namespace AspnetCoreMvcFull.ViewModels
{
  public class EditUserViewModel
  {
    public int Id { get; set; }

    [Required, EmailAddress]
    public string Email { get; set; }

    [Required]
    public string FirstName { get; set; }

    [Required]
    public string LastName { get; set; }

    [Required]
    public string Role { get; set; }

    [Phone]
    public string PhoneNumber { get; set; }
  }
}
