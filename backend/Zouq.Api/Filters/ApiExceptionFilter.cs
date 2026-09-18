using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Zouq.Domain.Common;

namespace Zouq.Api.Filters;

public sealed class ApiExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        var (status, message) = context.Exception switch
        {
            UnauthorizedAccessException => (403, context.Exception.Message),
            InvalidOperationException => (400, context.Exception.Message),
            KeyNotFoundException => (404, context.Exception.Message),
            _ => (500, "An unexpected error occurred.")
        };

        // Don't leak internals in production for 500s
        if (status == 500 && context.HttpContext.RequestServices
                .GetService(typeof(IHostEnvironment)) is IHostEnvironment env && env.IsDevelopment())
            message = context.Exception.Message;

        context.Result = new ObjectResult(ApiResponse<object>.Fail(message)) { StatusCode = status };
        context.ExceptionHandled = true;
    }
}
