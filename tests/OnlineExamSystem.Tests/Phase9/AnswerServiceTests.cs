using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using OnlineExamSystem.Application.DTOs;
using OnlineExamSystem.Domain.Entities;
using OnlineExamSystem.Infrastructure.Repositories;
using OnlineExamSystem.Infrastructure.Services;
using Xunit;

namespace OnlineExamSystem.Tests.Phase9;

public class AnswerServiceTests
{
    private readonly Mock<IAnswerRepository> _answerRepoMock = new();
    private readonly Mock<IExamAttemptRepository> _attemptRepoMock = new();
    private readonly Mock<IExamRepository> _examRepoMock = new();
    private readonly Mock<IExamSettingsRepository> _examSettingsRepoMock = new();
    private readonly Mock<IExamQuestionRepository> _examQRepoMock = new();
    private readonly Mock<IQuestionRepository> _questionRepoMock = new();
    private readonly Mock<IQuestionOptionRepository> _optionRepoMock = new();
    private readonly Mock<ILogger<AnswerService>> _loggerMock = new();
    private readonly AnswerService _service;

    public AnswerServiceTests()
    {
        _service = new AnswerService(
            _answerRepoMock.Object,
            _attemptRepoMock.Object,
            _examRepoMock.Object,
            _examSettingsRepoMock.Object,
            _examQRepoMock.Object,
            _questionRepoMock.Object,
            _optionRepoMock.Object,
            _loggerMock.Object);
    }

    private static ExamAttempt BuildAttempt(string status = "IN_PROGRESS") => new()
    {
        Id = 1, ExamId = 10, StudentId = 5, Status = status, StartTime = DateTime.UtcNow
    };

    private void SetupExamWindow(long examId = 10)
    {
        _examRepoMock.Setup(r => r.GetByIdAsync(examId)).ReturnsAsync(new Exam
        {
            Id = examId,
            StartTime = DateTime.UtcNow.AddHours(-1),
            EndTime = DateTime.UtcNow.AddHours(1),
            DurationMinutes = 120
        });
    }

    // ===== SubmitAnswerAsync =====

    [Fact]
    public async Task SubmitAnswer_AttemptNotFound_ReturnsFalse()
    {
        _attemptRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((ExamAttempt?)null);

        var (success, message, _) = await _service.SubmitAnswerAsync(1, new SubmitAnswerRequest { QuestionId = 1 });

        success.Should().BeFalse();
        message.Should().Contain("not found");
    }

    [Fact]
    public async Task SubmitAnswer_AttemptNotInProgress_ReturnsFalse()
    {
        _attemptRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(BuildAttempt("SUBMITTED"));

        var (success, message, _) = await _service.SubmitAnswerAsync(1, new SubmitAnswerRequest { QuestionId = 1 });

        success.Should().BeFalse();
        message.Should().Contain("not in progress");
    }

    [Fact]
    public async Task SubmitAnswer_NewAnswer_CreatesAndReturnsSuccess()
    {
        var attempt = BuildAttempt();
        var answer = new Answer
        {
            Id = 1, ExamAttemptId = 1, QuestionId = 2,
            TextContent = null, AnsweredAt = DateTime.UtcNow,
            AnswerOptions = new List<AnswerOption>()
        };

        _attemptRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(attempt);
    SetupExamWindow(10);
        _answerRepoMock.Setup(r => r.GetByAttemptAndQuestionAsync(1, 2)).ReturnsAsync((Answer?)null);
        _answerRepoMock.Setup(r => r.CreateAsync(It.IsAny<Answer>())).ReturnsAsync(answer);
        _answerRepoMock.SetupSequence(r => r.GetByAttemptAndQuestionAsync(1, 2))
            .ReturnsAsync((Answer?)null)
            .ReturnsAsync(answer);

        var (success, message, data) = await _service.SubmitAnswerAsync(1, new SubmitAnswerRequest
        {
            QuestionId = 2, TextContent = null, SelectedOptionIds = null
        });

        success.Should().BeTrue();
        message.Should().Be("Answer submitted");
    }

