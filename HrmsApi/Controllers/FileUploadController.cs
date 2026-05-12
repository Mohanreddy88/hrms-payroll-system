using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HrmsApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FileUploadController : ControllerBase
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<FileUploadController> _logger;

    public FileUploadController(IWebHostEnvironment env, ILogger<FileUploadController> logger)
    {
        _env = env;
        _logger = logger;
    }

    /// <summary>
    /// POST /api/fileupload/profile
    /// Upload employee profile picture
    /// </summary>
    [HttpPost("profile")]
    public async Task<IActionResult> UploadProfile(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file uploaded" });

        // Validate file type
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        
        if (!allowedExtensions.Contains(extension))
            return BadRequest(new { message = "Only image files (jpg, jpeg, png, gif) are allowed" });

        // Validate file size (max 5MB)
        if (file.Length > 5 * 1024 * 1024)
            return BadRequest(new { message = "File size must not exceed 5MB" });

        try
        {
            // Generate unique filename
            var fileName = $"{Guid.NewGuid()}{extension}";
            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "profiles");
            
            // Ensure directory exists
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var filePath = Path.Combine(uploadsFolder, fileName);

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Return relative URL path
            var fileUrl = $"/uploads/profiles/{fileName}";
            
            _logger.LogInformation("Profile picture uploaded: {FileName}", fileName);
            
            return Ok(new { filePath = fileUrl, fileName });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading profile picture");
            return StatusCode(500, new { message = "Error uploading file" });
        }
    }

    /// <summary>
    /// DELETE /api/fileupload/profile/{fileName}
    /// Delete profile picture
    /// </summary>
    [HttpDelete("profile/{fileName}")]
    public IActionResult DeleteProfile(string fileName)
    {
        try
        {
            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "profiles");
            var filePath = Path.Combine(uploadsFolder, fileName);

            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
                _logger.LogInformation("Profile picture deleted: {FileName}", fileName);
                return Ok(new { message = "File deleted successfully" });
            }

            return NotFound(new { message = "File not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting profile picture");
            return StatusCode(500, new { message = "Error deleting file" });
        }
    }
}
