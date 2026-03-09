namespace OnlineExamSystem.Application.DTOs;

/// <summary>
/// Request to create a new exam
/// </summary>
public class CreateExamRequest
{
    public string Title { get; set; } = string.Empty;
    public long SubjectId { get; set; }
    public long CreatedBy { get; set; }
    public int DurationMinutes { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
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
