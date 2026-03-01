using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ŠišAppApi.Filters;

public class ExceptionFilter : ExceptionFilterAttribute
{
    private readonly ILogger<ExceptionFilter> _logger;

    public ExceptionFilter(ILogger<ExceptionFilter> logger)
    {
        _logger = logger;
    }

    public override void OnException(ExceptionContext context)
    {
        if (context.Exception is UserException userException)
        {
            context.Result = new BadRequestObjectResult(new { userError = userException.Message });
            context.ExceptionHandled = true;
        }
        else
        {
            _logger.LogError(context.Exception, "Server side error");
            context.Result = new ObjectResult(new { ERROR = "Server side error" })
            {
                StatusCode = 500
            };
            context.ExceptionHandled = true;
        }
    }
}
