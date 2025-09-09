using Grpc.Net.Client;
using IdentityHub.Api.Controllers.Grpc.Protos;
using Microsoft.Extensions.Logging;

namespace IdentityHub.Samples
{
    /// <summary>
    /// Sample gRPC client demonstrating how to interact with IdentityHub gRPC services
    /// </summary>
    public class GrpcClientSample
    {
        private readonly GrpcChannel _channel;
        private readonly UserService.UserServiceClient _userClient;
        private readonly AuthService.AuthServiceClient _authClient;
        private readonly ILogger<GrpcClientSample> _logger;

        public GrpcClientSample(string serverAddress, ILogger<GrpcClientSample> logger)
        {
            _logger = logger;
            
            // Create gRPC channel
            _channel = GrpcChannel.ForAddress(serverAddress);
            
            // Create service clients
            _userClient = new UserService.UserServiceClient(_channel);
            _authClient = new AuthService.AuthServiceClient(_channel);
        }

        /// <summary>
        /// Demonstrates token validation
        /// </summary>
        public async Task<bool> ValidateTokenAsync(string token)
        {
            try
            {
                var request = new ValidateTokenRequest { Token = token };
                var response = await _authClient.ValidateTokenAsync(request);
                
                _logger.LogInformation("Token validation result: {IsValid}, Message: {Message}", 
                    response.IsValid, response.Message);
                
                if (response.IsValid)
                {
                    _logger.LogInformation("User ID: {UserId}, Email: {Email}, Roles: {Roles}", 
                        response.UserId, response.Email, string.Join(", ", response.Roles));
                }
                
                return response.IsValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating token");
                return false;
            }
        }

        /// <summary>
        /// Demonstrates credential validation
        /// </summary>
        public async Task<ValidateCredentialsResponse?> ValidateCredentialsAsync(string email, string password)
        {
            try
            {
                var request = new ValidateCredentialsRequest 
                { 
                    Email = email, 
                    Password = password 
                };
                
                var response = await _authClient.ValidateCredentialsAsync(request);
                
                _logger.LogInformation("Credential validation result: {IsValid}, Message: {Message}", 
                    response.IsValid, response.Message);
                
                if (response.IsValid && response.UserInfo != null)
                {
                    _logger.LogInformation("User validated: {FirstName} {LastName} ({Email})", 
                        response.UserInfo.FirstName, response.UserInfo.LastName, response.UserInfo.Email);
                }
                
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating credentials");
                return null;
            }
        }

