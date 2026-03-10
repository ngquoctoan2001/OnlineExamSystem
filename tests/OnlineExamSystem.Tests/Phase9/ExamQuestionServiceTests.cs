using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using OnlineExamSystem.Application.DTOs;
using OnlineExamSystem.Domain.Entities;
using OnlineExamSystem.Infrastructure.Repositories;
using OnlineExamSystem.Infrastructure.Services;
using Xunit;

namespace OnlineExamSystem.Tests.Phase9;

public class ExamQuestionServiceTests
{
    private readonly Mock<IExamQuestionRepository> _examQRepoMock = new();
    private readonly Mock<IExamRepository> _examRepoMock = new();
    private readonly Mock<IQuestionRepository> _questionRepoMock = new();
    private readonly Mock<IQuestionOptionRepository> _optionRepoMock = new();
    private readonly Mock<ILogger<ExamQuestionService>> _loggerMock = new();
    private readonly ExamQuestionService _service;

    public ExamQuestionServiceTests()
    {
        _service = new ExamQuestionService(
            _examQRepoMock.Object,
            _examRepoMock.Object,
            _questionRepoMock.Object,
            _optionRepoMock.Object,
            _loggerMock.Object);
    }

    private static Exam BuildExam(long id = 1) => new()
    {
        Id = id, Title = "Test Exam", Status = "DRAFT",
        DurationMinutes = 60, StartTime = DateTime.UtcNow, EndTime = DateTime.UtcNow.AddHours(2)
    };

    private static Question BuildQuestion(long id = 1) => new()
    {
        Id = id, Content = "What is 2+2?", Difficulty = "EASY"
    };

    // ===== AddQuestionToExamAsync =====

    [Fact]
    public async Task AddQuestion_ExamNotFound_ReturnsFalse()
    {
        _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Exam?)null);

        var (success, message, _) = await _service.AddQuestionToExamAsync(new AddQuestionToExamRequest { ExamId = 1, QuestionId = 1 });

        success.Should().BeFalse();
        message.Should().Be("Exam not found");
    }

    [Fact]
    public async Task AddQuestion_QuestionNotFound_ReturnsFalse()
    {
        _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(BuildExam());
        _questionRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Question?)null);

        var (success, message, _) = await _service.AddQuestionToExamAsync(new AddQuestionToExamRequest { ExamId = 1, QuestionId = 1 });

        success.Should().BeFalse();
        message.Should().Be("Question not found");
    }

    [Fact]
    public async Task AddQuestion_AlreadyExists_ReturnsFalse()
    {
        _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(BuildExam());
        _questionRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(BuildQuestion());
        _examQRepoMock.Setup(r => r.ExistsAsync(1, 1)).ReturnsAsync(true);

        var (success, message, _) = await _service.AddQuestionToExamAsync(new AddQuestionToExamRequest { ExamId = 1, QuestionId = 1 });

        success.Should().BeFalse();
        message.Should().Contain("already added");
    }

    [Fact]
    public async Task AddQuestion_Valid_ReturnsSuccess()
    {
        var exam = BuildExam();
        var question = BuildQuestion();
        var eq = new ExamQuestion
        {
            Id = 10, ExamId = 1, QuestionId = 1, QuestionOrder = 1,
            MaxScore = 5, AddedAt = DateTime.UtcNow, Question = question
        };

        _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(exam);
        _questionRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(question);
        _examQRepoMock.Setup(r => r.ExistsAsync(1, 1)).ReturnsAsync(false);
        _examQRepoMock.Setup(r => r.CreateAsync(It.IsAny<ExamQuestion>())).ReturnsAsync(eq);
        _examQRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(eq);
        _optionRepoMock.Setup(r => r.GetByQuestionIdAsync(1)).ReturnsAsync(new List<QuestionOption>());

        var (success, message, data) = await _service.AddQuestionToExamAsync(new AddQuestionToExamRequest
        {
            ExamId = 1, QuestionId = 1, QuestionOrder = 1, MaxScore = 5
        });

        success.Should().BeTrue();
        message.Should().Contain("successfully");
    }

    // ===== GetExamQuestionsAsync =====

    [Fact]
    public async Task GetExamQuestions_ExamNotFound_ReturnsFalse()
    {
        _examRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Exam?)null);

        var (success, message, _) = await _service.GetExamQuestionsAsync(99);

        success.Should().BeFalse();
        message.Should().Be("Exam not found");
    }

    [Fact]
    public async Task GetExamQuestions_ValidExam_ReturnsQuestions()
    {
        var exam = BuildExam();
        var eq = new ExamQuestion
        {
            Id = 1, ExamId = 1, QuestionId = 1, QuestionOrder = 1, MaxScore = 10,
            AddedAt = DateTime.UtcNow, Question = BuildQuestion()
        };
        _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(exam);
        _examQRepoMock.Setup(r => r.GetExamQuestionsAsync(1)).ReturnsAsync(new List<ExamQuestion> { eq });

        var (success, _, data) = await _service.GetExamQuestionsAsync(1);

        success.Should().BeTrue();
        data!.TotalQuestions.Should().Be(1);
        data.TotalScore.Should().Be(10);
    }

    // ===== RemoveQuestionFromExamAsync =====

    [Fact]
    public async Task RemoveQuestion_ExamNotFound_ReturnsFalse()
    {
        _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Exam?)null);

        var (success, message) = await _service.RemoveQuestionFromExamAsync(1, 1);

        success.Should().BeFalse();
        message.Should().Be("Exam not found");
    }
}
