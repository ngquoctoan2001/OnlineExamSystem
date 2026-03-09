using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using OnlineExamSystem.Application.DTOs.Auth;
using OnlineExamSystem.Application.DTOs.Common;
using OnlineExamSystem.Infrastructure.Services;

namespace OnlineExamSystem.API.Controllers;

/// <summary>
/// Authentication endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Login user with username and password
    /// </summary>
    /// <remarks>
    /// Sample request:
    /// 
    ///     POST /api/auth/login
    ///     {
    ///       "username": "admin",
    ///       "password": "password123"
    ///     }
    /// </remarks>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ResponseResult<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseResult<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ResponseResult<LoginResponse>>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ResponseResult<object> { Success = false, Message = "Invalid input" });

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        var userAgent = Request.Headers["User-Agent"].ToString();

        var (success, message, tokenData) = await _authService.LoginAsync(
            request.Username,
            request.Password,
            ipAddress,
            userAgent,
            cancellationToken);

        if (!success)
            return Unauthorized(new ResponseResult<object> { Success = false, Message = message });

        var response = new LoginResponse
        {
            AccessToken = tokenData?.AccessToken ?? string.Empty,
            RefreshToken = tokenData?.RefreshToken ?? string.Empty
        };

        return Ok(new ResponseResult<LoginResponse>
        {
            Success = true,
            Message = message,
            Data = response
        });
    }

    /// <summary>
    /// Register new user
    /// </summary>
    /// <remarks>
    /// Sample request:
    /// 
    ///     POST /api/auth/register
    ///     {
    ///       "username": "newuser",
    ///       "email": "user@example.com",
    ///       "password": "password123",
    ///       "confirmPassword": "password123",
    ///       "fullName": "New User"
    ///     }
    /// </remarks>
    [HttpPost("register")]
    [ProducesResponseType(typeof(ResponseResult<object>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ResponseResult<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResponseResult<object>>> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ResponseResult<object> { Success = false, Message = "Invalid input" });

        if (request.Password != request.ConfirmPassword)
            return BadRequest(new ResponseResult<object> { Success = false, Message = "Passwords do not match" });

        var (success, message) = await _authService.RegisterAsync(
            request.Username,
            request.Email,
            request.Password,
            request.FullName,
            cancellationToken);

        if (!success)
            return BadRequest(new ResponseResult<object> { Success = false, Message = message });

        return StatusCode(StatusCodes.Status201Created, new ResponseResult<object>
        {
            Success = true,
            Message = message
        });
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    /// <remarks>
    /// Sample request:
    /// 
    ///     POST /api/auth/refresh-token
    ///     {
    ///       "refreshToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    ///     }
    /// </remarks>
    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(ResponseResult<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseResult<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ResponseResult<LoginResponse>>> RefreshToken(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ResponseResult<object> { Success = false, Message = "Invalid input" });

        var (success, message, tokenData) = await _authService.RefreshTokenAsync(
            request.RefreshToken,
            cancellationToken);

        if (!success)
            return Unauthorized(new ResponseResult<object> { Success = false, Message = message });

        var response = new LoginResponse
        {
            AccessToken = tokenData?.AccessToken ?? string.Empty,
            RefreshToken = tokenData?.RefreshToken ?? string.Empty
        };

        return Ok(new ResponseResult<LoginResponse>
        {
            Success = true,
            Message = message,
            Data = response
        });
    }

    /// <summary>
    /// Logout user - clear all sessions
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(ResponseResult<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResponseResult<object>>> Logout(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst("sub")?.Value ?? User.FindFirst("nameid")?.Value;
        if (!long.TryParse(userIdClaim, out var userId))
            return Unauthorized(new ResponseResult<object> { Success = false, Message = "Invalid user" });

        var success = await _authService.LogoutAsync(userId, cancellationToken);

        return Ok(new ResponseResult<object>
        {
            Success = success,
            Message = success ? "Logged out successfully" : "Logout failed"
        });
    }

    /// <summary>
    /// Get current user profile
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ResponseResult<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseResult<object>), StatusCodes.Status401Unauthorized)]
    public ActionResult<ResponseResult<UserDto>> GetProfile()
    {
        var userIdClaim = User.FindFirst("sub")?.Value ?? User.FindFirst("nameid")?.Value;
        var username = User.FindFirst("unique_name")?.Value ?? User.Identity?.Name;

        if (!long.TryParse(userIdClaim, out var userId))
            return Unauthorized(new ResponseResult<object> { Success = false, Message = "Invalid user" });

        var user = new UserDto
        {
            Id = userId,
            Username = username ?? string.Empty,
            Role = "STUDENT" // TODO: Get from user roles
        };

        return Ok(new ResponseResult<UserDto>
        {
            Success = true,
            Data = user
        });
    }
}
