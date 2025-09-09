using IdentityHub.Api.Controllers.Http.Dtos;
using IdentityHub.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace IdentityHub.Api.Controllers.Http
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<UserController> _logger;

        public UserController(IAuthService authService, ILogger<UserController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Get current user profile
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///     GET /api/user/profile
        ///     Header: Authorization: Bearer {token}
        /// </remarks>
        [HttpGet("profile")]
        public async Task<ActionResult<ApiResponseDto<UserDto>>> GetProfile()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest(new ApiResponseDto<UserDto> { Success = false, Message = "Invalid user" });
            }

            var result = await _authService.GetUserAsync(userId);
            
            if (result.Success)
            {
                return Ok(result);
            }

            return NotFound(result);
        }

        /// <summary>
        /// Update current user profile
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///     PUT /api/user/profile
        ///     Header: Authorization: Bearer {token}
        ///     {
        ///         "firstName": "John",
        ///         "lastName": "Doe",
        ///         "phoneNumber": "1234567890"
        ///     }
        /// </remarks>
        [HttpPut("profile")]
        public async Task<ActionResult<ApiResponseDto<UserDto>>> UpdateProfile([FromBody] UpdateUserDto updateUserDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest(new ApiResponseDto<UserDto> { Success = false, Message = "Invalid user" });
            }

            var result = await _authService.UpdateUserAsync(userId, updateUserDto);
            
            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }

        /// <summary>
        /// Get user by ID (Admin only)
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///     GET /api/user/{id}
        ///     Header: Authorization: Bearer {admin_token}
        /// </remarks>
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponseDto<UserDto>>> GetUser(string id)
        {
            var result = await _authService.GetUserAsync(id);
            
            if (result.Success)
            {
                return Ok(result);
            }

            return NotFound(result);
        }
    }
}
