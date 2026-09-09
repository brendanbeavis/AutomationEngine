using System.Net;
using System.Net.Http;
using AutomationEngine.Dto;
using AutomationEngine.Services;
using AutomationEngine.Application.Abstractions;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AutomationEngine.Tests.Unit
{
    public class NtfyNotificationServiceTests
    {
        [Fact]
        public async Task SendSuccessNotificationAsync_WithConfiguredEndpoint_SendsSucceededMessage()
        {
            // Arrange
            var handler = new RecordingHttpMessageHandler();
            var httpClient = new HttpClient(handler);
            var settingsService = new Mock<ISettingsService>();
            settingsService.Setup(s => s.GetSettingsAsync()).ReturnsAsync(new AppSettingsDto
            {
                NtfyEndpoint = "https://ntfy.example.com/topic"
            });

            var service = new NtfyNotificationService(
                settingsService.Object,
                httpClient,
                Mock.Of<ILogger<NtfyNotificationService>>());

            // Act
            await service.SendSuccessNotificationAsync("Nightly Import", "Processed 42 records");

            // Assert
            handler.Request.Should().NotBeNull();
            handler.Request!.RequestUri.Should().Be(new Uri("https://ntfy.example.com/topic"));
            handler.Request.Headers.GetValues("Title").Should().ContainSingle().Which.Should().Be("[AE] Job Success: Nightly Import");
            handler.Request.Headers.GetValues("Tags").Should().ContainSingle().Which.Should().Be("white_check_mark");

            handler.RequestBody.Should().NotBeNullOrEmpty();
            handler.RequestBody.Should().Contain("## Job Succeeded");
            handler.RequestBody.Should().Contain("Nightly Import");
            handler.RequestBody.Should().Contain("Processed 42 records");
        }

        [Fact]
        public async Task SendSuccessNotificationAsync_WithoutConfiguredEndpoint_DoesNotSendRequest()
        {
            // Arrange
            var handler = new RecordingHttpMessageHandler();
            var httpClient = new HttpClient(handler);
            var settingsService = new Mock<ISettingsService>();
            settingsService.Setup(s => s.GetSettingsAsync()).ReturnsAsync(new AppSettingsDto
            {
                NtfyEndpoint = null
            });

            var service = new NtfyNotificationService(
                settingsService.Object,
                httpClient,
                Mock.Of<ILogger<NtfyNotificationService>>());

            // Act
            await service.SendSuccessNotificationAsync("Nightly Import", "Processed 42 records");

            // Assert
            handler.Request.Should().BeNull();
        }

        private sealed class RecordingHttpMessageHandler : HttpMessageHandler
        {
            public HttpRequestMessage? Request { get; private set; }
            public string? RequestBody { get; private set; }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Request = request;

                // Read content before it's disposed
                if (request.Content is not null)
                {
                    RequestBody = await request.Content.ReadAsStringAsync(cancellationToken);
                }

                return new HttpResponseMessage(HttpStatusCode.OK);
            }
        }
    }
}

