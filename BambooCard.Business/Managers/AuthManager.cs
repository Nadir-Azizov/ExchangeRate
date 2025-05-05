using BambooCard.Business.Abstractions;
using BambooCard.Business.Extensions;
using BambooCard.Business.Models.User;
using BambooCard.Domain.DbContext;
using BambooCard.Domain.Entities.User;
using BambooCard.Domain.Settings;
using BambooCard.Infrastructure.Enums;
using BambooCard.Infrastructure.Exceptions;
using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BambooCard.Business.Managers;

public class AuthManager(
    UserManager<AppUser> userManager,
    RoleManager<AppRole> roleManager,
    IOptions<JwtSettings> jwtSettings,
    BambooCardDbContext db) : IAuthManager
{
    private readonly string refreshProvider = "RefreshTokenProvider";
    private readonly string refreshPurpose = "RefreshToken";

    public async Task<bool> CreateUserAsync(RegisterDto model)
    {
        var user = model.Adapt<AppUser>();

        var result = await userManager.CreateAsync(user, model.Password);
        result.ThrowIfFailed();

        await SetRoleToUser(user, EUserRole.User);

        return result.Succeeded;
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto model)
    {
        var user = await userManager.FindByEmailAsync(model.Email)
            ?? throw new UnauthorizedException("Invalid credentials");

        if (!await userManager.CheckPasswordAsync(user, model.Password))
            throw new UnauthorizedAccessException("Invalid credentials");

        var access = GenerateJwtToken(user);
        var refresh = await GenerateRefreshTokenAsync(user);

        return new AuthResponseDto
        {
            AccessToken = access,
            RefreshToken = refresh,
            ExpiresAt = DateTime.UtcNow.AddMinutes(jwtSettings.Value.SessionLifeTime)
        };
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(RefreshDto model)
    {
        var tokenEntry = await db.Set<IdentityUserToken<int>>()
            .SingleOrDefaultAsync(t =>
                t.LoginProvider == refreshProvider &&
                t.Name == refreshPurpose &&
                t.Value == model.RefreshToken)
            ?? throw new UnauthorizedException("Refresh token not recognized");

        var user = await userManager.FindByIdAsync(tokenEntry.UserId.ToString())
            ?? throw new UnauthorizedException("Invalid user");

        var valid = await userManager.VerifyUserTokenAsync(
            user, refreshProvider, refreshPurpose, model.RefreshToken);

        if (!valid)
            throw new UnauthorizedException("Expired or invalid refresh token");

        await userManager.RemoveAuthenticationTokenAsync(
            user,
            refreshProvider,
            refreshPurpose);

        var newJwt = GenerateJwtToken(user);
        var newRefresh = await GenerateRefreshTokenAsync(user);

        return new AuthResponseDto
        {
            AccessToken = newJwt,
            RefreshToken = newRefresh,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(jwtSettings.Value.SessionLifeTime)
        };
    }

    public async Task<UserDetailsDto> GetCurrentUserAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId)
            ?? throw new BadRequestException("User not found");

        var response = user.Adapt<UserDetailsDto>();
        response.Roles = await userManager.GetRolesAsync(user);

        return response;
    }

    public async Task MakeUserAdminAsync(string email)
    {
        var user = await userManager.FindByEmailAsync(email)
            ?? throw new NotFoundException($"User with email '{email}' not found.");

        await SetRoleToUser(user, EUserRole.Admin);
    }

    private async Task SetRoleToUser(AppUser user, EUserRole role)
    {
        if (!await roleManager.RoleExistsAsync(role.ToString()))
            await roleManager.CreateAsync(new() { Name = role.ToString() });

        var result = await userManager.AddToRoleAsync(user, role.ToString());
        result.ThrowIfFailed();
    }

    private async Task<string> GenerateRefreshTokenAsync(AppUser user)
    {
        var refreshToken = await userManager.GenerateUserTokenAsync(
            user,
            refreshProvider,
            refreshPurpose);

        await userManager.SetAuthenticationTokenAsync(
            user,
            loginProvider: refreshProvider,
            tokenName: refreshPurpose,
            tokenValue: refreshToken);

        return refreshToken;
    }

    private string GenerateJwtToken(AppUser user)
    {
        var keyBytes = Encoding.UTF8.GetBytes(jwtSettings.Value.SecurityKey);
        var creds = new SigningCredentials(
                           new SymmetricSecurityKey(keyBytes),
                           SecurityAlgorithms.HmacSha256);

        var roles = userManager.GetRolesAsync(user).Result;
        var claims = new List<Claim> {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var lifetime = TimeSpan.FromMinutes(jwtSettings.Value.SessionLifeTime);
        var expiresAt = DateTime.UtcNow.Add(lifetime);

        var token = new JwtSecurityToken(
            issuer: jwtSettings.Value.Issuer,
            audience: jwtSettings.Value.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
