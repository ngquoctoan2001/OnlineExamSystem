using Microsoft.Extensions.Logging;
using OnlineExamSystem.Domain.Entities;
using OnlineExamSystem.Infrastructure.Repositories;

namespace OnlineExamSystem.Infrastructure.Services;

public interface IAuthService
{
    Task<(bool Success, string Message, LoginResponseDto?)> LoginAsync(string username, string password, string ipAddress, string userAgent, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> RegisterAsync(string username, string email, string password, string fullName, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message, LoginResponseDto?)> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<bool> LogoutAsync(long userId, CancellationToken cancellationToken = default);
    Task<bool> ValidateCredentialsAsync(string username, string password, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> ChangePasswordAsync(long userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> ResetPasswordAsync(long targetUserId, string newPassword, CancellationToken cancellationToken = default);
}

public class LoginResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IUserSessionRepository _sessionRepository;
    private readonly IUserLoginLogRepository _loginLogRepository;
    private readonly IJwtTokenProvider _tokenProvider;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepository,
        IUserSessionRepository sessionRepository,
        IUserLoginLogRepository loginLogRepository,
        IJwtTokenProvider tokenProvider,
        IPasswordHasher passwordHasher,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _sessionRepository = sessionRepository;
        _loginLogRepository = loginLogRepository;
        _tokenProvider = tokenProvider;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<(bool Success, string Message, LoginResponseDto?)> LoginAsync(
        string username, string password, string ipAddress, string userAgent,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetByUsernameAsync(username, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning($"Login attempt with non-existent username: {username}");
                return (false, "Invalid username or password", null);
            }

            if (!user.IsActive)
            {
                _logger.LogWarning($"Login attempt for inactive user: {username}");
                return (false, "User account is inactive", null);
            }

            if (!_passwordHasher.VerifyPassword(password, user.PasswordHash))
            {
                _logger.LogWarning($"Failed login attempt for user: {username}");
                return (false, "Invalid username or password", null);
            }

            var accessToken = _tokenProvider.GenerateAccessToken(user);
            var refreshToken = _tokenProvider.GenerateRefreshToken();

            var session = new UserSession
            {
                UserId = user.Id,
                RefreshToken = refreshToken,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };

            await _sessionRepository.CreateAsync(session, cancellationToken);

            await _loginLogRepository.CreateAsync(
                new UserLoginLog
                {
                    UserId = user.Id,
                    LoginTime = DateTime.UtcNow,
                    IpAddress = ipAddress,
                    DeviceInfo = userAgent
                },
                cancellationToken);

            _logger.LogInformation($"User {username} logged in");

            return (true, "Login successful", new LoginResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error during login for user: {username}");
            return (false, "An error occurred during login", null);
        }
    }

    public async Task<(bool Success, string Message)> RegisterAsync(
        string username, string email, string password, string fullName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var existingUser = await _userRepository.GetByUsernameAsync(username, cancellationToken);
            if (existingUser != null)
                return (false, "Username already exists");

            var existingEmail = await _userRepository.GetByEmailAsync(email, cancellationToken);
            if (existingEmail != null)
                return (false, "Email already exists");

            var passwordHash = _passwordHasher.HashPassword(password);

            var newUser = new User
            {
                Username = username,
                Email = email,
                PasswordHash = passwordHash,
                FullName = fullName,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _userRepository.CreateAsync(newUser, cancellationToken);
            _logger.LogInformation($"New user registered: {username}");

            return (true, "Registration successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error during registration for username: {username}");
            return (false, "An error occurred during registration");
        }
    }

    public async Task<(bool Success, string Message, LoginResponseDto?)> RefreshTokenAsync(
        string refreshToken, CancellationToken cancellationToken = default)
    {
        try
        {
            var session = await _sessionRepository.GetByRefreshTokenAsync(refreshToken, cancellationToken);
            if (session == null)
                return (false, "Invalid refresh token", null);

            if (session.ExpiresAt < DateTime.UtcNow)
                return (false, "Refresh token has expired", null);

            var user = await _userRepository.GetByIdAsync(session.UserId, cancellationToken);
            if (user == null || !user.IsActive)
                return (false, "User not found or inactive", null);

            var newAccessToken = _tokenProvider.GenerateAccessToken(user);
            var newRefreshToken = _tokenProvider.GenerateRefreshToken();

            session.RefreshToken = newRefreshToken;
            session.ExpiresAt = DateTime.UtcNow.AddDays(7);

            await _sessionRepository.CreateAsync(session, cancellationToken);
            _logger.LogInformation($"Token refreshed for user: {user.Username}");

            return (true, "Token refreshed successfully", new LoginResponseDto
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return (false, "An error occurred during token refresh", null);
        }
    }

    public async Task<bool> LogoutAsync(long userId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _sessionRepository.DeleteByUserIdAsync(userId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error logging out user: {userId}");
            return false;
        }
    }

    public async Task<bool> ValidateCredentialsAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetByUsernameAsync(username, cancellationToken);
            if (user == null || !user.IsActive)
                return false;

            return _passwordHasher.VerifyPassword(password, user.PasswordHash);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error validating credentials for user: {username}");
            return false;
        }
    }

    public async Task<(bool Success, string Message)> ChangePasswordAsync(long userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
                return (false, "User not found");

            if (!_passwordHasher.VerifyPassword(currentPassword, user.PasswordHash))
                return (false, "Current password is incorrect");

            if (newPassword.Length < 6)
                return (false, "New password must be at least 6 characters");

            user.PasswordHash = _passwordHasher.HashPassword(newPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user, cancellationToken);

            // Invalidate all existing sessions so re-login is required
            await _sessionRepository.DeleteByUserIdAsync(userId, cancellationToken);

            _logger.LogInformation("Password changed for user: {UserId}", userId);
            return (true, "Password changed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user: {UserId}", userId);
            return (false, "An error occurred while changing password");
        }
    }

    public async Task<(bool Success, string Message)> ResetPasswordAsync(long targetUserId, string newPassword, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(targetUserId, cancellationToken);
            if (user == null)
                return (false, "User not found");

            if (newPassword.Length < 6)
                return (false, "New password must be at least 6 characters");

            user.PasswordHash = _passwordHasher.HashPassword(newPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user, cancellationToken);

            // Invalidate sessions of the target user
            await _sessionRepository.DeleteByUserIdAsync(targetUserId, cancellationToken);

            _logger.LogInformation("Password reset by admin for user: {UserId}", targetUserId);
            return (true, "Password reset successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password for user: {UserId}", targetUserId);
            return (false, "An error occurred while resetting password");
        }
    }
}
