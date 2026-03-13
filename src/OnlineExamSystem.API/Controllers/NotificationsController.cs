using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineExamSystem.Application.DTOs;
using OnlineExamSystem.Application.DTOs.Common;
using OnlineExamSystem.Infrastructure.Repositories;
using OnlineExamSystem.Infrastructure.Services;
using System.Security.Claims;

namespace OnlineExamSystem.API.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
[Produces("application/json")]
[Tags("Notifications")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly IClassRepository _classRepository;

    public NotificationsController(INotificationService notificationService, IClassRepository classRepository)
    {
        _notificationService = notificationService;
        _classRepository = classRepository;
    }

    private long? GetCurrentUserId()
    {
        var claim = User.FindFirst("userId")?.Value
                    ?? User.FindFirst("UserId")?.Value
                    ?? User.FindFirst("sub")?.Value
                    ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return long.TryParse(claim, out var id) ? id : null;
    }

    private bool CanAccessUserNotifications(long userId)
    {
        if (User.IsInRole("ADMIN"))
            return true;

        var currentUserId = GetCurrentUserId();
        return currentUserId.HasValue && currentUserId.Value == userId;
    }

    [HttpGet]
    public async Task<ActionResult<ResponseResult<List<NotificationResponse>>>> GetUserNotifications(
        [FromQuery] long userId, [FromQuery] bool? unreadOnly = null)
    {
        if (!CanAccessUserNotifications(userId))
            return Forbid();

        var (success, message, data) = await _notificationService.GetUserNotificationsAsync(userId, unreadOnly);
        return Ok(new ResponseResult<List<NotificationResponse>> { Success = success, Message = message, Data = data });
    }

    [Authorize(Roles = "ADMIN,TEACHER")]
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
        var notification = await _notificationService.GetByIdAsync(id);
        if (notification == null)
            return NotFound(new ResponseResult<object> { Success = false, Message = "Notification not found" });

        if (!CanAccessUserNotifications(notification.UserId))
            return Forbid();

        var (success, message) = await _notificationService.MarkAsReadAsync(id);
        if (!success)
            return NotFound(new ResponseResult<object> { Success = false, Message = message });
        return Ok(new ResponseResult<object> { Success = true, Message = message });
    }

    [HttpPut("read-all")]
    public async Task<ActionResult<ResponseResult<object>>> MarkAllAsRead([FromQuery] long userId)
    {
        if (!CanAccessUserNotifications(userId))
            return Forbid();

        var (success, message) = await _notificationService.MarkAllAsReadAsync(userId);
        return Ok(new ResponseResult<object> { Success = success, Message = message });
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<ResponseResult<int>>> GetUnreadCount([FromQuery] long userId)
    {
        if (!CanAccessUserNotifications(userId))
            return Forbid();

        var count = await _notificationService.GetUnreadCountAsync(userId);
        return Ok(new ResponseResult<int> { Success = true, Message = "OK", Data = count });
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ResponseResult<object>>> Delete(long id)
    {
        var notification = await _notificationService.GetByIdAsync(id);
        if (notification == null)
            return NotFound(new ResponseResult<object> { Success = false, Message = "Notification not found" });

        if (!CanAccessUserNotifications(notification.UserId))
            return Forbid();

        var (success, message) = await _notificationService.DeleteAsync(id);
        if (!success)
            return NotFound(new ResponseResult<object> { Success = false, Message = message });

        return Ok(new ResponseResult<object> { Success = true, Message = message });
    }

    /// <summary>
    /// Send notification to all students in a class
    /// </summary>
    [HttpPost("send-class")]
    [Authorize(Roles = "ADMIN,TEACHER")]
    public async Task<ActionResult<ResponseResult<object>>> SendToClass([FromBody] SendNotificationToClassRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ResponseResult<object> { Success = false, Message = "Invalid request" });

        var students = await _classRepository.GetClassStudentsAsync(request.ClassId);
        if (students == null || students.Count == 0)
            return NotFound(new ResponseResult<object> { Success = false, Message = "No students found in class" });

        var sentCount = 0;
        foreach (var cs in students)
        {
            var student = cs.Student;
            if (student?.UserId > 0)
            {
                await _notificationService.CreateAsync(
                    student.UserId, request.Type, request.Title, request.Message,
                    request.RelatedEntityId, request.RelatedEntityType);
                sentCount++;
            }
        }

        return Ok(new ResponseResult<object>
        {
            Success = true,
            Message = $"Notification sent to {sentCount} students",
            Data = new { ClassId = request.ClassId, SentCount = sentCount }
        });
    }
}
