using System.Net;

namespace BambooCard.Infrastructure.Exceptions;

public class ConflictException : AppException
{
    public ConflictException(List<string> messages)
        : base(messages, HttpStatusCode.Conflict) { }
}
