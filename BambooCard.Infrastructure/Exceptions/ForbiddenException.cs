using System.Net;

namespace BambooCard.Infrastructure.Exceptions;

public class ForbiddenException : AppException
{
    public ForbiddenException(List<string> messages)
        : base(messages, HttpStatusCode.Forbidden) { }
}
