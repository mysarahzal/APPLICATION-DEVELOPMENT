using Microsoft.AspNetCore.Mvc;
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.Data;
using AspnetCoreMvcFull.ViewModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Threading.Tasks;

namespace AspnetCoreMvcFull.Controllers
{
  public class BinReportController : Controller
  {
    private readonly KUTIPDbContext _context;

    public BinReportController(KUTIPDbContext context)
    {
      _context = context;
    }

    // GET: BinReport/Submit
    public IActionResult SubmitBinReport()
    {
      return View();
    }

    // POST: BinReport/Submit
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitBinReport(BinReportViewModel model)
    {
      if (!ModelState.IsValid)
      {
        return View(model);  // Return the view with errors if the model is not valid
      }

      // Check if ImageFile is null before calling the DetectBinAsync method
      if (model.ImageFile == null)
      {
        ModelState.AddModelError("", "No image file was provided.");
        return View(model);  // Return the view with error if no image file is provided
      }

      // Call the bin detection method
      var detectedBinDetails = await DetectBinAsync(model.ImageFile);
      if (detectedBinDetails == null)
      {
        ModelState.AddModelError("", "Bin detection failed.");
        return View(model);  // Return the view with error if bin detection fails
      }

      // Check if the bin is valid for reporting (Make sure the bin is still active and not collected)
      var collectionPoint = await _context.CollectionPoints
          .FirstOrDefaultAsync(cp => cp.BinId == model.BinId && !cp.IsCollected);
      if (collectionPoint == null)
      {
        ModelState.AddModelError("", "No active collection point found for the specified Bin.");
        return View(model);  // Return the view with error if collection point is not found
      }

      // Create a new bin report
      var newBinReport = new BinReport
      {
        BinId = model.BinId,
        ReportedAt = DateTime.Now,
        Status = "reported",  // Set report status (this could be dynamic as per your application flow)
        Description = model.Description ?? string.Empty,  // Ensure Description is not null
        IsIssueReported = !string.IsNullOrEmpty(model.IssueDescription),
        IssueDescription = model.IssueDescription ?? string.Empty  // Ensure IssueDescription is not null
      };

      // Add the new bin report to the database
      _context.Add(newBinReport);
      await _context.SaveChangesAsync();  // Save changes in the database

      // Image handling (upload and store image)
      if (model.ImageFile != null && model.ImageFile.Length > 0)
      {
        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
        if (!Directory.Exists(uploadsFolder))
          Directory.CreateDirectory(uploadsFolder);

        var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(model.ImageFile.FileName);
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        // Save the uploaded image to the file system
        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
          await model.ImageFile.CopyToAsync(fileStream);  // Async file upload handling
        }

        // Create an Image record in the database
        var image = new Image
        {
          Id = Guid.NewGuid(),
          CollectionRecordId = newBinReport.Id,  // Link to the bin report (CollectionRecord)
          ImageUrl = $"/uploads/{uniqueFileName}",  // Use the uploaded image URL
          ImageType = "before",  // Type of the image (this could be "before", "during", etc.)
          CapturedAt = DateTime.Now,
          CreatedAt = DateTime.Now
        };

        _context.Images.Add(image);  // Add image record to the database
      }

      await _context.SaveChangesAsync();  // Save the image details in the database

      TempData["ShowSuccessMessage"] = true;  // Show success message after the report is submitted
      return RedirectToAction(nameof(SubmitBinReport));  // Redirect back to the bin report submission page
    }

    // Placeholder method for YOLOv8 bin detection
    private async Task<DetectedBinDetails?> DetectBinAsync(IFormFile binImage)
    {
      if (binImage == null)
      {
        return null;  // Return null if binImage is null
      }

      // Simulate async operation like API call
      await Task.Delay(1000); // Simulate async work

      // Simulated bin detection logic
      return new DetectedBinDetails
      {
        BinId = Guid.NewGuid(),  // Simulated bin ID for the demonstration
        Latitude = 1.2345M,      // Placeholder latitude (use M suffix for decimal literals)
        Longitude = 2.3456M      // Placeholder longitude (use M suffix for decimal literals)
      };
    }

    // GET: BinReport/SubmittedReports
    public async Task<IActionResult> SubmittedReports()
    {
      var reports = await (from cr in _context.CollectionRecords
                             //join bin in _context.Bins on cr.BinId equals bin.Id
                           join user in _context.Users on cr.UserId equals user.Id
                           //join truck in _context.Trucks on cr.TruckId equals truck.Id
                           join img in _context.Images on cr.Id equals img.CollectionRecordId into images
                           from image in images.DefaultIfEmpty() // To handle the case where there may not be an image
                           select new SubmittedReportViewModel
                           {
                             CollectionRecordId = cr.Id,
                             //BinPlateId = bin.BinPlateId,
                             PickupTimestamp = cr.PickupTimestamp,
                             GpsLatitude = cr.GpsLatitude,
                             GpsLongitude = cr.GpsLongitude,
                             CollectorEmail = user.Email,
                             //TruckLicensePlate = truck.LicensePlate,
                             IssueReported = cr.IssueReported,
                             IssueDescription = cr.IssueDescription,
                             ImageUrl = (image != null && image.ImageUrl != null) ? image.ImageUrl : "/uploads/default.png" // Default image if no image found
                           }).ToListAsync();

      return View(reports);
    }
  }
}
