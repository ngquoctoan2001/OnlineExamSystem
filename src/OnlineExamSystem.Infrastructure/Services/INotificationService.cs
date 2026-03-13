using OnlineExamSystem.Application.DTOs;

namespace OnlineExamSystem.Infrastructure.Services;

public interface INotificationService
{
    Task<NotificationResponse> CreateAsync(long userId, string type, string title, string message,
        long? relatedEntityId = null, string? relatedEntityType = null);
    Task<NotificationResponse?> GetByIdAsync(long id);
    Task<(bool Success, string Message, List<NotificationResponse>? Data)> GetUserNotificationsAsync(long userId, bool? unreadOnly = null);
    Task<(bool Success, string Message)> DeleteAsync(long id);
    Task<(bool Success, string Message)> MarkAsReadAsync(long id);
    Task<(bool Success, string Message)> MarkAllAsReadAsync(long userId);
    Task<int> GetUnreadCountAsync(long userId);
}
