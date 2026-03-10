using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using OnlineExamSystem.Application.DTOs;
using OnlineExamSystem.Domain.Entities;
using OnlineExamSystem.Infrastructure.Repositories;
using OnlineExamSystem.Infrastructure.Services;
using Xunit;

namespace OnlineExamSystem.Tests.Phase2;

public class ExamServiceTests
{
    private readonly Mock<IExamRepository> _examRepoMock;
    private readonly Mock<ITeacherRepository> _teacherRepoMock;
    private readonly Mock<ISubjectRepository> _subjectRepoMock;
    private readonly Mock<IExamSettingsRepository> _settingsRepoMock;
    private readonly Mock<ILogger<ExamService>> _loggerMock;
    private readonly Mock<IActivityLogService> _activityLogMock;
    private readonly ExamService _service;

    public ExamServiceTests()
    {
        _examRepoMock = new Mock<IExamRepository>();
        _teacherRepoMock = new Mock<ITeacherRepository>();
        _subjectRepoMock = new Mock<ISubjectRepository>();
        _settingsRepoMock = new Mock<IExamSettingsRepository>();
        _loggerMock = new Mock<ILogger<ExamService>>();
        _activityLogMock = new Mock<IActivityLogService>();

        _service = new ExamService(
            _examRepoMock.Object,
            _teacherRepoMock.Object,
            _subjectRepoMock.Object,
            _settingsRepoMock.Object,
            _activityLogMock.Object,
            _loggerMock.Object);
    }

    // ===== GetExamByIdAsync =====

    [Fact]
    public async Task GetExamByIdAsync_ExamExists_ReturnsSuccess()
    {
        var exam = BuildExam(id: 1, status: "DRAFT");
        _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(exam);

        var (success, message, data) = await _service.GetExamByIdAsync(1);

        success.Should().BeTrue();
        data.Should().NotBeNull();
        data!.Id.Should().Be(1);
        data.Title.Should().Be("Test Exam");
    }

