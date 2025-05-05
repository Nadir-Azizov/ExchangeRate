using System.Net;

namespace BambooCard.Infrastructure.Exceptions;

public class InternalServerException : AppException
{
    public InternalServerException(string message)
        : base([message], HttpStatusCode.InternalServerError) { }

    public InternalServerException(List<string> messages)
        : base(messages, HttpStatusCode.InternalServerError) { }
}
