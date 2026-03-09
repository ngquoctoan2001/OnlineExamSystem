namespace OnlineExamSystem.Application.DTOs;

public class CreateQuestionRequest
{
    public long SubjectId { get; set; }
    public long QuestionTypeId { get; set; }
    public string Content { get; set; } = null!;
    public string Difficulty { get; set; } = "MEDIUM";
    public List<CreateQuestionOptionRequest> Options { get; set; } = new();
}

public class UpdateQuestionRequest
{
    public string Content { get; set; } = null!;
    public string Difficulty { get; set; } = "MEDIUM";
    public bool IsPublished { get; set; }
    public List<CreateQuestionOptionRequest> Options { get; set; } = new();
}

public class QuestionResponse
{
    public long Id { get; set; }
    public long SubjectId { get; set; }
    public string? SubjectName { get; set; }
    public long QuestionTypeId { get; set; }
    public string? QuestionTypeName { get; set; }
    public string Content { get; set; } = null!;
    public string Difficulty { get; set; } = null!;
    public bool IsPublished { get; set; }
    public DateTime CreatedAt { get; set; }
    public int OptionCount { get; set; }
}

public class QuestionDetailResponse
{
    public long Id { get; set; }
    public long SubjectId { get; set; }
    public string? SubjectName { get; set; }
    public long QuestionTypeId { get; set; }
    public string? QuestionTypeName { get; set; }
    public string Content { get; set; } = null!;
    public string Difficulty { get; set; } = null!;
    public bool IsPublished { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<QuestionOptionResponse> Options { get; set; } = new();
}

public class QuestionListResponse
{
    public List<QuestionResponse> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (TotalCount + PageSize - 1) / PageSize;
}

public class CreateQuestionOptionRequest
{
    public string Label { get; set; } = null!;
    public string Content { get; set; } = null!;
    public bool IsCorrect { get; set; }
    public int OrderIndex { get; set; }
}

public class QuestionOptionResponse
{
    public long Id { get; set; }
    public long QuestionId { get; set; }
    public string Label { get; set; } = null!;
    public string Content { get; set; } = null!;
    public bool IsCorrect { get; set; }
    public int OrderIndex { get; set; }
}
