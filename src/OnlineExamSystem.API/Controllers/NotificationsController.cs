using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineExamSystem.Application.DTOs;
using OnlineExamSystem.Application.DTOs.Common;
using OnlineExamSystem.Infrastructure.Services;

namespace OnlineExamSystem.API.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
[Produces("application/json")]
[Tags("Notifications")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<ActionResult<ResponseResult<List<NotificationResponse>>>> GetUserNotifications(
        [FromQuery] long userId, [FromQuery] bool? unreadOnly = null)
    {
        var (success, message, data) = await _notificationService.GetUserNotificationsAsync(userId, unreadOnly);
        return Ok(new ResponseResult<List<NotificationResponse>> { Success = success, Message = message, Data = data });
    }

    [HttpPost]
    public async Task<ActionResult<ResponseResult<NotificationResponse>>> Create([FromBody] CreateNotificationRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ResponseResult<NotificationResponse> { Success = false, Message = "Invalid request" });

        var data = await _notificationService.CreateAsync(
            request.UserId, request.Type, request.Title, request.Message,
            request.RelatedEntityId, request.RelatedEntityType);

        return Ok(new ResponseResult<NotificationResponse> { Success = true, Message = "Notification created", Data = data });
    }

    [HttpPut("{id}/read")]
    public async Task<ActionResult<ResponseResult<object>>> MarkAsRead(long id)
    {
        var (success, message) = await _notificationService.MarkAsReadAsync(id);
        if (!success)
            return NotFound(new ResponseResult<object> { Success = false, Message = message });
        return Ok(new ResponseResult<object> { Success = true, Message = message });
    }

    [HttpPut("read-all")]
    public async Task<ActionResult<ResponseResult<object>>> MarkAllAsRead([FromQuery] long userId)
    {
        var (success, message) = await _notificationService.MarkAllAsReadAsync(userId);
        return Ok(new ResponseResult<object> { Success = success, Message = message });
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<ResponseResult<int>>> GetUnreadCount([FromQuery] long userId)
    {
        var count = await _notificationService.GetUnreadCountAsync(userId);
        return Ok(new ResponseResult<int> { Success = true, Message = "OK", Data = count });
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ResponseResult<object>>> Delete(long id)
    {
        var (success, message) = await _notificationService.DeleteAsync(id);
        if (!success)
            return NotFound(new ResponseResult<object> { Success = false, Message = message });

        return Ok(new ResponseResult<object> { Success = true, Message = message });
    }
}
