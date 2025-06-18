using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.ViewModels;
using Microsoft.EntityFrameworkCore;
using AspnetCoreMvcFull.Data;

namespace AspnetCoreMvcFull.Controllers;

public class DashboardsController : Controller
{
  private readonly KUTIPDbContext _context;

  public DashboardsController(KUTIPDbContext context)
  {
    _context = context;
  }

  public IActionResult Index()
  {
    var vm = new DashboardViewModel
    {
      CollectorCount = _context.Users.Count(u => u.Role.ToLower() == "collector"),
      DriverCount = _context.Users.Count(u => u.Role.ToLower() == "driver"),
    };

    return View(vm);
  }
}

