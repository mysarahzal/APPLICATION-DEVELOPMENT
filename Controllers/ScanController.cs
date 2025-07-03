using Microsoft.AspNetCore.Mvc;
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;

namespace AspnetCoreMvcFull.Controllers
{
  [Authorize(Roles = "Collector")]
  public class ScanController : Controller
  {
    private readonly KUTIPDbContext _context;
    private readonly ILogger<ScanController> _logger;

    public ScanController(KUTIPDbContext context, ILogger<ScanController> logger)
    {
      _context = context;
      _logger = logger;
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
        // Validate plate format
        var plateId = request.BinPlateId?.Trim().ToUpper();
        if (!IsValidBinPlateFormat(plateId))
        {
          return Json(new { success = false, message = $"Invalid bin plate format: {plateId}" });
        }

        // 1. Find bin by plate ID
        var bin = await _context.Bins
            .Include(b => b.Client)
            .FirstOrDefaultAsync(b => b.BinPlateId == plateId);

        if (bin == null)
        {
          return Json(new { success = false, message = $"Bin with plate ID {plateId} not found" });
        }

        // 2. Find collection point with same binId
        var collectionPoint = await _context.CollectionPoints
            .FirstOrDefaultAsync(cp => cp.BinId == bin.Id);

        if (collectionPoint == null)
        {
          return Json(new { success = false, message = "Collection point not found for this bin" });
        }

        // 3. Get schedule from collection point to extract truck_id and collector_id
        var schedule = await _context.Schedules
            .Include(s => s.Collector)
            .Include(s => s.Truck)
            .FirstOrDefaultAsync(s => s.Id == collectionPoint.ScheduleId);

        if (schedule == null)
        {
          return Json(new { success = false, message = "Schedule not found for this collection point" });
        }

        // Check if already collected
        var existingRecord = await _context.CollectionRecords
            .FirstOrDefaultAsync(cr => cr.CollectionPointId == collectionPoint.Id);

        if (existingRecord != null)
        {
          return Json(new
          {
            success = true,
            message = "Bin already collected",
            alreadyCollected = true,
            binPlateId = plateId,
            collectionTime = existingRecord.PickupTimestamp.ToString("dd/MM/yyyy HH:mm:ss")
          });
        }

        var currentTime = DateTime.UtcNow;
        var imageBytes = ConvertBase64ToBytes(request.ImageData);

        // Create CollectionRecord with data from existing tables
        var collectionRecord = new CollectionRecord
        {
          Id = Guid.NewGuid(),
          CollectionPointId = collectionPoint.Id,
          BinId = bin.Id,
          CollectorId = schedule.CollectorId,  // From schedule
          TruckId = schedule.TruckId,          // From schedule
          PickupTimestamp = currentTime,
          GpsLatitude = request.Latitude,
          GpsLongitude = request.Longitude,
          BinPlateIdCaptured = plateId,
          CreatedAt = currentTime
        };

        _context.CollectionRecords.Add(collectionRecord);

        // Create Image record with proper format
        var image = new Image
        {
          Id = Guid.NewGuid(),
          CollectionRecordId = collectionRecord.Id,
          ImageData = imageBytes,
          CapturedAt = currentTime,
          CreatedAt = currentTime
        };

        _context.Images.Add(image);

        // Update collection point status
        collectionPoint.IsCollected = true;
        collectionPoint.CollectedAt = currentTime;

        await _context.SaveChangesAsync();

        _logger.LogInformation($"Successfully saved collection record for plate {plateId} with image size: {imageBytes.Length} bytes");

        return Json(new
        {
          success = true,
          message = $"✅ Bin {plateId} successfully collected!",
          binPlateId = plateId,
          binLocation = bin.Location,
          binZone = bin.Zone,
          clientName = bin.Client?.ClientName ?? "Unknown",
          collectionTime = currentTime.ToString("dd/MM/yyyy HH:mm:ss"),
          collectionRecordId = collectionRecord.Id,
          imageId = image.Id,
          collectorName = $"{schedule.Collector?.FirstName} {schedule.Collector?.LastName}".Trim(),
          truckLicensePlate = schedule.Truck?.LicensePlate ?? "Unknown",
          gpsLocation = $"{request.Latitude}, {request.Longitude}",
          detectionMethod = request.IsManual ? "Manual Entry" : "Auto Detection"
        });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error processing detection");
        return Json(new { success = false, message = $"Error: {ex.Message}" });
      }
    }

