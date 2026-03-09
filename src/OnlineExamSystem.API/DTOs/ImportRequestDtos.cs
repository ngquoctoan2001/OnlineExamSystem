using System.ComponentModel.DataAnnotations;

namespace OnlineExamSystem.API.DTOs;

/// <summary>
/// Bulk teacher import request for API layer
/// </summary>
public class BulkImportTeacherRequest
{
    [Required]
    public IFormFile? ExcelFile { get; set; }
}

/// <summary>
/// Bulk student import request for API layer
/// </summary>
public class BulkImportStudentRequest
{
    [Required]
    public IFormFile? ExcelFile { get; set; }
}
