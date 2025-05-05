using System.Net;

namespace BambooCard.Infrastructure.Exceptions;

public class UnauthorizedException : AppException
{
    public UnauthorizedException(string message)
        : base([message], HttpStatusCode.Unauthorized) { }

    public UnauthorizedException(IEnumerable<string> messages)
        : base(messages.ToList(), HttpStatusCode.Unauthorized) { }
}