    [Fact]
    public async Task SubmitAnswer_ExistingAnswer_UpdatesAndReturnsSuccess()
    {
        var attempt = BuildAttempt();
        var existingAnswer = new Answer
        {
            Id = 5, ExamAttemptId = 1, QuestionId = 3,
            TextContent = "old text", AnsweredAt = DateTime.UtcNow.AddMinutes(-5),
            AnswerOptions = new List<AnswerOption>()
        };

        _attemptRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(attempt);
    SetupExamWindow(10);
        _answerRepoMock.Setup(r => r.GetByAttemptAndQuestionAsync(1, 3)).ReturnsAsync(existingAnswer);
        _answerRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Answer>())).ReturnsAsync(existingAnswer);

        var (success, message, data) = await _service.SubmitAnswerAsync(1, new SubmitAnswerRequest
        {
            QuestionId = 3, TextContent = "new text"
        });

        success.Should().BeTrue();
        _answerRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Answer>()), Times.Once);
    }

    // ===== UpdateAnswerAsync =====

    [Fact]
    public async Task UpdateAnswer_AttemptNotFound_ReturnsFalse()
    {
        _attemptRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((ExamAttempt?)null);

        var (success, message, _) = await _service.UpdateAnswerAsync(1, 1, new SubmitAnswerRequest());

        success.Should().BeFalse();
        message.Should().Contain("not found");
    }

    [Fact]
    public async Task UpdateAnswer_AnswerNotFound_ReturnsFalse()
    {
        _attemptRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(BuildAttempt());
        SetupExamWindow(10);
        _answerRepoMock.Setup(r => r.GetByAttemptAndQuestionAsync(1, 99)).ReturnsAsync((Answer?)null);

        var (success, message, _) = await _service.UpdateAnswerAsync(1, 99, new SubmitAnswerRequest());

        success.Should().BeFalse();
        message.Should().Contain("not found");
    }

    // ===== GetAnswerAsync =====

    [Fact]
    public async Task GetAnswer_NotFound_ReturnsFalse()
    {
        _answerRepoMock.Setup(r => r.GetByAttemptAndQuestionAsync(1, 99)).ReturnsAsync((Answer?)null);

        var (success, message, _) = await _service.GetAnswerAsync(1, 99);

        success.Should().BeFalse();
        message.Should().Contain("not found");
    }

    [Fact]
    public async Task GetAnswer_Found_ReturnsAnswer()
    {
        var answer = new Answer { Id = 7, ExamAttemptId = 1, QuestionId = 3, AnsweredAt = DateTime.UtcNow, AnswerOptions = new List<AnswerOption>() };
        _answerRepoMock.Setup(r => r.GetByAttemptAndQuestionAsync(1, 3)).ReturnsAsync(answer);

        var (success, _, data) = await _service.GetAnswerAsync(1, 3);

        success.Should().BeTrue();
        data!.QuestionId.Should().Be(3);
    }

    // ===== GetAttemptQuestionsAsync =====

    [Fact]
    public async Task GetAttemptQuestions_AttemptNotFound_ReturnsFalse()
    {
        _attemptRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((ExamAttempt?)null);

        var (success, message, _) = await _service.GetAttemptQuestionsAsync(99);

        success.Should().BeFalse();
        message.Should().Contain("not found");
    }

    [Fact]
    public async Task GetAttemptQuestions_ValidAttempt_ReturnsQuestions()
    {
        var attempt = BuildAttempt();
        attempt.ExamId = 10;
        var question = new Question { Id = 2, Content = "Test?", QuestionOptions = new List<QuestionOption>() };
        _attemptRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(attempt);
        _examQRepoMock.Setup(r => r.GetExamQuestionsAsync(10)).ReturnsAsync(new List<ExamQuestion>
        {
            new() { Id = 1, ExamId = 10, QuestionId = 2, MaxScore = 5,
                Question = question }
        });
        _answerRepoMock.Setup(r => r.GetByAttemptIdAsync(1)).ReturnsAsync(new List<Answer>());
        _questionRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(question);
        _optionRepoMock.Setup(r => r.GetByQuestionIdAsync(2)).ReturnsAsync(new List<QuestionOption>());

        var (success, _, data) = await _service.GetAttemptQuestionsAsync(1);

        success.Should().BeTrue();
        data.Should().HaveCount(1);
    }
}
