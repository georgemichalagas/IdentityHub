using IdentityHub.Api.Controllers.Http.Dtos;
using IdentityHub.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace IdentityHub.Api.Controllers.Http
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Register a new user
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///     POST /api/auth/register
        ///     {
        ///         "email": "user@example.com",
        ///         "password": "Password123!",
        ///         "confirmPassword": "Password123!"
        ///     }
        /// </remarks>
        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterDto registerDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _authService.RegisterAsync(registerDto);
            
            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }

        /// <summary>
        /// Authenticate user and return JWT tokens
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///     POST /api/auth/login
        ///     {
        ///         "email": "user@example.com",
        ///         "password": "Password123!"
        ///     }
        /// </remarks>
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto loginDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _authService.LoginAsync(loginDto);
            
            if (result.Success)
            {
                return Ok(result);
            }

            return Unauthorized(result);
        }

        /// <summary>
        /// Refresh access token using refresh token
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///     POST /api/auth/refresh-token
        ///     {
        ///         "refreshToken": "<refresh_token>"
        ///     }
        /// </remarks>
        [HttpPost("refresh-token")]
        public async Task<ActionResult<AuthResponseDto>> RefreshToken([FromBody] RefreshTokenDto refreshTokenDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _authService.RefreshTokenAsync(refreshTokenDto);
            
            if (result.Success)
            {
                return Ok(result);
            }

            return Unauthorized(result);
        }

        /// <summary>
        /// Logout user and revoke refresh tokens
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///     POST /api/auth/logout
        ///     Header: Authorization: Bearer {token}
        /// </remarks>
        [HttpPost("logout")]
        [Authorize]
        public async Task<ActionResult<AuthResponseDto>> Logout()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest(new AuthResponseDto { Success = false, Message = "Invalid user" });
            }

            var result = await _authService.LogoutAsync(userId);
            return Ok(result);
        }

        /// <summary>
        /// Request password reset
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///     POST /api/auth/forgot-password
        ///     {
        ///         "email": "user@example.com"
        ///     }
        /// </remarks>
        [HttpPost("forgot-password")]
        public async Task<ActionResult<ApiResponseDto<string>>> ForgotPassword([FromBody] ForgotPasswordDto forgotPasswordDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _authService.ForgotPasswordAsync(forgotPasswordDto);
            return Ok(result);
        }

        /// <summary>
        /// Reset password using reset token
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///     POST /api/auth/reset-password
        ///     {
        ///         "email": "user@example.com",
        ///         "token": "<reset_token>",
        ///         "newPassword": "NewPassword123!"
        ///     }
        /// </remarks>
        [HttpPost("reset-password")]
        public async Task<ActionResult<ApiResponseDto<string>>> ResetPassword([FromBody] ResetPasswordDto resetPasswordDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _authService.ResetPasswordAsync(resetPasswordDto);
            
            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }

        /// <summary>
        /// Change user password
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///     POST /api/auth/change-password
        ///     Header: Authorization: Bearer {token}
        ///     {
        ///         "currentPassword": "OldPassword123!",
        ///         "newPassword": "NewPassword123!"
        ///     }
        /// </remarks>
        [HttpPost("change-password")]
        [Authorize]
        public async Task<ActionResult<ApiResponseDto<string>>> ChangePassword([FromBody] ChangePasswordDto changePasswordDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest(new ApiResponseDto<string> { Success = false, Message = "Invalid user" });
            }

            var result = await _authService.ChangePasswordAsync(userId, changePasswordDto);
            
            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }

        /// <summary>
        /// Confirm email address
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///     GET /api/auth/confirm-email?userId=<user_id>&token=<confirmation_token>
        /// </remarks>
        [HttpGet("confirm-email")]
        public async Task<ActionResult<ApiResponseDto<string>>> ConfirmEmail(string userId, string token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            {
                return BadRequest(new ApiResponseDto<string> { Success = false, Message = "Invalid confirmation parameters" });
            }

            var result = await _authService.ConfirmEmailAsync(userId, token);
            
            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }
    }
}
