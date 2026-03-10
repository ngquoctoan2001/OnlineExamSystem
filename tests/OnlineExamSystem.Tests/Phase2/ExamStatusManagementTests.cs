using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using OnlineExamSystem.Application.DTOs;
using OnlineExamSystem.Domain.Entities;
using OnlineExamSystem.Infrastructure.Repositories;
using OnlineExamSystem.Infrastructure.Services;
using Xunit;

namespace OnlineExamSystem.Tests.Phase2;

/// <summary>
/// Comprehensive tests for exam status state machine transitions.
/// Valid: DRAFT → ACTIVE → CLOSED
/// Invalid: any reverse transitions
/// </summary>
public class ExamStatusManagementTests
{
    private readonly Mock<IExamRepository> _examRepoMock;
    private readonly ExamService _service;

    public ExamStatusManagementTests()
    {
        _examRepoMock = new Mock<IExamRepository>();
        var teacherRepo = new Mock<ITeacherRepository>();
        var subjectRepo = new Mock<ISubjectRepository>();
        var settingsRepo = new Mock<IExamSettingsRepository>();
        var logger = new Mock<ILogger<ExamService>>();
        var activityLog = new Mock<IActivityLogService>();

        _service = new ExamService(
            _examRepoMock.Object,
            teacherRepo.Object,
            subjectRepo.Object,
            settingsRepo.Object,
            activityLog.Object,
            logger.Object);
    }

    // ===== ACTIVATE TRANSITION (DRAFT → ACTIVE) =====

    [Theory]
    [InlineData("ACTIVE")]
    [InlineData("CLOSED")]
    public async Task ActivateExamAsync_NonDraftStatus_ReturnsFalse(string status)
    {
        _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(BuildExam(status));

        var (success, message, _) = await _service.ActivateExamAsync(1);

        success.Should().BeFalse();
        message.Should().Be("Only DRAFT exams can be activated");
    }

    [Fact]
    public async Task ActivateExamAsync_DraftWithFutureEndTime_TransitionsToActive()
    {
        var exam = BuildExam("DRAFT");
        exam.EndTime = DateTime.UtcNow.AddHours(3);
        var activated = BuildExam("ACTIVE");

        _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(exam);
        _examRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Exam>())).ReturnsAsync(activated);

        var (success, _, data) = await _service.ActivateExamAsync(1);

        success.Should().BeTrue();
        data!.Status.Should().Be("ACTIVE");
        _examRepoMock.Verify(r => r.UpdateAsync(It.Is<Exam>(e => e.Status == "ACTIVE")), Times.Once);
    }

    [Fact]
    public async Task ActivateExamAsync_ExamNotFound_ReturnsFalse()
    {
        _examRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Exam?)null);

        var (success, message, _) = await _service.ActivateExamAsync(99);

        success.Should().BeFalse();
        message.Should().Be("Exam not found");
    }

    // ===== CLOSE TRANSITION (ACTIVE → CLOSED) =====

    [Theory]
    [InlineData("DRAFT")]
    [InlineData("CLOSED")]
    public async Task CloseExamAsync_NonActiveStatus_ReturnsFalse(string status)
    {
        _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(BuildExam(status));

        var (success, message) = await _service.CloseExamAsync(1);

        success.Should().BeFalse();
        message.Should().Be("Only ACTIVE exams can be closed");
    }

    [Fact]
    public async Task CloseExamAsync_ActiveExam_TransitionsToClosed()
    {
        var exam = BuildExam("ACTIVE");
        _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(exam);
        _examRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Exam>())).ReturnsAsync(BuildExam("CLOSED"));

        var (success, message) = await _service.CloseExamAsync(1);

        success.Should().BeTrue();
        _examRepoMock.Verify(r => r.UpdateAsync(It.Is<Exam>(e => e.Status == "CLOSED")), Times.Once);
    }

    // ===== ChangeStatus Generic =====

    [Theory]
    [InlineData("DRAFT", "ACTIVE", true)]
    [InlineData("ACTIVE", "CLOSED", true)]
    [InlineData("DRAFT", "CLOSED", false)]
    [InlineData("ACTIVE", "DRAFT", false)]
    [InlineData("CLOSED", "ACTIVE", false)]
    [InlineData("CLOSED", "DRAFT", false)]
    public async Task ChangeStatusAsync_StateTransitionMatrix(string current, string target, bool shouldSucceed)
    {
        var exam = BuildExam(current);
        _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(exam);
        _examRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Exam>())).ReturnsAsync(exam);

        var (success, _) = await _service.ChangeStatusAsync(1, target);

        success.Should().Be(shouldSucceed);
    }

    [Theory]
    [InlineData("DRAFT")]
    [InlineData("ACTIVE")]
    [InlineData("CLOSED")]
    public async Task ChangeStatusAsync_SameStatus_ReturnsTrue(string status)
    {
        _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(BuildExam(status));

        var (success, message) = await _service.ChangeStatusAsync(1, status);

        success.Should().BeTrue();
        message.Should().Contain(status);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("PUBLISHED")]
    [InlineData("PENDING")]
    [InlineData("DELETED")]
    public async Task ChangeStatusAsync_InvalidStatusValues_ReturnsFalse(string status)
    {
        var (success, message) = await _service.ChangeStatusAsync(1, status);

        success.Should().BeFalse();
        message.Should().Be("Invalid status. Must be DRAFT, ACTIVE, or CLOSED");
    }

    // ===== DELETE RESTRICTIONS =====

    [Theory]
    [InlineData("ACTIVE")]
    [InlineData("CLOSED")]
    public async Task DeleteExamAsync_NonDraftExam_BlocksDeletion(string status)
    {
        _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(BuildExam(status));

        var (success, message) = await _service.DeleteExamAsync(1);

        success.Should().BeFalse();
        message.Should().Be("Cannot delete exam that is ACTIVE or CLOSED");
        _examRepoMock.Verify(r => r.DeleteAsync(It.IsAny<long>()), Times.Never);
    }

    [Fact]
    public async Task DeleteExamAsync_DraftExam_AllowsDeletion()
    {
        _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(BuildExam("DRAFT"));
        _examRepoMock.Setup(r => r.DeleteAsync(1)).ReturnsAsync(true);

        var (success, _) = await _service.DeleteExamAsync(1);

        success.Should().BeTrue();
        _examRepoMock.Verify(r => r.DeleteAsync(1), Times.Once);
    }

    // ===== UPDATE RESTRICTIONS =====

    [Theory]
    [InlineData("ACTIVE")]
    [InlineData("CLOSED")]
    public async Task UpdateExamAsync_NonDraftExam_BlocksUpdate(string status)
    {
        _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(BuildExam(status));

        var request = new UpdateExamRequest
        {
            Title = "New Title", SubjectId = 1, DurationMinutes = 60,
            StartTime = DateTime.UtcNow.AddDays(1), EndTime = DateTime.UtcNow.AddDays(1).AddHours(2)
        };

        var (success, message, _) = await _service.UpdateExamAsync(1, request);

        success.Should().BeFalse();
        message.Should().Be("Cannot update exam in ACTIVE or CLOSED status");
        _examRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Exam>()), Times.Never);
    }

    // ===== Helper =====

    private static Exam BuildExam(string status) => new Exam
    {
        Id = 1,
        Title = "Test",
        SubjectId = 1,
        Status = status,
        DurationMinutes = 60,
        StartTime = DateTime.UtcNow.AddDays(1),
        EndTime = DateTime.UtcNow.AddDays(1).AddHours(2),
        Subject = new Subject { Id = 1, Name = "Math" }
    };
}
