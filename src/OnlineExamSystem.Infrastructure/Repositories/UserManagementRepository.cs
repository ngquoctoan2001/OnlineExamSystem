using OnlineExamSystem.Domain.Entities;
using OnlineExamSystem.Application.DTOs;
using OnlineExamSystem.Infrastructure.Data;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace OnlineExamSystem.Infrastructure.Repositories;

public interface IUserManagementRepository
{
    /// <summary>
    /// Get user with roles
    /// </summary>
    Task<User?> GetUserWithRolesAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all users with pagination
    /// </summary>
    Task<(List<User> Users, int Total)> GetAllUsersAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update user
    /// </summary>
    Task<User?> UpdateUserAsync(long userId, string email, string fullName, bool isActive, CancellationToken cancellationToken = default);

    /// <summary>
    /// Assign role to user
    /// </summary>
    Task AssignRoleToUserAsync(long userId, long roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove role from user
    /// </summary>
    Task RemoveRoleFromUserAsync(long userId, long roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all roles
    /// </summary>
    Task<List<Role>> GetAllRolesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get role by name
    /// </summary>
    Task<Role?> GetRoleByNameAsync(string roleName, CancellationToken cancellationToken = default);
}

public class UserManagementRepository : IUserManagementRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UserManagementRepository> _logger;

    public UserManagementRepository(ApplicationDbContext context, ILogger<UserManagementRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<User?> GetUserWithRolesAsync(long userId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user with roles for user ID: {userId}", userId);
            throw;
        }
    }

    public async Task<(List<User> Users, int Total)> GetAllUsersAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .AsQueryable();

            var total = await query.CountAsync(cancellationToken);
            var users = await query
                .OrderBy(u => u.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (users, total);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all users");
            throw;
        }
    }

    public async Task<User?> UpdateUserAsync(long userId, string email, string fullName, bool isActive, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
            if (user == null)
                return null;

            user.Email = email;
            user.FullName = fullName;
            user.IsActive = isActive;
            user.UpdatedAt = DateTime.UtcNow;

            _context.Users.Update(user);
            await _context.SaveChangesAsync(cancellationToken);

            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user: {userId}", userId);
            throw;
        }
    }

    public async Task AssignRoleToUserAsync(long userId, long roleId, CancellationToken cancellationToken = default)
    {
        try
        {
            var exists = await _context.UserRoles
                .AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId, cancellationToken);

            if (!exists)
            {
                await _context.UserRoles.AddAsync(new UserRole
                {
                    UserId = userId,
                    RoleId = roleId,
                    AssignedAt = DateTime.UtcNow
                }, cancellationToken);

                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Role {roleId} assigned to user {userId}", roleId, userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning role to user: {userId}", userId);
            throw;
        }
    }

    public async Task RemoveRoleFromUserAsync(long userId, long roleId, CancellationToken cancellationToken = default)
    {
        try
        {
            var userRole = await _context.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId, cancellationToken);

            if (userRole != null)
            {
                _context.UserRoles.Remove(userRole);
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Role {roleId} removed from user {userId}", roleId, userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing role from user: {userId}", userId);
            throw;
        }
    }

    public async Task<List<Role>> GetAllRolesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Roles
                .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all roles");
            throw;
        }
    }

    public async Task<Role?> GetRoleByNameAsync(string roleName, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Roles
                .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(r => r.Name == roleName, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting role by name: {roleName}", roleName);
            throw;
        }
    }
}
