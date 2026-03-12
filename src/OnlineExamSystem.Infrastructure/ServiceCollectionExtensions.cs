using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OnlineExamSystem.Infrastructure.Data;
using OnlineExamSystem.Infrastructure.Repositories;
using OnlineExamSystem.Infrastructure.Services;

namespace OnlineExamSystem.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            options.UseNpgsql(
                connectionString,
                optionsBuilder =>
                {
                    optionsBuilder.MigrationsAssembly("OnlineExamSystem.Infrastructure");
                });

            if (!string.Equals(configuration["ASPNETCORE_ENVIRONMENT"], "Production", StringComparison.OrdinalIgnoreCase))
            {
                options.EnableDetailedErrors();
                options.EnableSensitiveDataLogging();
            }
        });

        // Services
        services.AddScoped<IJwtTokenProvider, JwtTokenProvider>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IDataSeeder, DataSeeder>();

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserSessionRepository, UserSessionRepository>();
        services.AddScoped<IUserLoginLogRepository, UserLoginLogRepository>();
        services.AddScoped<IUserManagementRepository, UserManagementRepository>();

        return services;
    }
}
