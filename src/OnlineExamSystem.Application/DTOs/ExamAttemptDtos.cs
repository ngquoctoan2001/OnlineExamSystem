namespace OnlineExamSystem.Application.DTOs;

public class StartExamAttemptRequest
{
    public long ExamId { get; set; }
    public long StudentId { get; set; }
}

public class ExamAttemptResponse
{
    public long Id { get; set; }
    public long ExamId { get; set; }
    public string? ExamTitle { get; set; }
    public long StudentId { get; set; }
    public string? StudentName { get; set; }
    public string? StudentCode { get; set; }
    public string Status { get; set; } = null!;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public decimal? Score { get; set; }
    public decimal? TotalPoints { get; set; }
    public bool? IsPassed { get; set; }
    public int TotalQuestions { get; set; }
    public int AnsweredQuestions { get; set; }
}

public class ExamAttemptListResponse
{
    public List<ExamAttemptResponse> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (TotalCount + PageSize - 1) / PageSize;
}

public class SubmitExamAttemptResponse
{
    public long AttemptId { get; set; }
    public string Status { get; set; } = null!;
    public DateTime SubmittedAt { get; set; }
    public bool IsLateSubmission { get; set; }
    public decimal LatePenaltyPercent { get; set; }
    public string Message { get; set; } = null!;
}

public class ExamAttemptDetailResponse
{
    public long Id { get; set; }
    public long ExamId { get; set; }
    public string? ExamTitle { get; set; }
    public long StudentId { get; set; }
    public string? StudentName { get; set; }
    public string Status { get; set; } = null!;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public decimal? Score { get; set; }
    public List<AttemptAnswerResponse> Answers { get; set; } = new();
}

public class AttemptAnswerResponse
{
    public long AnswerId { get; set; }
    public long QuestionId { get; set; }
    public string? TextContent { get; set; }
    public string? EssayContent { get; set; }
    public string? CanvasImage { get; set; }
}
