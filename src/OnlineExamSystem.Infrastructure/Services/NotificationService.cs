using OnlineExamSystem.Application.DTOs;
using OnlineExamSystem.Domain.Entities;
using OnlineExamSystem.Infrastructure.Repositories;

namespace OnlineExamSystem.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _repo;

    public NotificationService(INotificationRepository repo)
    {
        _repo = repo;
    }

    public async Task<NotificationResponse> CreateAsync(long userId, string type, string title, string message,
        long? relatedEntityId = null, string? relatedEntityType = null)
    {
        var notification = new Notification
        {
            UserId = userId,
            Type = type,
            Title = title,
            Message = message,
            RelatedEntityId = relatedEntityId,
            RelatedEntityType = relatedEntityType,
            CreatedAt = DateTime.UtcNow,
            IsRead = false
        };
        var created = await _repo.CreateAsync(notification);
        return MapToResponse(created);
    }

    public async Task<(bool Success, string Message, List<NotificationResponse>? Data)> GetUserNotificationsAsync(long userId, bool? unreadOnly = null)
    {
        var notifications = await _repo.GetByUserIdAsync(userId, unreadOnly);
        return (true, "OK", notifications.Select(MapToResponse).ToList());
    }

    public async Task<NotificationResponse?> GetByIdAsync(long id)
    {
        var notification = await _repo.GetByIdAsync(id);
        return notification == null ? null : MapToResponse(notification);
    }

    public async Task<(bool Success, string Message)> DeleteAsync(long id)
    {
        var result = await _repo.DeleteAsync(id);
        return result ? (true, "Notification deleted") : (false, "Notification not found");
    }

    public async Task<(bool Success, string Message)> MarkAsReadAsync(long id)
    {
        var result = await _repo.MarkAsReadAsync(id);
        return result ? (true, "Notification marked as read") : (false, "Notification not found");
    }

    public async Task<(bool Success, string Message)> MarkAllAsReadAsync(long userId)
    {
        await _repo.MarkAllAsReadAsync(userId);
        return (true, "All notifications marked as read");
    }

    public async Task<int> GetUnreadCountAsync(long userId)
    {
        return await _repo.GetUnreadCountAsync(userId);
    }

    private static NotificationResponse MapToResponse(Notification n) => new()
    {
        Id = n.Id,
        UserId = n.UserId,
        Type = n.Type,
        Title = n.Title,
        Message = n.Message,
        IsRead = n.IsRead,
        RelatedEntityId = n.RelatedEntityId,
        RelatedEntityType = n.RelatedEntityType,
        CreatedAt = n.CreatedAt,
        ReadAt = n.ReadAt
    };
}
