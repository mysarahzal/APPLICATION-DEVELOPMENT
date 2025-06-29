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
      var trucks = await _context.Trucks.ToListAsync();
      return View(trucks);
    }

    // GET: Truck/Create
    public IActionResult Create()
    {
      return View();
    }

    // POST: Truck/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("LicensePlate,Model")] Truck truck)
    {
      // Remove validation errors for fields we don't want to validate during creation
      ModelState.Remove("Status");
      ModelState.Remove("CollectionRecords");
      ModelState.Remove("CreatedAt");
      ModelState.Remove("UpdatedAt");
      ModelState.Remove("Id"); // Remove Id validation since it's auto-generated

      if (ModelState.IsValid)
      {
        try
        {
          // Don't set truck.Id - let the database auto-generate it
          truck.Status = "Available";
          truck.CreatedAt = DateTime.Now;
          truck.UpdatedAt = DateTime.Now;

          _context.Add(truck);
          await _context.SaveChangesAsync();

          TempData["Success"] = "Truck created successfully!";
          return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
          ModelState.AddModelError("", "An error occurred while creating the truck: " + ex.Message);
        }
      }

      return View(truck);
    }

    // GET: Truck/Details/5
    public async Task<IActionResult> Details(int? id)  // Changed from Guid? to int?
    {
      if (id == null)
      {
        return NotFound();
      }

      var truck = await _context.Trucks
          .FirstOrDefaultAsync(m => m.Id == id);

      if (truck == null)
      {
        return NotFound();
      }

      return View(truck);
    }

    // GET: Truck/Edit/5
    public async Task<IActionResult> Edit(int? id)  // Changed from Guid? to int?
    {
      if (id == null)
      {
        return NotFound();
      }

      var truck = await _context.Trucks.FindAsync(id);
      if (truck == null)
      {
        return NotFound();
      }
      return View(truck);
    }

    // POST: Truck/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,LicensePlate,Model,Status,CreatedAt")] Truck truck)  // Changed from Guid to int
    {
      if (id != truck.Id)
      {
        return NotFound();
      }

      // Remove validation errors for fields we don't want to validate during editing
      ModelState.Remove("CollectionRecords");
      ModelState.Remove("UpdatedAt");

      if (ModelState.IsValid)
      {
        try
        {
          truck.UpdatedAt = DateTime.Now;
          _context.Update(truck);
          await _context.SaveChangesAsync();

          TempData["Success"] = "Truck updated successfully!";
          return RedirectToAction(nameof(Index));
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
      return View(truck);
    }

    // GET: Truck/Delete/5
    public async Task<IActionResult> Delete(int? id)  // Changed from Guid? to int?
    {
      if (id == null)
      {
        return NotFound();
      }

      var truck = await _context.Trucks
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
    public async Task<IActionResult> DeleteConfirmed(int id)  // Changed from Guid to int
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

    private bool TruckExists(int id)  // Changed from Guid to int
    {
      return _context.Trucks.Any(e => e.Id == id);
    }
  }
}