        /// <summary>
        /// Demonstrates getting user by ID
        /// </summary>
        public async Task<UserData?> GetUserAsync(string userId)
        {
            try
            {
                var request = new GetUserRequest { UserId = userId };
                var response = await _userClient.GetUserAsync(request);
                
                _logger.LogInformation("Get user result: {Success}, Message: {Message}", 
                    response.Success, response.Message);
                
                if (response.Success && response.User != null)
                {
                    _logger.LogInformation("User found: {FirstName} {LastName} ({Email})", 
                        response.User.FirstName, response.User.LastName, response.User.Email);
                    return response.User;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by ID: {UserId}", userId);
                return null;
            }
        }

        /// <summary>
        /// Demonstrates getting user by email
        /// </summary>
        public async Task<UserData?> GetUserByEmailAsync(string email)
        {
            try
            {
                var request = new GetUserByEmailRequest { Email = email };
                var response = await _userClient.GetUserByEmailAsync(request);
                
                _logger.LogInformation("Get user by email result: {Success}, Message: {Message}", 
                    response.Success, response.Message);
                
                if (response.Success && response.User != null)
                {
                    _logger.LogInformation("User found: {FirstName} {LastName} (ID: {Id})", 
                        response.User.FirstName, response.User.LastName, response.User.Id);
                    return response.User;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by email: {Email}", email);
                return null;
            }
        }

        /// <summary>
        /// Demonstrates getting multiple users with pagination
        /// </summary>
        public async Task<GetUsersResponse?> GetUsersAsync(int page = 1, int pageSize = 10, string? searchTerm = null, bool includeInactive = false)
        {
            try
            {
                var request = new GetUsersRequest 
                { 
                    Page = page,
                    PageSize = pageSize,
                    SearchTerm = searchTerm ?? string.Empty,
                    IncludeInactive = includeInactive
                };
                
                var response = await _userClient.GetUsersAsync(request);
                
                _logger.LogInformation("Get users result: {Success}, Total: {TotalCount}, Page: {Page}/{PageSize}", 
                    response.Success, response.TotalCount, response.Page, response.PageSize);
                
                if (response.Success)
                {
                    _logger.LogInformation("Retrieved {Count} users", response.Users.Count);
                    foreach (var user in response.Users)
                    {
                        _logger.LogInformation("- {FirstName} {LastName} ({Email})", 
                            user.FirstName, user.LastName, user.Email);
                    }
                }
                
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users");
                return null;
            }
        }

        /// <summary>
        /// Demonstrates updating a user
        /// </summary>
        public async Task<UserData?> UpdateUserAsync(string userId, string? firstName = null, string? lastName = null, 
            string? phoneNumber = null, string? profilePictureUrl = null, bool? isActive = null)
        {
            try
            {
                var request = new UpdateUserRequest 
                { 
                    UserId = userId,
                    FirstName = firstName ?? string.Empty,
                    LastName = lastName ?? string.Empty,
                    PhoneNumber = phoneNumber ?? string.Empty,
                    ProfilePictureUrl = profilePictureUrl ?? string.Empty,
                    IsActive = isActive ?? true
                };
                
                var response = await _userClient.UpdateUserAsync(request);
                
                _logger.LogInformation("Update user result: {Success}, Message: {Message}", 
                    response.Success, response.Message);
                
                if (response.Success && response.User != null)
                {
                    _logger.LogInformation("User updated: {FirstName} {LastName}", 
                        response.User.FirstName, response.User.LastName);
                    return response.User;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user: {UserId}", userId);
                return null;
            }
        }

        /// <summary>
        /// Demonstrates checking if user exists
        /// </summary>
        public async Task<bool> UserExistsAsync(string email)
        {
            try
            {
                var request = new UserExistsRequest { Email = email };
                var response = await _userClient.UserExistsAsync(request);
                
                _logger.LogInformation("User exists check for {Email}: {Exists}", email, response.Exists);
                
                return response.Exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user exists: {Email}", email);
                return false;
            }
        }

        /// <summary>
        /// Demonstrates getting user roles
        /// </summary>
        public async Task<List<string>?> GetUserRolesAsync(string userId)
        {
            try
            {
                var request = new GetUserRolesRequest { UserId = userId };
                var response = await _userClient.GetUserRolesAsync(request);
                
                _logger.LogInformation("Get user roles result: {Success}, Message: {Message}", 
                    response.Success, response.Message);
                
                if (response.Success)
                {
                    var roles = response.Roles.ToList();
                    _logger.LogInformation("User roles: {Roles}", string.Join(", ", roles));
                    return roles;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user roles: {UserId}", userId);
                return null;
            }
        }

        /// <summary>
        /// Demonstrates checking user permissions
        /// </summary>
        public async Task<bool> CheckPermissionsAsync(string userId, string resource, string action)
        {
            try
            {
                var request = new CheckPermissionsRequest 
                { 
                    UserId = userId, 
                    Resource = resource, 
                    Action = action 
                };
                
                var response = await _authClient.CheckPermissionsAsync(request);
                
                _logger.LogInformation("Permission check for user {UserId} on {Resource}:{Action}: {HasPermission}, Message: {Message}", 
                    userId, resource, action, response.HasPermission, response.Message);
                
                return response.HasPermission;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking permissions for user {UserId}", userId);
                return false;
            }
        }

        /// <summary>
        /// Demonstrates token refresh
        /// </summary>
        public async Task<(string? AccessToken, string? RefreshToken)> RefreshTokenAsync(string accessToken, string refreshToken)
        {
            try
            {
                var request = new RefreshTokenRequest 
                { 
                    AccessToken = accessToken, 
                    RefreshToken = refreshToken 
                };
                
                var response = await _authClient.RefreshTokenAsync(request);
                
                _logger.LogInformation("Token refresh result: {Success}, Message: {Message}", 
                    response.Success, response.Message);
                
                if (response.Success)
                {
                    _logger.LogInformation("Tokens refreshed successfully");
                    return (response.AccessToken, response.RefreshToken);
                }
                
                return (null, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                return (null, null);
            }
        }

        /// <summary>
        /// Clean up resources
        /// </summary>
        public void Dispose()
        {
            _channel?.Dispose();
        }
    }
}

/// <summary>
/// Example usage of the gRPC client
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        // Setup logging
        using var loggerFactory = LoggerFactory.Create(builder =>
            builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        
        var logger = loggerFactory.CreateLogger<GrpcClientSample>();
        
        // Create gRPC client
        var client = new GrpcClientSample("https://localhost:7001", logger);
        
        try
        {
            // Example: Check if user exists
            var userExists = await client.UserExistsAsync("admin@identityhub.com");
            logger.LogInformation("Admin user exists: {Exists}", userExists);
            
            // Example: Get user by email
            if (userExists)
            {
                var user = await client.GetUserByEmailAsync("admin@identityhub.com");
                if (user != null)
                {
                    // Example: Get user roles
                    var roles = await client.GetUserRolesAsync(user.Id);
                    logger.LogInformation("User {Email} has roles: {Roles}", 
                        user.Email, roles != null ? string.Join(", ", roles) : "None");
                    
                    // Example: Check permissions
                    var hasAdminPermission = await client.CheckPermissionsAsync(user.Id, "admin", "read");
                    logger.LogInformation("User has admin permission: {HasPermission}", hasAdminPermission);
                }
            }
            
            // Example: Get all users
            var usersResponse = await client.GetUsersAsync(page: 1, pageSize: 5);
            if (usersResponse != null && usersResponse.Success)
            {
                logger.LogInformation("Found {TotalCount} total users", usersResponse.TotalCount);
            }
            
            logger.LogInformation("gRPC client sample completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error running gRPC client sample");
        }
        finally
        {
            client.Dispose();
        }
    }
}
