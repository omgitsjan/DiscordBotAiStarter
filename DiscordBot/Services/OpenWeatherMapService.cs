using DiscordBot.Interfaces;
using DiscordBot.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Threading.Tasks;

namespace DiscordBot.Services
{
    /// <summary>
    /// Service for accessing current weather data using the OpenWeatherMap API.
    /// </summary>
    public class OpenWeatherMapService(IHttpService httpService, IConfiguration configuration) : IOpenWeatherMapService
    {
        /// <summary>
        /// The OpenWeatherMap API key from configuration.
        /// </summary>
        private string? _openWeatherMapApiKey;

        /// <summary>
        /// The OpenWeatherMap API base URL from configuration.
        /// </summary>
        private string? _openWeatherMapUrl;

        /// <summary>
        /// Retrieves the current weather for a specified city.
        /// </summary>
        /// <param name="city">The name of the city to get weather for.</param>
        /// <returns>
        /// Tuple: Success flag, message string, and WeatherData object if successful.
        /// </returns>
        public async Task<(bool Success, string Message, WeatherData? WeatherData)> GetWeatherAsync(string city)
        {
            _openWeatherMapUrl = configuration["OpenWeatherMap:ApiUrl"] ?? string.Empty;
            _openWeatherMapApiKey = configuration["OpenWeatherMap:ApiKey"] ?? string.Empty;

            if (string.IsNullOrEmpty(_openWeatherMapApiKey) || string.IsNullOrEmpty(_openWeatherMapUrl))
            {
                const string errorMessage = "No OpenWeatherMap API key or URL configured. Please update your configuration.";
                Program.Log($"{nameof(GetWeatherAsync)}: {errorMessage}", LogLevel.Error);
                return (false, errorMessage, null);
            }

            string endpoint = $"{_openWeatherMapUrl}{Uri.EscapeDataString(city)}&units=metric&appid={_openWeatherMapApiKey}";
            HttpResponse response = await httpService.GetResponseFromUrl(
                endpoint,
                Method.Post,
                $"{nameof(GetWeatherAsync)}: Failed to fetch weather data for city '{city}'."
            );

            if (!response.IsSuccessStatusCode)
            {
                return (false, response.Content ?? "API call failed.", null);
            }

            try
            {
                JObject json = JObject.Parse(response.Content ?? "");
                var weather = new WeatherData
                {
                    City = json["name"]?.Value<string>(),
                    Description = json["weather"]?[0]?["description"]?.Value<string>(),
                    Temperature = json["main"]?["temp"]?.Value<double>(),
                    Humidity = json["main"]?["humidity"]?.Value<int>(),
                    WindSpeed = json["wind"]?["speed"]?.Value<double>()
                };

                string message =
                    $"In {weather.City}, the current weather: {weather.Description}. " +
                    $"Temperature: {weather.Temperature:F2}°C, Humidity: {weather.Humidity}%, Wind speed: {weather.WindSpeed} m/s.";

                Program.Log($"{nameof(GetWeatherAsync)}: Weather data fetched successfully. Response: {message}", LogLevel.Information);
                return (true, message, weather);
            }
            catch (Exception ex)
            {
                string errorMsg = $"Failed to parse weather data: {ex.Message}";
                Program.Log($"{nameof(GetWeatherAsync)}: {errorMsg}", LogLevel.Error);
                return (false, errorMsg, null);
            }
        }
    }
}
