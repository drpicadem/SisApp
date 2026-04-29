using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
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

    public override async Task OnExceptionAsync(ExceptionContext context)
    {
        var exception = context.Exception;
        var request = context.HttpContext.Request;
        var traceId = context.HttpContext.TraceIdentifier;
        var method = request.Method;
        var path = request.Path.HasValue ? request.Path.Value : string.Empty;
        var query = request.QueryString.HasValue ? request.QueryString.Value : string.Empty;
        var ipAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = request.Headers.UserAgent.ToString();
        var correlationId = request.Headers["X-Correlation-Id"].ToString();

        var userIdString = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var username = context.HttpContext.User.FindFirst(ClaimTypes.Name)?.Value;
        int? userId = int.TryParse(userIdString, out int id) ? id : null;

        using var scope = _logger.BeginScope(new Dictionary<string, object?>
        {
            ["TraceId"] = traceId,
            ["Method"] = method,
            ["Path"] = path,
            ["Query"] = query,
            ["UserId"] = userId,
            ["Username"] = username,
            ["IpAddress"] = ipAddress,
            ["UserAgent"] = userAgent,
            ["CorrelationId"] = correlationId
        });


        if (exception is UnauthorizedAccessException ||
            (exception is UserException ue && (ue.Message.Contains("nema pravo") || ue.Message.Contains("zakazane"))))
        {
            var dbContext = context.HttpContext.RequestServices.GetService<ApplicationDbContext>();
            if (dbContext != null && userId.HasValue)
            {
                var admin = await dbContext.Admins.FirstOrDefaultAsync(a => a.UserId == userId.Value);
                if (admin != null)
                {
                    dbContext.AdminLogs.Add(new AdminLog
                    {
                        AdminId = admin.Id,
                        Action = "Security Violation",
                        EntityType = "Security",
                        Notes = $"Odbijen pristup: {exception.Message}; TraceId: {traceId}; Ruta: {method} {path}{query}",
                        IpAddress = ipAddress,
                        CreatedAt = DateTime.UtcNow
                    });
                    await dbContext.SaveChangesAsync();
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
        else if (exception is BusinessException businessException)
        {
            context.Result = new BadRequestObjectResult(new { userError = businessException.Message });
            context.ExceptionHandled = true;
        }
        else if (exception is NotFoundException notFoundException)
        {
            context.Result = new NotFoundObjectResult(new { userError = notFoundException.Message });
            context.ExceptionHandled = true;
        }
        else
        {
            _logger.LogError(
                exception,
                "Server side error. TraceId: {TraceId}. Request: {Method} {Path}{Query}. UserId: {UserId}",
                traceId,
                method,
                path,
                query,
                userId);
            context.Result = new ObjectResult(new
            {
                ERROR = "Server side error",
                TraceId = traceId
            })
            { StatusCode = 500 };
            context.ExceptionHandled = true;
        }
    }
}
