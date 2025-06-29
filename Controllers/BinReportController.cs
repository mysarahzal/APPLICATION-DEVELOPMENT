using Microsoft.AspNetCore.Mvc;
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.Data;
using AspnetCoreMvcFull.ViewModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Authorization;

namespace AspnetCoreMvcFull.Controllers
{
  [Authorize]
  public class BinReportController : Controller
  {
    private readonly KUTIPDbContext _context;
    private readonly ILogger<BinReportController> _logger;

    public BinReportController(KUTIPDbContext context, ILogger<BinReportController> logger)
    {
      _context = context;
      _logger = logger;
    }

    // GET: BinReport/SubmitBinReport
    [Authorize(Roles = "Collector")]
    public IActionResult SubmitBinReport()
    {
      return View();
    }

    // POST: BinReport/SubmitBinReport
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Collector")]
    public async Task<IActionResult> SubmitBinReport(BinReportViewModel model)
    {
      try
      {
        _logger.LogInformation("BinReport submission started for BinPlateId: {BinPlateId}", model.BinPlateId);

        if (!ModelState.IsValid)
        {
          _logger.LogWarning("ModelState is invalid");
          foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
          {
            _logger.LogWarning("Validation error: {Error}", error.ErrorMessage);
          }
          return View(model);
        }

        // Check if ImageFile is provided
        if (model.ImageFile == null || model.ImageFile.Length == 0)
        {
          ModelState.AddModelError("ImageFile", "Please select an image file.");
          return View(model);
        }

        // Find the bin by BinPlateId
        var bin = await _context.Bins
            .Include(b => b.Client)
            .FirstOrDefaultAsync(b => b.BinPlateId == model.BinPlateId);

        if (bin == null)
        {
          ModelState.AddModelError("BinPlateId", $"Bin with Plate ID '{model.BinPlateId}' not found.");
          return View(model);
        }

        _logger.LogInformation("Found bin: {BinId} with PlateId: {PlateId}", bin.Id, bin.BinPlateId);

        // Create a new bin report
        var newBinReport = new BinReport
        {
          Id = Guid.NewGuid(),
          BinId = bin.Id,
          ReportedAt = DateTime.UtcNow,
          Status = "pending",
          Description = model.Description,
          IsIssueReported = model.IsIssueReported,
          Severity = model.Severity, // NEW: Add severity
          ReportedBy = User.Identity.Name ?? "Unknown Collector" // NEW: Auto-capture user
        };

        // Add the new bin report to the database
        _context.BinReports.Add(newBinReport);

        // Handle image upload
        if (model.ImageFile != null && model.ImageFile.Length > 0)
        {
          // Save image to file system
          var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "bin-reports");
          if (!Directory.Exists(uploadsFolder))
            Directory.CreateDirectory(uploadsFolder);

          var uniqueFileName = $"{newBinReport.Id}_{Guid.NewGuid()}{Path.GetExtension(model.ImageFile.FileName)}";
          var filePath = Path.Combine(uploadsFolder, uniqueFileName);

          using (var fileStream = new FileStream(filePath, FileMode.Create))
          {
            await model.ImageFile.CopyToAsync(fileStream);
          }

          var imageUrl = $"/uploads/bin-reports/{uniqueFileName}";

          // Create a BinReportImage record in the database
          var binReportImage = new BinReportImage
          {
            Id = Guid.NewGuid(),
            BinReportId = newBinReport.Id,
            ImagePath = imageUrl,
            FileName = model.ImageFile.FileName,
            ContentType = model.ImageFile.ContentType,
            FileSize = model.ImageFile.Length,
            CapturedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
          };

          _context.BinReportImages.Add(binReportImage);
          _logger.LogInformation("Image saved for bin report: {ReportId}", newBinReport.Id);
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Bin report saved successfully: {ReportId}", newBinReport.Id);

        TempData["SuccessMessage"] = $"Your bin report for {bin.BinPlateId} has been submitted successfully! It will be reviewed by our team.";
        return RedirectToAction(nameof(SubmitBinReport));
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error submitting bin report");
        ModelState.AddModelError("", "An error occurred while submitting the report. Please try again.");
        return View(model);
      }
    }

