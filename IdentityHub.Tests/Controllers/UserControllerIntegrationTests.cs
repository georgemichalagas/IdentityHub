using FluentAssertions;
using IdentityHub.Api.Controllers.Http.Dtos;
using IdentityHub.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace IdentityHub.Tests.Controllers;

[TestClass]
public class UserControllerIntegrationTests : IntegrationTestBase
{
    [TestMethod]
    public async Task GetProfile_WithValidToken_ShouldReturnUserProfile()
    {
        // Arrange
        var (accessToken, email) = await CreateUserAndGetTokenAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        // Act
        var response = await Client.GetAsync("/api/user/profile");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        
        result.GetProperty("success").GetBoolean().Should().BeTrue();
        result.GetProperty("data").GetProperty("email").GetString().Should().Be(email);
        result.GetProperty("data").GetProperty("firstName").GetString().Should().Be("Test");
        result.GetProperty("data").GetProperty("lastName").GetString().Should().Be("User");
    }

    [TestMethod]
    public async Task GetProfile_WithoutToken_ShouldReturnUnauthorized()
    {
        // Act
        var response = await Client.GetAsync("/api/user/profile");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task ChangePassword_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var (accessToken, email) = await CreateUserAndGetTokenAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var changePasswordDto = new ChangePasswordDto
        {
            CurrentPassword = TestHelpers.GenerateStrongPassword(),
            NewPassword = "NewPassword123!",
            ConfirmNewPassword = "NewPassword123!"
        };

        // Act
        var response = await Client.PostAsync("/api/auth/change-password", 
            TestHelpers.CreateJsonContent(changePasswordDto));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        
        result.GetProperty("success").GetBoolean().Should().BeTrue();
        result.GetProperty("message").GetString().Should().Contain("changed successfully");
    }

    [TestMethod]
    public async Task ChangePassword_WithWrongCurrentPassword_ShouldReturnBadRequest()
    {
        // Arrange
        var (accessToken, email) = await CreateUserAndGetTokenAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var changePasswordDto = new ChangePasswordDto
        {
            CurrentPassword = "WrongPassword123!",
            NewPassword = "NewPassword123!",
            ConfirmNewPassword = "NewPassword123!"
        };

        // Act
        var response = await Client.PostAsync("/api/auth/change-password", 
            TestHelpers.CreateJsonContent(changePasswordDto));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task ChangePassword_WithMismatchedNewPasswords_ShouldReturnBadRequest()
    {
        // Arrange
        var (accessToken, email) = await CreateUserAndGetTokenAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var changePasswordDto = new ChangePasswordDto
        {
            CurrentPassword = TestHelpers.GenerateStrongPassword(),
            NewPassword = "NewPassword123!",
            ConfirmNewPassword = "DifferentPassword123!"
        };

        // Act
        var response = await Client.PostAsync("/api/auth/change-password", 
            TestHelpers.CreateJsonContent(changePasswordDto));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task ForgotPassword_WithValidEmail_ShouldReturnSuccess()
    {
        // Arrange
        var (_, email) = await CreateUserAndGetTokenAsync();

        var forgotPasswordDto = new ForgotPasswordDto
        {
            Email = email
        };

        // Act
        var response = await Client.PostAsync("/api/auth/forgot-password", 
            TestHelpers.CreateJsonContent(forgotPasswordDto));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        
        result.GetProperty("success").GetBoolean().Should().BeTrue();
        result.GetProperty("message").GetString().Should().Contain("password reset link has been sent");
    }

    [TestMethod]
    public async Task ForgotPassword_WithNonExistentEmail_ShouldReturnSuccess()
    {
        // Arrange - Even for non-existent emails, we should return success for security reasons
        var forgotPasswordDto = new ForgotPasswordDto
        {
            Email = "nonexistent@example.com"
        };

        // Act
        var response = await Client.PostAsync("/api/auth/forgot-password", 
            TestHelpers.CreateJsonContent(forgotPasswordDto));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        
        result.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    private async Task<(string AccessToken, string Email)> CreateUserAndGetTokenAsync()
    {
        var email = TestHelpers.GenerateRandomEmail();
        var password = TestHelpers.GenerateStrongPassword();

        // Register user
        var registerDto = new RegisterDto
        {
            Email = email,
            Password = password,
            ConfirmPassword = password,
            FirstName = "Test",
            LastName = "User"
        };

        await Client.PostAsync("/api/auth/register", TestHelpers.CreateJsonContent(registerDto));

        // Login to get token
        var loginDto = new LoginDto
        {
            Email = email,
            Password = password
        };

        var loginResponse = await Client.PostAsync("/api/auth/login", 
            TestHelpers.CreateJsonContent(loginDto));
        var loginContent = await loginResponse.Content.ReadAsStringAsync();
        var loginResult = JsonSerializer.Deserialize<JsonElement>(loginContent);

        var accessToken = loginResult.GetProperty("accessToken").GetString()!;

        return (accessToken, email);
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
