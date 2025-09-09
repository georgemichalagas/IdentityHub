using Grpc.Core;
using IdentityHub.Api.Controllers.Http.Dtos;
using IdentityHub.Api.Controllers.Grpc.Protos;
using IdentityHub.Api.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace IdentityHub.Api.Controllers.Grpc
{
    public class GrpcAuthService : Protos.AuthService.AuthServiceBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<GrpcAuthService> _logger;

        public GrpcAuthService(IAuthService authService,ILogger<GrpcAuthService> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        public override async Task<ValidateTokenResponse> ValidateToken(ValidateTokenRequest request, ServerCallContext context)
        {
            try
            {
                var isValid = await _authService.ValidateTokenAsync(request.Token);
                
                if (isValid)
                {
                    // Extract token information for response
                    var tokenHandler = new JwtSecurityTokenHandler();
                    var token = tokenHandler.ReadJwtToken(request.Token);
                    
                    var userId = token.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
                    var email = token.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Email)?.Value ?? string.Empty;
                    var roles = token.Claims.Where(x => x.Type == ClaimTypes.Role).Select(c => c.Value).ToList();

                    return new ValidateTokenResponse
                    {
                        IsValid = true,
                        UserId = userId,
                        Email = email,
                        Roles = { roles },
                        Message = "Token is valid",
                        ExpiresAt = token.ValidTo.Ticks
                    };
                }

                return new ValidateTokenResponse
                {
                    IsValid = false,
                    Message = "Invalid token"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating token");
                return new ValidateTokenResponse
                {
                    IsValid = false,
                    Message = "Internal server error"
                };
            }
        }

        public override async Task<ValidateCredentialsResponse> ValidateCredentials(ValidateCredentialsRequest request, ServerCallContext context)
        {
            try
            {
                var (isValid, user) = await _authService.ValidateCredentialsAsync(request.Email, request.Password);
                
                if (isValid && user != null)
                {
                    return new ValidateCredentialsResponse
                    {
                        IsValid = true,
                        UserId = user.Id,
                        Message = "Credentials are valid",
                        UserInfo = new UserInfo
                        {
                            Id = user.Id,
                            Email = user.Email,
                            FirstName = user.FirstName ?? string.Empty,
                            LastName = user.LastName ?? string.Empty,
                            Roles = { user.Roles },
                            IsActive = user.IsActive
                        }
                    };
                }

                return new ValidateCredentialsResponse
                {
                    IsValid = false,
                    Message = "Invalid credentials"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating credentials for email: {Email}", request.Email);
                return new ValidateCredentialsResponse
                {
                    IsValid = false,
                    Message = "Internal server error"
                };
            }
        }

        public override async Task<CheckPermissionsResponse> CheckPermissions(CheckPermissionsRequest request, ServerCallContext context)
        {
            try
            {
                var hasPermission = await _authService.CheckUserPermissionAsync(request.UserId, request.Resource, request.Action);

                return new CheckPermissionsResponse
                {
                    HasPermission = hasPermission,
                    Message = hasPermission ? "Permission granted" : "Permission denied"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking permissions for user: {UserId}", request.UserId);
                return new CheckPermissionsResponse
                {
                    HasPermission = false,
                    Message = "Internal server error"
                };
            }
        }

        public override async Task<RefreshTokenResponse> RefreshToken(RefreshTokenRequest request, ServerCallContext context)
        {
            try
            {
                var refreshTokenDto = new RefreshTokenDto
                {
                    AccessToken = request.AccessToken,
                    RefreshToken = request.RefreshToken
                };

                var result = await _authService.RefreshTokenAsync(refreshTokenDto);

                if (result.Success)
                {
                    return new RefreshTokenResponse
                    {
                        Success = true,
                        Message = "Token refreshed successfully",
                        AccessToken = result.AccessToken ?? string.Empty,
                        RefreshToken = result.RefreshToken ?? string.Empty,
                        ExpiresAt = result.ExpiresAt?.Ticks ?? 0
                    };
                }

                return new RefreshTokenResponse
                {
                    Success = false,
                    Message = result.Message
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                return new RefreshTokenResponse
                {
                    Success = false,
                    Message = "Internal server error"
                };
            }
        }
    }
}
