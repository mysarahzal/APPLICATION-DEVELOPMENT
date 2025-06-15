using AspnetCoreMvcFull.Models;
using System.Collections.Generic;

namespace AspnetCoreMvcFull.ViewModels
{
  public class ManageClientsBinsViewModel
  {
    public IEnumerable<Client> Clients { get; set; }
    public IEnumerable<Bin> Bins { get; set; }
    public Client Client { get; set; }  // For Client Creation
    public Bin Bin { get; set; }      // For Bin Creation
  }
}


