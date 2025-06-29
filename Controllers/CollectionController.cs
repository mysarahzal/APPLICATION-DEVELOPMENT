using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AspnetCoreMvcFull.Data;
using AspnetCoreMvcFull.Models;
using Microsoft.AspNetCore.Authorization;

namespace AspnetCoreMvcFull.Controllers
{
  [Route("[controller]")]
  [Authorize(Roles = "Admin")]
  public class CollectionController : Controller
  {
    private readonly KUTIPDbContext _context;
    private readonly ILogger<CollectionController> _logger;

    public CollectionController(KUTIPDbContext context, ILogger<CollectionController> logger)
    {
      _context = context;
      _logger = logger;
    }

    // GET /Collection
    [HttpGet("")]
    public IActionResult Index() => View();

    // GET /Collection/GetCollectionRecords
    [HttpGet("GetCollectionRecords")]
    public async Task<IActionResult> GetCollectionRecords(
        int page = 1,
        int pageSize = 20,
        string search = "")
    {
      try
      {
        var query = _context.CollectionRecords
                            .Include(cr => cr.Bin).ThenInclude(b => b.Client)
                            .Include(cr => cr.Collector)
                            .Include(cr => cr.Truck)
                            .Include(cr => cr.CollectionPoint)
                            .Include(cr => cr.Image)
                            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
          query = query.Where(cr =>
                cr.BinPlateIdCaptured.Contains(search) ||
                cr.Bin.Location.Contains(search) ||
                cr.Bin.Zone.Contains(search) ||
                cr.Bin.Client.ClientName.Contains(search) ||
                cr.Collector.FirstName.Contains(search) ||
                cr.Collector.LastName.Contains(search) ||
                cr.Truck.LicensePlate.Contains(search));
        }

        var total = await query.CountAsync();

        var records = await query
           .OrderByDescending(cr => cr.PickupTimestamp)
           .Skip((page - 1) * pageSize)
           .Take(pageSize)
           .Select(cr => new
           {
             id = cr.Id,
             binPlateId = cr.BinPlateIdCaptured,
             binLocation = cr.Bin.Location,
             binZone = cr.Bin.Zone,
             fillLevel = cr.Bin.FillLevel,
             clientName = cr.Bin.Client.ClientName,
             collectorName = cr.Collector.FirstName + " " + cr.Collector.LastName,
             truckLicensePlate = cr.Truck.LicensePlate,
             pickupTimestamp = cr.PickupTimestamp.ToString("dd/MM/yyyy HH:mm:ss"),
             pickupDate = cr.PickupTimestamp.ToString("dd/MM/yyyy"),
             pickupTime = cr.PickupTimestamp.ToString("HH:mm:ss"),
             gpsLatitude = cr.GpsLatitude,
             gpsLongitude = cr.GpsLongitude,
             hasImage = cr.Image != null,
             imageId = cr.Image != null ? cr.Image.Id : Guid.Empty,
             orderInSchedule = cr.CollectionPoint.OrderInSchedule,
             createdAt = cr.CreatedAt.ToString("dd/MM/yyyy HH:mm:ss")
           })
           .ToListAsync();

        var totalPages = (int)Math.Ceiling(total / (double)pageSize);

        return Json(new
        {
          success = true,
          records,
          pagination = new
          {
            currentPage = page,
            pageSize,
            totalRecords = total,
            totalPages,
            hasNextPage = page < totalPages,
            hasPreviousPage = page > 1
          }
        });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error fetching records");
        return Json(new { success = false, message = ex.Message });
      }
    }

    // GET /Collection/GetImage/{id}
    [HttpGet("GetImage/{id:guid}")]
    public async Task<IActionResult> GetImage(Guid id)
    {
      try
      {
        var img = await _context.Images.FindAsync(id);
        if (img == null || img.ImageData?.Length == 0)
          return NotFound("Image not found");

        Response.Headers["Cache-Control"] = "public,max-age=3600";
        return File(img.ImageData, "image/jpeg");
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error retrieving image {ImageId}", id);
        return BadRequest($"Error retrieving image: {ex.Message}");
      }
    }
  }
}
