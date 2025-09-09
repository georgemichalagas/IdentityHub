using FluentAssertions;
using IdentityHub.Api.Data;
using IdentityHub.Api.Models;
using IdentityHub.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IdentityHub.Tests.Data;

[TestClass]
public class ApplicationDbContextIntegrationTests : IntegrationTestBase
{
    [TestMethod]
    public async Task Database_ShouldBeCreatedAndAccessible()
    {
        // Arrange & Act
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Assert
        var canConnect = await context.Database.CanConnectAsync();
        canConnect.Should().BeTrue();
    }

    [TestMethod]
    public async Task CreateUser_ShouldPersistToDatabase()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = new ApplicationUser
        {
            UserName = TestHelpers.GenerateRandomEmail(),
            Email = TestHelpers.GenerateRandomEmail(),
            FirstName = "Test",
            LastName = "User",
            EmailConfirmed = true
        };

        // Act
        var result = await userManager.CreateAsync(user, TestHelpers.GenerateStrongPassword());

        // Assert
        result.Succeeded.Should().BeTrue();

        var savedUser = await userManager.FindByEmailAsync(user.Email);
        savedUser.Should().NotBeNull();
        savedUser!.FirstName.Should().Be("Test");
        savedUser.LastName.Should().Be("User");
        savedUser.EmailConfirmed.Should().BeTrue();
    }

    [TestMethod]
    public async Task CreateRefreshToken_ShouldPersistToDatabase()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // Create a user first
        var user = new ApplicationUser
        {
            UserName = TestHelpers.GenerateRandomEmail(),
            Email = TestHelpers.GenerateRandomEmail(),
            FirstName = "Test",
            LastName = "User",
            EmailConfirmed = true
        };

        await userManager.CreateAsync(user, TestHelpers.GenerateStrongPassword());

        var refreshToken = new RefreshToken
        {
            Token = Guid.NewGuid().ToString(),
            UserId = user.Id,
            ExpiryDate = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        context.RefreshTokens.Add(refreshToken);
        await context.SaveChangesAsync();

        // Assert
        var savedToken = await context.RefreshTokens.FindAsync(refreshToken.Id);
        savedToken.Should().NotBeNull();
        savedToken!.Token.Should().Be(refreshToken.Token);
        savedToken.UserId.Should().Be(user.Id);
        savedToken.IsRevoked.Should().BeFalse();
    }

    [TestMethod]
    public async Task RevokeRefreshToken_ShouldUpdateDatabase()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // Create a user first
        var user = new ApplicationUser
        {
            UserName = TestHelpers.GenerateRandomEmail(),
            Email = TestHelpers.GenerateRandomEmail(),
            FirstName = "Test",
            LastName = "User",
            EmailConfirmed = true
        };

        await userManager.CreateAsync(user, TestHelpers.GenerateStrongPassword());

        var refreshToken = new RefreshToken
        {
            Token = Guid.NewGuid().ToString(),
            UserId = user.Id,
            ExpiryDate = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };

        context.RefreshTokens.Add(refreshToken);
        await context.SaveChangesAsync();

        // Act
        refreshToken.IsRevoked = true;
        refreshToken.RevokedAt = DateTime.UtcNow;
        context.RefreshTokens.Update(refreshToken);
        await context.SaveChangesAsync();

        // Assert
        var updatedToken = await context.RefreshTokens.FindAsync(refreshToken.Id);
        updatedToken.Should().NotBeNull();
        updatedToken!.IsRevoked.Should().BeTrue();
        updatedToken.RevokedAt.Should().NotBeNull();
    }

    [TestMethod]
    public async Task FindUserByEmail_ShouldReturnCorrectUser()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var email = TestHelpers.GenerateRandomEmail();
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = "Test",
            LastName = "User",
            EmailConfirmed = true
        };

        await userManager.CreateAsync(user, TestHelpers.GenerateStrongPassword());

        // Act
        var foundUser = await userManager.FindByEmailAsync(email);

        // Assert
        foundUser.Should().NotBeNull();
        foundUser!.Email.Should().Be(email);
        foundUser.FirstName.Should().Be("Test");
        foundUser.LastName.Should().Be("User");
    }

    [TestCleanup]
    public async Task TestCleanup()
    {
        // Clean database after each test
        await CleanDatabaseAsync();
    }
}
