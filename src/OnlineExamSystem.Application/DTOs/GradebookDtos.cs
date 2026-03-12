namespace OnlineExamSystem.Application.DTOs;

public class GradebookEntryResponse
{
    public long ExamId { get; set; }
    public string ExamTitle { get; set; } = string.Empty;
    public long? SubjectExamTypeId { get; set; }
    public string? SubjectExamTypeName { get; set; }
    public decimal Coefficient { get; set; } = 1;
    public decimal? Score { get; set; }
    public decimal? TotalPoints { get; set; }
    public decimal? ScoreOn10 { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? CompletedAt { get; set; }
}

public class StudentSubjectGradebookResponse
{
    public long StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentCode { get; set; } = string.Empty;
    public long SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public List<GradebookEntryResponse> Entries { get; set; } = new();
    public decimal? WeightedAverage { get; set; }
}

public class StudentFullGradebookResponse
{
    public long StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentCode { get; set; } = string.Empty;
    public List<SubjectGradeSummary> Subjects { get; set; } = new();
    public decimal? OverallAverage { get; set; }
}

public class SubjectGradeSummary
{
    public long SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public List<GradebookEntryResponse> Entries { get; set; } = new();
    public decimal? WeightedAverage { get; set; }
}

public class ClassSubjectGradebookResponse
{
    public long ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public long SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public List<StudentSubjectGradebookResponse> Students { get; set; } = new();
    public List<SubjectExamTypeResponse> ExamTypes { get; set; } = new();
}
