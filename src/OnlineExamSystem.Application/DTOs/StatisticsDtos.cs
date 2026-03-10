namespace OnlineExamSystem.Application.DTOs;

public class ExamStatisticResponse
{
    public long ExamId { get; set; }
    public string ExamTitle { get; set; } = string.Empty;
    public int TotalAttempts { get; set; }
    public int PassCount { get; set; }
    public int FailCount { get; set; }
    public decimal PassRate { get; set; }
    public decimal AverageScore { get; set; }
    public decimal MaxScore { get; set; }
    public decimal MinScore { get; set; }
    public DateTime CalculatedAt { get; set; }
}

public class ScoreDistributionResponse
{
    public long ExamId { get; set; }
    public List<ScoreBucket> Buckets { get; set; } = new();
}

public class ScoreBucket
{
    public string Label { get; set; } = string.Empty;
    public decimal Min { get; set; }
    public decimal Max { get; set; }
    public int Count { get; set; }
}

public class StudentPerformanceResponse
{
    public long StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public int TotalAttempts { get; set; }
    public decimal AverageScore { get; set; }
    public List<StudentAttemptSummary> Attempts { get; set; } = new();
}

public class StudentAttemptSummary
{
    public long AttemptId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public long ExamId { get; set; }
    public string ExamTitle { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public decimal? Score { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
}

public class ClassResultsResponse
{
    public long ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public long ExamId { get; set; }
    public string ExamTitle { get; set; } = string.Empty;
    public int TotalStudents { get; set; }
    public int AttemptedCount { get; set; }
    public decimal AverageScore { get; set; }
    public List<StudentAttemptSummary> StudentResults { get; set; } = new();
}
