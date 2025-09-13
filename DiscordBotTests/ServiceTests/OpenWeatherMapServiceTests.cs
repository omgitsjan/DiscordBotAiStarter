using DiscordBot.Interfaces;
using DiscordBot.Services;
using Microsoft.Extensions.Configuration;
using Moq;
using RestSharp;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DiscordBotTests.ServiceTests
{
    [TestFixture]
    public class OpenWeatherMapServiceTests
    {
        private Mock<IHttpService> _mockHttpService = null!;
        private OpenWeatherMapService _weatherMapService = null!;
        private IConfiguration _configuration = null!;

        [SetUp]
        public void Setup()
        {
            _mockHttpService = new Mock<IHttpService>();
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string?>("OpenWeatherMap:ApiKey", "test-api-key"),
                new KeyValuePair<string, string?>("OpenWeatherMap:ApiUrl", "https://api.openweathermap.org/data/2.5/weather?q=")
            });
            _configuration = configurationBuilder.Build();
            _weatherMapService = new OpenWeatherMapService(_mockHttpService.Object, _configuration);
        }

        [Test]
        public async Task GetWeatherAsync_WithValidResponse_ReturnsSuccessAndWeatherData()
        {
            // Arrange
            const string city = "Berlin";
            const string description = "light rain";
            const double temperature = 10.55;
            const int humidity = 76;
            const double windSpeed = 5.5;
            const string jsonResponse =
                "{\"name\": \"Berlin\"," +
                "\"weather\": [{\"description\": \"light rain\"}]," +
                "\"main\": {\"temp\": 10.55, \"humidity\": 76}," +
                "\"wind\": {\"speed\": 5.5}}";

            _mockHttpService.Setup(x => x.GetResponseFromUrl(It.IsAny<string>(), It.IsAny<Method>(), It.IsAny<string?>(),
                It.IsAny<List<KeyValuePair<string, string>>?>(), It.IsAny<object?>()))
                .ReturnsAsync(new HttpResponse(true, jsonResponse));

            // Act
            var (success, message, weatherData) = await _weatherMapService.GetWeatherAsync(city);

            // Assert
            Assert.That(success, Is.True);
            Assert.That(weatherData, Is.Not.Null);
            Assert.That(weatherData!.City, Is.EqualTo(city));
            Assert.That(weatherData.Description, Is.EqualTo(description));
            Assert.That(weatherData.Temperature, Is.EqualTo(temperature));
            Assert.That(weatherData.Humidity, Is.EqualTo(humidity));
            Assert.That(weatherData.WindSpeed, Is.EqualTo(windSpeed));
            Assert.That(message, Does.Contain(city));
            Assert.That(message, Does.Contain(description));
        }

        [Test]
        public async Task GetWeatherAsync_WithMissingConfiguration_ReturnsError()
        {
            // Arrange
            const string city = "Berlin";
            var emptyConfig = new ConfigurationBuilder().Build();
            var service = new OpenWeatherMapService(_mockHttpService.Object, emptyConfig);

            // Act
            var (success, message, weatherData) = await service.GetWeatherAsync(city);

            // Assert
            Assert.That(success, Is.False);
            Assert.That(message, Does.Contain("No OpenWeatherMap API key or URL configured"));
            Assert.That(weatherData, Is.Null);
        }

        [Test]
        public async Task GetWeatherAsync_WithApiError_ReturnsError()
        {
            // Arrange
            const string city = "InvalidCity";
            const string expectedMessage = "City not found";

            _mockHttpService.Setup(x => x.GetResponseFromUrl(It.IsAny<string>(), It.IsAny<Method>(), It.IsAny<string?>(),
                It.IsAny<List<KeyValuePair<string, string>>?>(), It.IsAny<object?>()))
                .ReturnsAsync(new HttpResponse(false, expectedMessage));

            // Act
            var (success, message, weatherData) = await _weatherMapService.GetWeatherAsync(city);

            // Assert
            Assert.That(success, Is.False);
            Assert.That(message, Is.EqualTo(expectedMessage));
            Assert.That(weatherData, Is.Null);
        }

        [Test]
        public async Task GetWeatherAsync_WithInvalidJson_ReturnsError()
        {
            // Arrange
            const string city = "Berlin";
            const string invalidJsonResponse = "Invalid JSON response";

            _mockHttpService.Setup(x => x.GetResponseFromUrl(It.IsAny<string>(), It.IsAny<Method>(), It.IsAny<string?>(),
                It.IsAny<List<KeyValuePair<string, string>>?>(), It.IsAny<object?>()))
                .ReturnsAsync(new HttpResponse(true, invalidJsonResponse));

            // Act
            var (success, message, weatherData) = await _weatherMapService.GetWeatherAsync(city);

            // Assert
            Assert.That(success, Is.False);
            Assert.That(message, Does.Contain("Failed to parse weather data"));
            Assert.That(weatherData, Is.Null);
        }

        [Test]
        public async Task GetWeatherAsync_WithPartialData_StillReturnsSuccess()
        {
            // Arrange
            const string city = "Berlin";
            const string jsonResponseWithMissingData =
                "{\"name\": \"Berlin\"," +
                "\"weather\": [{\"description\": \"sunny\"}]," +
                "\"main\": {\"temp\": 25.0}}"; // Missing humidity and wind data

            _mockHttpService.Setup(x => x.GetResponseFromUrl(It.IsAny<string>(), It.IsAny<Method>(), It.IsAny<string?>(),
                It.IsAny<List<KeyValuePair<string, string>>?>(), It.IsAny<object?>()))
                .ReturnsAsync(new HttpResponse(true, jsonResponseWithMissingData));

            // Act
            var (success, message, weatherData) = await _weatherMapService.GetWeatherAsync(city);

            // Assert
            Assert.That(success, Is.True);
            Assert.That(weatherData, Is.Not.Null);
            Assert.That(weatherData!.City, Is.EqualTo(city));
            Assert.That(weatherData.Description, Is.EqualTo("sunny"));
            Assert.That(weatherData.Temperature, Is.EqualTo(25.0));
        }
    }
}