    // GET: BinReport/SubmittedReports
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SubmittedReports()
    {
      try
      {
        var reports = await (from br in _context.BinReports
                             join bin in _context.Bins on br.BinId equals bin.Id
                             join client in _context.Clients on bin.ClientID equals client.ClientID
                             join img in _context.BinReportImages on br.Id equals img.BinReportId into images
                             from image in images.DefaultIfEmpty()
                             select new SubmittedReportViewModel
                             {
                               CollectionRecordId = br.Id,
                               BinPlateId = bin.BinPlateId,
                               BinLocation = bin.Location,
                               ClientName = client.ClientName,
                               PickupTimestamp = br.ReportedAt,
                               Status = br.Status,
                               Description = br.Description,
                               IsIssueReported = br.IsIssueReported,
                               Severity = br.Severity, // NEW: Include severity
                               ReportedBy = br.ReportedBy, // NEW: Include reporter
                               AcknowledgedBy = br.AcknowledgedBy, // NEW: Include acknowledger
                               AcknowledgedAt = br.AcknowledgedAt, // NEW: Include ack time
                               ImageUrl = image != null ? image.ImagePath : null,
                               FileName = image != null ? image.FileName : null,
                               FileSize = image != null ? image.FileSize : 0
                             }).OrderByDescending(r => r.PickupTimestamp).ToListAsync();

        return View(reports);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error retrieving submitted reports");
        return View(new List<SubmittedReportViewModel>());
      }
    }

    // GET: BinReport/Details/{id}
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Details(Guid id)
    {
      try
      {
        var report = await (from br in _context.BinReports
                            join bin in _context.Bins on br.BinId equals bin.Id
                            join client in _context.Clients on bin.ClientID equals client.ClientID
                            join img in _context.BinReportImages on br.Id equals img.BinReportId into images
                            from image in images.DefaultIfEmpty()
                            where br.Id == id
                            select new SubmittedReportViewModel
                            {
                              CollectionRecordId = br.Id,
                              BinPlateId = bin.BinPlateId,
                              BinLocation = bin.Location,
                              ClientName = client.ClientName,
                              PickupTimestamp = br.ReportedAt,
                              Status = br.Status,
                              Description = br.Description,
                              IsIssueReported = br.IsIssueReported,
                              Severity = br.Severity, // NEW: Include severity
                              ReportedBy = br.ReportedBy, // NEW: Include reporter
                              AcknowledgedBy = br.AcknowledgedBy, // NEW: Include acknowledger
                              AcknowledgedAt = br.AcknowledgedAt, // NEW: Include ack time
                              ImageUrl = image != null ? image.ImagePath : null,
                              FileName = image != null ? image.FileName : null,
                              FileSize = image != null ? image.FileSize : 0
                            }).FirstOrDefaultAsync();

        if (report == null)
        {
          return NotFound();
        }

        return View(report);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error retrieving report details: {ReportId}", id);
        return NotFound();
      }
    }

    // POST: BinReport/UpdateStatus/{id}
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> UpdateStatus(Guid id, string status)
    {
      try
      {
        var report = await _context.BinReports.FindAsync(id);
        if (report != null)
        {
          report.Status = status;

          // NEW: Track acknowledgment
          if (status == "acknowledged" || status == "resolved")
          {
            report.AcknowledgedBy = User.Identity.Name;
            report.AcknowledgedAt = DateTime.UtcNow;
          }

          await _context.SaveChangesAsync();

          TempData["SuccessMessage"] = "Report status updated successfully!";
          return Json(new { success = true, message = "Status updated successfully" });
        }

        return Json(new { success = false, message = "Report not found" });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error updating report status: {ReportId}", id);
        return Json(new { success = false, message = "Error updating status" });
      }
    }

    // NEW: GET: Alert summary for dashboard
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAlertSummary()
    {
      try
      {
        var reports = await _context.BinReports
            .Where(r => r.Status != "resolved")
            .GroupBy(r => r.Severity)
            .Select(g => new { Severity = g.Key, Count = g.Count() })
            .ToListAsync();

        var summary = new
        {
          TotalActive = reports.Sum(r => r.Count),
          Critical = reports.FirstOrDefault(r => r.Severity == "Critical")?.Count ?? 0,
          High = reports.FirstOrDefault(r => r.Severity == "High")?.Count ?? 0,
          Medium = reports.FirstOrDefault(r => r.Severity == "Medium")?.Count ?? 0,
          Low = reports.FirstOrDefault(r => r.Severity == "Low")?.Count ?? 0
        };

        return Json(summary);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error getting alert summary");
        return Json(new { TotalActive = 0, Critical = 0, High = 0, Medium = 0, Low = 0 });
      }
    }

    // NEW: POST: BinReport/Acknowledge/{id}
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Acknowledge(Guid id, string resolutionNotes = "")
    {
      var report = await _context.BinReports.FindAsync(id);
      if (report == null) return NotFound();

      report.Status = "acknowledged";
      report.AcknowledgedBy = User.Identity.Name;
      report.AcknowledgedAt = DateTime.UtcNow;

      if (!string.IsNullOrEmpty(resolutionNotes))
      {
        report.Description += $"\n\nResolution Notes: {resolutionNotes}";
        report.Status = "resolved";
      }

      await _context.SaveChangesAsync();

      TempData["SuccessMessage"] = report.Status == "resolved" ?
          "Report resolved successfully!" : "Report acknowledged successfully!";

      return RedirectToAction(nameof(SubmittedReports));
    }
  }
}
