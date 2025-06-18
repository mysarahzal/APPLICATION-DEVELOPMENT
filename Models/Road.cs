using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspnetCoreMvcFull.Models
{
  public class Road
  {
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
  }
}


