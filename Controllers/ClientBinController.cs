using Microsoft.AspNetCore.Mvc;
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.Data;
using AspnetCoreMvcFull.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AspnetCoreMvcFull.Controllers
{
  public class ClientBinController : Controller
  {
    private readonly KUTIPDbContext _dbContext;

    public ClientBinController(KUTIPDbContext dbContext)
    {
      _dbContext = dbContext;
    }

    // GET: Show dashboard with clients and bins
    public async Task<IActionResult> Index()
    {
      var clients = await _dbContext.Clients.Include(c => c.Bins).ToListAsync();
      var bins = await _dbContext.Bins.Include(b => b.Client).ToListAsync();

      var vm = new ManageClientsBinsViewModel
      {
        Clients = clients,
        Bins = bins,
        Client = new Client(), // Initialize empty client for form
        Bin = new Bin() // Initialize empty bin for form
      };

      return View("Index", vm);
    }

    // POST: Create Client
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateClient(ManageClientsBinsViewModel model)
    {
      // Only validate the Client part of the model
      ModelState.Clear();
      TryValidateModel(model.Client, nameof(model.Client));

      if (ModelState.IsValid)
      {
        try
        {
          _dbContext.Clients.Add(model.Client);
          await _dbContext.SaveChangesAsync();
          TempData["SuccessMessage"] = "Client created successfully!";
          return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
          ModelState.AddModelError("", "An error occurred while creating the client: " + ex.Message);
        }
      }

      // If we got this far, something failed, redisplay form
      var clients = await _dbContext.Clients.Include(c => c.Bins).ToListAsync();
      var bins = await _dbContext.Bins.Include(b => b.Client).ToListAsync();

      var vm = new ManageClientsBinsViewModel
      {
        Clients = clients,
        Bins = bins,
        Client = model.Client, // Preserve input and validation errors
        Bin = new Bin()
      };

      return View("Index", vm);
    }

    // POST: Edit Client
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditClient(ManageClientsBinsViewModel model)
    {
      ModelState.Clear();
      TryValidateModel(model.Client, nameof(model.Client));

      if (ModelState.IsValid)
      {
        try
        {
          var existingClient = await _dbContext.Clients.FindAsync(model.Client.ClientID);
          if (existingClient == null)
            return NotFound();

          existingClient.ClientName = model.Client.ClientName;
          existingClient.Location = model.Client.Location;
          existingClient.NumOfBins = model.Client.NumOfBins;

          await _dbContext.SaveChangesAsync();
          TempData["SuccessMessage"] = "Client updated successfully!";
          return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateConcurrencyException)
        {
          if (!await _dbContext.Clients.AnyAsync(c => c.ClientID == model.Client.ClientID))
            return NotFound();
          else
            throw;
        }
        catch (Exception ex)
        {
          ModelState.AddModelError("", "An error occurred while updating the client: " + ex.Message);
        }
      }

      var clients = await _dbContext.Clients.Include(c => c.Bins).ToListAsync();
      var bins = await _dbContext.Bins.Include(b => b.Client).ToListAsync();

      var vm = new ManageClientsBinsViewModel
      {
        Clients = clients,
        Bins = bins,
        Client = model.Client,
        Bin = new Bin()
      };

      return View("Index", vm);
    }

    // POST: Delete Client Confirmed
    [HttpPost, ActionName("DeleteClientConfirmed")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteClientConfirmed(int id)
    {
      try
      {
        var client = await _dbContext.Clients.Include(c => c.Bins).FirstOrDefaultAsync(c => c.ClientID == id);

        if (client == null)
          return NotFound();

        if (client.Bins != null && client.Bins.Any())
        {
          TempData["ErrorMessage"] = "Cannot delete client with assigned bins. Please delete or reassign bins first.";
          return RedirectToAction(nameof(Index));
        }

        _dbContext.Clients.Remove(client);
        await _dbContext.SaveChangesAsync();
        TempData["SuccessMessage"] = "Client deleted successfully!";
      }
      catch (Exception ex)
      {
        TempData["ErrorMessage"] = "An error occurred while deleting the client: " + ex.Message;
      }

      return RedirectToAction(nameof(Index));
    }

    // POST: Create Bin
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateBin(ManageClientsBinsViewModel model)
    {
      // Clear ModelState and only validate the Bin part
      ModelState.Clear();

      // Manual validation for required fields
      if (string.IsNullOrWhiteSpace(model.Bin.BinPlateId))
        ModelState.AddModelError("Bin.BinPlateId", "Bin Plate ID is required");

      if (string.IsNullOrWhiteSpace(model.Bin.Location))
        ModelState.AddModelError("Bin.Location", "Location is required");

      if (string.IsNullOrWhiteSpace(model.Bin.Zone))
        ModelState.AddModelError("Bin.Zone", "Zone is required");

      if (model.Bin.ClientID <= 0)
        ModelState.AddModelError("Bin.ClientID", "Please select a client");

      if (model.Bin.FillLevel < 0 || model.Bin.FillLevel > 100)
        ModelState.AddModelError("Bin.FillLevel", "Fill level must be between 0 and 100");

      // Check if the selected client exists
      if (model.Bin.ClientID > 0)
      {
        var clientExists = await _dbContext.Clients.AnyAsync(c => c.ClientID == model.Bin.ClientID);
        if (!clientExists)
        {
          ModelState.AddModelError("Bin.ClientID", "Selected client does not exist");
        }
      }

      if (ModelState.IsValid)
      {
        try
        {
          model.Bin.Id = Guid.NewGuid();
          _dbContext.Bins.Add(model.Bin);
          await _dbContext.SaveChangesAsync();
          TempData["SuccessMessage"] = "Bin created successfully!";
          return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
          ModelState.AddModelError("", "An error occurred while creating the bin: " + ex.Message);
        }
      }

      // If we got this far, something failed, redisplay form
      var clients = await _dbContext.Clients.Include(c => c.Bins).ToListAsync();
      var bins = await _dbContext.Bins.Include(b => b.Client).ToListAsync();

      var vm = new ManageClientsBinsViewModel
      {
        Clients = clients,
        Bins = bins,
        Client = new Client(),
        Bin = model.Bin // Preserve input and validation errors
      };

      return View("Index", vm);
    }

    // POST: Edit Bin
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditBin(ManageClientsBinsViewModel model)
    {
      ModelState.Clear();
      TryValidateModel(model.Bin, nameof(model.Bin));

      if (ModelState.IsValid)
      {
        try
        {
          _dbContext.Update(model.Bin);
          await _dbContext.SaveChangesAsync();
          TempData["SuccessMessage"] = "Bin updated successfully!";
          return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateConcurrencyException)
        {
          if (!await _dbContext.Bins.AnyAsync(b => b.Id == model.Bin.Id))
            return NotFound();
          else
            throw;
        }
        catch (Exception ex)
        {
          ModelState.AddModelError("", "An error occurred while updating the bin: " + ex.Message);
        }
      }

      var clients = await _dbContext.Clients.Include(c => c.Bins).ToListAsync();
      var bins = await _dbContext.Bins.Include(b => b.Client).ToListAsync();

      var vm = new ManageClientsBinsViewModel
      {
        Clients = clients,
        Bins = bins,
        Client = new Client(),
        Bin = model.Bin
      };

      return View("Index", vm);
    }

    // POST: Delete Bin Confirmed
    [HttpPost, ActionName("DeleteBinConfirmed")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteBinConfirmed(Guid id)
    {
      try
      {
        var bin = await _dbContext.Bins.FindAsync(id);
        if (bin == null)
          return NotFound();

        _dbContext.Bins.Remove(bin);
        await _dbContext.SaveChangesAsync();
        TempData["SuccessMessage"] = "Bin deleted successfully!";
      }
      catch (Exception ex)
      {
        TempData["ErrorMessage"] = "An error occurred while deleting the bin: " + ex.Message;
      }

      return RedirectToAction(nameof(Index));
    }

    // GET: Get Client for editing (AJAX endpoint)
    [HttpGet]
    public async Task<IActionResult> GetClient(int id)
    {
      var client = await _dbContext.Clients.FindAsync(id);
      if (client == null)
        return NotFound();

      return Json(client);
    }

    // GET: Get Bin for editing (AJAX endpoint)
    [HttpGet]
    public async Task<IActionResult> GetBin(Guid id)
    {
      var bin = await _dbContext.Bins.Include(b => b.Client).FirstOrDefaultAsync(b => b.Id == id);
      if (bin == null)
        return NotFound();

      return Json(bin);
    }
  }
}




