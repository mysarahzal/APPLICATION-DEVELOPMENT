using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.Data;

namespace AspnetCoreMvcFull.Controllers
{
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

      if (ModelState.IsValid)
      {
        try
        {
          truck.Id = Guid.NewGuid();
          truck.Status = "Available"; // Set this manually
          truck.CreatedAt = DateTime.Now;
          truck.UpdatedAt = DateTime.Now;
          // CollectionRecords will be null initially (which is fine)

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

    // GET: Truck/Edit/5
    public async Task<IActionResult> Edit(Guid? id)
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
    public async Task<IActionResult> Edit(Guid id, [Bind("Id,LicensePlate,Model,Status,CreatedAt")] Truck truck)
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
    public async Task<IActionResult> Delete(Guid? id)
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
    public async Task<IActionResult> DeleteConfirmed(Guid id)
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

    private bool TruckExists(Guid id)
    {
      return _context.Trucks.Any(e => e.Id == id);
    }
  }
}
