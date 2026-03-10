using FluentAssertions;
using Moq;
using OnlineExamSystem.Application.DTOs;
using OnlineExamSystem.Domain.Entities;
using OnlineExamSystem.Infrastructure.Repositories;
using OnlineExamSystem.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Xunit;

namespace OnlineExamSystem.Tests.Phase4;

public class AnswerServiceTests
{
    private readonly Mock<IAnswerRepository> _answerRepoMock = new();
    private readonly Mock<IExamAttemptRepository> _attemptRepoMock = new();
    private readonly Mock<IExamQuestionRepository> _examQuestionRepoMock = new();
    private readonly Mock<IQuestionRepository> _questionRepoMock = new();
    private readonly Mock<IQuestionOptionRepository> _optionRepoMock = new();
    private readonly Mock<ILogger<AnswerService>> _loggerMock = new();
    private readonly AnswerService _service;

    public AnswerServiceTests()
    {
        _service = new AnswerService(
            _answerRepoMock.Object,
            _attemptRepoMock.Object,
            _examQuestionRepoMock.Object,
            _questionRepoMock.Object,
            _optionRepoMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task SubmitAnswer_WhenAttemptNotFound_ReturnsFalse()
    {
        _attemptRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((ExamAttempt?)null);

        var result = await _service.SubmitAnswerAsync(1, new SubmitAnswerRequest { QuestionId = 1 });

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task SubmitAnswer_WhenAttemptNotInProgress_ReturnsFalse()
    {
        _attemptRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new ExamAttempt { Id = 1, Status = "SUBMITTED" });

        var result = await _service.SubmitAnswerAsync(1, new SubmitAnswerRequest { QuestionId = 1 });

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not in progress");
    }

    [Fact]
    public async Task SubmitAnswer_WhenNewAnswer_CreatesAndReturnsSuccess()
    {
        var attempt = new ExamAttempt { Id = 1, Status = "IN_PROGRESS" };
        var answer = new Answer { Id = 10, ExamAttemptId = 1, QuestionId = 5, AnswerOptions = new List<AnswerOption>() };

        _attemptRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(attempt);
        _answerRepoMock.Setup(r => r.GetByAttemptAndQuestionAsync(1, 5)).ReturnsAsync((Answer?)null);
        _answerRepoMock.Setup(r => r.CreateAsync(It.IsAny<Answer>())).ReturnsAsync(answer);
        _answerRepoMock.Setup(r => r.GetByAttemptAndQuestionAsync(1, 5)).ReturnsAsync(answer);

        var result = await _service.SubmitAnswerAsync(1, new SubmitAnswerRequest { QuestionId = 5, TextContent = "hello" });

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAttemptQuestions_WhenAttemptNotFound_ReturnsFalse()
    {
        _attemptRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((ExamAttempt?)null);

        var result = await _service.GetAttemptQuestionsAsync(99);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task GetAttemptQuestions_ReturnsQuestionList()
    {
        var attempt = new ExamAttempt { Id = 1, ExamId = 10, Status = "IN_PROGRESS" };
        var question = new Question
        {
            Id = 5,
            Content = "What is 2+2?",
            QuestionType = new QuestionType { Name = "MCQ" }
        };
        var examQuestion = new ExamQuestion { Id = 1, ExamId = 10, QuestionId = 5, QuestionOrder = 1, MaxScore = 2, Question = question };

        _attemptRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(attempt);
        _examQuestionRepoMock.Setup(r => r.GetExamQuestionsAsync(10)).ReturnsAsync(new List<ExamQuestion> { examQuestion });
        _answerRepoMock.Setup(r => r.GetByAttemptIdAsync(1)).ReturnsAsync(new List<Answer>());
        _questionRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(question);
        _optionRepoMock.Setup(r => r.GetByQuestionIdAsync(5)).ReturnsAsync(new List<QuestionOption>());

        var result = await _service.GetAttemptQuestionsAsync(1);

        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(1);
        result.Data![0].QuestionId.Should().Be(5);
        result.Data![0].IsAnswered.Should().BeFalse();
    }

    [Fact]
    public async Task GetAnswer_WhenNotFound_ReturnsFalse()
    {
        _answerRepoMock.Setup(r => r.GetByAttemptAndQuestionAsync(1, 5)).ReturnsAsync((Answer?)null);

        var result = await _service.GetAnswerAsync(1, 5);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task GetAnswer_WhenExists_ReturnsData()
    {
        var answer = new Answer { Id = 1, ExamAttemptId = 1, QuestionId = 5, AnswerOptions = new List<AnswerOption>() };
        _answerRepoMock.Setup(r => r.GetByAttemptAndQuestionAsync(1, 5)).ReturnsAsync(answer);

        var result = await _service.GetAnswerAsync(1, 5);

        result.Success.Should().BeTrue();
        result.Data!.QuestionId.Should().Be(5);
    }
}
