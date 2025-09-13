using DiscordBot.Services;
using Moq;
using Newtonsoft.Json;
using RestSharp;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace DiscordBotTests.ServiceTests
{
    [TestFixture]
    public class HttpServiceTests
    {
        private Mock<IRestClient> _mockRestClient = null!;
        private HttpService _httpService = null!;

        [SetUp]
        public void SetUp()
        {
            _mockRestClient = new Mock<IRestClient>();
            _httpService = new HttpService(_mockRestClient.Object);
        }

        [Test]
        public async Task GetResponseFromUrl_WithValidResponse_ReturnsSuccess()
        {
            // Arrange
            const string resource = "https://api.example.com/test";
            const string content = "response content";
            var response = new RestResponse
            {
                StatusCode = HttpStatusCode.OK,
                Content = content,
                IsSuccessStatusCode = true
            };

            _mockRestClient.Setup(client => client.ExecuteAsync(It.IsAny<RestRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            // Act
            var result = await _httpService.GetResponseFromUrl(resource);

            // Assert
            Assert.That(result.IsSuccessStatusCode, Is.True);
            Assert.That(result.Content, Is.EqualTo(content));
        }

        [Test]
        public async Task GetResponseFromUrl_WithErrorResponse_ReturnsError()
        {
            // Arrange
            const string resource = "https://api.example.com/test";
            const string errorMessage = "Custom error message";
            var response = new RestResponse
            {
                StatusCode = HttpStatusCode.BadRequest,
                ErrorMessage = "API Error",
                IsSuccessStatusCode = false
            };

            _mockRestClient.Setup(client => client.ExecuteAsync(It.IsAny<RestRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            // Act
            var result = await _httpService.GetResponseFromUrl(resource, errorMessage: errorMessage);

            // Assert
            Assert.That(result.IsSuccessStatusCode, Is.False);
            Assert.That(result.Content, Is.EqualTo($"StatusCode: {response.StatusCode} | {errorMessage}"));
        }

        [Test]
        public async Task GetResponseFromUrl_WithHeaders_AddsHeadersToRequest()
        {
            // Arrange
            const string resource = "https://api.example.com/test";
            var headers = new List<KeyValuePair<string, string>>
            {
                new("Authorization", "Bearer token"),
                new("Content-Type", "application/json")
            };
            var response = new RestResponse
            {
                StatusCode = HttpStatusCode.OK,
                Content = "OK",
                IsSuccessStatusCode = true
            };

            RestRequest? capturedRequest = null;
            _mockRestClient.Setup(client => client.ExecuteAsync(It.IsAny<RestRequest>(), It.IsAny<CancellationToken>()))
                .Callback<RestRequest, CancellationToken>((req, _) => capturedRequest = req)
                .ReturnsAsync(response);

            // Act
            var result = await _httpService.GetResponseFromUrl(resource, headers: headers);

            // Assert
            Assert.That(result.IsSuccessStatusCode, Is.True);
            Assert.That(capturedRequest, Is.Not.Null);

            foreach (var header in headers)
            {
                var headerParam = capturedRequest!.Parameters.FirstOrDefault(p => p.Name == header.Key && p.Type == ParameterType.HttpHeader);
                Assert.That(headerParam, Is.Not.Null);
                Assert.That(headerParam!.Value, Is.EqualTo(header.Value));
            }
        }

        [Test]
        public async Task GetResponseFromUrl_WithJsonBody_AddsJsonBodyToRequest()
        {
            // Arrange
            const string resource = "https://api.example.com/test";
            var jsonBodyObject = new { key = "value", number = 42 };
            var response = new RestResponse
            {
                StatusCode = HttpStatusCode.OK,
                IsSuccessStatusCode = true
            };

            RestRequest? capturedRequest = null;
            _mockRestClient.Setup(client => client.ExecuteAsync(It.IsAny<RestRequest>(), It.IsAny<CancellationToken>()))
                .Callback<RestRequest, CancellationToken>((req, _) => capturedRequest = req)
                .ReturnsAsync(response);

            // Act
            await _httpService.GetResponseFromUrl(resource, Method.Post, jsonBody: jsonBodyObject);

            // Assert
            Assert.That(capturedRequest, Is.Not.Null);
            var requestBodyParam = capturedRequest!.Parameters.FirstOrDefault(p => p.Type == ParameterType.RequestBody);
            Assert.That(requestBodyParam, Is.Not.Null);
        }

        [Test]
        public async Task GetResponseFromUrl_WithException_ReturnsErrorResponse()
        {
            // Arrange
            const string resource = "https://api.example.com/test";
            _mockRestClient.Setup(client => client.ExecuteAsync(It.IsAny<RestRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new System.Exception("Network error"));

            // Act
            var result = await _httpService.GetResponseFromUrl(resource);

            // Assert
            Assert.That(result.IsSuccessStatusCode, Is.False);
            Assert.That(result.Content, Does.Contain("Unknown error"));
            Assert.That(result.Content, Does.Contain("Network error"));
        }
    }
}
