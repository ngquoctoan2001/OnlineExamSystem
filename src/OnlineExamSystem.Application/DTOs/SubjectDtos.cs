namespace OnlineExamSystem.Application.DTOs;

/// <summary>
/// DTO for creating a new subject
/// </summary>
public class CreateSubjectRequest
{
    /// <summary>Subject code (unique)</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Subject name</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Subject description</summary>
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// DTO for updating subject information
/// </summary>
public class UpdateSubjectRequest
{
    /// <summary>Subject name</summary>
    public string? Name { get; set; }

    /// <summary>Subject description</summary>
    public string? Description { get; set; }
}

/// <summary>
/// DTO for subject response
/// </summary>
public class SubjectResponse
{
    /// <summary>Subject ID</summary>
    public long Id { get; set; }

    /// <summary>Subject code</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Subject name</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Subject description</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Number of questions</summary>
    public int QuestionCount { get; set; }

    /// <summary>Number of exams</summary>
    public int ExamCount { get; set; }
}

/// <summary>
/// DTO for subject list with pagination
/// </summary>
public class SubjectListResponse
{
    /// <summary>Total count</summary>
    public int TotalCount { get; set; }

    /// <summary>Page size</summary>
    public int PageSize { get; set; }

    /// <summary>Current page</summary>
    public int CurrentPage { get; set; }

    /// <summary>Total pages</summary>
    public int TotalPages { get; set; }

    /// <summary>Subjects</summary>
    public List<SubjectResponse> Subjects { get; set; } = new();
}
