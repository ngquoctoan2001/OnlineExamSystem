using FluentAssertions;
using Moq;
using OnlineExamSystem.Application.DTOs;
using OnlineExamSystem.Domain.Entities;
using OnlineExamSystem.Infrastructure.Repositories;
using OnlineExamSystem.Infrastructure.Services;
using Xunit;

namespace OnlineExamSystem.Tests.Phase7;

public class NotificationServiceTests
{
    private readonly Mock<INotificationRepository> _repoMock = new();
    private readonly NotificationService _service;

    public NotificationServiceTests()
    {
        _service = new NotificationService(_repoMock.Object);
    }

    // ===== CreateAsync =====

    [Fact]
    public async Task CreateAsync_ValidData_CreatesNotification()
    {
        var notification = new Notification
        {
            Id = 1, UserId = 10, Type = "EXAM_CREATED",
            Title = "New Exam", Message = "An exam was created",
            IsRead = false, CreatedAt = DateTime.UtcNow
        };
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Notification>())).ReturnsAsync(notification);

        var result = await _service.CreateAsync(10, "EXAM_CREATED", "New Exam", "An exam was created");

        result.Should().NotBeNull();
        result.UserId.Should().Be(10);
        result.Type.Should().Be("EXAM_CREATED");
        result.IsRead.Should().BeFalse();
    }

    [Fact]
    public async Task CreateAsync_WithRelatedEntity_SetsFields()
    {
        var notification = new Notification
        {
            Id = 2, UserId = 5, Type = "RESULT_PUBLISHED",
            Title = "Result Ready", Message = "Your result is ready",
            RelatedEntityId = 99, RelatedEntityType = "ExamAttempt",
            IsRead = false, CreatedAt = DateTime.UtcNow
        };
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Notification>())).ReturnsAsync(notification);

        var result = await _service.CreateAsync(5, "RESULT_PUBLISHED", "Result Ready", "Your result is ready", 99, "ExamAttempt");

        result.RelatedEntityId.Should().Be(99);
        result.RelatedEntityType.Should().Be("ExamAttempt");
    }

    // ===== GetUserNotificationsAsync =====

    [Fact]
    public async Task GetUserNotificationsAsync_ReturnsAll_WhenUnreadOnlyFalse()
    {
        var notifications = new List<Notification>
        {
            new() { Id = 1, UserId = 10, Type = "A", Title = "T1", Message = "M1", IsRead = true, CreatedAt = DateTime.UtcNow },
            new() { Id = 2, UserId = 10, Type = "B", Title = "T2", Message = "M2", IsRead = false, CreatedAt = DateTime.UtcNow }
        };
        _repoMock.Setup(r => r.GetByUserIdAsync(10, null)).ReturnsAsync(notifications);

        var (success, _, data) = await _service.GetUserNotificationsAsync(10);

        success.Should().BeTrue();
        data.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetUserNotificationsAsync_ReturnsUnread_WhenUnreadOnlyTrue()
    {
        var unread = new List<Notification>
        {
            new() { Id = 3, UserId = 10, Type = "C", Title = "T3", Message = "M3", IsRead = false, CreatedAt = DateTime.UtcNow }
        };
        _repoMock.Setup(r => r.GetByUserIdAsync(10, true)).ReturnsAsync(unread);

        var (success, _, data) = await _service.GetUserNotificationsAsync(10, unreadOnly: true);

        success.Should().BeTrue();
        data.Should().HaveCount(1);
        data![0].IsRead.Should().BeFalse();
    }

    // ===== MarkAsReadAsync =====

    [Fact]
    public async Task MarkAsReadAsync_ExistingNotification_ReturnsSuccess()
    {
        _repoMock.Setup(r => r.MarkAsReadAsync(1)).ReturnsAsync(true);

        var (success, message) = await _service.MarkAsReadAsync(1);

        success.Should().BeTrue();
        message.Should().Contain("read");
    }

    [Fact]
    public async Task MarkAsReadAsync_NonExisting_ReturnsFalse()
    {
        _repoMock.Setup(r => r.MarkAsReadAsync(999)).ReturnsAsync(false);

        var (success, message) = await _service.MarkAsReadAsync(999);

        success.Should().BeFalse();
        message.Should().Contain("not found");
    }

    // ===== MarkAllAsReadAsync =====

    [Fact]
    public async Task MarkAllAsReadAsync_ReturnsSuccess()
    {
        _repoMock.Setup(r => r.MarkAllAsReadAsync(10)).ReturnsAsync(true);

        var (success, message) = await _service.MarkAllAsReadAsync(10);

        success.Should().BeTrue();
        message.Should().Contain("All");
    }

    // ===== GetUnreadCountAsync =====

    [Fact]
    public async Task GetUnreadCountAsync_ReturnsCount()
    {
        _repoMock.Setup(r => r.GetUnreadCountAsync(10)).ReturnsAsync(5);

        var count = await _service.GetUnreadCountAsync(10);

        count.Should().Be(5);
    }
}
