using AspnetCoreMvcFull.Models;
using System.Collections.Generic;

namespace AspnetCoreMvcFull.Models.ViewModels
{
  public class ScheduleCreateViewModel
  {
    // Main model
    public Schedule Schedule { get; set; }

    // Dropdown options for trucks
    public List<(int Id, string Name)> Trucks { get; set; }

    // Dropdown options for routes
    public List<(int Id, string Name)> Routes { get; set; }

    // Dropdown options for statuses
    public List<string> Statuses { get; set; }

    // List of collectors/drivers
    public List<User> Collectors { get; set; }
  }
}
