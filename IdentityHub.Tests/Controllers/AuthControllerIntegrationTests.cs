using FluentAssertions;
using IdentityHub.Api.Controllers.Http.Dtos;
using IdentityHub.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace IdentityHub.Tests.Controllers;

[TestClass]
public class AuthControllerIntegrationTests : IntegrationTestBase
{
    [TestMethod]
    public async Task Register_WithValidData_ShouldReturnSuccess()
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
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        
        result.GetProperty("success").GetBoolean().Should().BeTrue();
        result.GetProperty("message").GetString().Should().Contain("registered successfully");
    }

    [TestMethod]
    public async Task Register_WithInvalidEmail_ShouldReturnBadRequest()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Email = "invalid-email",
            Password = TestHelpers.GenerateStrongPassword(),
            ConfirmPassword = TestHelpers.GenerateStrongPassword(),
            FirstName = "Test",
            LastName = "User"
        };

        // Act
        var response = await Client.PostAsync("/api/auth/register", 
            TestHelpers.CreateJsonContent(registerDto));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task Register_WithMismatchedPasswords_ShouldReturnBadRequest()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Email = TestHelpers.GenerateRandomEmail(),
            Password = TestHelpers.GenerateStrongPassword(),
            ConfirmPassword = "DifferentPassword123!",
            FirstName = "Test",
            LastName = "User"
        };

        // Act
        var response = await Client.PostAsync("/api/auth/register", 
            TestHelpers.CreateJsonContent(registerDto));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task Login_WithValidCredentials_ShouldReturnTokens()
    {
        // Arrange
        var email = TestHelpers.GenerateRandomEmail();
        var password = TestHelpers.GenerateStrongPassword();

        // First register a user
        var registerDto = new RegisterDto
        {
            Email = email,
            Password = password,
            ConfirmPassword = password,
            FirstName = "Test",
            LastName = "User"
        };

        await Client.PostAsync("/api/auth/register", TestHelpers.CreateJsonContent(registerDto));

        var loginDto = new LoginDto
        {
            Email = email,
            Password = password
        };

        // Act
        var response = await Client.PostAsync("/api/auth/login", 
            TestHelpers.CreateJsonContent(loginDto));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        
        result.GetProperty("success").GetBoolean().Should().BeTrue();
        result.GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();
        result.GetProperty("refreshToken").GetString().Should().NotBeNullOrEmpty();
    }

    [TestMethod]
    public async Task Login_WithInvalidCredentials_ShouldReturnUnauthorized()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = "nonexistent@example.com",
            Password = "WrongPassword123!"
        };

        // Act
        var response = await Client.PostAsync("/api/auth/login", 
            TestHelpers.CreateJsonContent(loginDto));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task RefreshToken_WithValidTokens_ShouldReturnNewTokens()
    {
        // Arrange
        var email = TestHelpers.GenerateRandomEmail();
        var password = TestHelpers.GenerateStrongPassword();

        // Register and login to get tokens
        var registerDto = new RegisterDto
        {
            Email = email,
            Password = password,
            ConfirmPassword = password,
            FirstName = "Test",
            LastName = "User"
        };

        await Client.PostAsync("/api/auth/register", TestHelpers.CreateJsonContent(registerDto));

        var loginDto = new LoginDto
        {
            Email = email,
            Password = password
        };

        var loginResponse = await Client.PostAsync("/api/auth/login", 
            TestHelpers.CreateJsonContent(loginDto));
        var loginContent = await loginResponse.Content.ReadAsStringAsync();
        var loginResult = JsonSerializer.Deserialize<JsonElement>(loginContent);

        var accessToken = loginResult.GetProperty("accessToken").GetString();
        var refreshToken = loginResult.GetProperty("refreshToken").GetString();

        var refreshTokenDto = new RefreshTokenDto
        {
            AccessToken = accessToken!,
            RefreshToken = refreshToken!
        };

        // Act
        var response = await Client.PostAsync("/api/auth/refresh-token", 
            TestHelpers.CreateJsonContent(refreshTokenDto));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        
        result.GetProperty("success").GetBoolean().Should().BeTrue();
        result.GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();
        result.GetProperty("refreshToken").GetString().Should().NotBeNullOrEmpty();
    }

    [TestMethod]
    public async Task Logout_WithValidToken_ShouldReturnSuccess()
    {
        // Arrange
        var email = TestHelpers.GenerateRandomEmail();
        var password = TestHelpers.GenerateStrongPassword();

        // Register and login to get token
        var registerDto = new RegisterDto
        {
            Email = email,
            Password = password,
            ConfirmPassword = password,
            FirstName = "Test",
            LastName = "User"
        };

        await Client.PostAsync("/api/auth/register", TestHelpers.CreateJsonContent(registerDto));

        var loginDto = new LoginDto
        {
            Email = email,
            Password = password
        };

        var loginResponse = await Client.PostAsync("/api/auth/login", 
            TestHelpers.CreateJsonContent(loginDto));
        var loginContent = await loginResponse.Content.ReadAsStringAsync();
        var loginResult = JsonSerializer.Deserialize<JsonElement>(loginContent);

        var accessToken = loginResult.GetProperty("accessToken").GetString();

        // Set authorization header
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        // Act
        var response = await Client.PostAsync("/api/auth/logout", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        
        result.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [TestCleanup]
    public async Task TestCleanup()
    {
        // Clear authorization header
        Client.DefaultRequestHeaders.Authorization = null;
        
        // Clean database after each test
        await CleanDatabaseAsync();
    }
}
