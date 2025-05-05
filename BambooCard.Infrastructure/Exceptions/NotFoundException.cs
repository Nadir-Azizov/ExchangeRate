using System.Net;

namespace BambooCard.Infrastructure.Exceptions;

public class NotFoundException : AppException
{
    public NotFoundException(string message)
        : base([message], HttpStatusCode.NotFound) { }

    public NotFoundException(IEnumerable<string> messages)
        : base(messages.ToList(), HttpStatusCode.NotFound) { }
}
