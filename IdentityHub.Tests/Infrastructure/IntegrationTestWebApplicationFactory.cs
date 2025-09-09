using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using IdentityHub.Api.Data;

namespace IdentityHub.Tests.Infrastructure;

public class IntegrationTestWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram>
    where TProgram : class
{
    public string ConnectionString => TestContainerManager.ConnectionString;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext registration
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add DbContext with test container connection string
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseNpgsql(ConnectionString);
            });

            // Build the service provider
            var serviceProvider = services.BuildServiceProvider();

            // Create a scope to obtain the database context
            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<IntegrationTestWebApplicationFactory<TProgram>>>();

            try
            {
                // Ensure the database is created
                db.Database.EnsureCreated();
                
                // Seed roles
                var roles = new[] { "Admin", "User" };
                foreach (var role in roles)
                {
                    if (!roleManager.RoleExistsAsync(role).Result)
                    {
                        roleManager.CreateAsync(new IdentityRole(role)).Wait();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred creating the test database or seeding roles.");
                throw;
            }
        });

        builder.UseEnvironment("Testing");
    }

    public async Task InitializeAsync()
    {
        await TestContainerManager.EnsureContainerStartedAsync();
    }

    public new async Task DisposeAsync()
    {
        // Don't stop the shared container here - it will be cleaned up when the process exits
        await base.DisposeAsync();
    }
}
