using System.ComponentModel.DataAnnotations;

namespace OnlineExamSystem.Application.DTOs;

public class CreateExamRequest
{
    [Required]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;

    [Range(1, long.MaxValue)]
    public long SubjectId { get; set; }

    [Range(1, long.MaxValue)]
    public long CreatedBy { get; set; }

    [Range(1, 600)]
    public int DurationMinutes { get; set; }

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Request to update an existing exam
/// </summary>
public class UpdateExamRequest
{
    public string Title { get; set; } = string.Empty;
    public long SubjectId { get; set; }
    public int DurationMinutes { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Response for a single exam
/// </summary>
public class ExamResponse
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public long SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public long CreatedBy { get; set; }
    public int DurationMinutes { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "DRAFT";
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Response for list of exams
/// </summary>
public class ExamListResponse
{
    public List<ExamResponse> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

/// <summary>
/// Request to create or update exam settings
/// </summary>
public class ConfigureExamSettingsRequest
{
    public bool ShuffleQuestions { get; set; } = false;
    public bool ShuffleAnswers { get; set; } = false;
    public bool ShowResultImmediately { get; set; } = false;
    public bool AllowReview { get; set; } = false;
}

/// <summary>
/// Response for exam settings
/// </summary>
public class ExamSettingsResponse
{
    public long Id { get; set; }
    public long ExamId { get; set; }
    public bool ShuffleQuestions { get; set; }
    public bool ShuffleAnswers { get; set; }
    public bool ShowResultImmediately { get; set; }
    public bool AllowReview { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Request to change exam status
/// </summary>
public class ChangeExamStatusRequest
{
    public string Status { get; set; } = string.Empty; // DRAFT, ACTIVE, CLOSED
}

/// <summary>
/// Response for exam activation
/// </summary>
public class ActivateExamResponse
{
    public long ExamId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime ActivatedAt { get; set; }
    public string Message { get; set; } = string.Empty;
}
