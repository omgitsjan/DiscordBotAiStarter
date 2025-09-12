using DiscordBot.Interfaces;
using DiscordBot.Services;
using Microsoft.Extensions.Configuration;
using Moq;
using Newtonsoft.Json;
using RestSharp;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DiscordBotTests.ServiceTests
{
    [TestFixture]
    public class Watch2GetherServiceTests
    {
        private Mock<IHttpService> _mockHttpService = null!;
        private Watch2GetherService _watch2GetherService = null!;
        private IConfiguration _configuration = null!;

        [SetUp]
        public void Setup()
        {
            _mockHttpService = new Mock<IHttpService>();
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string?>("Watch2Gether:ApiKey", "test-api-key"),
                new KeyValuePair<string, string?>("Watch2Gether:CreateRoomUrl", "https://api.watch2gether.com/rooms/create"),
                new KeyValuePair<string, string?>("Watch2Gether:ShowRoomUrl", "https://w2g.tv/rooms/")
            });
            _configuration = configurationBuilder.Build();
            _watch2GetherService = new Watch2GetherService(_mockHttpService.Object, _configuration);
        }

        [Test]
        public async Task CreateRoom_WithSuccessfulRequest_ReturnsSuccessAndRoomUrl()
        {
            // Arrange
            const string videoUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";
            const string streamKey = "AbCdEfGhIjKlMnOpQrStUvWxYz";
            var jsonResponse = JsonConvert.SerializeObject(new { streamkey = streamKey });
            static readonly string expectedRoomUrl = $"https://w2g.tv/rooms/{streamKey}";

            _mockHttpService.Setup(x => x.GetResponseFromUrl(
                It.IsAny<string>(),
                It.IsAny<Method>(),
                It.IsAny<string?>(),
                It.IsAny<List<KeyValuePair<string, string>>?>(),
                It.IsAny<object?>()))
                .ReturnsAsync(new HttpResponse(true, jsonResponse));

            // Act
            var (success, result) = await _watch2GetherService.CreateRoom(videoUrl);

            // Assert
            Assert.That(success, Is.True);
            Assert.That(result, Is.EqualTo(expectedRoomUrl));
        }

        [Test]
        public async Task CreateRoom_WithApiError_ReturnsFailureAndErrorMessage()
        {
            // Arrange
            const string videoUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";
            const string expectedErrorMessage = "API rate limit exceeded";

            _mockHttpService.Setup(x => x.GetResponseFromUrl(
                It.IsAny<string>(),
                It.IsAny<Method>(),
                It.IsAny<string?>(),
                It.IsAny<List<KeyValuePair<string, string>>?>(),
                It.IsAny<object?>()))
                .ReturnsAsync(new HttpResponse(false, expectedErrorMessage));

            // Act
            var (success, result) = await _watch2GetherService.CreateRoom(videoUrl);

            // Assert
            Assert.That(success, Is.False);
            Assert.That(result, Is.EqualTo(expectedErrorMessage));
        }

        [Test]
        public async Task CreateRoom_WithInvalidJson_HandlesDeserializationError()
        {
            // Arrange
            const string videoUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";
            const string invalidJsonResponse = "Invalid JSON response";

            _mockHttpService.Setup(x => x.GetResponseFromUrl(
                It.IsAny<string>(),
                It.IsAny<Method>(),
                It.IsAny<string?>(),
                It.IsAny<List<KeyValuePair<string, string>>?>(),
                It.IsAny<object?>()))
                .ReturnsAsync(new HttpResponse(true, invalidJsonResponse));

            // Act
            var (success, result) = await _watch2GetherService.CreateRoom(videoUrl);

            // Assert
            Assert.That(success, Is.True); // HTTP call was successful
            Assert.That(result, Does.Contain("Failed to deserialize response from Watch2Gether"));
        }

        [Test]
        public async Task CreateRoom_WithMissingConfiguration_ReturnsConfigurationError()
        {
            // Arrange
            const string videoUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";
            var emptyConfig = new ConfigurationBuilder().Build();
            var serviceWithEmptyConfig = new Watch2GetherService(_mockHttpService.Object, emptyConfig);

            // Act
            var (success, result) = await serviceWithEmptyConfig.CreateRoom(videoUrl);

            // Assert
            Assert.That(success, Is.False);
            Assert.That(result, Does.Contain("Could not load necessary configuration"));
        }

        [Test]
        public async Task CreateRoom_WithEmptyVideoUrl_StillCallsApi()
        {
            // Arrange
            const string videoUrl = "";
            const string streamKey = "EmptyUrlStreamKey";
            var jsonResponse = JsonConvert.SerializeObject(new { streamkey = streamKey });

            _mockHttpService.Setup(x => x.GetResponseFromUrl(
                It.IsAny<string>(),
                It.IsAny<Method>(),
                It.IsAny<string?>(),
                It.IsAny<List<KeyValuePair<string, string>>?>(),
                It.IsAny<object?>()))
                .ReturnsAsync(new HttpResponse(true, jsonResponse));

            // Act
            var (success, result) = await _watch2GetherService.CreateRoom(videoUrl);

            // Assert
            Assert.That(success, Is.True);
            _mockHttpService.Verify(x => x.GetResponseFromUrl(
                It.IsAny<string>(),
                It.IsAny<Method>(),
                It.IsAny<string?>(),
                It.IsAny<List<KeyValuePair<string, string>>?>(),
                It.IsAny<object?>()), Times.Once);
        }
    }
}
