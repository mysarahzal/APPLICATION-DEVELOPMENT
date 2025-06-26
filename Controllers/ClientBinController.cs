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

      // Add this validation after the existing validations in CreateBin method
      if (!model.Bin.Latitude.HasValue || !model.Bin.Longitude.HasValue)
        ModelState.AddModelError("", "Please select a location from the map suggestions to get coordinates");

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
    // POST: Edit Bin
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditBin(ManageClientsBinsViewModel model)
    {
      // Clear ModelState and manually validate the Bin
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

      // Check if the bin exists
      var existingBin = await _dbContext.Bins.FindAsync(model.Bin.Id);
      if (existingBin == null)
      {
        TempData["ErrorMessage"] = "Bin not found.";
        return RedirectToAction(nameof(Index));
      }

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
          // Update only the properties we want to change
          existingBin.BinPlateId = model.Bin.BinPlateId;
          existingBin.Location = model.Bin.Location;
          existingBin.FillLevel = model.Bin.FillLevel;
          existingBin.Zone = model.Bin.Zone;
          existingBin.ClientID = model.Bin.ClientID;

          // Add these lines after the existing property updates
          existingBin.Latitude = model.Bin.Latitude;
          existingBin.Longitude = model.Bin.Longitude;

          await _dbContext.SaveChangesAsync();
          TempData["SuccessMessage"] = "Bin updated successfully!";
          return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
          TempData["ErrorMessage"] = "An error occurred while updating the bin: " + ex.Message;
        }
      }
      else
      {
        // Log validation errors for debugging
        foreach (var error in ModelState)
        {
          Console.WriteLine($"Validation Error - Key: {error.Key}, Errors: {string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage))}");
        }
        TempData["ErrorMessage"] = "Please correct the validation errors and try again.";
      }

      return RedirectToAction(nameof(Index));
    }
    //[HttpPost]
    //[ValidateAntiForgeryToken]
    //public async Task<IActionResult> EditBin(ManageClientsBinsViewModel model)
    //{
    //  ModelState.Clear();
    //  TryValidateModel(model.Bin, nameof(model.Bin));

    //  if (ModelState.IsValid)
    //  {
    //    try
    //    {
    //      _dbContext.Update(model.Bin);
    //      await _dbContext.SaveChangesAsync();
    //      TempData["SuccessMessage"] = "Bin updated successfully!";
    //      return RedirectToAction(nameof(Index));
    //    }
    //    catch (DbUpdateConcurrencyException)
    //    {
    //      if (!await _dbContext.Bins.AnyAsync(b => b.Id == model.Bin.Id))
    //        return NotFound();
    //      else
    //        throw;
    //    }
    //    catch (Exception ex)
    //    {
    //      ModelState.AddModelError("", "An error occurred while updating the bin: " + ex.Message);
    //    }
    //  }

    //  var clients = await _dbContext.Clients.Include(c => c.Bins).ToListAsync();
    //  var bins = await _dbContext.Bins.Include(b => b.Client).ToListAsync();

    //  var vm = new ManageClientsBinsViewModel
    //  {
    //    Clients = clients,
    //    Bins = bins,
    //    Client = new Client(),
    //    Bin = model.Bin
    //  };

    //  return View("Index", vm);
    //}

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
    // GET: Get Bin for editing (AJAX endpoint)
    [HttpGet]
    public async Task<IActionResult> GetBin(string id) // Change from Guid to string
    {
      try
      {
        // Parse the string to Guid
        if (!Guid.TryParse(id, out Guid binId))
        {
          Console.WriteLine($"Invalid Guid format: {id}");
          return BadRequest("Invalid bin ID format");
        }

        Console.WriteLine($"Fetching bin with ID: {binId}");

        var bin = await _dbContext.Bins
            .Include(b => b.Client)
            .FirstOrDefaultAsync(b => b.Id == binId);

        if (bin == null)
        {
          Console.WriteLine("Bin not found.");
          return NotFound();
        }

        return Json(new
        {
          id = bin.Id.ToString(), // Convert Guid to string for JavaScript
          binPlateId = bin.BinPlateId,
          location = bin.Location,
          fillLevel = bin.FillLevel,
          zone = bin.Zone,
          clientID = bin.ClientID,
          clientName = bin.Client?.ClientName,
          // Add these properties to the JSON return object
          latitude = bin.Latitude,
          longitude = bin.Longitude,
        });
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Error fetching bin: {ex.Message}");
        return StatusCode(500, new { error = "Internal server error", message = ex.Message });
      }
    }
    //[HttpGet]
    //public async Task<IActionResult> GetBin(Guid id)
    //{
    //  try
    //  {
    //    // Log the incoming ID
    //    Console.WriteLine($"Fetching bin with ID: {id}");

    //    var bin = await _dbContext.Bins
    //        .Include(b => b.Client)
    //        .FirstOrDefaultAsync(b => b.Id == id);

    //    if (bin == null)
    //    {
    //      Console.WriteLine("Bin not found.");
    //      return NotFound();
    //    }

    //    // Optional: Remove circular references or problematic fields
    //    return Json(new
    //    {
    //      bin.Id,
    //      bin.BinPlateId,
    //      bin.Location,
    //      bin.FillLevel,
    //      bin.Zone,
    //      bin.ClientID,
    //      ClientName = bin.Client?.ClientName
    //    });
    //  }
    //  catch (Exception ex)
    //  {
    //    // Log full exception
    //    Console.WriteLine($"Error fetching bin: {ex.Message}");
    //    Console.WriteLine(ex.StackTrace);

    //    return StatusCode(500, new { error = "Internal server error", message = ex.Message });
    //  }
    //}
  }
}




