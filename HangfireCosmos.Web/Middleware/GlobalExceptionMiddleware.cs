using System.Net;
using System.Text.Json;
using Microsoft.Azure.Cosmos;

namespace HangfireCosmos.Web.Middleware;

/// <summary>
/// Global exception handling middleware for comprehensive error handling and logging
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public GlobalExceptionMiddleware(
        RequestDelegate next, 
        ILogger<GlobalExceptionMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred during request processing");
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = context.Response;
        response.ContentType = "application/json";

        var errorResponse = new ErrorResponse
        {
            TraceId = context.TraceIdentifier,
            Timestamp = DateTime.UtcNow,
            Path = context.Request.Path,
            Method = context.Request.Method
        };

        switch (exception)
        {
            case CosmosException cosmosEx:
                response.StatusCode = (int)cosmosEx.StatusCode;
                errorResponse.Error = "Cosmos DB Error";
                errorResponse.Message = cosmosEx.Message;
                errorResponse.Details = new
                {
                    cosmosEx.StatusCode,
                    cosmosEx.SubStatusCode,
                    cosmosEx.ActivityId,
                    cosmosEx.RequestCharge,
                    cosmosEx.RetryAfter
                };
                break;

            case ArgumentNullException nullEx:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse.Error = "Missing Required Parameter";
                errorResponse.Message = nullEx.Message;
                if (_environment.IsDevelopment())
                {
                    errorResponse.Details = new { nullEx.ParamName };
                }
                break;

            case ArgumentException argEx:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse.Error = "Invalid Argument";
                errorResponse.Message = argEx.Message;
                if (_environment.IsDevelopment())
                {
                    errorResponse.Details = new { argEx.ParamName };
                }
                break;

            case NotImplementedException:
                response.StatusCode = (int)HttpStatusCode.NotImplemented;
                errorResponse.Error = "Not Implemented";
                errorResponse.Message = "This feature is not yet implemented";
                break;

            case TimeoutException timeoutEx:
                response.StatusCode = (int)HttpStatusCode.RequestTimeout;
                errorResponse.Error = "Request Timeout";
                errorResponse.Message = timeoutEx.Message;
                break;

            case InvalidOperationException invalidOpEx:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse.Error = "Invalid Operation";
                errorResponse.Message = invalidOpEx.Message;
                break;

            case FileNotFoundException fileNotFoundEx:
                response.StatusCode = (int)HttpStatusCode.NotFound;
                errorResponse.Error = "File Not Found";
                errorResponse.Message = fileNotFoundEx.Message;
                if (_environment.IsDevelopment())
                {
                    errorResponse.Details = new { fileNotFoundEx.FileName };
                }
                break;

            case DirectoryNotFoundException dirNotFoundEx:
                response.StatusCode = (int)HttpStatusCode.NotFound;
                errorResponse.Error = "Directory Not Found";
                errorResponse.Message = dirNotFoundEx.Message;
                break;

            case UnauthorizedAccessException:
                response.StatusCode = (int)HttpStatusCode.Forbidden;
                errorResponse.Error = "Access Forbidden";
                errorResponse.Message = "You don't have permission to access this resource";
                break;

            case TaskCanceledException canceledEx when canceledEx.InnerException is TimeoutException:
                response.StatusCode = (int)HttpStatusCode.RequestTimeout;
                errorResponse.Error = "Request Timeout";
                errorResponse.Message = "The request timed out";
                break;

            case TaskCanceledException:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse.Error = "Request Canceled";
                errorResponse.Message = "The request was canceled";
                break;

            case OutOfMemoryException:
                response.StatusCode = (int)HttpStatusCode.InsufficientStorage;
                errorResponse.Error = "Out of Memory";
                errorResponse.Message = "The server is out of memory";
                break;

            case StackOverflowException:
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                errorResponse.Error = "Stack Overflow";
                errorResponse.Message = "A stack overflow occurred";
                break;

            default:
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                errorResponse.Error = "Internal Server Error";
                errorResponse.Message = _environment.IsDevelopment() 
                    ? exception.Message 
                    : "An unexpected error occurred";
                break;
        }

        // Add development-specific details
        if (_environment.IsDevelopment())
        {
            errorResponse.DeveloperInfo = new
            {
                ExceptionType = exception.GetType().Name,
                StackTrace = exception.StackTrace,
                InnerException = exception.InnerException?.Message,
                Data = exception.Data.Count > 0 ? exception.Data : null
            };
        }

        // Log the error with appropriate level
        var logLevel = response.StatusCode >= 500 ? LogLevel.Error : LogLevel.Warning;
        _logger.Log(logLevel, exception, 
            "HTTP {StatusCode} error occurred for {Method} {Path}. TraceId: {TraceId}", 
            response.StatusCode, context.Request.Method, context.Request.Path, context.TraceIdentifier);

        var jsonResponse = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = _environment.IsDevelopment()
        });

        await response.WriteAsync(jsonResponse);
    }
}

/// <summary>
/// Standard error response model
/// </summary>
public class ErrorResponse
{
    public string Error { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string TraceId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Path { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public object? Details { get; set; }
    public object? DeveloperInfo { get; set; }
}

/// <summary>
/// Extension methods for registering the global exception middleware
/// </summary>
public static class GlobalExceptionMiddlewareExtensions
{
    /// <summary>
    /// Adds the global exception handling middleware to the pipeline
    /// </summary>
    public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<GlobalExceptionMiddleware>();
    }
}