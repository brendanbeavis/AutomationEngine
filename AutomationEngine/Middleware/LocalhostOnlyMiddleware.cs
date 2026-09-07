using System;
using System.Net;
using System.Threading.Tasks;
using AutomationEngine.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AutomationEngine.Middleware
{
    /// <summary>
    /// Middleware to enforce localhost-only access. Rejects any requests from non-loopback addresses.
    /// This is a defense-in-depth measure for a single-machine, trusted environment application.
    /// </summary>
    public class LocalhostOnlyMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<LocalhostOnlyMiddleware> _logger;
        private readonly SecurityOptions _securityOptions;

        public LocalhostOnlyMiddleware(
            RequestDelegate next,
            ILogger<LocalhostOnlyMiddleware> logger,
            IOptions<SecurityOptions> securityOptions)
        {
            _next = next;
            _logger = logger;
            _securityOptions = securityOptions.Value;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Get the remote IP address
            var remoteIp = context.Connection.RemoteIpAddress;

            if (!_securityOptions.LocalhostOnly)
            {
                await _next(context);
                return;
            }

            // Check if the request is from localhost
            bool isLocalhost = remoteIp is not null && IPAddress.IsLoopback(remoteIp);

            if (!isLocalhost)
            {
                _logger.LogWarning(
                    "Rejected non-localhost request from {RemoteIp} to {Path}. " +
                    "This application is configured for localhost-only access.",
                    remoteIp?.ToString() ?? "unknown",
                    context.Request.Path);

                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Access Denied",
                    message = "This application is configured for localhost-only access.",
                    details = "To use this application, please access it from the local machine."
                });
                return;
            }

            // Request is from localhost, proceed normally
            await _next(context);
        }
    }
}
