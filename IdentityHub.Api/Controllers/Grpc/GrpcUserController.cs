using Grpc.Core;
using IdentityHub.Api.Controllers.Grpc.Protos;
using IdentityHub.Api.Models;
using IdentityHub.Api.Controllers.Http.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using IdentityHub.Api.Services;

namespace IdentityHub.Api.Controllers.Grpc
{
    [Authorize]
    public class GrpcUserService : UserService.UserServiceBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuthService _authService;
        private readonly ILogger<GrpcUserService> _logger;

        public GrpcUserService(
            UserManager<ApplicationUser> userManager,
            IAuthService authService,
            ILogger<GrpcUserService> logger)
        {
            _userManager = userManager;
            _authService = authService;
            _logger = logger;
        }

        public override async Task<UserResponse> GetUser(GetUserRequest request, ServerCallContext context)
        {
            try
            {
                var result = await _authService.GetUserAsync(request.UserId);
                
                if (result.Success && result.Data != null)
                {
                    return new UserResponse
                    {
                        Success = true,
                        Message = result.Message,
                        User = MapToUserData(result.Data)
                    };
                }

                return new UserResponse
                {
                    Success = false,
                    Message = result.Message
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user with ID: {UserId}", request.UserId);
                return new UserResponse
                {
                    Success = false,
                    Message = "Internal server error"
                };
            }
        }

        public override async Task<UserResponse> GetUserByEmail(GetUserByEmailRequest request, ServerCallContext context)
        {
            try
            {
                var result = await _authService.GetUserByEmailAsync(request.Email);
                
                if (result.Success && result.Data != null)
                {
                    return new UserResponse
                    {
                        Success = true,
                        Message = result.Message,
                        User = MapToUserData(result.Data)
                    };
                }

                return new UserResponse
                {
                    Success = false,
                    Message = result.Message
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user with email: {Email}", request.Email);
                return new UserResponse
                {
                    Success = false,
                    Message = "Internal server error"
                };
            }
        }

        public override async Task<GetUsersResponse> GetUsers(GetUsersRequest request, ServerCallContext context)
        {
            try
            {
                var result = await _authService.GetUsersAsync(
                    request.Page, 
                    request.PageSize, 
                    string.IsNullOrEmpty(request.SearchTerm) ? null : request.SearchTerm, 
                    request.IncludeInactive);

                if (result.Success && result.Data != null)
                {
                    var userData = result.Data.Select(MapToUserData).ToList();

                    return new GetUsersResponse
                    {
                        Success = true,
                        Message = result.Message,
                        Users = { userData },
                        TotalCount = result.Data.Count,
                        Page = request.Page > 0 ? request.Page : 1,
                        PageSize = request.PageSize > 0 ? request.PageSize : 10
                    };
                }

                return new GetUsersResponse
                {
                    Success = false,
                    Message = result.Message
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users");
                return new GetUsersResponse
                {
                    Success = false,
                    Message = "Internal server error"
                };
            }
        }

        public override async Task<UserResponse> UpdateUser(UpdateUserRequest request, ServerCallContext context)
        {
            try
            {
                var updateUserDto = new UpdateUserDto
                {
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    PhoneNumber = request.PhoneNumber,
                    ProfilePictureUrl = request.ProfilePictureUrl
                };

                var result = await _authService.UpdateUserAsync(request.UserId, updateUserDto);

                if (result.Success && result.Data != null)
                {
                    return new UserResponse
                    {
                        Success = true,
                        Message = result.Message,
                        User = MapToUserData(result.Data)
                    };
                }

                return new UserResponse
                {
                    Success = false,
                    Message = result.Message
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user with ID: {UserId}", request.UserId);
                return new UserResponse
                {
                    Success = false,
                    Message = "Internal server error"
                };
            }
        }

        public override async Task<UserExistsResponse> UserExists(UserExistsRequest request, ServerCallContext context)
        {
            try
            {
                var exists = await _authService.UserExistsAsync(request.Email);
                return new UserExistsResponse
                {
                    Exists = exists
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user exists with email: {Email}", request.Email);
                return new UserExistsResponse
                {
                    Exists = false
                };
            }
        }

        public override async Task<GetUserRolesResponse> GetUserRoles(GetUserRolesRequest request, ServerCallContext context)
        {
            try
            {
                var roles = await _authService.GetUserRolesAsync(request.UserId);

                return new GetUserRolesResponse
                {
                    Success = true,
                    Message = "User roles retrieved successfully",
                    Roles = { roles }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving roles for user with ID: {UserId}", request.UserId);
                return new GetUserRolesResponse
                {
                    Success = false,
                    Message = "Internal server error"
                };
            }
        }

        private static UserData MapToUserData(ApplicationUser user)
        {
            return new UserData
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                Username = user.UserName ?? string.Empty,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                ProfilePictureUrl = user.ProfilePictureUrl ?? string.Empty,
                IsActive = user.IsActive,
                EmailConfirmed = user.EmailConfirmed,
                CreatedAt = user.CreatedAt.ToString("O"),
                UpdatedAt = user.UpdatedAt?.ToString("O") ?? string.Empty
            };
        }

        private static UserData MapToUserData(UserDto userDto)
        {
            return new UserData
            {
                Id = userDto.Id,
                Email = userDto.Email,
                Username = userDto.Email, // Use email as username for DTO mapping
                FirstName = userDto.FirstName ?? string.Empty,
                LastName = userDto.LastName ?? string.Empty,
                PhoneNumber = userDto.PhoneNumber ?? string.Empty,
                ProfilePictureUrl = userDto.ProfilePictureUrl ?? string.Empty,
                IsActive = userDto.IsActive,
                EmailConfirmed = userDto.EmailConfirmed,
                CreatedAt = userDto.CreatedAt.ToString("O"),
                UpdatedAt = userDto.UpdatedAt?.ToString("O") ?? string.Empty
            };
        }
    }
}
