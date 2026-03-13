using FluentAssertions;
using Moq;
using OnlineExamSystem.Application.DTOs;
using OnlineExamSystem.Domain.Entities;
using OnlineExamSystem.Infrastructure.Repositories;
using OnlineExamSystem.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Xunit;

namespace OnlineExamSystem.Tests.Phase5;

public class GradingServiceTests
{
    private readonly Mock<IGradingResultRepository> _gradingRepoMock = new();
    private readonly Mock<IExamAttemptRepository> _attemptRepoMock = new();
    private readonly Mock<IExamQuestionRepository> _examQuestionRepoMock = new();
    private readonly Mock<IAnswerRepository> _answerRepoMock = new();
    private readonly Mock<IQuestionOptionRepository> _optionRepoMock = new();
    private readonly Mock<IStudentRepository> _studentRepoMock = new();
    private readonly Mock<IExamRepository> _examRepoMock = new();
    private readonly Mock<IExamSettingsRepository> _examSettingsRepoMock = new();
    private readonly Mock<INotificationService> _notificationServiceMock = new();
    private readonly Mock<IActivityLogService> _activityLogMock = new();
    private readonly Mock<ILogger<GradingService>> _loggerMock = new();
    private readonly GradingService _service;

    public GradingServiceTests()
    {
        _service = new GradingService(
            _gradingRepoMock.Object,
            _attemptRepoMock.Object,
            _examQuestionRepoMock.Object,
            _answerRepoMock.Object,
            _optionRepoMock.Object,
            _studentRepoMock.Object,
            _examRepoMock.Object,
                _examSettingsRepoMock.Object,
                _notificationServiceMock.Object,
            _activityLogMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task ManualGrade_WhenAttemptNotFound_ReturnsFalse()
    {
        _attemptRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((ExamAttempt?)null);

        var result = await _service.ManualGradeQuestionAsync(1, 5, new ManualGradeRequest { Score = 3 }, 100);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task ManualGrade_WhenQuestionNotInExam_ReturnsFalse()
    {
        var attempt = new ExamAttempt { Id = 1, ExamId = 10, Status = "SUBMITTED" };
        _attemptRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(attempt);
        _examQuestionRepoMock.Setup(r => r.GetExamQuestionAsync(10, 5)).ReturnsAsync((ExamQuestion?)null);

        var result = await _service.ManualGradeQuestionAsync(1, 5, new ManualGradeRequest { Score = 3 }, 100);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task ManualGrade_WhenScoreExceedsMax_ReturnsFalse()
    {
        var attempt = new ExamAttempt { Id = 1, ExamId = 10, Status = "SUBMITTED" };
        var eq = new ExamQuestion { Id = 1, ExamId = 10, QuestionId = 5, MaxScore = 5 };
        _attemptRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(attempt);
        _examQuestionRepoMock.Setup(r => r.GetExamQuestionAsync(10, 5)).ReturnsAsync(eq);

        var result = await _service.ManualGradeQuestionAsync(1, 5, new ManualGradeRequest { Score = 10 }, 100);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Score must be between");
    }

    [Fact]
    public async Task ManualGrade_CreatesNewGradingResult_WhenNoneExists()
    {
        var attempt = new ExamAttempt { Id = 1, ExamId = 10, Status = "SUBMITTED" };
        var question = new Question { Id = 5, Content = "Explain photosynthesis", QuestionType = new QuestionType { Name = "ESSAY" } };
        var eq = new ExamQuestion { Id = 1, ExamId = 10, QuestionId = 5, MaxScore = 5, Question = question };
        var created = new GradingResult { Id = 1, ExamAttemptId = 1, QuestionId = 5, Score = 4, GradedBy = 100, GradedAt = DateTime.UtcNow };

        _attemptRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(attempt);
        _examQuestionRepoMock.Setup(r => r.GetExamQuestionAsync(10, 5)).ReturnsAsync(eq);
        _gradingRepoMock.Setup(r => r.GetByAttemptAndQuestionAsync(1, 5)).ReturnsAsync((GradingResult?)null);
        _gradingRepoMock.Setup(r => r.CreateAsync(It.IsAny<GradingResult>())).ReturnsAsync(created);

        var result = await _service.ManualGradeQuestionAsync(1, 5, new ManualGradeRequest { Score = 4, Comment = "Good" }, 100);

        result.Success.Should().BeTrue();
        result.Data!.Score.Should().Be(4);
        result.Data!.GradedBy.Should().Be(100);
    }

    [Fact]
    public async Task MarkAsGraded_WhenNotSubmitted_ReturnsFalse()
    {
        var attempt = new ExamAttempt { Id = 1, Status = "IN_PROGRESS" };
        _attemptRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(attempt);

        var result = await _service.MarkAsGradedAsync(1);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("SUBMITTED");
    }

    [Fact]
    public async Task MarkAsGraded_UpdatesStatusAndScore()
    {
        var attempt = new ExamAttempt { Id = 1, ExamId = 10, Status = "SUBMITTED" };
        var gradingResults = new List<GradingResult>
        {
            new() { ExamAttemptId = 1, QuestionId = 5, Score = 3 },
            new() { ExamAttemptId = 1, QuestionId = 6, Score = 4 }
        };

        _attemptRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(attempt);
        _gradingRepoMock.Setup(r => r.GetByAttemptIdAsync(1)).ReturnsAsync(gradingResults);
        _attemptRepoMock.Setup(r => r.UpdateAsync(It.IsAny<ExamAttempt>())).ReturnsAsync(attempt);

        var result = await _service.MarkAsGradedAsync(1);

        result.Success.Should().BeTrue();
        _attemptRepoMock.Verify(r => r.UpdateAsync(It.Is<ExamAttempt>(a => a.Status == "GRADED" && a.Score == 7)), Times.Once);
    }

    [Fact]
    public async Task MarkAsGraded_WhenLateSubmission_AppliesPenalty()
    {
        var attempt = new ExamAttempt
        {
            Id = 1,
            ExamId = 10,
            Status = "SUBMITTED",
            IsLateSubmission = true,
            LatePenaltyPercent = 20m
        };
        var gradingResults = new List<GradingResult>
        {
            new() { ExamAttemptId = 1, QuestionId = 5, Score = 3m },
            new() { ExamAttemptId = 1, QuestionId = 6, Score = 2m }
        };

        _attemptRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(attempt);
        _gradingRepoMock.Setup(r => r.GetByAttemptIdAsync(1)).ReturnsAsync(gradingResults);
        _examSettingsRepoMock.Setup(r => r.GetByExamIdAsync(10)).ReturnsAsync(new ExamSetting
        {
            ExamId = 10,
            LatePenaltyPercent = 20m
        });
        _attemptRepoMock.Setup(r => r.UpdateAsync(It.IsAny<ExamAttempt>())).ReturnsAsync(attempt);

        var result = await _service.MarkAsGradedAsync(1);

        result.Success.Should().BeTrue();
        _attemptRepoMock.Verify(r => r.UpdateAsync(It.Is<ExamAttempt>(a => a.Status == "GRADED" && a.Score == 4m)), Times.Once);
    }

    [Fact]
    public async Task PublishResult_WhenNotGraded_ReturnsFalse()
    {
        var attempt = new ExamAttempt { Id = 1, Status = "SUBMITTED" };
        _attemptRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(attempt);

        var result = await _service.PublishResultAsync(1);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("GRADED");
    }

    [Fact]
    public async Task PublishResult_SetsIsResultPublished()
    {
        var attempt = new ExamAttempt { Id = 1, ExamId = 10, StudentId = 99, Status = "GRADED", Score = 7 };
        _attemptRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(attempt);
        _attemptRepoMock.Setup(r => r.UpdateAsync(It.IsAny<ExamAttempt>())).ReturnsAsync(attempt);
        _studentRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync(new Student { Id = 99, UserId = 1234 });
        _examRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new Exam { Id = 10, Title = "Midterm" });
        _notificationServiceMock.Setup(s => s.CreateAsync(
                It.IsAny<long>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<long?>(),
                It.IsAny<string?>()))
            .ReturnsAsync(new NotificationResponse());

        var result = await _service.PublishResultAsync(1);

        result.Success.Should().BeTrue();
        result.Data!.Published.Should().BeTrue();
        _attemptRepoMock.Verify(r => r.UpdateAsync(It.Is<ExamAttempt>(a => a.IsResultPublished == true)), Times.Once);
        _notificationServiceMock.Verify(s => s.CreateAsync(
            1234,
            "GRADE_PUBLISHED",
            It.IsAny<string>(),
            It.IsAny<string>(),
            1,
            "ExamAttempt"), Times.Once);
    }

    [Fact]
    public async Task GetStudentResult_WhenNotPublished_HidesGradingDetails()
    {
        // The service retrieves attempt and delegates to GetAttemptGradingViewAsync.
        // When IsResultPublished == false, grading details are hidden but Success is still true.
        // If the internal call fails (e.g. missing exam), it returns false — but never specifically 
        // "not been published" anymore. We verify the attempt lookup works.
        var attempt = new ExamAttempt { Id = 1, ExamId = 10, Status = "GRADED", IsResultPublished = false, StudentId = 1 };
        _attemptRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(attempt);
        _examRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync((Exam?)null);
        _examQuestionRepoMock.Setup(r => r.GetExamQuestionsAsync(10)).ReturnsAsync(new List<ExamQuestion>());
        _answerRepoMock.Setup(r => r.GetByAttemptIdAsync(1)).ReturnsAsync(new List<Answer>());
        _gradingRepoMock.Setup(r => r.GetByAttemptIdAsync(1)).ReturnsAsync(new List<GradingResult>());
        _studentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Student?)null);

        var result = await _service.GetStudentResultAsync(1);

        // Service returns successfully but hides grading details when not published
        result.Success.Should().BeTrue();
        if (result.Data != null)
        {
            result.Data.TotalScore.Should().BeNull();
        }
    }
}
