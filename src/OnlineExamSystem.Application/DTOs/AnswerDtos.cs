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

public class SaveCanvasRequest
{
    public long QuestionId { get; set; }
    public string CanvasData { get; set; } = string.Empty;
}

public class FlagQuestionRequest
{
    public long QuestionId { get; set; }
}

public class AutosaveRequest
{
    public long AttemptId { get; set; }
    public List<AutosaveAnswerItem> Answers { get; set; } = new();
}

public class AutosaveAnswerItem
{
    public long QuestionId { get; set; }
    public List<long>? SelectedOptionIds { get; set; }
    public string? TextContent { get; set; }
    public string? EssayContent { get; set; }
}

public class AutosaveResponse
{
    public long AttemptId { get; set; }
    public int SavedCount { get; set; }
    public DateTime SavedAt { get; set; }
}
