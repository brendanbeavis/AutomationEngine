using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http;
using System.Text;
using AutomationEngine.Middleware;
using AutomationEngine.Options;
using AutomationEngine.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace AutomationEngine.Tests.Unit
{
    public class ConfigurationOptionsTests
    {
        [Fact]
        public void ServerOptions_WithInvalidPort_FailsValidation()
        {
            var options = new ServerOptions { Port = 70000 };

            var validationErrors = Validate(options);

            validationErrors.Should().NotBeEmpty();
        }

        [Fact]
        public void ResilienceOptions_WithInvalidRetryAttempts_FailsValidation()
        {
            var options = new ResilienceOptions { RetryAttempts = -1 };

            var validationErrors = Validate(options);

            validationErrors.Should().NotBeEmpty();
        }

        [Fact]
        public void SchedulerOptions_WithInvalidLoopDelay_FailsValidation()
        {
            var options = new SchedulerOptions { LoopDelaySeconds = 0 };

            var validationErrors = Validate(options);

            validationErrors.Should().NotBeEmpty();
        }

        [Fact]
        public void DesignTimeDatabaseOptions_WithMissingFilePath_FailsValidation()
        {
            var options = new DesignTimeDatabaseOptions { FilePath = string.Empty };

            var validationErrors = Validate(options);

            validationErrors.Should().NotBeEmpty();
        }

        [Fact]
        public async Task JobApiClient_GetJobHistoryAsync_UsesConfiguredHistoryLimit()
        {
            var handler = new RecordingHttpMessageHandler();
            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("http://localhost:5001/")
            };

            IOptions<JobApiOptions> options = new OptionsWrapper<JobApiOptions>(new JobApiOptions { HistoryLimit = 123 });
            var client = new JobApiClient(httpClient, Mock.Of<ILogger<JobApiClient>>(), options);

            await client.GetJobHistoryAsync("job-1");

            handler.LastRequest.Should().NotBeNull();
            handler.LastRequest!.RequestUri!.Query.Should().Contain("limit=123");
        }

        [Fact]
        public async Task LocalhostOnlyMiddleware_WhenDisabled_AllowsNonLocalhostRequest()
        {
            var wasNextCalled = false;
            var middleware = new LocalhostOnlyMiddleware(
                _ =>
                {
                    wasNextCalled = true;
                    return Task.CompletedTask;
                },
                Mock.Of<ILogger<LocalhostOnlyMiddleware>>(),
                new OptionsWrapper<SecurityOptions>(new SecurityOptions { LocalhostOnly = false }));

            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = IPAddress.Parse("192.168.1.100");

            await middleware.InvokeAsync(context);

            wasNextCalled.Should().BeTrue();
        }

        private static List<ValidationResult> Validate<T>(T options)
        {
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(options!, new ValidationContext(options!), results, validateAllProperties: true);
            return results;
        }

        private sealed class RecordingHttpMessageHandler : HttpMessageHandler
        {
            public HttpRequestMessage? LastRequest { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                LastRequest = request;
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("[]", Encoding.UTF8, "application/json")
                };

                return Task.FromResult(response);
            }
        }
    }
}
