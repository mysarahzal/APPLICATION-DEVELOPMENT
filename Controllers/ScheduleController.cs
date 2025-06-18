using Microsoft.AspNetCore.Mvc;
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.Data;
using Microsoft.EntityFrameworkCore;
using System;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace AspnetCoreMvcFull.Controllers
{
  public class ScheduleController : Controller
  {
    private readonly KUTIPDbContext _context;
    private readonly ILogger<ScheduleController> _logger;

    public ScheduleController(KUTIPDbContext context, ILogger<ScheduleController> logger)
    {
      _context = context;
      _logger = logger;
    }

    // UC10 – Admin Views All Job Schedules
    public async Task<IActionResult> Index()
    {
      try
      {
        _logger.LogInformation("Index action started");

        var schedules = await _context.Schedules
            .Include(s => s.Collector)
            .Include(s => s.Road)
            .OrderBy(s => s.ActualStartTime)
            .ThenBy(s => s.ScheduleStartTime)
            .ToListAsync();

        _logger.LogInformation("Loaded {Count} schedules", schedules.Count);

        return View(schedules);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error in Index action");
        TempData["ErrorMessage"] = "An error occurred while loading schedules.";
        return View(new List<Schedule>());
      }
    }

    // UC09 – Driver Views Their Own Schedule
    public async Task<IActionResult> MySchedule()
    {
      try
      {
        _logger.LogInformation("MySchedule action started");

        // Simulate logged-in user ID for now - in real app, get from authentication
        var userId = 1;

        var mySchedules = await _context.Schedules
            .Include(s => s.Road)
            .Include(s => s.Collector)
            .Where(s => s.CollectorId == userId)
            .OrderBy(s => s.ActualStartTime)
            .ThenBy(s => s.ScheduleStartTime)
            .ToListAsync();

        _logger.LogInformation("Loaded {Count} schedules for user {UserId}", mySchedules.Count, userId);

        ViewBag.CurrentUserId = userId;
        return View(mySchedules);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error in MySchedule action");
        TempData["ErrorMessage"] = "An error occurred while loading your schedules.";
        return View(new List<Schedule>());
      }
    }

    // UC08 – Admin Creates a New Schedule (GET)
    public async Task<IActionResult> Create()
    {
      try
      {
        _logger.LogInformation("Create GET action started");

        await PopulateDropdowns();

        // Set default values
        var schedule = new Schedule
        {
          ActualStartTime = DateTime.Today.AddDays(1),
          //ScheduleStartTime = new TimeSpan(8, 0, 0),
          //Priority = "Medium",
          Status = "Pending"
        };

        return View(schedule);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error in Create GET action");
        TempData["ErrorMessage"] = "An error occurred while loading the create form.";
        return RedirectToAction(nameof(Index));
      }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Schedule schedule)
    {
      try
      {
        // Custom validation
        if (schedule.ScheduleStartTime < DateTime.Today)
        {
          ModelState.AddModelError("ScheduledDate", "Cannot schedule for past dates.");
        }

        if (ModelState.IsValid)
        {
          _logger.LogInformation("Creating new schedule for CollectorId: {CollectorId}, RouteId: {RouteId}",
              schedule.CollectorId, schedule.RouteId);

          schedule.CreatedAt = DateTime.Now;
          schedule.UpdatedAt = DateTime.Now;

          _context.Schedules.Add(schedule);
          await _context.SaveChangesAsync();

          TempData["SuccessMessage"] = "Schedule created successfully!";
          return RedirectToAction(nameof(Index));
        }
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error saving schedule to database");
        ModelState.AddModelError("", "Unable to save schedule. Please try again.");
      }

      // Re-populate dropdowns on error
      await PopulateDropdowns();
      return View(schedule);
    }

    // UC08 – Admin Edits Schedule (GET)
    public async Task<IActionResult> Edit(int? id)
    {
      if (id == null)
      {
        _logger.LogWarning("Edit action: Id is null");
        return NotFound();
      }

      try
      {
        _logger.LogInformation("Edit action started for schedule ID: {Id}", id);

        var schedule = await _context.Schedules
            .Include(s => s.Road)
            .Include(s => s.Collector)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (schedule == null)
        {
          TempData["ErrorMessage"] = "Schedule not found.";
          return RedirectToAction(nameof(Index));
        }

        await PopulateDropdowns();
        return View(schedule);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error loading schedule for edit");
        TempData["ErrorMessage"] = "An error occurred while loading the schedule.";
        return RedirectToAction(nameof(Index));
      }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Schedule updatedSchedule)
    {
      if (id != updatedSchedule.Id)
      {
        _logger.LogWarning("ID mismatch in Edit: {SubmittedId} vs {ScheduleId}", id, updatedSchedule.Id);
        return NotFound();
      }

      try
      {
        // Custom validation
        if (updatedSchedule.ActualStartTime < DateTime.Today && updatedSchedule.Status == "Pending")
        {
          ModelState.AddModelError("ScheduledDate", "Cannot schedule pending tasks for past dates.");
        }

        if (ModelState.IsValid)
        {
          _logger.LogInformation("Updating schedule ID: {Id}", id);

          updatedSchedule.UpdatedAt = DateTime.Now;
          _context.Update(updatedSchedule);
          await _context.SaveChangesAsync();

          TempData["SuccessMessage"] = "Schedule updated successfully!";
          return RedirectToAction(nameof(Index));
        }
      }
      catch (DbUpdateConcurrencyException)
      {
        if (!ScheduleExists(updatedSchedule.Id))
        {
          TempData["ErrorMessage"] = "Schedule no longer exists.";
          return RedirectToAction(nameof(Index));
        }
        else
        {
          throw;
        }
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error updating schedule ID: {Id}", id);
        ModelState.AddModelError("", "An error occurred while updating the schedule.");
      }

      // Re-load data for dropdowns
      await PopulateDropdowns();
      return View(updatedSchedule);
    }

    // UC08 – Admin Deletes a Schedule (GET)
    public async Task<IActionResult> Delete(int? id)
    {
      if (id == null)
      {
        _logger.LogWarning("Delete: Id is null");
        return NotFound();
      }

      try
      {
        _logger.LogInformation("Delete action started for schedule ID: {Id}", id);

        var schedule = await _context.Schedules
            .Include(s => s.Collector)
            .Include(s => s.Road)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (schedule == null)
        {
          TempData["ErrorMessage"] = "Schedule not found.";
          return RedirectToAction(nameof(Index));
        }

        return View(schedule);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error loading schedule for delete");
        TempData["ErrorMessage"] = "An error occurred while loading the schedule.";
        return RedirectToAction(nameof(Index));
      }
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
      try
      {
        _logger.LogInformation("Delete confirmed for schedule ID: {Id}", id);

        var schedule = await _context.Schedules.FindAsync(id);

        if (schedule != null)
        {
          _context.Schedules.Remove(schedule);
          await _context.SaveChangesAsync();
          TempData["SuccessMessage"] = "Schedule deleted successfully!";
        }
        else
        {
          TempData["ErrorMessage"] = "Schedule not found.";
        }

        return RedirectToAction(nameof(Index));
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error deleting schedule ID: {Id}", id);
        TempData["ErrorMessage"] = "An error occurred while deleting the schedule.";
        return RedirectToAction(nameof(Index));
      }
    }

    private bool ScheduleExists(int id)
    {
      return _context.Schedules.Any(e => e.Id == id);
    }

    private async Task PopulateDropdowns()
    {
      var collectors = await _context.Users
          .Where(u => u.Role == "Driver" || u.Role == "Operator")
          .OrderBy(u => u.FirstName)
          .ToListAsync();

      var routes = await _context.Roads
          .OrderBy(r => r.Name)
          .ToListAsync();

      ViewBag.CollectorId = new SelectList(collectors, "Id", "FullName");
      ViewBag.RouteId = new SelectList(routes, "Id", "DisplayText");

      ViewBag.Status = new SelectList(new[]
      {
                new { Value = "Pending", Text = "Pending" },
                new { Value = "In Progress", Text = "In Progress" },
                new { Value = "Completed", Text = "Completed" },
                new { Value = "Missed", Text = "Missed" }
            }, "Value", "Text");

      ViewBag.Priority = new SelectList(new[]
      {
                new { Value = "Low", Text = "Low Priority" },
                new { Value = "Medium", Text = "Medium Priority" },
                new { Value = "High", Text = "High Priority" }
            }, "Value", "Text");
    }
  }
}
