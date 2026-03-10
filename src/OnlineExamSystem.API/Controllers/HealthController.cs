using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineExamSystem.Infrastructure.Data;

namespace OnlineExamSystem.API.Controllers;

[ApiController]
[Route("api/health")]
[Tags("Health")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;
    private readonly ApplicationDbContext _context;

    public HealthController(ILogger<HealthController> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetHealth()
    {
        string dbStatus;
        try
        {
            await _context.Database.CanConnectAsync();
            dbStatus = "connected";
        }
        catch
        {
            dbStatus = "disconnected";
        }

        var response = new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            database = dbStatus,
            version = "1.0.0"
        };

        _logger.LogInformation("Health check: database={DbStatus}", dbStatus);
        return Ok(response);
    }

    [HttpGet("status")]
    [AllowAnonymous]
    public IActionResult Status()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
            version = "1.0.0"
        });
    }
}

