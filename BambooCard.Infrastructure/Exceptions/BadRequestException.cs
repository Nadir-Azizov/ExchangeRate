using System.Net;

namespace BambooCard.Infrastructure.Exceptions;

public class BadRequestException : AppException
{
    public BadRequestException(IEnumerable<string> messages)
        : base(messages.ToList(), HttpStatusCode.BadRequest) { }

    public BadRequestException(string message)
        : base([message], HttpStatusCode.BadRequest) { }
}