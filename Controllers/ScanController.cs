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

        // Create Image record
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
          gpsLocation = $"{request.Latitude}, {request.Longitude}"
        });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error processing detection");
        return Json(new { success = false, message = $"Error: {ex.Message}" });
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
      var base64 = base64Data.Contains(",") ? base64Data.Split(',')[1] : base64Data;
      return Convert.FromBase64String(base64);
    }

    [HttpGet]
    public async Task<IActionResult> GetImage(Guid imageId)
    {
      var image = await _context.Images.FindAsync(imageId);
      if (image?.ImageData == null) return NotFound();
      return File(image.ImageData, "image/jpeg");
    }
  }

  public class DetectionRequest
  {
    public string BinPlateId { get; set; } = string.Empty;
    public string ImageData { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
  }
}