    [Fact]
    public async Task GetExamByIdAsync_ExamNotFound_ReturnsFalse()
    {
        _examRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Exam?)null);

        var (success, message, data) = await _service.GetExamByIdAsync(99);

        success.Should().BeFalse();
        message.Should().Be("Exam not found");
        data.Should().BeNull();
    }

    // ===== GetAllExamsAsync =====

    [Fact]
    public async Task GetAllExamsAsync_ReturnsPagedList()
    {
        var exams = new List<Exam> { BuildExam(1), BuildExam(2) };
        _examRepoMock.Setup(r => r.GetAllAsync(1, 20)).ReturnsAsync((exams, 2));

        var (success, _, data) = await _service.GetAllExamsAsync(1, 20);

        success.Should().BeTrue();
        data!.Items.Should().HaveCount(2);
        data.TotalCount.Should().Be(2);
        data.Page.Should().Be(1);
    }

    // ===== CreateExamAsync =====

    [Fact]
    public async Task CreateExamAsync_ValidRequest_ReturnsCreatedExam()
    {
        var request = BuildCreateRequest();
        var teacher = new Teacher { Id = 1 };
        var subject = new Subject { Id = 1, Name = "Math" };
        var createdExam = BuildExam(id: 10);

        _examRepoMock.Setup(r => r.TitleExistsAsync("Math Exam", null)).ReturnsAsync(false);
        _teacherRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(teacher);
        _subjectRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(subject);
        _examRepoMock.Setup(r => r.CreateAsync(It.IsAny<Exam>())).ReturnsAsync(createdExam);

        var (success, message, data) = await _service.CreateExamAsync(request);

        success.Should().BeTrue();
        data.Should().NotBeNull();
        data!.Id.Should().Be(10);
    }

    [Fact]
    public async Task CreateExamAsync_EmptyTitle_ReturnsFalse()
    {
        var request = BuildCreateRequest(title: "");

        var (success, message, data) = await _service.CreateExamAsync(request);

        success.Should().BeFalse();
        message.Should().Be("Exam title is required");
    }

    [Fact]
    public async Task CreateExamAsync_DurationZero_ReturnsFalse()
    {
        var request = BuildCreateRequest(durationMinutes: 0);

        var (success, message, data) = await _service.CreateExamAsync(request);

        success.Should().BeFalse();
        message.Should().Be("Duration must be greater than 0");
    }

    [Fact]
    public async Task CreateExamAsync_StartTimeAfterEndTime_ReturnsFalse()
    {
        var now = DateTime.UtcNow;
        var request = BuildCreateRequest(startTime: now.AddHours(2), endTime: now.AddHours(1));

        var (success, message, data) = await _service.CreateExamAsync(request);

        success.Should().BeFalse();
        message.Should().Be("Start time must be before end time");
    }

    [Fact]
    public async Task CreateExamAsync_DuplicateTitle_ReturnsFalse()
    {
        var request = BuildCreateRequest();
        _examRepoMock.Setup(r => r.TitleExistsAsync("Math Exam", null)).ReturnsAsync(true);

        var (success, message, data) = await _service.CreateExamAsync(request);

        success.Should().BeFalse();
        message.Should().Be("An exam with this title already exists");
    }

    [Fact]
    public async Task CreateExamAsync_TeacherNotFound_ReturnsFalse()
    {
        var request = BuildCreateRequest();
        _examRepoMock.Setup(r => r.TitleExistsAsync("Math Exam", null)).ReturnsAsync(false);
        _teacherRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Teacher?)null);

        var (success, message, data) = await _service.CreateExamAsync(request);

        success.Should().BeFalse();
        message.Should().Be("Teacher not found");
    }

    [Fact]
    public async Task CreateExamAsync_SubjectNotFound_ReturnsFalse()
    {
        var request = BuildCreateRequest();
        _examRepoMock.Setup(r => r.TitleExistsAsync("Math Exam", null)).ReturnsAsync(false);
        _teacherRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Teacher { Id = 1 });
        _subjectRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Subject?)null);

        var (success, message, data) = await _service.CreateExamAsync(request);

        success.Should().BeFalse();
        message.Should().Be("Subject not found");
    }

    // ===== UpdateExamAsync =====

    [Fact]
    public async Task UpdateExamAsync_DraftExam_UpdatesSuccessfully()
    {
        var exam = BuildExam(id: 1, status: "DRAFT");
        var request = new UpdateExamRequest
        {
            Title = "Updated Title",
            SubjectId = 1,
            DurationMinutes = 90,
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(1).AddHours(2),
            Description = "Updated"
        };
        var subject = new Subject { Id = 1, Name = "Math" };
        var updatedExam = BuildExam(id: 1, title: "Updated Title", status: "DRAFT");

        _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(exam);
        _examRepoMock.Setup(r => r.TitleExistsAsync("Updated Title", 1)).ReturnsAsync(false);
        _subjectRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(subject);
        _examRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Exam>())).ReturnsAsync(updatedExam);

        var (success, message, data) = await _service.UpdateExamAsync(1, request);

        success.Should().BeTrue();
        data!.Title.Should().Be("Updated Title");
    }

    [Fact]
    public async Task UpdateExamAsync_ActiveExam_ReturnsFalse()
    {
        _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(BuildExam(1, status: "ACTIVE"));

        var (success, message, data) = await _service.UpdateExamAsync(1, new UpdateExamRequest
        {
            Title = "New Title", SubjectId = 1, DurationMinutes = 60,
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(1).AddHours(2)
        });

        success.Should().BeFalse();
        message.Should().Be("Cannot update exam in ACTIVE or CLOSED status");
    }

    // ===== DeleteExamAsync =====

    [Fact]
    public async Task DeleteExamAsync_DraftExam_DeletesSuccessfully()
    {
        _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(BuildExam(1, status: "DRAFT"));
        _examRepoMock.Setup(r => r.DeleteAsync(1)).ReturnsAsync(true);

        var (success, message) = await _service.DeleteExamAsync(1);

        success.Should().BeTrue();
        message.Should().Be("Exam deleted successfully");
    }

    [Fact]
    public async Task DeleteExamAsync_ActiveExam_ReturnsFalse()
    {
        _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(BuildExam(1, status: "ACTIVE"));

        var (success, message) = await _service.DeleteExamAsync(1);

        success.Should().BeFalse();
        message.Should().Be("Cannot delete exam that is ACTIVE or CLOSED");
    }

    [Fact]
    public async Task DeleteExamAsync_ClosedExam_ReturnsFalse()
    {
        _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(BuildExam(1, status: "CLOSED"));

        var (success, message) = await _service.DeleteExamAsync(1);

        success.Should().BeFalse();
        message.Should().Be("Cannot delete exam that is ACTIVE or CLOSED");
    }

    // ===== ActivateExamAsync =====

    [Fact]
    public async Task ActivateExamAsync_DraftExam_Activates()
    {
        var exam = BuildExam(1, status: "DRAFT");
        exam.EndTime = DateTime.UtcNow.AddHours(2);
        var activatedExam = BuildExam(1, status: "ACTIVE");
        activatedExam.EndTime = exam.EndTime;

        _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(exam);
        _examRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Exam>())).ReturnsAsync(activatedExam);

        var (success, message, data) = await _service.ActivateExamAsync(1);

        success.Should().BeTrue();
        data!.Status.Should().Be("ACTIVE");
    }

    [Fact]
    public async Task ActivateExamAsync_AlreadyActive_ReturnsFalse()
    {
        _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(BuildExam(1, status: "ACTIVE"));

        var (success, message, data) = await _service.ActivateExamAsync(1);

        success.Should().BeFalse();
        message.Should().Be("Only DRAFT exams can be activated");
    }

    [Fact]
    public async Task ActivateExamAsync_PastEndTime_ReturnsFalse()
    {
        var exam = BuildExam(1, status: "DRAFT");
        exam.EndTime = DateTime.UtcNow.AddHours(-1); // already past

        _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(exam);

        var (success, message, data) = await _service.ActivateExamAsync(1);

        success.Should().BeFalse();
        message.Should().Be("Exam end time must be in the future");
    }

    [Fact]
    public async Task ActivateExamAsync_ClosedExam_ReturnsFalse()
    {
        _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(BuildExam(1, status: "CLOSED"));

        var (success, message, data) = await _service.ActivateExamAsync(1);

        success.Should().BeFalse();
        message.Should().Be("Only DRAFT exams can be activated");
    }

    // ===== CloseExamAsync =====

    [Fact]
    public async Task CloseExamAsync_ActiveExam_Closes()
    {
        var activeExam = BuildExam(1, status: "ACTIVE");
        var closedExam = BuildExam(1, status: "CLOSED");

        _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(activeExam);
        _examRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Exam>())).ReturnsAsync(closedExam);

        var (success, message) = await _service.CloseExamAsync(1);

        success.Should().BeTrue();
        message.Should().Be("Exam closed successfully");
    }

    [Fact]
    public async Task CloseExamAsync_DraftExam_ReturnsFalse()
    {
        _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(BuildExam(1, status: "DRAFT"));

        var (success, message) = await _service.CloseExamAsync(1);

        success.Should().BeFalse();
        message.Should().Be("Only ACTIVE exams can be closed");
    }

    // ===== ChangeStatusAsync =====

    [Fact]
    public async Task ChangeStatusAsync_DraftToActive_Succeeds()
    {
        var exam = BuildExam(1, status: "DRAFT");
        _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(exam);
        _examRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Exam>())).ReturnsAsync(exam);

        var (success, message) = await _service.ChangeStatusAsync(1, "ACTIVE");

        success.Should().BeTrue();
        message.Should().Be("Exam activated");
    }

    [Fact]
    public async Task ChangeStatusAsync_ActiveToClosed_Succeeds()
    {
        var exam = BuildExam(1, status: "ACTIVE");
        _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(exam);
        _examRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Exam>())).ReturnsAsync(exam);

        var (success, message) = await _service.ChangeStatusAsync(1, "CLOSED");

        success.Should().BeTrue();
        message.Should().Be("Exam closed");
    }

    [Fact]
    public async Task ChangeStatusAsync_InvalidStatus_ReturnsFalse()
    {
        var (success, message) = await _service.ChangeStatusAsync(1, "INVALID");

        success.Should().BeFalse();
        message.Should().Be("Invalid status. Must be DRAFT, ACTIVE, or CLOSED");
    }

    [Fact]
    public async Task ChangeStatusAsync_ClosedToDraft_ReturnsFalse()
    {
        _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(BuildExam(1, status: "CLOSED"));

        var (success, message) = await _service.ChangeStatusAsync(1, "DRAFT");

        success.Should().BeFalse();
        message.Should().Contain("Cannot transition from CLOSED to DRAFT");
    }

    // ===== ConfigureSettingsAsync =====

    [Fact]
    public async Task ConfigureSettingsAsync_NewSettings_CreatesAndReturns()
    {
        var exam = BuildExam(1);
        var request = new ConfigureExamSettingsRequest
        {
            ShuffleQuestions = true,
            ShuffleAnswers = false,
            ShowResultImmediately = true,
            AllowReview = true
        };
        var createdSettings = new ExamSetting
        {
            Id = 1, ExamId = 1,
            ShuffleQuestions = true, ShuffleAnswers = false,
            ShowResultImmediately = true, AllowReview = true
        };

        _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(exam);
        _settingsRepoMock.Setup(r => r.GetByExamIdAsync(1)).ReturnsAsync((ExamSetting?)null);
        _settingsRepoMock.Setup(r => r.CreateAsync(It.IsAny<ExamSetting>())).ReturnsAsync(createdSettings);

        var (success, message, data) = await _service.ConfigureSettingsAsync(1, request);

        success.Should().BeTrue();
        data!.ShuffleQuestions.Should().BeTrue();
        data.ShowResultImmediately.Should().BeTrue();
        _settingsRepoMock.Verify(r => r.CreateAsync(It.IsAny<ExamSetting>()), Times.Once);
    }

    [Fact]
    public async Task ConfigureSettingsAsync_ExistingSettings_UpdatesAndReturns()
    {
        var exam = BuildExam(1);
        var existing = new ExamSetting { Id = 1, ExamId = 1 };
        var request = new ConfigureExamSettingsRequest { ShuffleQuestions = true };
        var updated = new ExamSetting { Id = 1, ExamId = 1, ShuffleQuestions = true };

        _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(exam);
        _settingsRepoMock.Setup(r => r.GetByExamIdAsync(1)).ReturnsAsync(existing);
        _settingsRepoMock.Setup(r => r.UpdateAsync(It.IsAny<ExamSetting>())).ReturnsAsync(updated);

        var (success, message, data) = await _service.ConfigureSettingsAsync(1, request);

        success.Should().BeTrue();
        _settingsRepoMock.Verify(r => r.UpdateAsync(It.IsAny<ExamSetting>()), Times.Once);
        _settingsRepoMock.Verify(r => r.CreateAsync(It.IsAny<ExamSetting>()), Times.Never);
    }

    [Fact]
    public async Task ConfigureSettingsAsync_ExamNotFound_ReturnsFalse()
    {
        _examRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Exam?)null);

        var (success, message, data) = await _service.ConfigureSettingsAsync(99, new ConfigureExamSettingsRequest());

        success.Should().BeFalse();
        message.Should().Be("Exam not found");
    }

    // ===== GetSettingsAsync =====

    [Fact]
    public async Task GetSettingsAsync_SettingsExist_ReturnsSettings()
    {
        var settings = new ExamSetting { Id = 1, ExamId = 1, ShuffleQuestions = true };

        _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(BuildExam(1));
        _settingsRepoMock.Setup(r => r.GetByExamIdAsync(1)).ReturnsAsync(settings);

        var (success, message, data) = await _service.GetSettingsAsync(1);

        success.Should().BeTrue();
        data!.ShuffleQuestions.Should().BeTrue();
    }

    [Fact]
    public async Task GetSettingsAsync_NoSettingsExist_ReturnsDefaults()
    {
        _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(BuildExam(1));
        _settingsRepoMock.Setup(r => r.GetByExamIdAsync(1)).ReturnsAsync((ExamSetting?)null);

        var (success, message, data) = await _service.GetSettingsAsync(1);

        success.Should().BeTrue();
        message.Should().Be("Default settings");
        data!.ShuffleQuestions.Should().BeFalse();
        data.AllowReview.Should().BeFalse();
    }

    // ===== Helpers =====

    private static Exam BuildExam(long id = 1, string title = "Test Exam", string status = "DRAFT")
    {
        return new Exam
        {
            Id = id,
            Title = title,
            SubjectId = 1,
            CreatedBy = 1,
            DurationMinutes = 60,
            Status = status,
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(1).AddHours(2),
            Description = "Test",
            Subject = new Subject { Id = 1, Name = "Math" },
            CreatedAt = DateTime.UtcNow
        };
    }

    private static CreateExamRequest BuildCreateRequest(
        string title = "Math Exam",
        int durationMinutes = 60,
        long teacherId = 1,
        long subjectId = 1,
        DateTime? startTime = null,
        DateTime? endTime = null)
    {
        var now = DateTime.UtcNow;
        return new CreateExamRequest
        {
            Title = title,
            SubjectId = subjectId,
            CreatedBy = teacherId,
            DurationMinutes = durationMinutes,
            StartTime = startTime ?? now.AddDays(1),
            EndTime = endTime ?? now.AddDays(1).AddHours(2),
            Description = "Test exam"
        };
    }
}
