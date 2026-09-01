using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using AutomationEngine.Options;
using System.Threading.Tasks;

namespace AutomationEngine.Middleware
{
    /// <summary>
    /// Middleware to validate optional admin secret for programmatic API access.
    /// This middleware is optional and only enforced if AdminSecret is configured in appsettings.
    /// When enabled, requires X-Admin-Secret header to match the configured secret.
    /// 
    /// Use case: Allow external scripts/tools to call APIs programmatically while maintaining
    /// localhost-only restriction. Disabled by default (AdminSecret: null in appsettings.json).
    /// </summary>
    public class AdminSecretValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AdminSecretValidationMiddleware> _logger;
        private readonly string? _adminSecret;

        public AdminSecretValidationMiddleware(RequestDelegate next, ILogger<AdminSecretValidationMiddleware> logger, ISecurityOptions securityOptions)
        {
            _next = next;
            _logger = logger;
            _adminSecret = securityOptions.AdminSecret;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // If admin secret is not configured, allow all requests (default, UI-only mode)
            if (string.IsNullOrWhiteSpace(_adminSecret))
            {
                await _next(context);
                return;
            }

            // Admin secret is configured - validate it for API requests (not UI/static)
            var path = context.Request.Path.ToString().ToLower();
            bool isApiRequest = path.StartsWith("/api/") || path.StartsWith("/hubs/");

            if (!isApiRequest)
            {
                // UI requests (Razor Components, static files) don't need the secret
                await _next(context);
                return;
            }

            // For API requests, validate the X-Admin-Secret header
            if (!context.Request.Headers.TryGetValue("X-Admin-Secret", out var headerValue))
            {
                _logger.LogWarning("API request from {RemoteIp} missing X-Admin-Secret header. Path: {Path}", 
                    context.Connection.RemoteIpAddress, path);

                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Unauthorized",
                    message = "X-Admin-Secret header is required for API access"
                });
                return;
            }

            // Validate the secret matches
            if (!headerValue.ToString().Equals(_adminSecret, StringComparison.Ordinal))
            {
                _logger.LogWarning("API request from {RemoteIp} provided invalid X-Admin-Secret. Path: {Path}", 
                    context.Connection.RemoteIpAddress, path);

                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Unauthorized",
                    message = "Invalid X-Admin-Secret"
                });
                return;
            }

            // Secret is valid, proceed
            _logger.LogInformation("API request from {RemoteIp} authenticated via admin secret. Path: {Path}", 
                context.Connection.RemoteIpAddress, path);

            await _next(context);
        }
    }
}
