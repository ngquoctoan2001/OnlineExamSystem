using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineExamSystem.Application.DTOs;
using OnlineExamSystem.Application.DTOs.Common;
using OnlineExamSystem.Infrastructure.Data;
using OnlineExamSystem.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace OnlineExamSystem.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "ADMIN")]
[Produces("application/json")]
[Tags("Admin")]
public class AdminController : ControllerBase
{
    private readonly IStatisticsService _statisticsService;
    private readonly IActivityLogService _activityLogService;
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IStatisticsService statisticsService,
        IActivityLogService activityLogService,
        ApplicationDbContext dbContext,
        ILogger<AdminController> logger)
    {
        _statisticsService = statisticsService;
        _activityLogService = activityLogService;
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Get system-wide statistics
    /// </summary>
    [HttpGet("system-stats")]
    public async Task<ActionResult<ResponseResult<SystemStatsResponse>>> GetSystemStats()
    {
        var stats = new SystemStatsResponse
        {
            TotalUsers = await _dbContext.Users.CountAsync(),
            TotalTeachers = await _dbContext.Teachers.CountAsync(),
            TotalStudents = await _dbContext.Students.CountAsync(),
            TotalClasses = await _dbContext.Classes.CountAsync(),
            TotalExams = await _dbContext.Exams.CountAsync(),
            TotalQuestions = await _dbContext.Questions.CountAsync(),
            ActiveExams = await _dbContext.Exams.CountAsync(e => e.Status == "ACTIVE"),
            TotalAttempts = await _dbContext.ExamAttempts.CountAsync()
        };

        return Ok(new ResponseResult<SystemStatsResponse>
        {
            Success = true,
            Message = "System statistics retrieved",
            Data = stats
        });
    }

    /// <summary>
    /// Get system activity logs
    /// </summary>
    [HttpGet("logs")]
    public async Task<ActionResult<ResponseResult<ActivityLogPagedResponse>>> GetLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? action = null,
        [FromQuery] long? userId = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var (success, message, data) = await _activityLogService.GetLogsAsync(page, pageSize, action, userId, from, to);
        return Ok(new ResponseResult<ActivityLogPagedResponse>
        {
            Success = success,
            Message = message,
            Data = data
        });
    }

    /// <summary>
    /// Backup database (placeholder - requires admin infrastructure)
    /// </summary>
    [HttpPost("backup")]
    public async Task<ActionResult<ResponseResult<BackupResponse>>> BackupDatabase()
    {
        _logger.LogInformation("Database backup requested by admin");

        // In a production system, this would trigger a pg_dump or similar
        var backupId = Guid.NewGuid().ToString("N");
        var timestamp = DateTime.UtcNow;

        await _activityLogService.LogAsync(null, "DATABASE_BACKUP", "System", 0,
            $"Backup initiated: {backupId}");

        return Ok(new ResponseResult<BackupResponse>
        {
            Success = true,
            Message = "Database backup initiated",
            Data = new BackupResponse
            {
                BackupId = backupId,
                Status = "INITIATED",
                CreatedAt = timestamp
            }
        });
    }

    /// <summary>
    /// Restore database (placeholder - requires admin infrastructure)
    /// </summary>
    [HttpPost("restore")]
    public async Task<ActionResult<ResponseResult<object>>> RestoreDatabase([FromBody] RestoreRequest request)
    {
        _logger.LogWarning("Database restore requested for backup: {BackupId}", request.BackupId);

        await _activityLogService.LogAsync(null, "DATABASE_RESTORE", "System", 0,
            $"Restore requested for backup: {request.BackupId}");

        return Ok(new ResponseResult<object>
        {
            Success = true,
            Message = "Database restore initiated. This operation may take several minutes.",
            Data = new { request.BackupId, Status = "INITIATED", RequestedAt = DateTime.UtcNow }
        });
    }

    /// <summary>
    /// System health check
    /// </summary>
    [HttpGet("health")]
    [AllowAnonymous]
    public async Task<ActionResult<ResponseResult<HealthCheckResponse>>> HealthCheck()
    {
        var dbHealthy = false;
        try
        {
            dbHealthy = await _dbContext.Database.CanConnectAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
        }

        var response = new HealthCheckResponse
        {
            Status = dbHealthy ? "Healthy" : "Unhealthy",
            Database = dbHealthy ? "Connected" : "Disconnected",
            Timestamp = DateTime.UtcNow,
            Version = typeof(AdminController).Assembly.GetName().Version?.ToString() ?? "1.0.0"
        };

        return dbHealthy
            ? Ok(new ResponseResult<HealthCheckResponse> { Success = true, Message = "System is healthy", Data = response })
            : StatusCode(503, new ResponseResult<HealthCheckResponse> { Success = false, Message = "System is unhealthy", Data = response });
    }
}

public class BackupResponse
{
    public string BackupId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class RestoreRequest
{
    public string BackupId { get; set; } = string.Empty;
}

public class HealthCheckResponse
{
    public string Status { get; set; } = string.Empty;
    public string Database { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Version { get; set; } = string.Empty;
}
