using BambooCard.Infrastructure.Exceptions;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace BambooCard.WebAPI.Extensions;

public static class HttpContextExtensions
{
    public static string GetUserId(this HttpContext ctx)
    {
        var user = ctx.User;
        if (user?.Identity?.IsAuthenticated != true)
            throw new UnauthorizedException("User is not authenticated");

        var id = user.FindFirstValue(JwtRegisteredClaimNames.Sub)
              ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(id))
            throw new UnauthorizedException("User is not authenticated");

        return id;
    }
}
