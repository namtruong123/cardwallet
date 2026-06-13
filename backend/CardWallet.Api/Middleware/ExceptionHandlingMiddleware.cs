using System.Net;
using System.Text.Json;
using CardWallet.Application.Exceptions;
using MySqlConnector;

namespace CardWallet.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred.");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = HttpStatusCode.InternalServerError;
        var response = new ErrorResponse
        {
            Message = "Đã xảy ra lỗi nội bộ. Vui lòng thử lại sau."
        };

        if (exception is AppException appException)
        {
            statusCode = (HttpStatusCode)appException.StatusCode;
            response.Message = appException.Message;

            if (appException is BadRequestException badRequest && badRequest.Errors != null)
            {
                response.Errors = badRequest.Errors;
            }
        }
        else if (IsDatabaseConnectionException(exception))
        {
            statusCode = HttpStatusCode.ServiceUnavailable;
            response.Message = "Không kết nối được MySQL. Vui lòng kiểm tra database/connection string rồi thử đăng nhập lại.";
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        return context.Response.WriteAsync(json);
    }

    private static bool IsDatabaseConnectionException(Exception exception)
    {
        for (var current = exception; current != null; current = current.InnerException!)
        {
            if (current is MySqlException)
                return true;
        }

        return false;
    }
}

public sealed class ErrorResponse
{
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, string[]>? Errors { get; set; }
}