    // NEW: Manual form submission endpoint
    [HttpPost]
    public async Task<IActionResult> ProcessManualEntry([FromBody] ManualEntryRequest request)
    {
      try
      {
        // Convert manual entry to detection request format
        var detectionRequest = new DetectionRequest
        {
          BinPlateId = request.BinPlateId,
          ImageData = request.ImageData,
          Latitude = request.Latitude,
          Longitude = request.Longitude,
          IsManual = true
        };

        // Use the same processing logic
        var result = await ProcessDetection(detectionRequest);
        return result;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error processing manual entry");
        return Json(new { success = false, message = $"Manual entry error: {ex.Message}" });
      }
    }

    // NEW: Validate bin plate endpoint for real-time feedback
    [HttpPost]
    public async Task<IActionResult> ValidateBinPlate([FromBody] ValidatePlateRequest request)
    {
      try
      {
        var plateId = request.BinPlateId?.Trim().ToUpper();

        if (!IsValidBinPlateFormat(plateId))
        {
          return Json(new
          {
            valid = false,
            message = "Invalid format. Use ABC1234 format (3 letters + 4 numbers)"
          });
        }

        var bin = await _context.Bins
            .Include(b => b.Client)
            .FirstOrDefaultAsync(b => b.BinPlateId == plateId);

        if (bin == null)
        {
          return Json(new
          {
            valid = false,
            message = $"Bin {plateId} not found in database"
          });
        }

        // Check if already collected
        var collectionPoint = await _context.CollectionPoints
            .FirstOrDefaultAsync(cp => cp.BinId == bin.Id);

        if (collectionPoint != null)
        {
          var existingRecord = await _context.CollectionRecords
              .FirstOrDefaultAsync(cr => cr.CollectionPointId == collectionPoint.Id);

          if (existingRecord != null)
          {
            return Json(new
            {
              valid = false,
              message = $"Bin {plateId} already collected on {existingRecord.PickupTimestamp:dd/MM/yyyy HH:mm}",
              alreadyCollected = true
            });
          }
        }

        return Json(new
        {
          valid = true,
          message = $"✅ Bin {plateId} found - Ready to collect",
          binInfo = new
          {
            plateId = bin.BinPlateId,
            location = bin.Location,
            zone = bin.Zone,
            clientName = bin.Client?.ClientName ?? "Unknown"
          }
        });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error validating bin plate");
        return Json(new { valid = false, message = $"Validation error: {ex.Message}" });
      }
    }

    private bool IsValidBinPlateFormat(string plateId)
    {
      if (string.IsNullOrEmpty(plateId) || plateId.Length != 7)
        return false;

      return new Regex(@"^[A-Z]{3}\d{4}$").IsMatch(plateId);
    }

    private byte[] ConvertBase64ToBytes(string base64Data)
    {
      try
      {
        // Handle data URL format (data:image/jpeg;base64,...)
        if (base64Data.StartsWith("data:"))
        {
          var base64Index = base64Data.IndexOf("base64,");
          if (base64Index != -1)
          {
            base64Data = base64Data.Substring(base64Index + 7);
          }
        }

        // Clean the base64 string
        base64Data = base64Data.Replace(" ", "").Replace("\n", "").Replace("\r", "");

        var imageBytes = Convert.FromBase64String(base64Data);

        _logger.LogInformation($"Converted base64 to {imageBytes.Length} bytes");

        return imageBytes;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error converting base64 to bytes");
        throw new ArgumentException("Invalid base64 image data", ex);
      }
    }

    [HttpGet]
    public async Task<IActionResult> GetImage(Guid imageId)
    {
      try
      {
        var image = await _context.Images.FindAsync(imageId);
        if (image?.ImageData == null)
        {
          _logger.LogWarning($"Image not found for ID: {imageId}");
          return NotFound("Image not found");
        }

        _logger.LogInformation($"Serving image {imageId} with size: {image.ImageData.Length} bytes");

        // Set proper headers for image display
        Response.Headers.Add("Cache-Control", "public, max-age=3600");
        Response.Headers.Add("Content-Disposition", "inline");

        return File(image.ImageData, "image/jpeg");
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error retrieving image {ImageId}", imageId);
        return BadRequest($"Error retrieving image: {ex.Message}");
      }
    }
  }

  public class DetectionRequest
  {
    public string BinPlateId { get; set; } = string.Empty;
    public string ImageData { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public bool IsManual { get; set; } = false;
  }

  public class ManualEntryRequest
  {
    public string BinPlateId { get; set; } = string.Empty;
    public string ImageData { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
  }

  public class ValidatePlateRequest
  {
    public string BinPlateId { get; set; } = string.Empty;
  }
}
