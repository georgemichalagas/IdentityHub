using FluentAssertions;
using Grpc.Net.Client;
using IdentityHub.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IdentityHub.Api.Controllers.Http.Dtos;
using Grpc.Core;
using IdentityHub.Api.Controllers.Grpc.Protos;

namespace IdentityHub.Tests.Grpc;

[TestClass]
public class GrpcUserServiceIntegrationTests : IntegrationTestBase
{
    private GrpcChannel? _channel;
    private UserService.UserServiceClient? _client;

    [TestInitialize]
    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        
        _channel = GrpcChannel.ForAddress(Client.BaseAddress!, new GrpcChannelOptions
        {
            HttpClient = Client
        });
        _client = new UserService.UserServiceClient(_channel);
    }

    [TestCleanup]
    public override void TestCleanup()
    {
        _channel?.Dispose();
        base.TestCleanup();
    }

    [TestMethod]
    public async Task GetUserByEmail_WithValidEmail_ShouldReturnUser()
    {
        // Arrange
        var (accessToken, email) = await CreateUserAndGetTokenAsync();

        var request = new GetUserByEmailRequest
        {
            Email = email
        };

        var headers = new Metadata
        {
            { "Authorization", $"Bearer {accessToken}" }
        };

        // Act
        var response = await _client!.GetUserByEmailAsync(request, headers);

        // Assert
        response.Should().NotBeNull();
        response.Success.Should().BeTrue();
        response.User.Email.Should().Be(email);
        response.User.FirstName.Should().Be("Test");
        response.User.LastName.Should().Be("User");
    }

    [TestMethod]
    public async Task GetUserByEmail_WithoutToken_ShouldThrowUnauthenticated()
    {
        // Arrange
        var request = new GetUserByEmailRequest
        {
            Email = "test@example.com"
        };

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<RpcException>(
            () => _client!.GetUserByEmailAsync(request).ResponseAsync);

        exception.StatusCode.Should().Be(StatusCode.Unauthenticated);
    }

    [TestMethod]
    public async Task UserExists_WithExistingEmail_ShouldReturnTrue()
    {
        // Arrange
        var (accessToken, email) = await CreateUserAndGetTokenAsync();

        var request = new UserExistsRequest
        {
            Email = email
        };

        var headers = new Metadata
        {
            { "Authorization", $"Bearer {accessToken}" }
        };

        // Act
        var response = await _client!.UserExistsAsync(request, headers);

        // Assert
        response.Should().NotBeNull();
        response.Exists.Should().BeTrue();
    }

    [TestMethod]
    public async Task UserExists_WithNonExistentEmail_ShouldReturnFalse()
    {
        // Arrange
        var (accessToken, _) = await CreateUserAndGetTokenAsync();

        var request = new UserExistsRequest
        {
            Email = "nonexistent@example.com"
        };

        var headers = new Metadata
        {
            { "Authorization", $"Bearer {accessToken}" }
        };

        // Act
        var response = await _client!.UserExistsAsync(request, headers);

        // Assert
        response.Should().NotBeNull();
        response.Exists.Should().BeFalse();
    }

    [TestMethod]
    public async Task GetUsers_WithValidRequest_ShouldReturnUserList()
    {
        // Arrange
        var (accessToken, _) = await CreateUserAndGetTokenAsync();

        var request = new GetUsersRequest
        {
            Page = 1,
            PageSize = 10,
            SearchTerm = "",
            IncludeInactive = true
        };

        var headers = new Metadata
        {
            { "Authorization", $"Bearer {accessToken}" }
        };

        // Act
        var response = await _client!.GetUsersAsync(request, headers);

        // Assert
        response.Should().NotBeNull();
        response.Success.Should().BeTrue();
        response.Users.Should().HaveCountGreaterThan(0);
        response.TotalCount.Should().BeGreaterThan(0);
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
        var loginResult = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(loginContent);

        var accessToken = loginResult.GetProperty("accessToken").GetString()!;

        return (accessToken, email);
    }
}
