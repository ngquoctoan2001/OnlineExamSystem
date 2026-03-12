using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineExamSystem.Application.DTOs.Common;

namespace OnlineExamSystem.API.Controllers;

[ApiController]
[Route("api/files")]
[Authorize]
[Tags("Files")]
public class FilesController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;

    public FilesController(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    /// <summary>
    /// Get/download a file by ID
    /// </summary>
    [HttpGet("{fileId}")]
    public IActionResult GetFile(string fileId)
    {
        if (string.IsNullOrWhiteSpace(fileId) || fileId.Contains("..") || fileId.Contains('/') || fileId.Contains('\\'))
            return BadRequest(new ResponseResult<object> { Success = false, Message = "Invalid file ID" });

        var uploadsDir = Path.Combine(_environment.ContentRootPath, "uploads");
        if (!Directory.Exists(uploadsDir))
            return NotFound(new ResponseResult<object> { Success = false, Message = "File not found" });

        var subfolders = new[] { "images", "pdfs", "exam-docs", "canvas" };
        foreach (var subfolder in subfolders)
        {
            var folderPath = Path.Combine(uploadsDir, subfolder);
            if (!Directory.Exists(folderPath)) continue;

            var matchingFiles = Directory.GetFiles(folderPath, fileId + ".*");
            if (matchingFiles.Length > 0)
            {
                var filePath = matchingFiles[0];
                // Verify the resolved path is within uploads directory
                var fullPath = Path.GetFullPath(filePath);
                if (!fullPath.StartsWith(Path.GetFullPath(uploadsDir), StringComparison.OrdinalIgnoreCase))
                    return BadRequest(new ResponseResult<object> { Success = false, Message = "Invalid file path" });

                var contentType = GetContentType(filePath);
                var fileName = Path.GetFileName(filePath);
                return PhysicalFile(fullPath, contentType, fileName);
            }
        }

        return NotFound(new ResponseResult<object> { Success = false, Message = "File not found" });
    }

    private static string GetContentType(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".webp" => "image/webp",
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            _ => "application/octet-stream"
        };
    }
}
