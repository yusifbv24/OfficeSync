using IdentityService.Application.Interfaces;
using IdentityService.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IdentityService.IntegrationTests
{
    public class CustomWebApplicationFactory:WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureAppConfiguration((context, config) =>
            {
                // Clear existing configuration
                config.Sources.Clear();

                // Override configuration for testing
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=TestDb;",
                    ["Jwt:SecretKey"] = "ThisIsAVerySecureTestSecretKeyForIntegrationTesting123456",
                    ["Jwt:Issuer"] = "TestIssuer",
                    ["Jwt:Audience"] = "TestAudience",
                    ["Jwt:AccessTokenExpirationMinutes"] = "60",
                    ["Jwt:RefreshTokenExpirationDays"] = "7",
                    ["Serilog:WriteTo:1:Name"] = "Console"
                });
            });

            builder.ConfigureServices(services =>
            {
                // Remove the existing DbContext registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<IdentityDbContext>));

                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add in-memory database for testing
                services.AddDbContext<IdentityDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDatabase");
                    options.EnableSensitiveDataLogging();
                });

                // Build the service provider
                var sp = services.BuildServiceProvider();

                // Create a scope to obtain a reference to the database context
                using var scope = sp.CreateScope();
                var scopedServices = scope.ServiceProvider;
                var db = scopedServices.GetRequiredService<IdentityDbContext>();
                var passwordHasher = scopedServices.GetRequiredService<IPasswordHasher>();
                var logger = scopedServices.GetRequiredService<ILogger<CustomWebApplicationFactory>>();

                // Ensure the database is created
                db.Database.EnsureCreated();

                try
                {
                    // Seed test data
                    SeedTestData(db, passwordHasher).Wait();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occurred seeding the database with test data.");
                    throw;
                }
            });

            base.ConfigureWebHost(builder);
        }

        private static async Task SeedTestData(IdentityDbContext context, IPasswordHasher passwordHasher)
        {
            // Clear existing data
            context.RefreshTokens.RemoveRange(context.RefreshTokens);
            context.Users.RemoveRange(context.Users);
            await context.SaveChangesAsync();

            // Add test users
            var testUsers = new[]
            {
            new Domain.Entities.User
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Username = "testuser",
                Email = "testuser@example.com",
                PasswordHash = passwordHasher.HashPassword("TestPassword123!"),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Domain.Entities.User
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Username = "admin",
                Email = "admin@example.com",
                PasswordHash = passwordHasher.HashPassword("AdminPassword123!"),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Domain.Entities.User
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Username = "inactiveuser",
                Email = "inactive@example.com",
                PasswordHash = passwordHasher.HashPassword("InactivePassword123!"),
                IsActive = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

            await context.Users.AddRangeAsync(testUsers);
            await context.SaveChangesAsync();
        }
    }
}