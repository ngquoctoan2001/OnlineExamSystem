using FluentAssertions;
using Moq;
using OnlineExamSystem.Application.DTOs;
using OnlineExamSystem.Domain.Entities;
using OnlineExamSystem.Infrastructure.Repositories;
using OnlineExamSystem.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Xunit;

namespace OnlineExamSystem.Tests.Phase6;

public class StatisticsServiceTests
{
    private readonly Mock<IExamStatisticRepository> _statRepoMock = new();
    private readonly Mock<IExamAttemptRepository> _attemptRepoMock = new();
    private readonly Mock<IExamRepository> _examRepoMock = new();
    private readonly Mock<IStudentRepository> _studentRepoMock = new();
    private readonly Mock<IClassRepository> _classRepoMock = new();
    private readonly Mock<ILogger<StatisticsService>> _loggerMock = new();
    private readonly StatisticsService _service;

    public StatisticsServiceTests()
    {
        _service = new StatisticsService(
            _statRepoMock.Object,
            _attemptRepoMock.Object,
            _examRepoMock.Object,
            _studentRepoMock.Object,
            _classRepoMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task CalculateStats_WhenExamNotFound_ReturnsFalse()
    {
        _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Exam?)null);

        var result = await _service.CalculateAndSaveExamStatisticsAsync(1);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task CalculateStats_WithGradedAttempts_ComputesCorrectly()
    {
        var exam = new Exam { Id = 1, Title = "Math Exam", TotalScore = 10 };
        var attempts = new List<ExamAttempt>
        {
            new() { Id = 1, ExamId = 1, Status = "GRADED", Score = 4 },
            new() { Id = 2, ExamId = 1, Status = "GRADED", Score = 8 },
            new() { Id = 3, ExamId = 1, Status = "GRADED", Score = 6 }
        };
        var savedStat = new ExamStatistic
        {
            ExamId = 1, TotalAttempts = 3, PassCount = 2, FailCount = 1,
            AverageScore = 6, MaxScore = 8, MinScore = 4, CalculatedAt = DateTime.UtcNow
        };

        _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(exam);
        _attemptRepoMock.Setup(r => r.GetExamAttemptsAsync(1)).ReturnsAsync(attempts);
        _statRepoMock.Setup(r => r.UpdateOrCreateAsync(It.IsAny<ExamStatistic>())).ReturnsAsync(savedStat);

        var result = await _service.CalculateAndSaveExamStatisticsAsync(1);

        result.Success.Should().BeTrue();
        result.Data!.TotalAttempts.Should().Be(3);
        result.Data!.PassCount.Should().Be(2);
        result.Data!.FailCount.Should().Be(1);
    }

    [Fact]
    public async Task GetStats_WhenNotCalculated_ReturnsFalse()
    {
        var exam = new Exam { Id = 1, Title = "Math Exam" };
        _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(exam);
        _statRepoMock.Setup(r => r.GetByExamIdAsync(1)).ReturnsAsync((ExamStatistic?)null);

        var result = await _service.GetExamStatisticsAsync(1);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not yet calculated");
    }

    [Fact]
    public async Task GetDistribution_ReturnsCorrectBuckets()
    {
        var exam = new Exam { Id = 1, Title = "Math Exam", TotalScore = 10 };
        var attempts = new List<ExamAttempt>
        {
            new() { Id = 1, ExamId = 1, Score = 1 },   // bucket 0-2
            new() { Id = 2, ExamId = 1, Score = 5 },   // bucket 4-6
            new() { Id = 3, ExamId = 1, Score = 9 },   // bucket 8-10
            new() { Id = 4, ExamId = 1, Score = 9 }    // bucket 8-10
        };

        _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(exam);
        _attemptRepoMock.Setup(r => r.GetExamAttemptsAsync(1)).ReturnsAsync(attempts);

        var result = await _service.GetScoreDistributionAsync(1);

        result.Success.Should().BeTrue();
        result.Data!.Buckets.Should().HaveCount(5);
        result.Data!.Buckets.Single(b => b.Label == "8-10").Count.Should().Be(2);
        result.Data!.Buckets.Single(b => b.Label == "0-2").Count.Should().Be(1);
    }

    [Fact]
    public async Task GetStudentPerformance_WhenStudentNotFound_ReturnsFalse()
    {
        _studentRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Student?)null);

        var result = await _service.GetStudentPerformanceAsync(99);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task GetStudentPerformance_ReturnsAttempts()
    {
        var student = new Student { Id = 1, User = new User { FullName = "Nguyen Van A" } };
        var attempts = new List<ExamAttempt>
        {
            new() { Id = 1, ExamId = 10, Status = "GRADED", Score = 8, StartTime = DateTime.UtcNow },
            new() { Id = 2, ExamId = 11, Status = "SUBMITTED", Score = null, StartTime = DateTime.UtcNow }
        };
        var exam10 = new Exam { Id = 10, Title = "Math", Subject = new Subject { Name = "Math" } };
        var exam11 = new Exam { Id = 11, Title = "Physics", Subject = new Subject { Name = "Physics" } };

        _studentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(student);
        _attemptRepoMock.Setup(r => r.GetStudentAttemptsAsync(1)).ReturnsAsync(attempts);
        _examRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(exam10);
        _examRepoMock.Setup(r => r.GetByIdAsync(11)).ReturnsAsync(exam11);

        var result = await _service.GetStudentPerformanceAsync(1);

        result.Success.Should().BeTrue();
        result.Data!.TotalAttempts.Should().Be(2);
        result.Data!.AverageScore.Should().Be(8); // only 1 scored attempt
        result.Data!.Attempts.Should().HaveCount(2);
    }
}
