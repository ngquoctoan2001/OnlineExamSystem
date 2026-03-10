using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using OnlineExamSystem.Application.DTOs.Auth;
using OnlineExamSystem.Application.DTOs.Common;
using OnlineExamSystem.Infrastructure.Services;
using OnlineExamSystem.Infrastructure.Repositories;

namespace OnlineExamSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("auth")]
[Produces("application/json")]
[Tags("Authentication")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, IUserRepository userRepository, ILogger<AuthController> logger)
    {
        _authService = authService;
        _userRepository = userRepository;
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
        var userIdClaim = User.FindFirst("sub")?.Value
            ?? User.FindFirst("nameid")?.Value
            ?? User.FindFirst("UserId")?.Value
            ?? User.FindFirst("userId")?.Value;
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
    public async Task<ActionResult<ResponseResult<UserDto>>> GetProfile(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst("sub")?.Value
            ?? User.FindFirst("nameid")?.Value
            ?? User.FindFirst("UserId")?.Value
            ?? User.FindFirst("userId")?.Value;

        if (!long.TryParse(userIdClaim, out var userId))
            return Unauthorized(new ResponseResult<object> { Success = false, Message = "Invalid user" });

        var dbUser = await _userRepository.GetUserWithRolesAsync(userId, cancellationToken);
        if (dbUser == null)
            return NotFound(new ResponseResult<object> { Success = false, Message = "User not found" });

        var role = dbUser.UserRoles?.Select(ur => ur.Role?.Name).FirstOrDefault(r => !string.IsNullOrWhiteSpace(r)) ?? string.Empty;

        var user = new UserDto
        {
            Id = userId,
            Username = dbUser.Username,
            Email = dbUser.Email,
            FullName = dbUser.FullName,
            IsActive = dbUser.IsActive,
            Role = role
        };

        return Ok(new ResponseResult<UserDto>
        {
            Success = true,
            Data = user
        });
    }

    /// <summary>
    /// Change password for current user
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(typeof(ResponseResult<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseResult<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseResult<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ResponseResult<object>>> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ResponseResult<object> { Success = false, Message = "Invalid input" });

        if (request.NewPassword != request.ConfirmNewPassword)
            return BadRequest(new ResponseResult<object> { Success = false, Message = "Confirm password does not match" });

        var userIdClaim = User.FindFirst("sub")?.Value
            ?? User.FindFirst("nameid")?.Value
            ?? User.FindFirst("UserId")?.Value
            ?? User.FindFirst("userId")?.Value;
        if (!long.TryParse(userIdClaim, out var userId))
            return Unauthorized(new ResponseResult<object> { Success = false, Message = "Invalid user" });

        var (success, message) = await _authService.ChangePasswordAsync(
            userId,
            request.CurrentPassword,
            request.NewPassword,
            cancellationToken);

        if (!success)
            return BadRequest(new ResponseResult<object> { Success = false, Message = message });

        return Ok(new ResponseResult<object> { Success = true, Message = message });
    }
}
