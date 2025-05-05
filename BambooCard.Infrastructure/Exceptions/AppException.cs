using System.Net;

namespace BambooCard.Infrastructure.Exceptions;

public class AppException : Exception
{
    public HttpStatusCode StatusCode { get; }

    public AppException(List<string> messages, HttpStatusCode statusCode = HttpStatusCode.BadRequest)
        : base(string.Join("; ", messages))
    {
        StatusCode = statusCode;
    }
}