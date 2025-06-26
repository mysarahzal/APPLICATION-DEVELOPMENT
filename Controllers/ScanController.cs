using Microsoft.AspNetCore.Mvc;
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using System.Security.Claims;

namespace AspnetCoreMvcFull.Controllers
{
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
        // Trim and uppercase the plate ID
        var plateId = request.BinPlateId?.Trim().ToUpper();

        // Validate bin plate format (3 letters + 4 numbers)
        if (!IsValidBinPlateFormat(plateId))
        {
          return Json(new { success = false, message = $"Invalid bin plate format: {plateId}" });
        }

        // Find bin in database
        var bin = await _context.Bins
            .Include(b => b.Client)
            .FirstOrDefaultAsync(b => b.BinPlateId == plateId);

        if (bin == null)
        {
          return Json(new { success = false, message = $"Bin with plate ID {plateId} not found in database" });
        }

        // Get collector ID - Multiple options for flexibility
        int collectorId = GetCollectorId(request);

        var currentTime = DateTime.UtcNow;

        // Find active schedule for the collector
        var activeSchedule = await _context.Schedules
            .FirstOrDefaultAsync(s => s.CollectorId == collectorId
                                    && s.Status == "in_progress"
                                    && s.ScheduleStartTime <= currentTime
                                    && s.ScheduleEndTime >= currentTime);

        if (activeSchedule == null)
        {
          return Json(new { success = false, message = "No active schedule found for current collector" });
        }

        // Fetch related CollectionPoint matching schedule_id and bin_id
        var collectionPoint = await _context.CollectionPoints
            .FirstOrDefaultAsync(cp => cp.ScheduleId == activeSchedule.Id && cp.BinId == bin.Id);

        if (collectionPoint == null)
        {
          return Json(new { success = false, message = "This bin is not scheduled for collection in your current route" });
        }

        // Check if CollectionRecord already exists for this collection point
        var existingRecord = await _context.CollectionRecords
            .FirstOrDefaultAsync(cr => cr.CollectionPointId == collectionPoint.Id);

        if (existingRecord != null)
        {
          return Json(new
          {
            success = true,
            message = "Already recorded",
            alreadyCollected = true,
            collectionTime = existingRecord.PickupTimestamp.ToString("dd/MM/yyyy HH:mm:ss")
          });
        }

        // Convert base64 image to byte array
        var imageBytes = ConvertBase64ToBytes(request.ImageData);

        // Create new CollectionRecord
        var collectionRecord = new CollectionRecord
        {
          Id = Guid.NewGuid(),
          CollectionPointId = collectionPoint.Id,
          BinId = bin.Id,
          CollectorId = activeSchedule.CollectorId,
          TruckId = activeSchedule.TruckId,
          PickupTimestamp = currentTime,
          GpsLatitude = request.Latitude,
          GpsLongitude = request.Longitude,
          BinPlateIdCaptured = plateId,
          CreatedAt = currentTime
        };

        _context.CollectionRecords.Add(collectionRecord);

        // Create child Image record
        var image = new Image
        {
          Id = Guid.NewGuid(),
          CollectionRecordId = collectionRecord.Id,
          ImageData = imageBytes,
          CapturedAt = currentTime,
          CreatedAt = currentTime
        };

        _context.Images.Add(image);

        // Mark CollectionPoint as collected
        collectionPoint.IsCollected = true;
        collectionPoint.CollectedAt = currentTime;

        await _context.SaveChangesAsync();

