using IdentityHub.Api.Controllers.Http.Dtos;
using IdentityHub.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using IdentityHub.Api.Data;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Security.Cryptography;

namespace IdentityHub.Api.Services
{
    public interface IAuthService
    {
        // Authentication methods
        Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto);
        Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
        Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenDto refreshTokenDto);
        Task<AuthResponseDto> LogoutAsync(string userId);

        // Password management
        Task<ApiResponseDto<string>> ForgotPasswordAsync(ForgotPasswordDto forgotPasswordDto);
        Task<ApiResponseDto<string>> ResetPasswordAsync(ResetPasswordDto resetPasswordDto);
        Task<ApiResponseDto<string>> ChangePasswordAsync(string userId, ChangePasswordDto changePasswordDto);
        Task<ApiResponseDto<string>> ConfirmEmailAsync(string userId, string token);

        // User management
        Task<ApiResponseDto<UserDto>> GetUserAsync(string userId);
        Task<ApiResponseDto<UserDto>> GetUserByEmailAsync(string email);
        Task<ApiResponseDto<UserDto>> UpdateUserAsync(string userId, UpdateUserDto updateUserDto);
        Task<ApiResponseDto<List<UserDto>>> GetUsersAsync(int page = 1, int pageSize = 10, string? searchTerm = null, bool includeInactive = false);

        // Validation methods (for gRPC)
        Task<bool> ValidateTokenAsync(string token);
        Task<(bool IsValid, UserDto? User)> ValidateCredentialsAsync(string email, string password);
        Task<bool> UserExistsAsync(string email);
        Task<List<string>> GetUserRolesAsync(string userId);
        Task<bool> CheckUserPermissionAsync(string userId, string resource, string action);
    }

    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context,
            IMapper mapper,
            IConfiguration configuration,
            ILogger<AuthService> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _mapper = mapper;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto)
        {
            try
            {
                var existingUser = await _userManager.FindByEmailAsync(registerDto.Email);
                if (existingUser != null)
                {
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "User with this email already exists"
                    };
                }

                var user = new ApplicationUser
                {
                    UserName = registerDto.Email,
                    Email = registerDto.Email,
                    FirstName = registerDto.FirstName,
                    LastName = registerDto.LastName,
                    PhoneNumber = registerDto.PhoneNumber,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(user, registerDto.Password);
                if (!result.Succeeded)
                {
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "Failed to create user: " + string.Join(", ", result.Errors.Select(e => e.Description))
                    };
                }

                // Add default role
                await _userManager.AddToRoleAsync(user, "User");

                // Generate email confirmation token
                var emailToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                // In production, send this token via email

                _logger.LogInformation($"User {user.Email} registered successfully");

                var userDto = _mapper.Map<UserDto>(user);
                userDto.Roles = (await _userManager.GetRolesAsync(user)).ToList();

                return new AuthResponseDto
                {
                    Success = true,
                    Message = "User registered successfully. Please confirm your email.",
                    User = userDto
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user registration");
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "An error occurred during registration"
                };
            }
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(loginDto.Email);
                if (user == null || !user.IsActive)
                {
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "Invalid email or password"
                    };
                }

                var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, lockoutOnFailure: true);
                if (!result.Succeeded)
                {
                    var message = result.IsLockedOut ? "Account is locked out" : "Invalid email or password";
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = message
                    };
                }

                var accessToken = await GenerateAccessTokenAsync(user);
                var refreshToken = GenerateRefreshToken();

                // Save refresh token to database
                var refreshTokenEntity = new RefreshToken
                {
                    Token = refreshToken,
                    UserId = user.Id,
                    ExpiryDate = DateTime.UtcNow.AddDays(7) // 7 days expiry
                };

                _context.RefreshTokens.Add(refreshTokenEntity);
                await _context.SaveChangesAsync();

                var userDto = _mapper.Map<UserDto>(user);
                userDto.Roles = (await _userManager.GetRolesAsync(user)).ToList();

                _logger.LogInformation($"User {user.Email} logged in successfully");

                return new AuthResponseDto
                {
                    Success = true,
                    Message = "Login successful",
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(GetAccessTokenExpiryMinutes()),
                    User = userDto
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user login");
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "An error occurred during login"
                };
            }
        }

        public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenDto refreshTokenDto)
        {
            try
            {
                var refreshTokenEntity = await _context.RefreshTokens
                    .Include(rt => rt.User)
                    .FirstOrDefaultAsync(rt => rt.Token == refreshTokenDto.RefreshToken && !rt.IsRevoked);

                if (refreshTokenEntity == null || refreshTokenEntity.ExpiryDate <= DateTime.UtcNow)
                {
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "Invalid or expired refresh token"
                    };
                }

                // Generate new tokens
                var newAccessToken = await GenerateAccessTokenAsync(refreshTokenEntity.User);
                var newRefreshToken = GenerateRefreshToken();

                // Revoke old refresh token
                refreshTokenEntity.IsRevoked = true;
                refreshTokenEntity.RevokedAt = DateTime.UtcNow;

                // Create new refresh token
                var newRefreshTokenEntity = new RefreshToken
                {
                    Token = newRefreshToken,
                    UserId = refreshTokenEntity.UserId,
                    ExpiryDate = DateTime.UtcNow.AddDays(7)
                };

                _context.RefreshTokens.Add(newRefreshTokenEntity);
                await _context.SaveChangesAsync();

                var userDto = _mapper.Map<UserDto>(refreshTokenEntity.User);
                userDto.Roles = (await _userManager.GetRolesAsync(refreshTokenEntity.User)).ToList();

                return new AuthResponseDto
                {
                    Success = true,
                    Message = "Token refreshed successfully",
                    AccessToken = newAccessToken,
                    RefreshToken = newRefreshToken,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(GetAccessTokenExpiryMinutes()),
                    User = userDto
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token refresh");
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "An error occurred during token refresh"
                };
            }
        }

        public async Task<AuthResponseDto> LogoutAsync(string userId)
        {
            try
            {
                // Revoke all refresh tokens for the user
                var refreshTokens = await _context.RefreshTokens
                    .Where(rt => rt.UserId == userId && !rt.IsRevoked)
                    .ToListAsync();

                foreach (var token in refreshTokens)
                {
                    token.IsRevoked = true;
                    token.RevokedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                return new AuthResponseDto
                {
                    Success = true,
                    Message = "Logout successful"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "An error occurred during logout"
                };
            }
        }

        public async Task<ApiResponseDto<string>> ForgotPasswordAsync(ForgotPasswordDto forgotPasswordDto)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(forgotPasswordDto.Email);
                if (user == null)
                {
                    // Don't reveal that the user doesn't exist
                    return new ApiResponseDto<string>
                    {
                        Success = true,
                        Message = "If an account with that email exists, a password reset link has been sent."
                    };
                }

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                // In production, send this token via email

                _logger.LogInformation($"Password reset requested for user {user.Email}");

                return new ApiResponseDto<string>
                {
                    Success = true,
                    Message = "If an account with that email exists, a password reset link has been sent.",
                    Data = token // Remove this in production
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during forgot password");
                return new ApiResponseDto<string>
                {
                    Success = false,
                    Message = "An error occurred while processing the request"
                };
            }
        }

        public async Task<ApiResponseDto<string>> ResetPasswordAsync(ResetPasswordDto resetPasswordDto)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(resetPasswordDto.Email);
                if (user == null)
                {
                    return new ApiResponseDto<string>
                    {
                        Success = false,
                        Message = "Invalid reset token"
                    };
                }

                var result = await _userManager.ResetPasswordAsync(user, resetPasswordDto.Token, resetPasswordDto.NewPassword);
                if (!result.Succeeded)
                {
                    return new ApiResponseDto<string>
                    {
                        Success = false,
                        Message = "Failed to reset password",
                        Errors = result.Errors.Select(e => e.Description).ToList()
                    };
                }

                _logger.LogInformation($"Password reset successful for user {user.Email}");

                return new ApiResponseDto<string>
                {
                    Success = true,
                    Message = "Password reset successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during password reset");
                return new ApiResponseDto<string>
                {
                    Success = false,
                    Message = "An error occurred while resetting password"
                };
            }
        }

        public async Task<ApiResponseDto<string>> ChangePasswordAsync(string userId, ChangePasswordDto changePasswordDto)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return new ApiResponseDto<string>
                    {
                        Success = false,
                        Message = "User not found"
                    };
                }

                var result = await _userManager.ChangePasswordAsync(user, changePasswordDto.CurrentPassword, changePasswordDto.NewPassword);
                if (!result.Succeeded)
                {
                    return new ApiResponseDto<string>
                    {
                        Success = false,
                        Message = "Failed to change password",
                        Errors = result.Errors.Select(e => e.Description).ToList()
                    };
                }

                _logger.LogInformation($"Password changed for user {user.Email}");

                return new ApiResponseDto<string>
                {
                    Success = true,
                    Message = "Password changed successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during password change");
                return new ApiResponseDto<string>
                {
                    Success = false,
                    Message = "An error occurred while changing password"
                };
            }
        }

        public async Task<ApiResponseDto<string>> ConfirmEmailAsync(string userId, string token)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return new ApiResponseDto<string>
                    {
                        Success = false,
                        Message = "Invalid confirmation token"
                    };
                }

                var result = await _userManager.ConfirmEmailAsync(user, token);
                if (!result.Succeeded)
                {
                    return new ApiResponseDto<string>
                    {
                        Success = false,
                        Message = "Failed to confirm email",
                        Errors = result.Errors.Select(e => e.Description).ToList()
                    };
                }

                _logger.LogInformation($"Email confirmed for user {user.Email}");

                return new ApiResponseDto<string>
                {
                    Success = true,
                    Message = "Email confirmed successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during email confirmation");
                return new ApiResponseDto<string>
                {
                    Success = false,
                    Message = "An error occurred while confirming email"
                };
            }
        }

        public async Task<ApiResponseDto<UserDto>> GetUserAsync(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return new ApiResponseDto<UserDto>
                    {
                        Success = false,
                        Message = "User not found"
                    };
                }

                var userDto = _mapper.Map<UserDto>(user);
                userDto.Roles = (await _userManager.GetRolesAsync(user)).ToList();

                return new ApiResponseDto<UserDto>
                {
                    Success = true,
                    Message = "User retrieved successfully",
                    Data = userDto
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user");
                return new ApiResponseDto<UserDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving user"
                };
            }
        }

        public async Task<ApiResponseDto<UserDto>> UpdateUserAsync(string userId, UpdateUserDto updateUserDto)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return new ApiResponseDto<UserDto>
                    {
                        Success = false,
                        Message = "User not found"
                    };
                }

                user.FirstName = updateUserDto.FirstName ?? user.FirstName;
                user.LastName = updateUserDto.LastName ?? user.LastName;
                user.PhoneNumber = updateUserDto.PhoneNumber ?? user.PhoneNumber;
                user.ProfilePictureUrl = updateUserDto.ProfilePictureUrl ?? user.ProfilePictureUrl;
                user.UpdatedAt = DateTime.UtcNow;

                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    return new ApiResponseDto<UserDto>
                    {
                        Success = false,
                        Message = "Failed to update user",
                        Errors = result.Errors.Select(e => e.Description).ToList()
                    };
                }

                var userDto = _mapper.Map<UserDto>(user);
                userDto.Roles = (await _userManager.GetRolesAsync(user)).ToList();

                _logger.LogInformation($"User {user.Email} updated successfully");

                return new ApiResponseDto<UserDto>
                {
                    Success = true,
                    Message = "User updated successfully",
                    Data = userDto
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user");
                return new ApiResponseDto<UserDto>
                {
                    Success = false,
                    Message = "An error occurred while updating user"
                };
            }
        }

        private async Task<string> GenerateAccessTokenAsync(ApplicationUser user)
        {
            var userRoles = await _userManager.GetRolesAsync(user);
            var authClaims = new List<Claim>
            {
                new(ClaimTypes.Name, user.UserName ?? string.Empty),
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Email, user.Email ?? string.Empty),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            foreach (var userRole in userRoles)
            {
                authClaims.Add(new Claim(ClaimTypes.Role, userRole));
            }

            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:SecretKey"] ?? "DefaultSecretKey"));
            var credentials = new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["JwtSettings:Issuer"],
                audience: _configuration["JwtSettings:Audience"],
                expires: DateTime.UtcNow.AddMinutes(GetAccessTokenExpiryMinutes()),
                claims: authClaims,
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        private int GetAccessTokenExpiryMinutes()
        {
            return int.Parse(_configuration["JwtSettings:ExpiryInMinutes"] ?? "15");
        }

        // Additional methods for gRPC support
        public Task<bool> ValidateTokenAsync(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtSettings = _configuration.GetSection("JwtSettings");
                var secretKey = jwtSettings["SecretKey"] ?? "DefaultSecretKeyForDevelopmentOnlyChangeInProduction123456789";
                var key = Encoding.UTF8.GetBytes(secretKey);

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings["Issuer"] ?? "IdentityHub",
                    ValidAudience = jwtSettings["Audience"] ?? "IdentityHubClient",
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ClockSkew = TimeSpan.Zero
                };

                tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
                return Task.FromResult(validatedToken != null);
            }
            catch
            {
                return Task.FromResult(false);
            }
        }

        public async Task<(bool IsValid, UserDto? User)> ValidateCredentialsAsync(string email, string password)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    return (false, null);
                }

                var result = await _signInManager.CheckPasswordSignInAsync(user, password, false);
                if (result.Succeeded)
                {
                    var userDto = _mapper.Map<UserDto>(user);
                    userDto.Roles = (await _userManager.GetRolesAsync(user)).ToList();
                    return (true, userDto);
                }

                return (false, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating credentials for email: {Email}", email);
                return (false, null);
            }
        }

        public async Task<ApiResponseDto<UserDto>> GetUserByEmailAsync(string email)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    return new ApiResponseDto<UserDto>
                    {
                        Success = false,
                        Message = "User not found"
                    };
                }

                var userDto = _mapper.Map<UserDto>(user);
                userDto.Roles = (await _userManager.GetRolesAsync(user)).ToList();

                return new ApiResponseDto<UserDto>
                {
                    Success = true,
                    Message = "User retrieved successfully",
                    Data = userDto
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user by email: {Email}", email);
                return new ApiResponseDto<UserDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving user"
                };
            }
        }

        public async Task<bool> UserExistsAsync(string email)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                return user != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user exists: {Email}", email);
                return false;
            }
        }

        public async Task<List<string>> GetUserRolesAsync(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return new List<string>();
                }

                var roles = await _userManager.GetRolesAsync(user);
                return roles.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user roles for user: {UserId}", userId);
                return new List<string>();
            }
        }

        public async Task<bool> CheckUserPermissionAsync(string userId, string resource, string action)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null || !user.IsActive)
                {
                    return false;
                }

                var roles = await _userManager.GetRolesAsync(user);

                // Basic permission logic - can be extended based on your requirements
                return resource.ToLower() switch
                {
                    "admin" => roles.Contains("Admin"),
                    "user" => roles.Any(r => r == "Admin" || r == "User"),
                    _ => user.IsActive // Default: check if user is active
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking permissions for user: {UserId}", userId);
                return false;
            }
        }

        public async Task<ApiResponseDto<List<UserDto>>> GetUsersAsync(int page = 1, int pageSize = 10, string? searchTerm = null, bool includeInactive = false)
        {
            try
            {
                var query = _userManager.Users.AsQueryable();

                // Apply search filter
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    query = query.Where(u =>
                        (u.Email != null && u.Email.Contains(searchTerm)) ||
                        (u.FirstName != null && u.FirstName.Contains(searchTerm)) ||
                        (u.LastName != null && u.LastName.Contains(searchTerm)));
                }

                // Apply active filter
                if (!includeInactive)
                {
                    query = query.Where(u => u.IsActive);
                }

                var totalCount = await query.CountAsync();

                // Apply pagination
                pageSize = pageSize > 0 ? pageSize : 10;
                page = page > 0 ? page : 1;
                var skip = (page - 1) * pageSize;

                var users = await query
                    .Skip(skip)
                    .Take(pageSize)
                    .ToListAsync();

                var userDtos = new List<UserDto>();
                foreach (var user in users)
                {
                    var userDto = _mapper.Map<UserDto>(user);
                    userDto.Roles = (await _userManager.GetRolesAsync(user)).ToList();
                    userDtos.Add(userDto);
                }

                return new ApiResponseDto<List<UserDto>>
                {
                    Success = true,
                    Message = "Users retrieved successfully",
                    Data = userDtos
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users");
                return new ApiResponseDto<List<UserDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving users"
                };
            }
        }
    }
}
