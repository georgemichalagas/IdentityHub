# IdentityHub.Tests

This project contains integration tests for the IdentityHub API using MSTest, Testcontainers, and PostgreSQL.

## Features

- **Integration Tests**: Full end-to-end testing with real database
- **Testcontainers**: Uses Docker containers for PostgreSQL database isolation
- **MSTest Framework**: Microsoft's testing framework with async support
- **FluentAssertions**: Readable and expressive test assertions
- **HTTP & gRPC Testing**: Tests both REST API and gRPC endpoints

## Test Structure

```
IdentityHub.Tests/
├── Controllers/           # HTTP API endpoint tests
│   ├── AuthControllerIntegrationTests.cs
│   └── UserControllerIntegrationTests.cs
├── Data/                  # Database and repository tests
│   └── ApplicationDbContextIntegrationTests.cs
├── Infrastructure/        # Test infrastructure and utilities
│   ├── IntegrationTestBase.cs
│   ├── IntegrationTestWebApplicationFactory.cs
│   └── TestHelpers.cs
└── Services/              # Service layer tests
    └── AuthServiceIntegrationTests.cs
```

## Running Tests

### Prerequisites

- Docker Desktop installed and running
- .NET 8 SDK

### Command Line

```bash
# Run all tests
dotnet test

# Run with verbose output
dotnet test --verbosity normal

# Run specific test class
dotnet test --filter "AuthControllerIntegrationTests"

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Visual Studio

1. Open Test Explorer (Test → Test Explorer)
2. Build the solution
3. Run tests from Test Explorer

## Test Features

### Authentication Tests
- User registration with validation
- User login with JWT token generation
- Token refresh functionality
- Logout and token revocation

### User Management Tests
- User profile retrieval
- Password change functionality
- Password reset workflow
- User data validation

### Database Tests
- Entity persistence and retrieval
- Database constraints and relationships
- Transaction handling
- Connection pooling

### Infrastructure Tests
- Application startup and configuration
- Dependency injection container
- Middleware pipeline
- Error handling

## Test Configuration

Tests use a dedicated test environment with:
- Isolated PostgreSQL container per test run
- In-memory configurations where appropriate
- Test-specific connection strings
- Clean database state between tests

## Best Practices

1. **Test Isolation**: Each test class runs with a fresh database container
2. **Data Cleanup**: Database is cleaned between individual tests
3. **Realistic Data**: Tests use generated realistic test data
4. **Async Operations**: All tests properly handle async/await patterns
5. **Assertions**: Uses FluentAssertions for readable test assertions

## Adding New Tests

1. Inherit from `IntegrationTestBase` for integration tests
2. Use `[TestClass]` and `[TestMethod]` attributes
3. Call `await CleanDatabaseAsync()` in test cleanup if needed
4. Use `TestHelpers` for generating test data
5. Follow the Arrange-Act-Assert pattern

## Example Test

```csharp
[TestClass]
public class ExampleIntegrationTests : IntegrationTestBase
{
    [TestMethod]
    public async Task CreateUser_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Email = TestHelpers.GenerateRandomEmail(),
            Password = TestHelpers.GenerateStrongPassword(),
            ConfirmPassword = TestHelpers.GenerateStrongPassword(),
            FirstName = "Test",
            LastName = "User"
        };

        // Act
        var response = await Client.PostAsync("/api/auth/register", 
            TestHelpers.CreateJsonContent(registerDto));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [TestCleanup]
    public async Task TestCleanup()
    {
        await CleanDatabaseAsync();
    }
}
```

## Troubleshooting

### Docker Issues
- Ensure Docker Desktop is running
- Check if PostgreSQL image can be pulled
- Verify port availability (PostgreSQL uses random ports)

### Test Failures
- Check test output for database connection errors
- Verify all required packages are installed
- Ensure no port conflicts with other services

### Performance
- Tests may be slower on first run (Docker image download)
- Consider parallel test execution settings
- Monitor container resource usage
