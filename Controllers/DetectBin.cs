using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AspnetCoreMvcFull.Controllers
{
  [Authorize(Roles = "Collector")]
  public class DetectBin : Controller
  {
    public IActionResult Index()
    {
      return View();
    }
  }
}
