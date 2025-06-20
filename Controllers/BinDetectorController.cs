using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AspnetCoreMvcFull.Data;
using AspnetCoreMvcFull.Models;
using Microsoft.AspNetCore.Http;
using System;
using Microsoft.EntityFrameworkCore;

namespace AspnetCoreMvcFull.Controllers
{
  [Route("BinDetector/[action]")]
  public class BinDetectorController : Controller
  {
    private readonly KUTIPDbContext _context;

    public BinDetectorController(KUTIPDbContext context)
    {
      _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> DetectBin([FromBody] DetectBinRequest request)
    {
      if (string.IsNullOrEmpty(request.Base64Image))
        return Json(new { success = false, message = "No image received" });

      // 1️⃣ Decode and Save the Base64 image to wwwroot/uploads
      var base64Data = Regex.Match(request.Base64Image, @"data:image/(?<type>.+?),(?<data>.+)").Groups["data"].Value;
      var imageBytes = Convert.FromBase64String(base64Data);
      var fileName = $"{Guid.NewGuid()}.png";
      var imagePath = Path.Combine("wwwroot", "uploads", fileName);
      await System.IO.File.WriteAllBytesAsync(imagePath, imageBytes);

      // 2️⃣ Run Python YOLO script
      var plateText = await RunPythonScript("python", "wwwroot/yolo_model/yolo_detect.py", imagePath);
      plateText = plateText.Trim().ToUpper();

      if (plateText == "NOPLATE")
        return Json(new { success = false, message = "No number plate detected." });

      // 3️⃣ Match plate to Bin in DB
      var bin = await _context.Bins.FirstOrDefaultAsync(b => b.BinPlateId.ToUpper() == plateText);
      if (bin == null)
        return Json(new { success = false, message = $"Plate '{plateText}' not matched to any Bin." });

      // 4️⃣ Create CollectionRecord
      var recordId = Guid.NewGuid();
      var collection = new CollectionRecord
      {
        Id = recordId,
        BinId = bin.Id,
        PickupTimestamp = DateTime.Now,
        BinPlateIdCaptured = plateText,
        GpsLatitude = 0, // Can add logic to get actual coords if needed
        GpsLongitude = 0,
        IssueReported = false,
        UserId = HttpContext.Session.GetInt32("UserId") ?? 0
      };
      _context.CollectionRecords.Add(collection);

      // 5️⃣ Save the image
      var image = new AspnetCoreMvcFull.Models.Image
      {
        Id = Guid.NewGuid(),
        CollectionRecordId = recordId,
        ImageUrl = $"/uploads/{fileName}",
        ImageType = "before",
        CapturedAt = DateTime.Now,
        CreatedAt = DateTime.Now
      };
      _context.Images.Add(image);

      await _context.SaveChangesAsync();

      return Json(new { success = true, plate = plateText });
    }

    private async Task<string> RunPythonScript(string pythonExe, string scriptPath, string imagePath)
    {
      var psi = new ProcessStartInfo
      {
        FileName = pythonExe,
        Arguments = $"\"{scriptPath}\" \"{imagePath}\"",
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
      };

      var process = Process.Start(psi);
      string output = await process.StandardOutput.ReadToEndAsync();
      string error = await process.StandardError.ReadToEndAsync();
      await process.WaitForExitAsync();

      return string.IsNullOrWhiteSpace(error) ? output : "NOPLATE";
    }
  }

  public class DetectBinRequest
  {
    public string Base64Image { get; set; }
  }
}
