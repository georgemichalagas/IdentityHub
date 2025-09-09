using Testcontainers.PostgreSql;

namespace IdentityHub.Tests.Infrastructure;

public static class TestContainerManager
{
    private static readonly PostgreSqlContainer _sharedPostgresContainer = new PostgreSqlBuilder()
        .WithImage("postgres:15")
        .WithDatabase("identityhub_test")
        .WithUsername("test")
        .WithPassword("test123")
        .WithCleanUp(true)
        .Build();

    private static bool _containerStarted = false;
    private static readonly object _lock = new object();

    public static string ConnectionString => _sharedPostgresContainer.GetConnectionString();

    public static Task EnsureContainerStartedAsync()
    {
        lock (_lock)
        {
            if (!_containerStarted)
            {
                _sharedPostgresContainer.StartAsync().Wait();
                _containerStarted = true;
            }
        }
        return Task.CompletedTask;
    }

    public static Task StopContainerAsync()
    {
        lock (_lock)
        {
            if (_containerStarted)
            {
                _sharedPostgresContainer.StopAsync().Wait();
                _containerStarted = false;
            }
        }
        return Task.CompletedTask;
    }
}
