using Microsoft.Extensions.Logging;
using OnlineExamSystem.Application.DTOs;
using OnlineExamSystem.Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace OnlineExamSystem.Infrastructure.Services;

/// <summary>
/// Service contract for bulk import operations
/// </summary>
public interface IImportService
{
    Task<(bool Success, ImportResult Result)> ImportTeachersAsync(Stream excelStream, long userId);
    Task<(bool Success, ImportResult Result)> ImportStudentsAsync(Stream excelStream, long userId);
}

/// <summary>
/// Bulk import service implementation
/// </summary>
public class ImportService : IImportService
{
    private readonly ITeacherService _teacherService;
    private readonly IStudentService _studentService;
    private readonly IExcelParserService _excelParser;
    private readonly ILogger<ImportService> _logger;

    public ImportService(
        ITeacherService teacherService,
        IStudentService studentService,
        IExcelParserService excelParser,
        ILogger<ImportService> logger)
    {
        _teacherService = teacherService;
        _studentService = studentService;
        _excelParser = excelParser;
        _logger = logger;
    }

    /// <summary>
    /// Import teachers from Excel file
    /// </summary>
    public async Task<(bool Success, ImportResult Result)> ImportTeachersAsync(Stream excelStream, long userId)
    {
        var result = new ImportResult { ImportedAt = DateTime.UtcNow };

        if (excelStream == null || excelStream.Length == 0)
        {
            result.Errors.Add(new ImportError { RowNumber = 0, ErrorMessage = "Stream is empty or null" });
            _logger.LogWarning("Teacher import attempted with null or empty stream");
            return (false, result);
        }

        try
        {
            // Parse Excel file
            var rows = await _excelParser.ParseExcelAsync<ImportTeacherRow>(excelStream);
            result.TotalRows = rows.Count;

            if (rows.Count == 0)
            {
                result.Errors.Add(new ImportError { RowNumber = 0, ErrorMessage = "No data rows found in Excel file" });
                _logger.LogWarning("No data rows found in teacher import file");
                return (false, result);
            }

            // Process each row
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                var rowNumber = i + 2; // Row 1 is header, so data starts at row 2

                try
                {
                    // Validate required fields
                    if (string.IsNullOrWhiteSpace(row.EmployeeCode) ||
                        string.IsNullOrWhiteSpace(row.FirstName) ||
                        string.IsNullOrWhiteSpace(row.LastName))
                    {
                        result.FailedCount++;
                        result.Errors.Add(new ImportError
                        {
                            RowNumber = rowNumber,
                            ErrorMessage = "Employee code, first name, and last name are required",
                            RowData = GetRowData(row)
                        });
                        continue;
                    }

                    // Create teacher request
                    var request = new CreateTeacherRequest
                    {
                        EmployeeId = row.EmployeeCode.Trim(),
                        FullName = $"{row.FirstName.Trim()} {row.LastName.Trim()}",
                        Email = row.Email?.Trim() ?? $"{row.EmployeeCode.Trim()}@school.edu",
                        Username = row.EmployeeCode.Trim().ToLower(),
                        Password = "TempPass123!", // Temporary password - user should change on first login
                        Department = row.Department?.Trim() ?? "General"
                    };

                    // Create teacher
                    var (success, message, data) = await _teacherService.CreateTeacherAsync(request);

                    if (success)
                    {
                        result.SuccessCount++;
                        _logger.LogInformation($"Successfully imported teacher: {row.EmployeeCode}");
                    }
                    else
                    {
                        result.FailedCount++;
                        result.Errors.Add(new ImportError
                        {
                            RowNumber = rowNumber,
                            ErrorMessage = message,
                            RowData = GetRowData(row)
                        });
                        _logger.LogWarning($"Failed to import teacher at row {rowNumber}: {message}");
                    }
                }
                catch (Exception ex)
                {
                    result.FailedCount++;
                    result.Errors.Add(new ImportError
                    {
                        RowNumber = rowNumber,
                        ErrorMessage = $"Exception: {ex.Message}",
                        RowData = GetRowData(row)
                    });
                    _logger.LogError($"Error processing teacher import at row {rowNumber}: {ex.Message}");
                }
            }

            _logger.LogInformation($"Teacher import completed: {result.SuccessCount} success, {result.FailedCount} failed");
            return (result.FailedCount == 0, result);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Critical error during teacher import: {ex.Message}");
            result.Errors.Add(new ImportError { RowNumber = 0, ErrorMessage = $"Critical error: {ex.Message}" });
            return (false, result);
        }
    }

    /// <summary>
    /// Import students from Excel file
    /// </summary>
    public async Task<(bool Success, ImportResult Result)> ImportStudentsAsync(Stream excelStream, long userId)
    {
        var result = new ImportResult { ImportedAt = DateTime.UtcNow };

        if (excelStream == null || excelStream.Length == 0)
        {
            result.Errors.Add(new ImportError { RowNumber = 0, ErrorMessage = "Stream is empty or null" });
            _logger.LogWarning("Student import attempted with null or empty stream");
            return (false, result);
        }

        try
        {
            // Parse Excel file
            var rows = await _excelParser.ParseExcelAsync<ImportStudentRow>(excelStream);
            result.TotalRows = rows.Count;

            if (rows.Count == 0)
            {
                result.Errors.Add(new ImportError { RowNumber = 0, ErrorMessage = "No data rows found in Excel file" });
                _logger.LogWarning("No data rows found in student import file");
                return (false, result);
            }

            // Process each row
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                var rowNumber = i + 2; // Row 1 is header, so data starts at row 2

                try
                {
                    // Validate required fields
                    if (string.IsNullOrWhiteSpace(row.StudentCode) ||
                        string.IsNullOrWhiteSpace(row.FirstName) ||
                        string.IsNullOrWhiteSpace(row.LastName))
                    {
                        result.FailedCount++;
                        result.Errors.Add(new ImportError
                        {
                            RowNumber = rowNumber,
                            ErrorMessage = "Student code, first name, and last name are required",
                            RowData = GetRowData(row)
                        });
                        continue;
                    }

                    // Create student request
                    var request = new CreateStudentRequest
                    {
                        StudentCode = row.StudentCode.Trim(),
                        FullName = $"{row.FirstName.Trim()} {row.LastName.Trim()}",
                        Email = row.Email?.Trim() ?? $"{row.StudentCode.Trim()}@school.edu",
                        Username = row.StudentCode.Trim().ToLower(),
                        Password = "TempPass123!", // Temporary password - user should change on first login
                        RollNumber = string.Empty // Will be assigned separately if needed
                    };

                    // Create student
                    var (success, message, data) = await _studentService.CreateStudentAsync(request);

                    if (success)
                    {
                        result.SuccessCount++;
                        _logger.LogInformation($"Successfully imported student: {row.StudentCode}");
                    }
                    else
                    {
                        result.FailedCount++;
                        result.Errors.Add(new ImportError
                        {
                            RowNumber = rowNumber,
                            ErrorMessage = message,
                            RowData = GetRowData(row)
                        });
                        _logger.LogWarning($"Failed to import student at row {rowNumber}: {message}");
                    }
                }
                catch (Exception ex)
                {
                    result.FailedCount++;
                    result.Errors.Add(new ImportError
                    {
                        RowNumber = rowNumber,
                        ErrorMessage = $"Exception: {ex.Message}",
                        RowData = GetRowData(row)
                    });
                    _logger.LogError($"Error processing student import at row {rowNumber}: {ex.Message}");
                }
            }

            _logger.LogInformation($"Student import completed: {result.SuccessCount} success, {result.FailedCount} failed");
            return (result.FailedCount == 0, result);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Critical error during student import: {ex.Message}");
            result.Errors.Add(new ImportError { RowNumber = 0, ErrorMessage = $"Critical error: {ex.Message}" });
            return (false, result);
        }
    }

    /// <summary>
    /// Helper to convert object to dictionary for error tracking
    /// </summary>
    private static Dictionary<string, string?> GetRowData<T>(T obj) where T : class
    {
        var result = new Dictionary<string, string?>();
        foreach (var prop in typeof(T).GetProperties())
        {
            var value = prop.GetValue(obj);
            result[prop.Name] = value?.ToString();
        }
        return result;
    }
}
