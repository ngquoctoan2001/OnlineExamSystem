using System.ComponentModel.DataAnnotations;

namespace OnlineExamSystem.Application.DTOs;

/// <summary>
/// Import result for bulk operations
/// </summary>
public class ImportResult
{
    public int TotalRows { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public List<ImportError> Errors { get; set; } = new();
    public DateTime ImportedAt { get; set; }
}

/// <summary>
/// Error record for failed imports
/// </summary>
public class ImportError
{
    public int RowNumber { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public Dictionary<string, string?> RowData { get; set; } = new();
}

/// <summary>
/// Teacher import from Excel
/// </summary>
public class ImportTeacherRow
{
    [Required]
    public string? EmployeeCode { get; set; }

    [Required]
    public string? FirstName { get; set; }

    [Required]
    public string? LastName { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Department { get; set; }
}

/// <summary>
/// Student import from Excel
/// </summary>
public class ImportStudentRow
{
    [Required]
    public string? StudentCode { get; set; }

    [Required]
    public string? FirstName { get; set; }

    [Required]
    public string? LastName { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    public string? ClassName { get; set; }

    public DateTime? DateOfBirth { get; set; }
}

/// <summary>
/// Import history entry
/// </summary>
public class ImportHistoryResponse
{
    public long Id { get; set; }
    public string ImportType { get; set; } = string.Empty; // "Teacher" or "Student"
    public int TotalRows { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public long ImportedByUserId { get; set; }
    public DateTime ImportedAt { get; set; }
    public string FileName { get; set; } = string.Empty;
}

/// <summary>
/// Paginated import history response
/// </summary>
public class ImportHistoryListResponse
{
    public long Id { get; set; }
    public string ImportType { get; set; } = string.Empty;
    public int TotalRows { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public DateTime ImportedAt { get; set; }
}

/// <summary>
/// Question import from Excel row
/// Expected columns: Content, QuestionType, Subject, Difficulty, OptionA, OptionB, OptionC, OptionD, CorrectOption
/// </summary>
public class ImportQuestionRow
{
    [Required]
    public string? Content { get; set; }

    /// <summary>MCQ, TRUE_FALSE, SHORT_ANSWER, ESSAY, DRAWING</summary>
    public string QuestionType { get; set; } = "MCQ";

    public string? Subject { get; set; }

    /// <summary>EASY, MEDIUM, HARD</summary>
    public string Difficulty { get; set; } = "MEDIUM";

    public string? OptionA { get; set; }
    public string? OptionB { get; set; }
    public string? OptionC { get; set; }
    public string? OptionD { get; set; }

    /// <summary>A, B, C, or D — the label of the correct option (for MCQ/TRUE_FALSE)</summary>
    public string? CorrectOption { get; set; }
}
