using FluentAssertions;
using IdentityHub.Api.Controllers.Http.Dtos;
using IdentityHub.Api.Services;
using IdentityHub.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IdentityHub.Tests.Services;

[TestClass]
public class AuthServiceIntegrationTests : IntegrationTestBase
{
    [TestMethod]
    public async Task RegisterAsync_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();

        var registerDto = new RegisterDto
        {
            Email = TestHelpers.GenerateRandomEmail(),
            Password = TestHelpers.GenerateStrongPassword(),
            ConfirmPassword = TestHelpers.GenerateStrongPassword(),
            FirstName = "Test",
            LastName = "User"
        };

        // Act
        var result = await authService.RegisterAsync(registerDto);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("registered successfully");
    }

    [TestMethod]
    public async Task RegisterAsync_WithDuplicateEmail_ShouldReturnFailure()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();

        var email = TestHelpers.GenerateRandomEmail();
        var registerDto1 = new RegisterDto
        {
            Email = email,
            Password = TestHelpers.GenerateStrongPassword(),
            ConfirmPassword = TestHelpers.GenerateStrongPassword(),
            FirstName = "Test",
            LastName = "User"
        };

        var registerDto2 = new RegisterDto
        {
            Email = email, // Same email
            Password = TestHelpers.GenerateStrongPassword(),
            ConfirmPassword = TestHelpers.GenerateStrongPassword(),
            FirstName = "Test2",
            LastName = "User2"
        };

        // Act
        var result1 = await authService.RegisterAsync(registerDto1);
        var result2 = await authService.RegisterAsync(registerDto2);

        // Assert
        result1.Success.Should().BeTrue();
        result2.Success.Should().BeFalse();
        result2.Message.Should().Contain("already exists");
    }

    [TestMethod]
    public async Task LoginAsync_WithValidCredentials_ShouldReturnTokens()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();

        var email = TestHelpers.GenerateRandomEmail();
        var password = TestHelpers.GenerateStrongPassword();

        var registerDto = new RegisterDto
        {
            Email = email,
            Password = password,
            ConfirmPassword = password,
            FirstName = "Test",
            LastName = "User"
        };

        await authService.RegisterAsync(registerDto);

        var loginDto = new LoginDto
        {
            Email = email,
            Password = password
        };

        // Act
        var result = await authService.LoginAsync(loginDto);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [TestMethod]
    public async Task LoginAsync_WithInvalidCredentials_ShouldReturnFailure()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();

        var loginDto = new LoginDto
        {
            Email = "nonexistent@example.com",
            Password = "WrongPassword123!"
        };

        // Act
        var result = await authService.LoginAsync(loginDto);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.AccessToken.Should().BeNullOrEmpty();
        result.Message.Should().Contain("Invalid");
    }

    [TestMethod]
    public async Task RefreshTokenAsync_WithValidTokens_ShouldReturnNewTokens()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();

        var email = TestHelpers.GenerateRandomEmail();
        var password = TestHelpers.GenerateStrongPassword();

        // Register and login to get initial tokens
        var registerDto = new RegisterDto
        {
            Email = email,
            Password = password,
            ConfirmPassword = password,
            FirstName = "Test",
            LastName = "User"
        };

        await authService.RegisterAsync(registerDto);

        var loginDto = new LoginDto
        {
            Email = email,
            Password = password
        };

        var loginResult = await authService.LoginAsync(loginDto);

        var refreshTokenDto = new RefreshTokenDto
        {
            AccessToken = loginResult.AccessToken!,
            RefreshToken = loginResult.RefreshToken!
        };

        // Act
        var result = await authService.RefreshTokenAsync(refreshTokenDto);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.AccessToken.Should().NotBe(loginResult.AccessToken); // Should be a new token
    }

    [TestMethod]
    public async Task RefreshTokenAsync_WithInvalidTokens_ShouldReturnFailure()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();

        var refreshTokenDto = new RefreshTokenDto
        {
            AccessToken = "invalid-access-token",
            RefreshToken = "invalid-refresh-token"
        };

        // Act
        var result = await authService.RefreshTokenAsync(refreshTokenDto);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.AccessToken.Should().BeNullOrEmpty();
    }

    [TestCleanup]
    public async Task TestCleanup()
    {
        // Clean database after each test
        await CleanDatabaseAsync();
    }
}
