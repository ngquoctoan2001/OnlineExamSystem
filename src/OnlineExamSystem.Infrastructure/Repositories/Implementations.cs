using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OnlineExamSystem.Domain.Entities;
using OnlineExamSystem.Infrastructure.Data;

namespace OnlineExamSystem.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UserRepository> _logger;

    public UserRepository(ApplicationDbContext context, ILogger<UserRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<User?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting user by id: {id}");
            return null;
        }
    }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Username == username, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting user by username: {username}");
            return null;
        }
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting user by email: {email}");
            return null;
        }
    }

    public async Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all users");
            return new List<User>();
        }
    }

    public async Task<User> CreateAsync(User user, CancellationToken cancellationToken = default)
    {
        try
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync(cancellationToken);
            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error creating user: {user.Username}");
            throw;
        }
    }

    public async Task<User> UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        try
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync(cancellationToken);
            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating user: {user.Id}");
            throw;
        }
    }

    public async Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await GetByIdAsync(id, cancellationToken);
            if (user == null) return false;

            _context.Users.Remove(user);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting user: {id}");
            return false;
        }
    }

    public async Task<bool> DeleteAsync(User user, CancellationToken cancellationToken = default)
    {
        try
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting user: {user.Id}");
            return false;
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
            _logger.LogError(ex, $"Error getting role by name: {roleName}");
            return null;
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
                _logger.LogInformation($"Role {roleId} assigned to user {userId}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error assigning role to user: {userId}");
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
                _logger.LogInformation($"Role {roleId} removed from user {userId}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error removing role from user: {userId}");
            throw;
        }
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
            _logger.LogError(ex, $"Error getting user with roles for user ID: {userId}");
            return null;
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
            return (new List<User>(), 0);
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
            _logger.LogError(ex, $"Error updating user: {userId}");
            return null;
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
            return new List<Role>();
        }
    }
}

public class UserSessionRepository : IUserSessionRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UserSessionRepository> _logger;

    public UserSessionRepository(ApplicationDbContext context, ILogger<UserSessionRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<UserSession?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.UserSessions
                .FirstOrDefaultAsync(us => us.Id == id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting session by id: {id}");
            return null;
        }
    }

    public async Task<UserSession?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        try
        {
            var session = await _context.UserSessions
                .FirstOrDefaultAsync(us => us.RefreshToken == refreshToken, cancellationToken);

            if (session?.ExpiresAt < DateTime.UtcNow)
                return null;

            return session;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting session by refresh token");
            return null;
        }
    }

    public async Task<IEnumerable<UserSession>> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.UserSessions
                .Where(us => us.UserId == userId)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting sessions for user: {userId}");
            return new List<UserSession>();
        }
    }

    public async Task<UserSession> CreateAsync(UserSession session, CancellationToken cancellationToken = default)
    {
        try
        {
            _context.UserSessions.Add(session);
            await _context.SaveChangesAsync(cancellationToken);
            return session;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error creating session for user: {session.UserId}");
            throw;
        }
    }

    public async Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        try
        {
            var session = await GetByIdAsync(id, cancellationToken);
            if (session == null) return false;

            _context.UserSessions.Remove(session);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting session: {id}");
            return false;
        }
    }

    public async Task<bool> DeleteByUserIdAsync(long userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var sessions = await GetByUserIdAsync(userId, cancellationToken);
            _context.UserSessions.RemoveRange(sessions);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting sessions for user: {userId}");
            return false;
        }
    }
}

public class UserLoginLogRepository : IUserLoginLogRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UserLoginLogRepository> _logger;

    public UserLoginLogRepository(ApplicationDbContext context, ILogger<UserLoginLogRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<UserLoginLog> CreateAsync(UserLoginLog log, CancellationToken cancellationToken = default)
    {
        try
        {
            _context.UserLoginLogs.Add(log);
            await _context.SaveChangesAsync(cancellationToken);
            return log;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error creating login log for user: {log.UserId}");
            throw;
        }
    }

    public async Task<IEnumerable<UserLoginLog>> GetByUserIdAsync(long userId, int limit = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.UserLoginLogs
                .Where(ull => ull.UserId == userId)
                .OrderByDescending(ull => ull.LoginTime)
                .Take(limit)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting login logs for user: {userId}");
            return new List<UserLoginLog>();
        }
    }
}
