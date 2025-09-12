using DiscordBot.Interfaces;
using DiscordBot.Services;
using Microsoft.Extensions.Configuration;
using Moq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DiscordBotTests.ServiceTests
{
    [TestFixture]
    public class OpenAiServiceTests
    {
        private Mock<IHttpService> _mockHttpService = null!;
        private OpenAiService _openAiService = null!;
        private IConfiguration _configuration = null!;

        [SetUp]
        public void Setup()
        {
            _mockHttpService = new Mock<IHttpService>();
            _configuration = CreateTestConfiguration();
            _openAiService = new OpenAiService(_mockHttpService.Object, _configuration);
        }

        private static IConfiguration CreateTestConfiguration()
        {
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string?>("OpenAi:ApiKey", "test-api-key"),
                new KeyValuePair<string, string?>("OpenAi:ChatGPTApiUrl", "https://api.openai.com/v1/chat/completions"),
                new KeyValuePair<string, string?>("OpenAi:DallEApiUrl", "https://api.openai.com/v1/images/generations")
            });
            return configurationBuilder.Build();
        }

        [Test]
        public async Task ChatGptAsync_WithValidResponse_ReturnsSuccess()
        {
            // Arrange
            const string message = "Hello, how are you?";
            const string responseText = "I'm doing well, thank you!";
            const string jsonResponse = "{\"choices\": [{\"message\": {\"content\": \"" + responseText + "\"}}]}";

            _mockHttpService.Setup(x => x.GetResponseFromUrl(It.IsAny<string>(), It.IsAny<Method>(), It.IsAny<string?>(),
                It.IsAny<List<KeyValuePair<string, string>>?>(), It.IsAny<object?>()))
                .ReturnsAsync(new HttpResponse(true, jsonResponse));

            // Act
            var result = await _openAiService.ChatGptAsync(message);

            // Assert
            Assert.That(result.Item1, Is.True);
            Assert.That(result.Item2, Is.EqualTo(responseText));
        }

        [Test]
        public async Task ChatGptAsync_WithMissingConfiguration_ReturnsError()
        {
            // Arrange
            const string message = "Hello, how are you?";
            var emptyConfig = new ConfigurationBuilder().Build();
            var service = new OpenAiService(_mockHttpService.Object, emptyConfig);

            // Act
            var result = await service.ChatGptAsync(message);

            // Assert
            Assert.That(result.Item1, Is.False);
            Assert.That(result.Item2, Does.Contain("No OpenAI API key or ChatGPT API URL provided"));
        }

        [Test]
        public async Task ChatGptAsync_WithEmptyResponse_ReturnsError()
        {
            // Arrange
            const string message = "Hello, how are you?";
            const string jsonResponse = "{\"choices\": [{\"message\": {\"content\": \"\"}}]}";

            _mockHttpService.Setup(x => x.GetResponseFromUrl(It.IsAny<string>(), It.IsAny<Method>(), It.IsAny<string?>(),
                It.IsAny<List<KeyValuePair<string, string>>?>(), It.IsAny<object?>()))
                .ReturnsAsync(new HttpResponse(true, jsonResponse));

            // Act
            var result = await _openAiService.ChatGptAsync(message);

            // Assert
            Assert.That(result.Item1, Is.False);
            Assert.That(result.Item2, Is.EqualTo("Could not deserialize response from ChatGPT API!"));
        }

        [Test]
        public async Task ChatGptAsync_WithApiError_ReturnsError()
        {
            // Arrange
            const string message = "Hello, how are you?";
            const string expectedError = "API rate limit exceeded";

            _mockHttpService.Setup(x => x.GetResponseFromUrl(It.IsAny<string>(), It.IsAny<Method>(), It.IsAny<string?>(),
                It.IsAny<List<KeyValuePair<string, string>>?>(), It.IsAny<object?>()))
                .ReturnsAsync(new HttpResponse(false, expectedError));

            // Act
            var result = await _openAiService.ChatGptAsync(message);

            // Assert
            Assert.That(result.Item1, Is.False);
            Assert.That(result.Item2, Is.EqualTo(expectedError));
        }

        [Test]
        public async Task DallEAsync_WithValidResponse_ReturnsSuccess()
        {
            // Arrange
            const string message = "A beautiful landscape";
            const string imageUrl = "https://example.com/generated-image.png";
            const string jsonResponse = "{\"data\": [{\"url\": \"" + imageUrl + "\"}]}";

            _mockHttpService.Setup(x => x.GetResponseFromUrl(It.IsAny<string>(), It.IsAny<Method>(), It.IsAny<string?>(),
                It.IsAny<List<KeyValuePair<string, string>>?>(), It.IsAny<object?>()))
                .ReturnsAsync(new HttpResponse(true, jsonResponse));

            // Act
            var result = await _openAiService.DallEAsync(message);

            // Assert
            Assert.That(result.Item1, Is.True);
            Assert.That(result.Item2, Is.EqualTo($"Here is your generated image: {imageUrl}"));
        }

        [Test]
        public async Task DallEAsync_WithMissingConfiguration_ReturnsError()
        {
            // Arrange
            const string message = "A beautiful landscape";
            var emptyConfig = new ConfigurationBuilder().Build();
            var service = new OpenAiService(_mockHttpService.Object, emptyConfig);

            // Act
            var result = await service.DallEAsync(message);

            // Assert
            Assert.That(result.Item1, Is.False);
            Assert.That(result.Item2, Does.Contain("No OpenAI API key or DALL-E API URL provided"));
        }

        [Test]
        public async Task DallEAsync_WithDeserializationError_ReturnsError()
        {
            // Arrange
            const string message = "A beautiful landscape";
            const string jsonResponse = "{\"data\": [{}]}";

            _mockHttpService.Setup(x => x.GetResponseFromUrl(It.IsAny<string>(), It.IsAny<Method>(), It.IsAny<string?>(),
                It.IsAny<List<KeyValuePair<string, string>>?>(), It.IsAny<object?>()))
                .ReturnsAsync(new HttpResponse(true, jsonResponse));

            // Act
            var result = await _openAiService.DallEAsync(message);

            // Assert
            Assert.That(result.Item1, Is.False);
            Assert.That(result.Item2, Is.EqualTo("Could not deserialize image URL from DALL-E API!"));
        }

        [Test]
        public async Task DallEAsync_WithApiError_ReturnsError()
        {
            // Arrange
            const string message = "A beautiful landscape";
            const string expectedError = "Content policy violation";

            _mockHttpService.Setup(x => x.GetResponseFromUrl(It.IsAny<string>(), It.IsAny<Method>(), It.IsAny<string?>(),
                It.IsAny<List<KeyValuePair<string, string>>?>(), It.IsAny<object?>()))
                .ReturnsAsync(new HttpResponse(false, expectedError));

            // Act
            var result = await _openAiService.DallEAsync(message);

            // Assert
            Assert.That(result.Item1, Is.False);
            Assert.That(result.Item2, Is.EqualTo(expectedError));
        }
    }
}
