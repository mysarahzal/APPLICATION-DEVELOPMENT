using Microsoft.AspNetCore.Mvc;
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.Data;
using Microsoft.EntityFrameworkCore;
using AspnetCoreMvcFull.Models.ViewModels;

namespace AspnetCoreMvcFull.Controllers
{
  public class ScheduleController : Controller
  {
    private readonly KUTIPDbContext _context;

    public ScheduleController(KUTIPDbContext context)
    {
      _context = context;
    }

    // GET: Schedule
    public async Task<IActionResult> Index()
    {
      var schedules = await _context.Schedules
          .Include(s => s.Collector)
          .Include(s => s.Road)
          .ToListAsync();
      return View("Index", schedules);
    }

    // GET: Schedule/Details/5
    public async Task<IActionResult> Details(int? id)
    {
      if (id == null) return NotFound();

      var schedule = await _context.Schedules
          .Include(s => s.Collector)
          .Include(s => s.Road)
          .FirstOrDefaultAsync(m => m.Id == id);

      if (schedule == null) return NotFound();

      return View("Details", schedule);
    }

    // GET: Schedule/Create
    public IActionResult Create()
    {
      // Initialize ViewModel
      var viewModel = new ScheduleCreateViewModel
      {
        Schedule = new Schedule(),

        // TruckId dropdown - Static list with ID and name
        Trucks = new List<(int Id, string Name)>
        {
            (1, "JSA 1283"), (2, "JSD 9471"), (3, "JSH 2208"), (4, "JSB 7733"),
            (5, "JSF 6451"), (6, "JSP 8882"), (7, "JSG 1130"), (8, "JSV 5321"),
            (9, "JSR 1109"), (10, "JTN 4427"), (11, "JTR 3001"), (12, "JTS 7182"),
            (13, "JTX 2156"), (14, "JTU 9843"), (15, "JTB 1430"), (16, "JUD 5062"),
            (17, "JUP 6245"), (18, "JUQ 7554"), (19, "JUV 1903"), (20, "JUX 8027"),
            (21, "JVF 6363"), (22, "JVM 2931"), (23, "JWA 9100"), (24, "JWJ 5742"),
            (25, "JWX 7289"), (26, "JXA 4411"), (27, "JXB 3399"), (28, "JXD 7608"),
            (29, "JXQ 8173"), (30, "JXY 2930")
        },

        // Route dropdown - Static list with ID and name
        Routes = new List<(int Id, string Name)>
        {
            (1, "JALAN SHAHBANDAR 1-3"),
            (2, "JALAN SHAHBANDAR 4"),
            (3, "JALAN SHAHBANDAR 5-8"),
            (4, "JALAN LAKSAMANA 1"),
            (5, "JALAN LAKSAMANA 2"),
            (6, "JALAN PAHLAWAN 1"),
            (7, "JALAN PAHLAWAN 2"),
            (8, "JALAN BENTARA 1"),
            (9, "JALAN BENTARA 20"),
            (10, "JALAN PERKASA 3-5"),
            (11, "POH CHEONG (JLN SEELEONG)"),
            (12, "FAMILY MART CALTEX")
        },

        // Status dropdown
        Statuses = new List<string> { "Scheduled", "Missed", "Completed" },

        // Collector & Driver users
        Collectors = _context.Users
              .Where(u => u.Role == "Driver" || u.Role == "Collector")
              .ToList()
      };

      return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ScheduleCreateViewModel viewModel)
    {
      if (ModelState.IsValid)
      {
        var schedule = viewModel.Schedule;
        schedule.CreatedAt = DateTime.Now;
        schedule.UpdatedAt = DateTime.Now;

        _context.Schedules.Add(schedule);
        await _context.SaveChangesAsync();

        System.Diagnostics.Debug.WriteLine("✅ Schedule saved successfully.");
        return RedirectToAction(nameof(Index));
      }

      System.Diagnostics.Debug.WriteLine("❌ Model state invalid.");

      // If we got this far, something failed; redisplay form
      viewModel.Collectors = _context.Users
          .Where(u => u.Role == "Driver" || u.Role == "Collector")
          .ToList();

      viewModel.Trucks = new List<(int Id, string Name)>
    {
        (1, "JSA 1283"), (2, "JSD 9471"), (3, "JSH 2208"), (4, "JSB 7733"),
        (5, "JSF 6451"), (6, "JSP 8882"), (7, "JSG 1130"), (8, "JSV 5321"),
        (9, "JSR 1109"), (10, "JTN 4427"), (11, "JTR 3001"), (12, "JTS 7182"),
        (13, "JTX 2156"), (14, "JTU 9843"), (15, "JTB 1430"), (16, "JUD 5062"),
        (17, "JUP 6245"), (18, "JUQ 7554"), (19, "JUV 1903"), (20, "JUX 8027"),
        (21, "JVF 6363"), (22, "JVM 2931"), (23, "JWA 9100"), (24, "JWJ 5742"),
        (25, "JWX 7289"), (26, "JXA 4411"), (27, "JXB 3399"), (28, "JXD 7608"),
        (29, "JXQ 8173"), (30, "JXY 2930")
    };

      viewModel.Routes = new List<(int Id, string Name)>
    {
        (1, "JALAN SHAHBANDAR 1-3"),
        (2, "JALAN SHAHBANDAR 4"),
        (3, "JALAN SHAHBANDAR 5-8"),
        (4, "JALAN LAKSAMANA 1"),
        (5, "JALAN LAKSAMANA 2"),
        (6, "JALAN PAHLAWAN 1"),
        (7, "JALAN PAHLAWAN 2"),
        (8, "JALAN BENTARA 1"),
        (9, "JALAN BENTARA 20"),
        (10, "JALAN PERKASA 3-5"),
        (11, "POH CHEONG (JLN SEELEONG)"),
        (12, "FAMILY MART CALTEX")
    };

      viewModel.Statuses = new List<string> { "Scheduled", "Missed", "Completed" };

      return View(viewModel);
    }

