using DiscordBot.Interfaces;
using DiscordBot.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using RestSharp;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DiscordBotTests.ServiceTests
{
    [TestFixture]
    public class CryptoServiceTests
    {
        private Mock<IHttpService> _mockHttpService = null!;
        private Mock<ILogger<CryptoService>> _mockLogger = null!;
        private CryptoService _cryptoService = null!;
        private IConfiguration _configuration = null!;

        [SetUp]
        public void Setup()
        {
            _mockHttpService = new Mock<IHttpService>();
            _mockLogger = new Mock<ILogger<CryptoService>>();

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string?>("ByBit:ApiUrl", "https://api.bybit.com/v5/market/tickers?symbol="),
            });
            _configuration = configurationBuilder.Build();
            _cryptoService = new CryptoService(_mockHttpService.Object, _configuration);
        }

        [Test]
        public async Task GetCryptoPriceAsync_WithValidResponse_ReturnsSuccess()
        {
            // Arrange
            const string symbol = "BTC";
            const string physicalCurrency = "USDT";
            const string jsonResponse = "{\"result\": {\"list\": [{\"lastPrice\": \"50000.00\"}]}}";
            const string expectedPrice = "50000.00";

            _mockHttpService.Setup(x => x.GetResponseFromUrl(It.IsAny<string>(), It.IsAny<Method>(), It.IsAny<string?>(),
                It.IsAny<List<KeyValuePair<string, string>>?>(), It.IsAny<object?>()))
                .ReturnsAsync(new HttpResponse(true, jsonResponse));

            // Act
            var (success, result) = await _cryptoService.GetCryptoPriceAsync(symbol, physicalCurrency);

            // Assert
            Assert.That(success, Is.True);
            Assert.That(result, Is.EqualTo(expectedPrice));
        }

        [Test]
        public async Task GetCryptoPriceAsync_WithApiError_ReturnsErrorMessage()
        {
            // Arrange
            const string symbol = "BTC";
            const string physicalCurrency = "USDT";
            const string expectedErrorMessage = "API Error";

            _mockHttpService.Setup(x => x.GetResponseFromUrl(It.IsAny<string>(), It.IsAny<Method>(), It.IsAny<string?>(),
                It.IsAny<List<KeyValuePair<string, string>>?>(), It.IsAny<object?>()))
                .ReturnsAsync(new HttpResponse(false, expectedErrorMessage));

            // Act
            var (success, result) = await _cryptoService.GetCryptoPriceAsync(symbol, physicalCurrency);

            // Assert
            Assert.That(success, Is.False);
            Assert.That(result, Is.EqualTo(expectedErrorMessage));
        }

        [Test]
        public async Task GetCryptoPriceAsync_WithInvalidJson_ReturnsFallbackMessage()
        {
            // Arrange
            const string symbol = "BTC";
            const string physicalCurrency = "USDT";
            const string invalidJsonResponse = "Invalid JSON";

            _mockHttpService.Setup(x => x.GetResponseFromUrl(It.IsAny<string>(), It.IsAny<Method>(), It.IsAny<string?>(),
                It.IsAny<List<KeyValuePair<string, string>>?>(), It.IsAny<object?>()))
                .ReturnsAsync(new HttpResponse(true, invalidJsonResponse));

            // Act
            var (success, result) = await _cryptoService.GetCryptoPriceAsync(symbol, physicalCurrency);

            // Assert
            Assert.That(success, Is.False);
            Assert.That(result, Is.EqualTo($"Could not fetch price for {symbol} (invalid API response)."));
        }

        [Test]
        public async Task GetCryptoPriceAsync_WithMissingConfiguration_ReturnsConfigError()
        {
            // Arrange
            var emptyConfig = new ConfigurationBuilder().Build();
            var serviceWithEmptyConfig = new CryptoService(_mockHttpService.Object, emptyConfig);

            // Act
            var (success, result) = await serviceWithEmptyConfig.GetCryptoPriceAsync("BTC", "USDT");

            // Assert
            Assert.That(success, Is.False);
            Assert.That(result, Does.Contain("No ByBit API URL configured"));
        }

        [Test]
        public async Task GetCryptoPriceAsync_WithEmptyLastPrice_ReturnsFallbackMessage()
        {
            // Arrange
            const string symbol = "BTC";
            const string physicalCurrency = "USDT";
            const string jsonResponseMissingLastPrice = "{\"result\": {\"list\": [{}]}}";

            _mockHttpService.Setup(x => x.GetResponseFromUrl(It.IsAny<string>(), It.IsAny<Method>(), It.IsAny<string?>(),
                It.IsAny<List<KeyValuePair<string, string>>?>(), It.IsAny<object?>()))
                .ReturnsAsync(new HttpResponse(true, jsonResponseMissingLastPrice));

            // Act
            var (success, result) = await _cryptoService.GetCryptoPriceAsync(symbol, physicalCurrency);

            // Assert
            Assert.That(success, Is.False);
            Assert.That(result, Is.EqualTo($"Could not fetch price for {symbol}."));
        }
    }
}
