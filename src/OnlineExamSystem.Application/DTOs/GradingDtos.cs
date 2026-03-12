namespace OnlineExamSystem.Application.DTOs;

public class GradingResultResponse
{
    public long Id { get; set; }
    public long ExamAttemptId { get; set; }
    public long QuestionId { get; set; }
    public string QuestionContent { get; set; } = string.Empty;
    public string QuestionType { get; set; } = string.Empty;
    public decimal Points { get; set; }
    public decimal Score { get; set; }
    public string? Comment { get; set; }
    public string? Annotations { get; set; }
    public long? GradedBy { get; set; }
    public DateTime? GradedAt { get; set; }
    public bool IsAutoGraded { get; set; }
}

public class ManualGradeRequest
{
    public decimal Score { get; set; }
    public string? Comment { get; set; }
    public string? Annotations { get; set; }
}

public class BatchGradeRequest
{
    public List<BatchGradeItem> Grades { get; set; } = new();
}

public class BatchGradeItem
{
    public long QuestionId { get; set; }
    public decimal Score { get; set; }
    public string? Comment { get; set; }
    public string? Annotations { get; set; }
}

public class AttemptGradingViewResponse
{
    public long AttemptId { get; set; }
    public long StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string ExamTitle { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal? TotalScore { get; set; }
    public List<QuestionGradingItem> Questions { get; set; } = new();
}

public class QuestionGradingItem
{
    public long QuestionId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string QuestionType { get; set; } = string.Empty;
    public decimal Points { get; set; }
    public List<long> SelectedOptionIds { get; set; } = new();
    public string? TextContent { get; set; }
    public string? EssayContent { get; set; }
    public string? CanvasImage { get; set; }
    public List<QuestionOptionGradeInfo> Options { get; set; } = new();
    public GradingResultResponse? GradingResult { get; set; }
}

public class QuestionOptionGradeInfo
{
    public long Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public bool WasSelected { get; set; }
}

public class PendingGradingAttemptResponse
{
    public long AttemptId { get; set; }
    public long StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public bool HasUngraded { get; set; }
}

public class PublishResultResponse
{
    public long AttemptId { get; set; }
    public decimal? TotalScore { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool Published { get; set; }
}

public class GradeQuestionRequest
{
    public long QuestionId { get; set; }
    public decimal Score { get; set; }
    public string? Comment { get; set; }
}

public class AddAnnotationRequest
{
    public long QuestionId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? Type { get; set; }
}