        return Json(new
        {
          success = true,
          message = $"✅ Bin {plateId} successfully collected and recorded!",
          binPlateId = plateId,
          binLocation = bin.Location,
          binZone = bin.Zone,
          fillLevel = bin.FillLevel,
          clientName = bin.Client?.ClientName ?? "Unknown Client",
          collectionTime = currentTime.ToString("dd/MM/yyyy HH:mm:ss"),
          collectionRecordId = collectionRecord.Id,
          collectionPointId = collectionPoint.Id,
          imageId = image.Id,
          scheduleId = activeSchedule.Id,
          collectorId = collectorId
        });
      }
      catch (Exception ex)
      {
        _logger?.LogError(ex, "Error processing detection for plate {PlateId}", request.BinPlateId);
        return Json(new { success = false, message = $"Error processing detection: {ex.Message}" });
      }
    }

    // Flexible method to get collector ID - you can modify this based on your needs
    private int GetCollectorId(DetectionRequest request)
    {
      // Option 1: From request body (for testing)
      if (request.CollectorId.HasValue && request.CollectorId.Value > 0)
      {
        return request.CollectorId.Value;
      }

      // Option 2: From user claims (when you implement login)
      var collectorIdClaim = HttpContext.User.FindFirst("CollectorId")?.Value;
      if (int.TryParse(collectorIdClaim, out var claimCollectorId))
      {
        return claimCollectorId;
      }

      // Option 3: From session (if you store it there)
      if (HttpContext.Session.GetInt32("CollectorId").HasValue)
      {
        return HttpContext.Session.GetInt32("CollectorId").Value;
      }

      // Option 4: Default collector for testing (change this to your test collector ID)
      return 1; // Replace with actual collector ID from your database
    }

    private bool IsValidBinPlateFormat(string plateId)
    {
      if (string.IsNullOrEmpty(plateId) || plateId.Length != 7)
        return false;

      var regex = new Regex(@"^[A-Z]{3}\d{4}$");
      return regex.IsMatch(plateId);
    }

    private byte[] ConvertBase64ToBytes(string base64Data)
    {
      try
      {
        if (string.IsNullOrEmpty(base64Data))
          throw new ArgumentException("Image data is required");

        // Remove data URL prefix if present (e.g., "data:image/jpeg;base64,")
        var base64 = base64Data.Contains(",") ? base64Data.Split(',')[1] : base64Data;

        // Convert base64 string to byte array
        return Convert.FromBase64String(base64);
      }
      catch (Exception ex)
      {
        throw new Exception($"Error converting base64 image: {ex.Message}");
      }
    }

    [HttpGet]
    public async Task<IActionResult> GetImage(Guid imageId)
    {
      try
      {
        var image = await _context.Images.FindAsync(imageId);

        if (image == null || image.ImageData == null)
        {
          return NotFound("Image not found");
        }

        return File(image.ImageData, "image/jpeg");
      }
      catch (Exception ex)
      {
        _logger?.LogError(ex, "Error retrieving image {ImageId}", imageId);
        return BadRequest($"Error retrieving image: {ex.Message}");
      }
    }

    [HttpGet]
    public async Task<IActionResult> GetCollectionStatus(string binPlateId)
    {
      try
      {
        var plateId = binPlateId?.Trim().ToUpper();

        var bin = await _context.Bins
            .Include(b => b.Client)
            .FirstOrDefaultAsync(b => b.BinPlateId == plateId);

        if (bin == null)
        {
          return Json(new { success = false, message = "Bin not found" });
        }

        var collectionRecord = await _context.CollectionRecords
            .Include(cr => cr.CollectionPoint)
            .Where(cr => cr.BinId == bin.Id)
            .OrderByDescending(cr => cr.PickupTimestamp)
            .FirstOrDefaultAsync();

        return Json(new
        {
          success = true,
          binPlateId = plateId,
          binLocation = bin.Location,
          isCollected = collectionRecord != null,
          lastCollectionTime = collectionRecord?.PickupTimestamp.ToString("dd/MM/yyyy HH:mm:ss"),
          collectionRecordId = collectionRecord?.Id
        });
      }
      catch (Exception ex)
      {
        _logger?.LogError(ex, "Error getting collection status for plate {PlateId}", binPlateId);
        return Json(new { success = false, message = $"Error getting collection status: {ex.Message}" });
      }
    }
  }

  public class DetectionRequest
  {
    public string BinPlateId { get; set; } = string.Empty;
    public string ImageData { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public int? CollectorId { get; set; } // Optional - for testing without login
  }
}
