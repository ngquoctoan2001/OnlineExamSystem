using FluentAssertions;
using Moq;
using OnlineExamSystem.Application.DTOs;
using OnlineExamSystem.Domain.Entities;
using OnlineExamSystem.Infrastructure.Repositories;
using OnlineExamSystem.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Xunit;

namespace OnlineExamSystem.Tests.Phase4;

public class AutoGradeTests
{
    private readonly Mock<IExamAttemptRepository> _attemptRepoMock = new();
    private readonly Mock<IExamRepository> _examRepoMock = new();
    private readonly Mock<IStudentRepository> _studentRepoMock = new();
    private readonly Mock<IExamViolationRepository> _violationRepoMock = new();
    private readonly Mock<IAnswerRepository> _answerRepoMock = new();
    private readonly Mock<IGradingResultRepository> _gradingRepoMock = new();
    private readonly Mock<IExamQuestionRepository> _examQuestionRepoMock = new();
    private readonly Mock<IQuestionOptionRepository> _optionRepoMock = new();
    private readonly Mock<IActivityLogService> _activityLogMock = new();
    private readonly Mock<ILogger<ExamAttemptService>> _loggerMock = new();
    private readonly ExamAttemptService _service;

    public AutoGradeTests()
    {
        _service = new ExamAttemptService(
            _attemptRepoMock.Object,
            _examRepoMock.Object,
            _studentRepoMock.Object,
            _violationRepoMock.Object,
            _answerRepoMock.Object,
            _gradingRepoMock.Object,
            _examQuestionRepoMock.Object,
            _optionRepoMock.Object,
            _activityLogMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Submit_WhenAttemptNotFound_ReturnsFalse()
    {
        _attemptRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((ExamAttempt?)null);

        var result = await _service.SubmitAttemptAsync(1);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task Submit_WhenNotInProgress_ReturnsFalse()
    {
        var attempt = new ExamAttempt { Id = 1, Status = "SUBMITTED" };
        _attemptRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(attempt);

        var result = await _service.SubmitAttemptAsync(1);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Submit_WithMcqQuestion_AutoGradesCorrectly()
    {
        var attempt = new ExamAttempt { Id = 1, ExamId = 10, Status = "IN_PROGRESS" };
        var correctOption = new QuestionOption { Id = 20, QuestionId = 5, IsCorrect = true };
        var question = new Question
        {
            Id = 5,
            QuestionType = new QuestionType { Name = "MCQ" }
        };
        var examQuestion = new ExamQuestion { Id = 1, ExamId = 10, QuestionId = 5, MaxScore = 2, Question = question };
        var answer = new Answer
        {
            Id = 1, ExamAttemptId = 1, QuestionId = 5,
            AnswerOptions = new List<AnswerOption> { new AnswerOption { AnswerId = 1, OptionId = 20 } }
        };
        var updatedAttempt = new ExamAttempt { Id = 1, ExamId = 10, Status = "SUBMITTED", Score = 2 };

        _attemptRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(attempt);
        _attemptRepoMock.Setup(r => r.UpdateAsync(It.IsAny<ExamAttempt>())).ReturnsAsync(updatedAttempt);
        _examQuestionRepoMock.Setup(r => r.GetExamQuestionsAsync(10)).ReturnsAsync(new List<ExamQuestion> { examQuestion });
        _answerRepoMock.Setup(r => r.GetByAttemptIdAsync(1)).ReturnsAsync(new List<Answer> { answer });
        _optionRepoMock.Setup(r => r.GetCorrectOptionsAsync(It.IsAny<List<long>>())).ReturnsAsync(new List<QuestionOption> { correctOption });
        _gradingRepoMock.Setup(r => r.GetByAttemptAndQuestionAsync(1, 5)).ReturnsAsync((GradingResult?)null);
        _gradingRepoMock.Setup(r => r.CreateAsync(It.IsAny<GradingResult>())).ReturnsAsync(new GradingResult { ExamAttemptId = 1, QuestionId = 5, Score = 2 });

        var result = await _service.SubmitAttemptAsync(1);

        result.Success.Should().BeTrue();
        result.Data!.Status.Should().Be("SUBMITTED");
        _gradingRepoMock.Verify(r => r.CreateAsync(It.Is<GradingResult>(g => g.Score == 2)), Times.Once);
    }

    [Fact]
    public async Task Submit_WithMcqWrongAnswer_GivesZeroScore()
    {
        var attempt = new ExamAttempt { Id = 1, ExamId = 10, Status = "IN_PROGRESS" };
        var correctOption = new QuestionOption { Id = 20, QuestionId = 5, IsCorrect = true };
        var question = new Question { Id = 5, QuestionType = new QuestionType { Name = "MCQ" } };
        var examQuestion = new ExamQuestion { Id = 1, ExamId = 10, QuestionId = 5, MaxScore = 2, Question = question };
        // Student selected option 21 (wrong), correct is 20
        var answer = new Answer
        {
            Id = 1, ExamAttemptId = 1, QuestionId = 5,
            AnswerOptions = new List<AnswerOption> { new AnswerOption { AnswerId = 1, OptionId = 21 } }
        };

        _attemptRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(attempt);
        _attemptRepoMock.Setup(r => r.UpdateAsync(It.IsAny<ExamAttempt>())).ReturnsAsync(attempt);
        _examQuestionRepoMock.Setup(r => r.GetExamQuestionsAsync(10)).ReturnsAsync(new List<ExamQuestion> { examQuestion });
        _answerRepoMock.Setup(r => r.GetByAttemptIdAsync(1)).ReturnsAsync(new List<Answer> { answer });
        _optionRepoMock.Setup(r => r.GetCorrectOptionsAsync(It.IsAny<List<long>>())).ReturnsAsync(new List<QuestionOption> { correctOption });
        _gradingRepoMock.Setup(r => r.GetByAttemptAndQuestionAsync(1, 5)).ReturnsAsync((GradingResult?)null);
        _gradingRepoMock.Setup(r => r.CreateAsync(It.IsAny<GradingResult>())).ReturnsAsync(new GradingResult { ExamAttemptId = 1, QuestionId = 5, Score = 0 });

        var result = await _service.SubmitAttemptAsync(1);

        result.Success.Should().BeTrue();
        _gradingRepoMock.Verify(r => r.CreateAsync(It.Is<GradingResult>(g => g.Score == 0)), Times.Once);
    }

    [Fact]
    public async Task LogViolation_WhenAttemptNotFound_ReturnsFalse()
    {
        _attemptRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((ExamAttempt?)null);

        var result = await _service.LogViolationAsync(1, new LogViolationRequest { ViolationType = "TAB_SWITCH" });

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task LogViolation_WhenAttemptInProgress_CreatesViolation()
    {
        var attempt = new ExamAttempt { Id = 1, Status = "IN_PROGRESS" };
        var violation = new ExamViolation { Id = 1, ExamAttemptId = 1, ViolationType = "TAB_SWITCH" };

        _attemptRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(attempt);
        _violationRepoMock.Setup(r => r.CreateAsync(It.IsAny<ExamViolation>())).ReturnsAsync(violation);

        var result = await _service.LogViolationAsync(1, new LogViolationRequest { ViolationType = "TAB_SWITCH" });

        result.Success.Should().BeTrue();
        result.Data!.ViolationType.Should().Be("TAB_SWITCH");
    }
}
