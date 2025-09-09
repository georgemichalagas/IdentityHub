using Microsoft.Extensions.DependencyInjection;
using IdentityHub.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IdentityHub.Tests.Infrastructure;

public abstract class IntegrationTestBase
{
    private static IntegrationTestWebApplicationFactory<Program>? _factory;
    private static readonly object _lock = new object();
    
    protected static IntegrationTestWebApplicationFactory<Program> Factory
    {
        get
        {
            if (_factory == null)
            {
                lock (_lock)
                {
                    if (_factory == null)
                    {
                        _factory = new IntegrationTestWebApplicationFactory<Program>();
                        // Start the container synchronously
                        TestContainerManager.EnsureContainerStartedAsync().Wait();
                    }
                }
            }
            return _factory;
        }
    }
    
    protected HttpClient Client { get; private set; } = null!;

    [TestInitialize]
    public virtual async Task InitializeAsync()
    {
        Client = Factory.CreateClient();
        await CleanDatabaseAsync();
    }

    [TestCleanup]
    public virtual void TestCleanup()
    {
        Client?.Dispose();
    }

    protected ApplicationDbContext GetDbContext()
    {
        var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return context;
    }

    protected async Task SeedDatabaseAsync(Func<ApplicationDbContext, Task> seedAction)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await seedAction(context);
    }

    protected async Task CleanDatabaseAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        // Remove all data but keep the schema and roles
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"RefreshTokens\" CASCADE");
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"AspNetUserRoles\" CASCADE");
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"AspNetUsers\" CASCADE");
        // Don't truncate AspNetRoles - keep the seeded roles
    }
}
