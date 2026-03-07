using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ŠišAppApi.Data;
using ŠišAppApi.Models;
using System.Security.Claims;

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
        var exception = context.Exception;
        var userIdString = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        int? userId = int.TryParse(userIdString, out int id) ? id : null;

        // Security logging
        if (exception is UnauthorizedAccessException || 
            (exception is UserException ue && (ue.Message.Contains("nema pravo") || ue.Message.Contains("zakazane"))))
        {
            var dbContext = context.HttpContext.RequestServices.GetService<ApplicationDbContext>();
            if (dbContext != null && userId.HasValue)
            {
                var admin = dbContext.Admins.FirstOrDefault(a => a.UserId == userId.Value);
                if (admin != null)
                {
                    dbContext.AdminLogs.Add(new AdminLog
                    {
                        AdminId = admin.Id,
                        Action = "Security Violation",
                        EntityType = "Security",
                        Notes = $"Odbijen pristup: {exception.Message}",
                        IpAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString(),
                        CreatedAt = DateTime.UtcNow
                    });
                    dbContext.SaveChanges();
                }
            }
        }

        if (exception is UnauthorizedAccessException)
        {
            context.Result = new ObjectResult(new { userError = "Nemate pravo pristupa ovoj akciji." }) { StatusCode = 403 };
            context.ExceptionHandled = true;
        }
        else if (exception is UserException userException)
        {
            context.Result = new BadRequestObjectResult(new { userError = userException.Message });
            context.ExceptionHandled = true;
        }
        else
        {
            _logger.LogError(exception, "Server side error");
            context.Result = new ObjectResult(new { ERROR = "Server side error" }) { StatusCode = 500 };
            context.ExceptionHandled = true;
        }
    }
}
