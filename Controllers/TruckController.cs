using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.Data;
using Microsoft.AspNetCore.Authorization;

namespace AspnetCoreMvcFull.Controllers
{
  [Authorize(Roles = "Admin")]
  public class TruckController : Controller
  {
    private readonly KUTIPDbContext _context;

    public TruckController(KUTIPDbContext context)
    {
      _context = context;
    }

    // GET: Truck
    public async Task<IActionResult> Index()
    {
      var trucks = await _context.Trucks
          .Include(t => t.Driver)
          .OrderBy(t => t.LicensePlate)
          .ToListAsync();
      return View(trucks);
    }

    // GET: Truck/Create
    public async Task<IActionResult> Create()
    {
      try
      {
        // Get available drivers (users with role "Driver" who don't have a truck assigned)
        var assignedDriverIds = await _context.Trucks
            .Select(t => t.DriverId)
            .ToListAsync();

        var availableDrivers = await _context.Users
            .Where(u => u.Role == "Driver" && !assignedDriverIds.Contains(u.Id))
            .OrderBy(u => u.FirstName)
            .ToListAsync();

        ViewBag.Drivers = availableDrivers;

        if (!availableDrivers.Any())
        {
          ViewBag.ErrorMessage = "No available drivers found. Please create drivers first or check if all drivers are already assigned to trucks.";
        }

        return View();
      }
      catch (Exception ex)
      {
        ViewBag.ErrorMessage = "Error loading drivers: " + ex.Message;
        ViewBag.Drivers = new List<User>();
        return View();
      }
    }

    // POST: Truck/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("LicensePlate,Model,DriverId")] Truck truck)
    {
      // Remove validation errors for fields we don't want to validate during creation
      ModelState.Remove("Status");
      ModelState.Remove("CollectionRecords");
      ModelState.Remove("CreatedAt");
      ModelState.Remove("UpdatedAt");
      ModelState.Remove("Id");
      ModelState.Remove("Driver");

      if (ModelState.IsValid)
      {
        try
        {
          // Check if driver exists and has role "Driver"
          var driver = await _context.Users.FindAsync(truck.DriverId);
          if (driver == null || driver.Role != "Driver")
          {
            ModelState.AddModelError("DriverId", "Selected driver does not exist or is not a driver.");
          }

          // Check if driver is already assigned to another truck
          var existingTruck = await _context.Trucks
              .FirstOrDefaultAsync(t => t.DriverId == truck.DriverId);
          if (existingTruck != null)
          {
            ModelState.AddModelError("DriverId", "This driver is already assigned to another truck.");
          }

          // Check if license plate already exists
          var existingPlate = await _context.Trucks
              .FirstOrDefaultAsync(t => t.LicensePlate.ToLower() == truck.LicensePlate.ToLower());
          if (existingPlate != null)
          {
            ModelState.AddModelError("LicensePlate", "A truck with this license plate already exists.");
          }

          if (ModelState.IsValid)
          {
            truck.Status = "Available";
            truck.CreatedAt = DateTime.Now;
            truck.UpdatedAt = DateTime.Now;

            _context.Add(truck);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Truck created successfully and assigned to {driver.FirstName} {driver.LastName}!";
            return RedirectToAction(nameof(Index));
          }
        }
        catch (Exception ex)
        {
          ModelState.AddModelError("", "An error occurred while creating the truck: " + ex.Message);
        }
      }

      // Reload drivers if validation fails
      try
      {
        var assignedDriverIds = await _context.Trucks
            .Where(t => t.Id != truck.Id) // Exclude current truck if editing
            .Select(t => t.DriverId)
            .ToListAsync();

        var availableDrivers = await _context.Users
            .Where(u => u.Role == "Driver" && !assignedDriverIds.Contains(u.Id))
            .OrderBy(u => u.FirstName)
            .ToListAsync();

        ViewBag.Drivers = availableDrivers;
      }
      catch
      {
        ViewBag.Drivers = new List<User>();
      }

      return View(truck);
    }

