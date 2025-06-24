using Microsoft.AspNetCore.Mvc;
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace AspnetCoreMvcFull.Controllers
{
  public class ScanController : Controller
  {
    private readonly KUTIPDbContext _context;
    private readonly string _uploadsPath;

    public ScanController(KUTIPDbContext context)
    {
      _context = context;
      _uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

      if (!Directory.Exists(_uploadsPath))
        Directory.CreateDirectory(_uploadsPath);
    }

    public IActionResult Index()
    {
      return View();
    }

    [HttpPost]
    public async Task<IActionResult> ProcessDetection([FromBody] DetectionRequest request)
    {
      try
      {
        // Validate bin plate format (3 letters + 4 numbers)
        if (!IsValidBinPlateFormat(request.BinPlateId))
        {
          return Json(new { success = false, message = $"Invalid bin plate format: {request.BinPlateId}" });
        }

        // Find bin in database
        var bin = await _context.Bins
            .Include(b => b.Client) // Include client details
            .FirstOrDefaultAsync(b => b.BinPlateId == request.BinPlateId);

        if (bin == null)
        {
          return Json(new { success = false, message = $"Bin with plate ID {request.BinPlateId} not found in database" });
        }

        // Save the captured image first
        var imageFileName = await SaveBase64Image(request.ImageData);

        // Create collection record
        var collectionRecord = new CollectionRecord
        {
          Id = Guid.NewGuid(),
          CollectionPointId = Guid.NewGuid(), // Set to appropriate collection point if needed
          BinId = bin.Id,
          UserId = 1, // Set to current user ID (int type)
          //TruckId = Guid.NewGuid(), // Set to current truck
          PickupTimestamp = DateTime.UtcNow, // Current time
          GpsLatitude = 0.0m, // Default value since bin doesn't have coordinates
          GpsLongitude = 0.0m, // Default value since bin doesn't have coordinates
          BinPlateIdCaptured = request.BinPlateId,
          IssueReported = false,
          IssueDescription = string.Empty
        };

        _context.CollectionRecords.Add(collectionRecord);

        // Save image record linked to collection record
        var image = new Image
        {
          Id = Guid.NewGuid(),
          CollectionRecordId = collectionRecord.Id,
          ImageUrl = $"/uploads/{imageFileName}",
          ImageType = "during", // Type of image captured
          CapturedAt = DateTime.UtcNow, // Current time
          CreatedAt = DateTime.UtcNow // Current time
        };

        _context.Images.Add(image);

        await _context.SaveChangesAsync();

        return Json(new
        {
          success = true,
          message = $"✅ Bin {request.BinPlateId} successfully detected and recorded!",
          binPlateId = request.BinPlateId,
          binLocation = bin.Location, // Use Location field from bin
          binZone = bin.Zone,
          fillLevel = bin.FillLevel,
          clientName = bin.Client?.ClientName ?? "Unknown Client",
          collectionTime = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm:ss"),
          collectionRecordId = collectionRecord.Id
        });
      }
      catch (Exception ex)
      {
        return Json(new { success = false, message = $"Error processing detection: {ex.Message}" });
      }
    }

    private bool IsValidBinPlateFormat(string plateId)
    {
      if (string.IsNullOrEmpty(plateId) || plateId.Length != 7)
        return false;

      var regex = new Regex(@"^[A-Z]{3}\d{4}$");
      return regex.IsMatch(plateId.ToUpper());
    }

    private async Task<string> SaveBase64Image(string base64Data)
    {
      try
      {
        var base64 = base64Data.Contains(",") ? base64Data.Split(',')[1] : base64Data;
        var imageBytes = Convert.FromBase64String(base64);

        var fileName = $"{Guid.NewGuid()}.jpg";
        var filePath = Path.Combine(_uploadsPath, fileName);

        await System.IO.File.WriteAllBytesAsync(filePath, imageBytes);
        return fileName;
      }
      catch (Exception ex)
      {
        throw new Exception($"Error saving image: {ex.Message}");
      }
    }
  }

  public class DetectionRequest
  {
    public string BinPlateId { get; set; } = string.Empty;
    public string ImageData { get; set; } = string.Empty;
  }
}