    // GET: Schedule/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
      if (id == null) return NotFound();

      var schedule = await _context.Schedules.FindAsync(id);
      if (schedule == null) return NotFound();

      ViewBag.Collectors = _context.Users.Where(u => u.Role == "Collector").ToList();
      ViewBag.Routes = _context.Roads.ToList();
      return View("Edit", schedule);
    }

    [HttpPost]
    //[ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Schedule schedule)
    {
      if (id != schedule.Id) return NotFound();

      if (ModelState.IsValid)
      {
        schedule.UpdatedAt = DateTime.Now;
        _context.Update(schedule);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
      }
      ViewBag.Collectors = _context.Users.Where(u => u.Role == "Collector").ToList();
      ViewBag.Routes = _context.Roads.ToList();
      return View("Edit", schedule);
    }

    // GET: Schedule/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
      if (id == null) return NotFound();

      var schedule = await _context.Schedules
          .Include(s => s.Collector)
          .Include(s => s.Road)
          .FirstOrDefaultAsync(m => m.Id == id);

      if (schedule == null) return NotFound();

      return View("Delete", schedule);
    }

    [HttpPost, ActionName("Delete")]
    //[ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
      var schedule = await _context.Schedules.FindAsync(id);
      _context.Schedules.Remove(schedule);
      await _context.SaveChangesAsync();
      return RedirectToAction(nameof(Index));
    }

    //[HttpGet]
    //public IActionResult Test()
    //{
    //  return View();
    //}

    //[HttpPost]
    ////[ValidateAntiForgeryToken]
    //public IActionResult TestForm(string name)
    //{
    //  ViewBag.Message = $"Submitted: {name}";
    //  return RedirectToAction("Test");
    //}
  }
}
