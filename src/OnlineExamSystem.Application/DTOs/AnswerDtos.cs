namespace OnlineExamSystem.Application.DTOs;

public class SubmitAnswerRequest
{
    public long QuestionId { get; set; }
    public List<long>? SelectedOptionIds { get; set; }
    public string? TextContent { get; set; }
    public string? EssayContent { get; set; }
    public string? CanvasImage { get; set; }
}

public class AnswerResponse
{
    public long Id { get; set; }
    public long ExamAttemptId { get; set; }
    public long QuestionId { get; set; }
    public List<long> SelectedOptionIds { get; set; } = new();
    public string? TextContent { get; set; }
    public string? EssayContent { get; set; }
    public string? CanvasImage { get; set; }
    public DateTime? AnsweredAt { get; set; }
}

public class AttemptQuestionResponse
{
    public long QuestionId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string QuestionType { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
    public decimal Points { get; set; }
    public List<AttemptQuestionOptionResponse> Options { get; set; } = new();
    public bool IsAnswered { get; set; }
    public AnswerResponse? CurrentAnswer { get; set; }
}

public class AttemptQuestionOptionResponse
{
    public long Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
}

public class LogViolationRequest
{
    public string ViolationType { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class ViolationResponse
{
    public long Id { get; set; }
    public long ExamAttemptId { get; set; }
    public string ViolationType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime OccurredAt { get; set; }
}
