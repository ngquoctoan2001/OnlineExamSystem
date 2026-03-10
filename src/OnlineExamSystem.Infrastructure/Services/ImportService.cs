using Microsoft.Extensions.Logging;
using OnlineExamSystem.Application.DTOs;
using OnlineExamSystem.Domain.Entities;
using OnlineExamSystem.Infrastructure.Data;
using OnlineExamSystem.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace OnlineExamSystem.Infrastructure.Services;

/// <summary>
/// Service contract for bulk import operations
/// </summary>
public interface IImportService
{
    Task<(bool Success, ImportResult Result)> ImportTeachersAsync(Stream excelStream, long userId);
    Task<(bool Success, ImportResult Result)> ImportStudentsAsync(Stream excelStream, long userId);
    Task<(bool Success, ImportResult Result)> ImportQuestionsAsync(Stream excelStream, long userId);
}

/// <summary>
/// Bulk import service implementation
/// </summary>
public class ImportService : IImportService
{
    private readonly ITeacherService _teacherService;
    private readonly IStudentService _studentService;
    private readonly IExcelParserService _excelParser;
    private readonly IQuestionRepository _questionRepository;
    private readonly IQuestionOptionRepository _optionRepository;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ImportService> _logger;

    public ImportService(
        ITeacherService teacherService,
        IStudentService studentService,
        IExcelParserService excelParser,
        IQuestionRepository questionRepository,
        IQuestionOptionRepository optionRepository,
        ApplicationDbContext context,
        ILogger<ImportService> logger)
    {
        _teacherService = teacherService;
        _studentService = studentService;
        _excelParser = excelParser;
        _questionRepository = questionRepository;
        _optionRepository = optionRepository;
        _context = context;
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

    /// <summary>
    /// Import questions from Excel file.
    /// Expected columns: Content, QuestionType, Subject, Difficulty, OptionA, OptionB, OptionC, OptionD, CorrectOption
    /// </summary>
    public async Task<(bool Success, ImportResult Result)> ImportQuestionsAsync(Stream excelStream, long userId)
    {
        var result = new ImportResult { ImportedAt = DateTime.UtcNow };

        if (excelStream == null || excelStream.Length == 0)
        {
            result.Errors.Add(new ImportError { RowNumber = 0, ErrorMessage = "Stream is empty or null" });
            return (false, result);
        }

        try
        {
            var rows = await _excelParser.ParseExcelAsync<ImportQuestionRow>(excelStream);
            result.TotalRows = rows.Count;

            if (rows.Count == 0)
            {
                result.Errors.Add(new ImportError { RowNumber = 0, ErrorMessage = "No data rows found in Excel file" });
                return (false, result);
            }

            // Cache question types and subjects to avoid N+1 queries
            var questionTypes = await _context.QuestionTypes.AsNoTracking().ToListAsync();
            var subjects = await _context.Subjects.AsNoTracking().ToListAsync();

            var validDifficulties = new[] { "EASY", "MEDIUM", "HARD" };

            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                var rowNumber = i + 2;

                try
                {
                    if (string.IsNullOrWhiteSpace(row.Content))
                    {
                        result.FailedCount++;
                        result.Errors.Add(new ImportError
                        {
                            RowNumber = rowNumber,
                            ErrorMessage = "Content is required",
                            RowData = GetRowData(row)
                        });
                        continue;
                    }

                    // Resolve question type
                    var questionType = questionTypes.FirstOrDefault(qt =>
                        string.Equals(qt.Name, row.QuestionType, StringComparison.OrdinalIgnoreCase));

                    if (questionType == null)
                    {
                        result.FailedCount++;
                        result.Errors.Add(new ImportError
                        {
                            RowNumber = rowNumber,
                            ErrorMessage = $"Unknown question type: '{row.QuestionType}'. Valid values: {string.Join(", ", questionTypes.Select(q => q.Name))}",
                            RowData = GetRowData(row)
                        });
                        continue;
                    }

                    // Resolve subject (optional)
                    long subjectId = 0;
                    if (!string.IsNullOrWhiteSpace(row.Subject))
                    {
                        var subject = subjects.FirstOrDefault(s =>
                            string.Equals(s.Name, row.Subject, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(s.Code, row.Subject, StringComparison.OrdinalIgnoreCase));

                        if (subject == null)
                        {
                            result.FailedCount++;
                            result.Errors.Add(new ImportError
                            {
                                RowNumber = rowNumber,
                                ErrorMessage = $"Subject not found: '{row.Subject}'",
                                RowData = GetRowData(row)
                            });
                            continue;
                        }
                        subjectId = subject.Id;
                    }
                    else
                    {
                        // Use first subject as default if not specified
                        subjectId = subjects.FirstOrDefault()?.Id ?? 0;
                        if (subjectId == 0)
                        {
                            result.FailedCount++;
                            result.Errors.Add(new ImportError { RowNumber = rowNumber, ErrorMessage = "No subject found in the system", RowData = GetRowData(row) });
                            continue;
                        }
                    }

                    var difficulty = validDifficulties.Contains(row.Difficulty.ToUpper()) ? row.Difficulty.ToUpper() : "MEDIUM";

                    var question = new Question
                    {
                        Content = row.Content.Trim(),
                        QuestionTypeId = questionType.Id,
                        SubjectId = subjectId,
                        CreatedBy = userId,
                        Difficulty = difficulty,
                        IsPublished = false,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _questionRepository.CreateAsync(question);

                    // Create options for MCQ / TRUE_FALSE
                    var optionMap = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["A"] = row.OptionA,
                        ["B"] = row.OptionB,
                        ["C"] = row.OptionC,
                        ["D"] = row.OptionD
                    };

                    var options = new List<QuestionOption>();
                    int orderIndex = 0;
                    foreach (var (label, content) in optionMap)
                    {
                        if (string.IsNullOrWhiteSpace(content)) continue;

                        options.Add(new QuestionOption
                        {
                            QuestionId = question.Id,
                            Label = label,
                            Content = content.Trim(),
                            IsCorrect = string.Equals(label, row.CorrectOption, StringComparison.OrdinalIgnoreCase),
                            OrderIndex = orderIndex++
                        });
                    }

                    if (options.Any())
                        await _optionRepository.CreateBatchAsync(options);

                    result.SuccessCount++;
                    _logger.LogInformation("Imported question from row {Row}", rowNumber);
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
                    _logger.LogError(ex, "Error processing question import at row {Row}", rowNumber);
                }
            }

            _logger.LogInformation("Question import completed: {Success} success, {Failed} failed", result.SuccessCount, result.FailedCount);
            return (result.FailedCount == 0, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error during question import");
            result.Errors.Add(new ImportError { RowNumber = 0, ErrorMessage = $"Critical error: {ex.Message}" });
            return (false, result);
        }
    }
}
