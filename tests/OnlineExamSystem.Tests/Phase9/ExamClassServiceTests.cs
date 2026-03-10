using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using OnlineExamSystem.Application.DTOs;
using OnlineExamSystem.Application.Services;
using OnlineExamSystem.Domain.Entities;
using OnlineExamSystem.Infrastructure.Repositories;
using OnlineExamSystem.Infrastructure.Services;
using Xunit;

namespace OnlineExamSystem.Tests.Phase9;

public class ExamClassServiceTests
{
    private readonly Mock<IExamClassRepository> _examClassRepoMock = new();
    private readonly Mock<IExamRepository> _examRepoMock = new();
    private readonly Mock<IClassRepository> _classRepoMock = new();
    private readonly Mock<ILogger<ExamClassService>> _loggerMock = new();
    private readonly ExamClassService _service;

    public ExamClassServiceTests()
    {
        _service = new ExamClassService(
            _examClassRepoMock.Object,
            _examRepoMock.Object,
            _classRepoMock.Object,
            _loggerMock.Object);
    }

    private static Exam BuildExam(long id = 1) => new()
    {
        Id = id, Title = "Math Exam", Status = "DRAFT",
        DurationMinutes = 60, StartTime = DateTime.UtcNow, EndTime = DateTime.UtcNow.AddHours(2)
    };

    private static Class BuildClass(long id = 1) => new()
    {
        Id = id, Name = "Class A"
    };

    // ===== AssignClassToExamAsync =====

    [Fact]
    public async Task AssignClassToExam_ExamNotFound_ReturnsFalse()
    {
        _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Exam?)null);

        var (success, message, _) = await _service.AssignClassToExamAsync(1, 1);

        success.Should().BeFalse();
        message.Should().Be("Exam not found");
    }

    [Fact]
    public async Task AssignClassToExam_ClassNotFound_ReturnsFalse()
    {
        _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(BuildExam());
        _classRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Class?)null);

        var (success, message, _) = await _service.AssignClassToExamAsync(1, 1);

        success.Should().BeFalse();
        message.Should().Be("Class not found");
    }

    [Fact]
    public async Task AssignClassToExam_AlreadyAssigned_ReturnsFalse()
    {
        _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(BuildExam());
        _classRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(BuildClass());
        _examClassRepoMock.Setup(r => r.ExistsAsync(1, 1)).ReturnsAsync(true);

        var (success, message, _) = await _service.AssignClassToExamAsync(1, 1);

        success.Should().BeFalse();
        message.Should().Contain("already assigned");
    }

    [Fact]
    public async Task AssignClassToExam_ValidRequest_ReturnsSuccess()
    {
        var exam = BuildExam();
        var cls = BuildClass();
        var examClass = new ExamClass { ExamId = 1, ClassId = 1, AssignedAt = DateTime.UtcNow };

        _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(exam);
        _classRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(cls);
        _examClassRepoMock.Setup(r => r.ExistsAsync(1, 1)).ReturnsAsync(false);
        _examClassRepoMock.Setup(r => r.CreateAsync(It.IsAny<ExamClass>())).ReturnsAsync(examClass);

        var (success, message, data) = await _service.AssignClassToExamAsync(1, 1);

        success.Should().BeTrue();
        message.Should().Contain("successfully");
        data.Should().NotBeNull();
        data!.ExamId.Should().Be(1);
        data.ClassId.Should().Be(1);
    }

    // ===== GetExamClassesAsync =====

    [Fact]
    public async Task GetExamClassesAsync_ExamNotFound_ReturnsFalse()
    {
        _examRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Exam?)null);

        var (success, message, _) = await _service.GetExamClassesAsync(99);

        success.Should().BeFalse();
        message.Should().Be("Exam not found");
    }

    [Fact]
    public async Task GetExamClassesAsync_ValidExam_ReturnsClasses()
    {
        var exam = BuildExam();
        var examClasses = new List<ExamClass>
        {
            new() { ExamId = 1, ClassId = 1, Class = BuildClass(), AssignedAt = DateTime.UtcNow }
        };
        _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(exam);
        _examClassRepoMock.Setup(r => r.GetExamClassesAsync(1)).ReturnsAsync(examClasses);

        var (success, _, data) = await _service.GetExamClassesAsync(1);

        success.Should().BeTrue();
        data!.Classes.Should().HaveCount(1);
        data.ExamTitle.Should().Be("Math Exam");
    }

    // ===== RemoveClassFromExamAsync =====

    [Fact]
    public async Task RemoveClassFromExam_ExamNotFound_ReturnsFalse()
    {
        _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Exam?)null);

        var (success, message) = await _service.RemoveClassFromExamAsync(1, 1);

        success.Should().BeFalse();
        message.Should().Be("Exam not found");
    }
}
