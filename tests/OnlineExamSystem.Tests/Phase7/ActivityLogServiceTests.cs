using FluentAssertions;
using Moq;
using OnlineExamSystem.Application.DTOs;
using OnlineExamSystem.Domain.Entities;
using OnlineExamSystem.Infrastructure.Repositories;
using OnlineExamSystem.Infrastructure.Services;
using Xunit;

namespace OnlineExamSystem.Tests.Phase7;

public class ActivityLogServiceTests
{
    private readonly Mock<IActivityLogRepository> _repoMock = new();
    private readonly ActivityLogService _service;

    public ActivityLogServiceTests()
    {
        _service = new ActivityLogService(_repoMock.Object);
    }

    // ===== LogAsync =====

    [Fact]
    public async Task LogAsync_CreatesLogWithCorrectFields()
    {
        ActivityLog? captured = null;
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<ActivityLog>()))
            .Callback<ActivityLog>(l => captured = l)
            .ReturnsAsync((ActivityLog l) => l);

        await _service.LogAsync(42, "EXAM_CREATED", "Exam", 7, "Title: Test", "127.0.0.1");

        captured.Should().NotBeNull();
        captured!.UserId.Should().Be(42);
        captured.Action.Should().Be("EXAM_CREATED");
        captured.EntityType.Should().Be("Exam");
        captured.EntityId.Should().Be(7);
        captured.Detail.Should().Be("Title: Test");
        captured.IpAddress.Should().Be("127.0.0.1");
    }

    [Fact]
    public async Task LogAsync_NullUserId_LogsWithoutUserId()
    {
        ActivityLog? captured = null;
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<ActivityLog>()))
            .Callback<ActivityLog>(l => captured = l)
            .ReturnsAsync((ActivityLog l) => l);

        await _service.LogAsync(null, "SYSTEM_ACTION");

        captured!.UserId.Should().BeNull();
        captured.Action.Should().Be("SYSTEM_ACTION");
    }

    // ===== GetLogsAsync =====

    [Fact]
    public async Task GetLogsAsync_ReturnsPaginatedLogs()
    {
        var logs = new List<ActivityLog>
        {
            new() { Id = 1, UserId = 10, Action = "EXAM_CREATED", OccurredAt = DateTime.UtcNow },
            new() { Id = 2, UserId = 10, Action = "EXAM_SUBMITTED", OccurredAt = DateTime.UtcNow }
        };
        _repoMock.Setup(r => r.GetAllAsync(1, 20, null, null, null, null))
            .ReturnsAsync((logs, 2));

        var (success, _, data) = await _service.GetLogsAsync(1, 20);

        success.Should().BeTrue();
        data.Should().NotBeNull();
        data!.Logs.Should().HaveCount(2);
        data.TotalCount.Should().Be(2);
        data.Page.Should().Be(1);
        data.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task GetLogsAsync_FilterByAction_PassesFilterDown()
    {
        _repoMock.Setup(r => r.GetAllAsync(1, 20, "EXAM_CREATED", null, null, null))
            .ReturnsAsync((new List<ActivityLog>(), 0));

        var (success, _, data) = await _service.GetLogsAsync(1, 20, action: "EXAM_CREATED");

        success.Should().BeTrue();
        data!.TotalCount.Should().Be(0);
        _repoMock.Verify(r => r.GetAllAsync(1, 20, "EXAM_CREATED", null, null, null), Times.Once);
    }

    [Fact]
    public async Task GetLogsAsync_InvalidPage_DefaultsToOne()
    {
        _repoMock.Setup(r => r.GetAllAsync(1, 20, null, null, null, null))
            .ReturnsAsync((new List<ActivityLog>(), 0));

        var (success, _, data) = await _service.GetLogsAsync(0, 20);

        success.Should().BeTrue();
        data!.Page.Should().Be(1);
    }

    [Fact]
    public async Task GetLogsAsync_TotalPages_CalculatedCorrectly()
    {
        var logs = Enumerable.Range(1, 5).Select(i => new ActivityLog
        {
            Id = i, Action = "TEST", OccurredAt = DateTime.UtcNow
        }).ToList();
        _repoMock.Setup(r => r.GetAllAsync(1, 5, null, null, null, null))
            .ReturnsAsync((logs, 13));

        var (_, _, data) = await _service.GetLogsAsync(1, 5);

        data!.TotalPages.Should().Be(3); // ceil(13/5) = 3
    }
}
