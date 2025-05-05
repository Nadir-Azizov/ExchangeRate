using BambooCard.Infrastructure.Exceptions;
using Microsoft.AspNetCore.Identity;

namespace BambooCard.Business.Extensions;

public static class IdentityExtensions
{
    public static void ThrowIfFailed(this IdentityResult identityResult)
    {
        if (!identityResult.Succeeded)
        {
            var errors = identityResult.Errors.Select(e => e.Description);
            throw new BadRequestException(errors);
        }
    }
}
