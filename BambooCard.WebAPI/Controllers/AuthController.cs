using BambooCard.Business.Abstractions;
using BambooCard.Business.Models.User;
using BambooCard.Infrastructure.Enums;
using BambooCard.Infrastructure.Results;
using BambooCard.WebAPI.Controllers.Base;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BambooCard.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthManager authManager) : CustomController
{
    /// <summary>
    /// Registers a new user with the "User" role.
    /// </summary>
    [HttpPost("register")]
    public async Task<ActionResult<ResponseResult<bool>>> Register([FromBody] RegisterDto model)
    {
        return Ok(await authManager.CreateUserAsync(model));
    }

    /// <summary>
    /// Authenticates credentials and returns access + refresh tokens.
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<ResponseResult<AuthResponseDto>>> Login([FromBody] LoginDto model)
    {
        return Ok(await authManager.LoginAsync(model));
    }

    /// <summary>
    /// Accepts only the refresh token string, returns new access + refresh pair.
    /// </summary>
    [HttpPost("refresh")]
    public async Task<ActionResult<ResponseResult<AuthResponseDto>>> Refresh([FromBody] RefreshDto model)
    {
        return Ok(await authManager.RefreshTokenAsync(model));
    }

    /// <summary>
    /// Returns logged in user details.
    /// </summary>
    [HttpGet("user")]
    [Authorize]
    public async Task<ActionResult<ResponseResult<UserDetailsDto>>> GetUser()
    {
        return Ok(await authManager.GetCurrentUserAsync(CurrentUserId));
    }

    /// <summary>
    /// Grants the Admin role to the specified user.
    /// </summary>
    [HttpPost("make-admin")]
    [Authorize(Roles = nameof(EUserRole.Admin))]
    public async Task<IActionResult> MakeAdmin([FromQuery] string email)
    {
        await authManager.MakeUserAdminAsync(email);
        return NoContent();
    }
}
