using FluentAssertions;
using Grpc.Net.Client;
using IdentityHub.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.Json;
using IdentityHub.Api.DTOs;
using IdentityHub.Api.Controllers.Grpc.Protos;

namespace IdentityHub.Tests.Grpc;

[TestClass]
public class GrpcAuthServiceIntegrationTests : IntegrationTestBase
{
    private GrpcChannel? _channel;
    private AuthService.AuthServiceClient? _client;

    [TestInitialize]
    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        
        _channel = GrpcChannel.ForAddress(Client.BaseAddress!, new GrpcChannelOptions
        {
            HttpClient = Client
        });
        _client = new AuthService.AuthServiceClient(_channel);
    }

    [TestCleanup]
    public override void TestCleanup()
    {
        _channel?.Dispose();
        base.TestCleanup();
    }

    [TestMethod]
    public async Task RefreshTokenAsync_WithValidTokens_ShouldReturnNewTokens()
    {
        // Arrange
        var email = TestHelpers.GenerateRandomEmail();
        var password = TestHelpers.GenerateStrongPassword();

        // Register and login to get tokens via HTTP API first
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

        var refreshRequest = new RefreshTokenRequest
        {
            AccessToken = accessToken!,
            RefreshToken = refreshToken!
        };

        // Act
        var response = await _client!.RefreshTokenAsync(refreshRequest);

        // Assert
        response.Should().NotBeNull();
        response.Success.Should().BeTrue();
        response.AccessToken.Should().NotBeNullOrEmpty();
        response.RefreshToken.Should().NotBeNullOrEmpty();
        response.AccessToken.Should().NotBe(accessToken); // Should be a new token
    }
}
