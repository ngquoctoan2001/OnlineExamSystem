using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineExamSystem.Application.DTOs.Common;

namespace OnlineExamSystem.API.Controllers;

[ApiController]
[Route("api/upload")]
[Authorize]
[Produces("application/json")]
[Tags("Upload")]
public class UploadController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<UploadController> _logger;

    private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
    private static readonly HashSet<string> AllowedDocExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".tex", ".latex" };

    private const long MaxFileSize = 10 * 1024 * 1024; // 10MB

    public UploadController(IWebHostEnvironment environment, ILogger<UploadController> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    /// <summary>
    /// Upload an image file
    /// </summary>
    [HttpPost("image")]
    [Authorize(Roles = "ADMIN,TEACHER")]
    public async Task<ActionResult<ResponseResult<FileUploadResponse>>> UploadImage(IFormFile file)
    {
        return await ProcessUpload(file, "images", AllowedImageExtensions);
    }

    /// <summary>
    /// Upload a PDF file
    /// </summary>
    [HttpPost("pdf")]
    [Authorize(Roles = "ADMIN,TEACHER")]
    public async Task<ActionResult<ResponseResult<FileUploadResponse>>> UploadPdf(IFormFile file)
    {
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".pdf" };
        return await ProcessUpload(file, "pdfs", allowed);
    }

    /// <summary>
    /// Upload an exam document
    /// </summary>
    [HttpPost("exam-doc")]
    [Authorize(Roles = "ADMIN,TEACHER")]
    public async Task<ActionResult<ResponseResult<FileUploadResponse>>> UploadExamDoc(IFormFile file)
    {
        return await ProcessUpload(file, "exam-docs", AllowedDocExtensions);
    }

    /// <summary>
    /// Upload a canvas drawing
    /// </summary>
    [HttpPost("canvas")]
    [Authorize(Roles = "ADMIN,TEACHER,STUDENT")]
    public async Task<ActionResult<ResponseResult<FileUploadResponse>>> UploadCanvas(IFormFile file)
    {
        return await ProcessUpload(file, "canvas", AllowedImageExtensions);
    }

    private async Task<ActionResult<ResponseResult<FileUploadResponse>>> ProcessUpload(
        IFormFile? file, string subfolder, HashSet<string> allowedExtensions)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new ResponseResult<FileUploadResponse>
            {
                Success = false,
                Message = "No file provided or file is empty"
            });

        if (file.Length > MaxFileSize)
            return BadRequest(new ResponseResult<FileUploadResponse>
            {
                Success = false,
                Message = $"File size exceeds maximum allowed size of {MaxFileSize / (1024 * 1024)}MB"
            });

        var extension = Path.GetExtension(file.FileName);
        if (!allowedExtensions.Contains(extension))
            return BadRequest(new ResponseResult<FileUploadResponse>
            {
                Success = false,
                Message = $"File type '{extension}' is not allowed. Allowed types: {string.Join(", ", allowedExtensions)}"
            });

        var uploadsDir = Path.Combine(_environment.ContentRootPath, "uploads", subfolder);
        Directory.CreateDirectory(uploadsDir);

        var fileId = Guid.NewGuid().ToString("N");
        var safeFileName = fileId + extension;
        var filePath = Path.Combine(uploadsDir, safeFileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        _logger.LogInformation("File uploaded: {FileName} -> {FilePath}", file.FileName, safeFileName);

        return Ok(new ResponseResult<FileUploadResponse>
        {
            Success = true,
            Message = "File uploaded successfully",
            Data = new FileUploadResponse
            {
                FileId = fileId,
                FileName = file.FileName,
                FileSize = file.Length,
                ContentType = file.ContentType,
                Url = $"/api/files/{fileId}"
            }
        });
    }
}

public class FileUploadResponse
{
    public string FileId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}
