using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineExamSystem.Application.DTOs;
using OnlineExamSystem.Application.DTOs.Common;
using OnlineExamSystem.Infrastructure.Repositories;
using OnlineExamSystem.Infrastructure.Services;
using OnlineExamSystem.Domain.Entities;

namespace OnlineExamSystem.API.Controllers;

/// <summary>
/// User management endpoints (Admin only)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
[Tags("Users")]
public class UsersController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuthService _authService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IAuthService authService,
        ILogger<UsersController> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Get all users with pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ResponseResult<List<UserDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResponseResult<List<UserDto>>>> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1 || pageSize < 1)
            return BadRequest(new ResponseResult<List<UserDto>>
            {
                Success = false,
                Message = "Page number and page size must be greater than 0"
            });

        var (users, total) = await _userRepository.GetAllUsersAsync(pageNumber, pageSize, cancellationToken);

        var userDtos = users.Select(u => new UserDto
        {
            Id = u.Id,
            Username = u.Username,
            Email = u.Email,
            FullName = u.FullName,
            IsActive = u.IsActive,
            Roles = u.UserRoles?.Select(ur => ur.Role?.Name ?? "").ToList() ?? new(),
            CreatedAt = u.CreatedAt,
            UpdatedAt = u.UpdatedAt
        }).ToList();

        return Ok(new ResponseResult<List<UserDto>>
        {
            Success = true,
            Data = userDtos,
            Message = $"Retrieved {userDtos.Count} users out of {total} total"
        });
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ResponseResult<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseResult<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResponseResult<UserDto>>> GetById(
        long id,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetUserWithRolesAsync(id, cancellationToken);
        if (user == null)
            return NotFound(new ResponseResult<object>
            {
                Success = false,
                Message = "User not found"
            });

        var userDto = new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FullName = user.FullName,
            IsActive = user.IsActive,
            Roles = user.UserRoles?.Select(ur => ur.Role?.Name ?? "").ToList() ?? new(),
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };

        return Ok(new ResponseResult<UserDto>
        {
            Success = true,
            Data = userDto
        });
    }

    /// <summary>
    /// Create new user (Admin only)
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ResponseResult<UserDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ResponseResult<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResponseResult<UserDto>>> Create(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ResponseResult<object>
            {
                Success = false,
                Message = "Invalid input"
            });

        // Check if user already exists
        var existingUser = await _userRepository.GetByUsernameAsync(request.Username, cancellationToken);
        if (existingUser != null)
            return BadRequest(new ResponseResult<object>
            {
                Success = false,
                Message = "Username already exists"
            });

        try
        {
            var passwordHash = _passwordHasher.HashPassword(request.Password);
            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                FullName = request.FullName,
                PasswordHash = passwordHash,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _userRepository.CreateAsync(user, cancellationToken);

            // Assign roles
            if (request.Roles.Count > 0)
            {
                foreach (var roleName in request.Roles)
                {
                    var role = await _userRepository.GetRoleByNameAsync(roleName, cancellationToken);
                    if (role != null)
                    {
                        await _userRepository.AssignRoleToUserAsync(user.Id, role.Id, cancellationToken);
                    }
                }
            }

            var userDto = new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                IsActive = user.IsActive,
                Roles = request.Roles,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };

            return StatusCode(StatusCodes.Status201Created, new ResponseResult<UserDto>
            {
                Success = true,
                Message = "User created successfully",
                Data = userDto
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            return BadRequest(new ResponseResult<object>
            {
                Success = false,
                Message = "Error creating user"
            });
        }
    }

    /// <summary>
    /// Update user
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ResponseResult<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseResult<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResponseResult<UserDto>>> Update(
        long id,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.UpdateUserAsync(
            id,
            request.Email,
            request.FullName,
            request.IsActive,
            cancellationToken);

        if (user == null)
            return NotFound(new ResponseResult<object>
            {
                Success = false,
                Message = "User not found"
            });

        // Update roles if provided
        if (request.Roles.Count > 0)
        {
            var currentRoles = (await _userRepository.GetUserWithRolesAsync(id, cancellationToken))?.UserRoles ?? new List<UserRole>();

            // Remove roles not in request
            foreach (var userRole in currentRoles)
            {
                if (!request.Roles.Contains(userRole.Role?.Name ?? ""))
                {
                    await _userRepository.RemoveRoleFromUserAsync(id, userRole.RoleId, cancellationToken);
                }
            }

            // Add new roles
            foreach (var roleName in request.Roles)
            {
                var role = await _userRepository.GetRoleByNameAsync(roleName, cancellationToken);
                if (role != null && !currentRoles.Any(ur => ur.RoleId == role.Id))
                {
                    await _userRepository.AssignRoleToUserAsync(id, role.Id, cancellationToken);
                }
            }
        }

        var updatedUser = await _userRepository.GetUserWithRolesAsync(id, cancellationToken);
        var userDto = new UserDto
        {
            Id = updatedUser!.Id,
            Username = updatedUser.Username,
            Email = updatedUser.Email,
            FullName = updatedUser.FullName,
            IsActive = updatedUser.IsActive,
            Roles = updatedUser.UserRoles?.Select(ur => ur.Role?.Name ?? "").ToList() ?? new(),
            CreatedAt = updatedUser.CreatedAt,
            UpdatedAt = updatedUser.UpdatedAt
        };

        return Ok(new ResponseResult<UserDto>
        {
            Success = true,
            Message = "User updated successfully",
            Data = userDto
        });
    }

    /// <summary>
    /// Delete user
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ResponseResult<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseResult<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResponseResult<object>>> Delete(
        long id,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        if (user == null)
            return NotFound(new ResponseResult<object>
            {
                Success = false,
                Message = "User not found"
            });

        await _userRepository.DeleteAsync(user, cancellationToken);

        return Ok(new ResponseResult<object>
        {
            Success = true,
            Message = "User deleted successfully"
        });
    }

    /// <summary>
    /// Reset password for user (Admin only)
    /// </summary>
    [HttpPost("{id}/reset-password")]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(ResponseResult<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseResult<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseResult<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResponseResult<object>>> ResetPassword(
        long id,
        [FromBody] OnlineExamSystem.Application.DTOs.Auth.ResetPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ResponseResult<object> { Success = false, Message = "Invalid input" });

        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        if (user == null)
            return NotFound(new ResponseResult<object> { Success = false, Message = "User not found" });

        var (success, message) = await _authService.ResetPasswordAsync(id, request.NewPassword, cancellationToken);
        if (!success)
            return BadRequest(new ResponseResult<object> { Success = false, Message = message });

        return Ok(new ResponseResult<object> { Success = true, Message = message });
    }

    /// <summary>
    /// Toggle active status for user (Admin only)
    /// </summary>
    [HttpPatch("{id}/active")]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(ResponseResult<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseResult<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResponseResult<UserDto>>> ToggleActive(
        long id,
        [FromQuery] bool isActive,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        if (user == null)
            return NotFound(new ResponseResult<object> { Success = false, Message = "User not found" });

        user.IsActive = isActive;
        user.UpdatedAt = DateTime.UtcNow;
        var updated = await _userRepository.UpdateAsync(user, cancellationToken);

        var userWithRoles = await _userRepository.GetUserWithRolesAsync(id, cancellationToken);
        var dto = new UserDto
        {
            Id = updated.Id,
            Username = updated.Username,
            Email = updated.Email,
            FullName = updated.FullName,
            IsActive = updated.IsActive,
            Roles = userWithRoles?.UserRoles?.Select(ur => ur.Role?.Name ?? string.Empty).ToList() ?? new(),
            CreatedAt = updated.CreatedAt,
            UpdatedAt = updated.UpdatedAt
        };

        return Ok(new ResponseResult<UserDto>
        {
            Success = true,
            Message = isActive ? "User activated" : "User deactivated",
            Data = dto
        });
    }

    /// <summary>
    /// Get all roles
    /// </summary>
    [HttpGet("roles/list")]
    [ProducesResponseType(typeof(ResponseResult<List<RoleDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResponseResult<List<RoleDto>>>> GetRoles(
        CancellationToken cancellationToken = default)
    {
        var roles = await _userRepository.GetAllRolesAsync(cancellationToken);

        var roleDtos = roles.Select(r => new RoleDto
        {
            Id = r.Id,
            Name = r.Name,
            Description = r.Description,
            Permissions = r.RolePermissions?.Select(rp => new PermissionDto
            {
                Id = rp.Permission?.Id ?? 0,
                Name = rp.Permission?.Name ?? "",
                Description = rp.Permission?.Description ?? ""
            }).ToList() ?? new()
        }).ToList();

        return Ok(new ResponseResult<List<RoleDto>>
        {
            Success = true,
            Data = roleDtos
        });
    }
}
