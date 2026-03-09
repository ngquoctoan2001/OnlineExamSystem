namespace OnlineExamSystem.Application.DTOs;

/// <summary>
/// Request to assign a class to an exam
/// </summary>
public class AssignClassToExamRequest
{
    public long ExamId { get; set; }
    public long ClassId { get; set; }
}

/// <summary>
/// Response for exam-class assignment
/// </summary>
public class ExamClassResponse
{
    public long ExamId { get; set; }
    public long ClassId { get; set; }
    public string ExamTitle { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; }
}

/// <summary>
/// Response for list of exam-class assignments
/// </summary>
public class ExamClassListResponse
{
    public List<ExamClassResponse> Items { get; set; } = new();
    public int TotalCount { get; set; }
}

/// <summary>
/// Response for classes assigned to an exam
/// </summary>
public class ExamClassesResponse
{
    public long ExamId { get; set; }
    public string ExamTitle { get; set; } = string.Empty;
    public List<ClassAssignmentResponse> Classes { get; set; } = new();
}

/// <summary>
/// Individual class assignment detail
/// </summary>
public class ClassAssignmentResponse
{
    public long ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public int StudentCount { get; set; }
    public DateTime AssignedAt { get; set; }
}