    // GET: Truck/Details/5
    public async Task<IActionResult> Details(int? id)
    {
      if (id == null)
      {
        return NotFound();
      }

      var truck = await _context.Trucks
          .Include(t => t.Driver)
          .FirstOrDefaultAsync(m => m.Id == id);

      if (truck == null)
      {
        return NotFound();
      }

      return View(truck);
    }

    // GET: Truck/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
      if (id == null)
      {
        return NotFound();
      }

      var truck = await _context.Trucks
          .Include(t => t.Driver)
          .FirstOrDefaultAsync(t => t.Id == id);

      if (truck == null)
      {
        return NotFound();
      }

      try
      {
        // Get available drivers (excluding current driver)
        var assignedDriverIds = await _context.Trucks
            .Where(t => t.Id != id)
            .Select(t => t.DriverId)
            .ToListAsync();

        var availableDrivers = await _context.Users
            .Where(u => u.Role == "Driver" && !assignedDriverIds.Contains(u.Id))
            .OrderBy(u => u.FirstName)
            .ToListAsync();

        ViewBag.Drivers = availableDrivers;
      }
      catch
      {
        ViewBag.Drivers = new List<User>();
      }

      return View(truck);
    }

    // POST: Truck/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,LicensePlate,Model,Status,DriverId,CreatedAt")] Truck truck)
    {
      if (id != truck.Id)
      {
        return NotFound();
      }

      // Remove validation errors for fields we don't want to validate during editing
      ModelState.Remove("CollectionRecords");
      ModelState.Remove("UpdatedAt");
      ModelState.Remove("Driver");

      if (ModelState.IsValid)
      {
        try
        {
          // Check if driver exists and has role "Driver"
          var driver = await _context.Users.FindAsync(truck.DriverId);
          if (driver == null || driver.Role != "Driver")
          {
            ModelState.AddModelError("DriverId", "Selected driver does not exist or is not a driver.");
          }

          // Check if driver is already assigned to another truck (excluding current truck)
          var existingTruck = await _context.Trucks
              .FirstOrDefaultAsync(t => t.DriverId == truck.DriverId && t.Id != truck.Id);
          if (existingTruck != null)
          {
            ModelState.AddModelError("DriverId", "This driver is already assigned to another truck.");
          }

          if (ModelState.IsValid)
          {
            truck.UpdatedAt = DateTime.Now;
            _context.Update(truck);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Truck updated successfully!";
            return RedirectToAction(nameof(Index));
          }
        }
        catch (DbUpdateConcurrencyException)
        {
          if (!TruckExists(truck.Id))
          {
            return NotFound();
          }
          else
          {
            throw;
          }
        }
        catch (Exception ex)
        {
          ModelState.AddModelError("", "An error occurred while updating the truck: " + ex.Message);
        }
      }

      // Reload drivers if validation fails
      try
      {
        var assignedDriverIds = await _context.Trucks
            .Where(t => t.Id != id)
            .Select(t => t.DriverId)
            .ToListAsync();

        var availableDrivers = await _context.Users
            .Where(u => u.Role == "Driver" && !assignedDriverIds.Contains(u.Id))
            .OrderBy(u => u.FirstName)
            .ToListAsync();

        ViewBag.Drivers = availableDrivers;
      }
      catch
      {
        ViewBag.Drivers = new List<User>();
      }

      return View(truck);
    }

    // GET: Truck/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
      if (id == null)
      {
        return NotFound();
      }

      var truck = await _context.Trucks
          .Include(t => t.Driver)
          .FirstOrDefaultAsync(m => m.Id == id);

      if (truck == null)
      {
        return NotFound();
      }

      return View(truck);
    }

    // POST: Truck/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
      var truck = await _context.Trucks.FindAsync(id);
      if (truck != null)
      {
        _context.Trucks.Remove(truck);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Truck deleted successfully!";
      }

      return RedirectToAction(nameof(Index));
    }

    private bool TruckExists(int id)
    {
      return _context.Trucks.Any(e => e.Id == id);
    }
  }
}
