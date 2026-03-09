namespace OnlineExamSystem.Application.DTOs;

public class AddQuestionToExamRequest
{
    public long ExamId { get; set; }
    public long QuestionId { get; set; }
    public int QuestionOrder { get; set; }
    public int MaxScore { get; set; } = 1;
}

public class ReorderExamQuestionsRequest
{
    public List<ExamQuestionOrderItem> Questions { get; set; } = new();
}

public class ExamQuestionOrderItem
{
    public long ExamQuestionId { get; set; }
    public int NewOrder { get; set; }
}

public class ExamQuestionResponse
{
    public long Id { get; set; }
    public long ExamId { get; set; }
    public long QuestionId { get; set; }
    public string? QuestionContent { get; set; }
    public string? QuestionDifficulty { get; set; }
    public int QuestionOrder { get; set; }
    public int MaxScore { get; set; }
    public int OptionCount { get; set; }
    public DateTime AddedAt { get; set; }
}

public class ExamQuestionDetailResponse
{
    public long Id { get; set; }
    public long ExamId { get; set; }
    public long QuestionId { get; set; }
    public string? QuestionContent { get; set; }
    public string? QuestionDifficulty { get; set; }
    public int QuestionOrder { get; set; }
    public int MaxScore { get; set; }
    public DateTime AddedAt { get; set; }
    public List<QuestionOptionResponse> Options { get; set; } = new();
}

public class ExamQuestionsListResponse
{
    public long ExamId { get; set; }
    public string? ExamTitle { get; set; }
    public List<ExamQuestionResponse> Questions { get; set; } = new();
    public int TotalQuestions { get; set; }
    public int TotalScore { get; set; }
}
