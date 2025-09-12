using DiscordBot.Services;
using Microsoft.Extensions.Logging;
using Moq;
using System.IO;
using System.Threading.Tasks;

namespace DiscordBotTests.ServiceTests
{
    [TestFixture]
    public class HelperServiceTests
    {
        private HelperService _helperService = null!;
        private Mock<ILogger<HelperService>> _mockLogger = null!;
        private const string TestJsonFilePath = "testExcuses.json";

        [SetUp]
        public void Setup()
        {
            _mockLogger = new Mock<ILogger<HelperService>>();

            // Create a test JSON file with some developer excuses
            const string testJsonContent = "{\"en\": [\"Test excuse 1\", \"Test excuse 2\", \"Test excuse 3\"]}";
            File.WriteAllText(TestJsonFilePath, testJsonContent);
        }

        [TearDown]
        public void Cleanup()
        {
            // Delete the test JSON file after each test
            if (File.Exists(TestJsonFilePath))
            {
                File.Delete(TestJsonFilePath);
            }
        }

        [Test]
        public async Task GetRandomDeveloperExcuseAsync_WithValidFile_ReturnsRandomExcuse()
        {
            // Arrange
            _helperService = new HelperService(_mockLogger.Object, TestJsonFilePath);

            // Act
            var result = await _helperService.GetRandomDeveloperExcuseAsync();

            // Assert
            Assert.That(result, Is.EqualTo("Test excuse 1").Or.EqualTo("Test excuse 2").Or.EqualTo("Test excuse 3"));
        }

        [Test]
        public async Task GetRandomDeveloperExcuseAsync_WithInvalidJsonFile_ReturnsFallbackMessage()
        {
            // Arrange
            await File.WriteAllTextAsync(TestJsonFilePath, "Invalid JSON");
            _helperService = new HelperService(_mockLogger.Object, TestJsonFilePath);

            // Act
            var result = await _helperService.GetRandomDeveloperExcuseAsync();

            // Assert
            Assert.That(result, Is.EqualTo("Could not fetch a developer excuse. Please check the configuration or file content."));
        }

        [Test]
        public async Task GetRandomDeveloperExcuseAsync_WithEmptyJsonFile_ReturnsFallbackMessage()
        {
            // Arrange
            await File.WriteAllTextAsync(TestJsonFilePath, "{}");
            _helperService = new HelperService(_mockLogger.Object, TestJsonFilePath);

            // Act
            var result = await _helperService.GetRandomDeveloperExcuseAsync();

            // Assert
            Assert.That(result, Is.EqualTo("Could not fetch a developer excuse. Please check the configuration or file content."));
        }

        [Test]
        public async Task GetRandomDeveloperExcuseAsync_WithMissingFile_ReturnsFallbackMessage()
        {
            // Arrange
            File.Delete(TestJsonFilePath);
            _helperService = new HelperService(_mockLogger.Object, TestJsonFilePath);

            // Act
            var result = await _helperService.GetRandomDeveloperExcuseAsync();

            // Assert
            Assert.That(result, Is.EqualTo("Could not fetch a developer excuse. Please check the configuration or file content."));
        }

        [Test]
        public async Task GetRandomDeveloperExcuseAsync_WithDefaultConstructor_WorksWithDefaultPath()
        {
            // Arrange
            var defaultPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "excuses.json");
            Directory.CreateDirectory(Path.GetDirectoryName(defaultPath)!);
            const string testJsonContent = "{\"en\": [\"Default excuse\"]}";
            File.WriteAllText(defaultPath, testJsonContent);

            _helperService = new HelperService(_mockLogger.Object);

            try
            {
                // Act
                var result = await _helperService.GetRandomDeveloperExcuseAsync();

                // Assert
                Assert.That(result, Is.EqualTo("Default excuse"));
            }
            finally
            {
                // Cleanup
                if (File.Exists(defaultPath))
                    File.Delete(defaultPath);
            }
        }
    }
}
