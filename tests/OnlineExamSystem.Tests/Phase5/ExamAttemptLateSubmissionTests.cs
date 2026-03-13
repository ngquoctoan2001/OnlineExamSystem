using FluentAssertions;
using Moq;
using OnlineExamSystem.Domain.Entities;
using OnlineExamSystem.Infrastructure.Repositories;
using OnlineExamSystem.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Xunit;

namespace OnlineExamSystem.Tests.Phase5;

public class ExamAttemptLateSubmissionTests
{
    private readonly Mock<IExamAttemptRepository> _attemptRepoMock = new();
    private readonly Mock<IExamRepository> _examRepoMock = new();
    private readonly Mock<IExamSettingsRepository> _examSettingsRepoMock = new();
    private readonly Mock<IExamClassRepository> _examClassRepoMock = new();
    private readonly Mock<IStudentRepository> _studentRepoMock = new();
    private readonly Mock<IExamViolationRepository> _violationRepoMock = new();
    private readonly Mock<IAnswerRepository> _answerRepoMock = new();
    private readonly Mock<IGradingResultRepository> _gradingRepoMock = new();
    private readonly Mock<IExamQuestionRepository> _examQuestionRepoMock = new();
    private readonly Mock<IQuestionOptionRepository> _optionRepoMock = new();
    private readonly Mock<IActivityLogService> _activityLogMock = new();
    private readonly Mock<ILogger<ExamAttemptService>> _loggerMock = new();

    private readonly ExamAttemptService _service;

    public ExamAttemptLateSubmissionTests()
    {
        _service = new ExamAttemptService(
            _attemptRepoMock.Object,
            _examRepoMock.Object,
            _examSettingsRepoMock.Object,
            _examClassRepoMock.Object,
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
    public async Task SubmitAttempt_WithinGraceWindow_AllowsLateSubmissionWithPenaltyMetadata()
    {
        var now = DateTime.UtcNow;
        var attempt = new ExamAttempt
        {
            Id = 1,
            ExamId = 10,
            StudentId = 100,
            Status = "IN_PROGRESS",
            StartTime = now.AddMinutes(-70)
        };
        var exam = new Exam
        {
            Id = 10,
            DurationMinutes = 60,
            StartTime = now.AddHours(-2),
            EndTime = now.AddHours(1),
            Status = "ACTIVE"
        };
        var settings = new ExamSetting
        {
            ExamId = 10,
            AllowLateSubmission = true,
            GracePeriodMinutes = 20,
            LatePenaltyPercent = 15m
        };

        _attemptRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(attempt);
        _examRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(exam);
        _examSettingsRepoMock.Setup(r => r.GetByExamIdAsync(10)).ReturnsAsync(settings);
        _attemptRepoMock.Setup(r => r.UpdateAsync(It.IsAny<ExamAttempt>())).ReturnsAsync((ExamAttempt a) => a);
        _examQuestionRepoMock.Setup(r => r.GetExamQuestionsAsync(10)).ReturnsAsync(new List<ExamQuestion>());

        var result = await _service.SubmitAttemptAsync(1);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.IsLateSubmission.Should().BeTrue();
        result.Data.LatePenaltyPercent.Should().Be(15m);
    }

    [Fact]
    public async Task SubmitAttempt_AfterGraceWindow_ReturnsFailure()
    {
        var now = DateTime.UtcNow;
        var attempt = new ExamAttempt
        {
            Id = 1,
            ExamId = 10,
            StudentId = 100,
            Status = "IN_PROGRESS",
            StartTime = now.AddMinutes(-95)
        };
        var exam = new Exam
        {
            Id = 10,
            DurationMinutes = 60,
            StartTime = now.AddHours(-2),
            EndTime = now.AddHours(1),
            Status = "ACTIVE"
        };
        var settings = new ExamSetting
        {
            ExamId = 10,
            AllowLateSubmission = true,
            GracePeriodMinutes = 20,
            LatePenaltyPercent = 10m
        };

        _attemptRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(attempt);
        _examRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(exam);
        _examSettingsRepoMock.Setup(r => r.GetByExamIdAsync(10)).ReturnsAsync(settings);

        var result = await _service.SubmitAttemptAsync(1);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Late submission window");
    }
}
