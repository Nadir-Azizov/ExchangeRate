using BambooCard.Infrastructure.Exceptions;
using BambooCard.Infrastructure.Results;
using System.Net;
using System.Text.Json;

namespace BambooCard.WebAPI.Middlewares;

public class ExceptionHandlerMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (NotFoundException ex)
        {
            await HandleExceptionAsync(context, ex, HttpStatusCode.NotFound);
        }
        catch (BadRequestException ex)
        {
            await HandleExceptionAsync(context, ex, HttpStatusCode.BadRequest);
        }
        catch (UnauthorizedException ex)
        {
            await HandleExceptionAsync(context, ex, HttpStatusCode.Unauthorized);
        }
        catch (InternalServerException ex)
        {
            await HandleExceptionAsync(context, ex, HttpStatusCode.InternalServerError);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex, HttpStatusCode.InternalServerError);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception, HttpStatusCode statusCode)
    {
        var responseResult = new ResponseResult<object>(
            exception.Message,
            statusCode
        );

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var jsonResponse = JsonSerializer.Serialize(responseResult);

        return context.Response.WriteAsync(jsonResponse);
    }
}