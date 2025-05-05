using System.Net;

namespace BambooCard.Infrastructure.Results;

public class ResponseResult<T>
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public T Data { get; set; }
    public List<string> Errors { get; set; }
    public HttpStatusCode StatusCode { get; set; }

    public ResponseResult(T data, string message = "Success", HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        Success = true;
        Message = message;
        Data = data;
        StatusCode = statusCode;
        Errors = [];
    }

    public ResponseResult(string errorMessage, HttpStatusCode statusCode = HttpStatusCode.BadRequest)
    {
        Success = false;
        StatusCode = statusCode;
        Errors = [errorMessage];
    }

    public ResponseResult(List<string> errors, HttpStatusCode statusCode = HttpStatusCode.BadRequest)
    {
        Success = false;
        Message = "Request failed";
        StatusCode = statusCode;
        Errors = errors;
    }
}
