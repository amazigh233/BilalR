using Booking.Api.Contracts.Common;
using Microsoft.AspNetCore.Mvc;

namespace Booking.Api.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected ActionResult HandleKnownException(Exception exception)
    {
        return exception switch
        {
            KeyNotFoundException => NotFound(ToError(exception.Message)),
            ArgumentException => BadRequest(ToError(exception.Message)),
            InvalidOperationException => BadRequest(ToError(exception.Message)),
            _ => Problem("An unexpected error occurred.")
        };
    }

    protected static ApiErrorResponse ToError(string message)
    {
        return new ApiErrorResponse(message);
    }
}
