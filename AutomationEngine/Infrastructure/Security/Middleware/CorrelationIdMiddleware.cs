using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace AutomationEngine.Infrastructure.Security.Middleware
{
    /// <summary>
    /// Middleware that adds correlation ID to all HTTP requests for distributed tracing.
    /// Enables end-to-end request tracking across logs, services, and async operations.
    /// </summary>
    /// <remarks>
    /// Correlation ID flow:
    /// 1. Checks for existing correlation-id header in the request
    /// 2. If not found, generates a new one using a format: {machine-name}-{timestamp}-{random}
    /// 3. Stores it in LogContext for Serilog enrichment
    /// 4. Adds it to response headers for client-side tracing
    /// 5. Makes it available via HttpContext.Items for middleware/handler access
    /// </remarks>
    public class CorrelationIdMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<CorrelationIdMiddleware> _logger;
        private const string CorrelationIdHeader = "X-Correlation-ID";
        private const string CorrelationIdKey = "CorrelationId";

        public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Try to get existing correlation ID from request header
            var correlationId = context.Request.Headers.TryGetValue(CorrelationIdHeader, out var headerValue)
                ? headerValue.ToString()
                : null;

            // If no correlation ID exists, generate a new one
            if (string.IsNullOrEmpty(correlationId))
            {
                correlationId = GenerateCorrelationId();
            }

            // Store in HttpContext for downstream access
            context.Items[CorrelationIdKey] = correlationId;

            // Add to response headers for client tracing
            context.Response.Headers.Add(CorrelationIdHeader, correlationId);

            // Push to LogContext so Serilog enriches all logs for this request
            using (LogContext.PushProperty("CorrelationId", correlationId))
            using (LogContext.PushProperty("RequestPath", context.Request.Path))
            using (LogContext.PushProperty("RequestMethod", context.Request.Method))
            {
                _logger.LogDebug(
                    "Request started | CorrelationId: {CorrelationId} | Method: {Method} | Path: {Path}",
                    correlationId, context.Request.Method, context.Request.Path);

                try
                {
                    await _next(context);

                    _logger.LogDebug(
                        "Request completed | CorrelationId: {CorrelationId} | StatusCode: {StatusCode}",
                        correlationId, context.Response.StatusCode);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Request failed | CorrelationId: {CorrelationId} | Method: {Method} | Path: {Path}",
                        correlationId, context.Request.Method, context.Request.Path);
                    throw;
                }
            }
        }

        /// <summary>
        /// Generates a unique correlation ID combining machine name, timestamp, and random component.
        /// Format: {MachineName}-{UtcTicks}-{RandomPart}
        /// </summary>
        private static string GenerateCorrelationId()
        {
            var machine = Environment.MachineName.Substring(0, Math.Min(5, Environment.MachineName.Length));
            var timestamp = DateTimeOffset.UtcNow.Ticks;
            var random = Random.Shared.Next(10000, 99999);
            return $"{machine}-{timestamp}-{random}";
        }
    }

    /// <summary>
    /// Extension methods for CorrelationIdMiddleware registration
    /// </summary>
    public static class CorrelationIdMiddlewareExtensions
    {
        /// <summary>
        /// Adds CorrelationIdMiddleware to the request pipeline
        /// Should be added early in the middleware chain, before other middleware
        /// </summary>
        public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
        {
            return app.UseMiddleware<CorrelationIdMiddleware>();
        }

        /// <summary>
        /// Retrieves the correlation ID from HttpContext
        /// </summary>
        public static string? GetCorrelationId(this HttpContext context)
        {
            if (context.Items.TryGetValue("CorrelationId", out var correlationId))
            {
                return correlationId as string;
            }

            return null;
        }
    }
}
