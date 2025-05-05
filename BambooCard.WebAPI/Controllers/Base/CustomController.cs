using BambooCard.Infrastructure.Exceptions;
using BambooCard.Infrastructure.Results;
using BambooCard.WebAPI.Extensions;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace BambooCard.WebAPI.Controllers.Base;

public class CustomController : ControllerBase
{
    protected string CurrentUserId => HttpContext.GetUserId();

    protected ActionResult<ResponseResult<T>> HandleException<T>(Exception ex)
    {
        var response = ex switch
        {
            NotFoundException => new ResponseResult<T>(ex.Message, HttpStatusCode.NotFound),
            BadRequestException => new ResponseResult<T>(ex.Message, HttpStatusCode.BadRequest),
            _ => new ResponseResult<T>("An unexpected error occurred.", HttpStatusCode.InternalServerError)
        };

        return StatusCode((int)response.StatusCode, response);
    }

    protected ActionResult<ResponseResult<T>> Ok<T>(T data, string message = "Success")
    {
        var response = new ResponseResult<T>(data, message);
        return base.Ok(response);
    }

    protected ActionResult<ResponseResult<T>> NotFound<T>(string errorMessage)
    {
        var response = new ResponseResult<T>(errorMessage, HttpStatusCode.NotFound);
        return base.NotFound(response);
    }

    protected ActionResult<ResponseResult<T>> BadRequest<T>(string errorMessage)
    {
        var response = new ResponseResult<T>(errorMessage, HttpStatusCode.BadRequest);
        return base.BadRequest(response);
    }

    protected ActionResult<ResponseResult<T>> Unauthorized<T>(string errorMessage)
    {
        var response = new ResponseResult<T>(errorMessage, HttpStatusCode.Unauthorized);
        return base.Unauthorized(response);
    }

    protected ActionResult<ResponseResult<T>> InternalServerErrorResponse<T>(string errorMessage)
    {
        var response = new ResponseResult<T>(errorMessage, HttpStatusCode.InternalServerError);
        return base.StatusCode((int)HttpStatusCode.InternalServerError, response);
    }
}