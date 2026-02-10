using DiscordBot.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DiscordBot.Services
{
    /// <summary>
    /// Service for interacting with OpenAI APIs like ChatGPT and DALL-E.
    /// Handles API requests, response parsing, and error management.
    /// </summary>
    public class OpenAiService : IOpenAiService
    {
        private readonly IHttpService _httpService;
        private readonly string _chatGptApiUrl;
        private readonly string _dallEApiUrl;
        private readonly string _openAiApiKey;

        public OpenAiService(IHttpService httpService, IConfiguration configuration)
        {
            _httpService = httpService;
            _openAiApiKey = configuration["OpenAi:ApiKey"] ?? string.Empty;
            _chatGptApiUrl = configuration["OpenAi:ChatGPTApiUrl"] ?? "https://api.openai.com/v1/chat/completions";
            _dallEApiUrl = configuration["OpenAi:DallEApiUrl"] ?? "https://api.openai.com/v1/images/generations";
        }

        /// <summary>
        /// Sends a prompt to the ChatGPT API and returns the generated response.
        /// </summary>
        /// <param name="message">The user prompt for ChatGPT.</param>
        /// <returns>Tuple with success indicator and response or error message.</returns>
        public async Task<Tuple<bool, string>> ChatGptAsync(string message)
        {
            bool success = false;

            if (string.IsNullOrEmpty(_openAiApiKey) || string.IsNullOrEmpty(_chatGptApiUrl))
            {
                const string errorMessage = "No OpenAI API key or ChatGPT API URL provided. Please update configuration.";
                Program.Log($"{nameof(ChatGptAsync)}: {errorMessage}", LogLevel.Error);
                return Tuple.Create(success, errorMessage);
            }

            var headers = new List<KeyValuePair<string, string>>
            {
                new("Content-Type", "application/json"),
                new("Authorization", $"Bearer {_openAiApiKey}")
            };

            var data = new
            {
                model = "gpt-4o",
                messages = new[] { new { role = "user", content = message } }
            };

            HttpResponse response = await _httpService.GetResponseFromUrl(
                _chatGptApiUrl, Method.Post, "Unknown error occurred in ChatGptAsync", headers, data);

            if (response is { IsSuccessStatusCode: true, Content: not null })
            {
                var chatContent = JsonConvert.DeserializeObject<dynamic>(response.Content)?["choices"][0]["message"]["content"];
                string responseText = chatContent?.ToString() ?? "";

                if (string.IsNullOrEmpty(responseText))
                {
                    const string msg = "Could not deserialize response from ChatGPT API!";
                    Program.Log($"{nameof(ChatGptAsync)}: {msg}", LogLevel.Error);
                    return Tuple.Create(success, msg);
                }
                success = true;
                return Tuple.Create(success, responseText);
            }
            else
            {
                return Tuple.Create(success, response.Content?.TrimStart('\n') ?? "Unknown error.");
            }
        }

        /// <summary>
        /// Sends a prompt to the DALL-E API and returns the image URL or an error message.
        /// </summary>
        /// <param name="message">The description of the image to generate.</param>
        /// <returns>Tuple with success indicator and either the image URL or error text.</returns>
        public async Task<Tuple<bool, string>> DallEAsync(string message)
        {
            bool success = false;

            if (string.IsNullOrEmpty(_openAiApiKey) || string.IsNullOrEmpty(_dallEApiUrl))
            {
                const string errorMessage = "No OpenAI API key or DALL-E API URL provided. Please update configuration.";
                Program.Log($"{nameof(DallEAsync)}: {errorMessage}", LogLevel.Error);
                return Tuple.Create(success, errorMessage);
            }

            var headers = new List<KeyValuePair<string, string>>
            {
                new("Content-Type", "application/json"),
                new("Authorization", $"Bearer {_openAiApiKey}")
            };

            var data = new
            {
                model = "dall-e-3",
                prompt = message,
                n = 1,
                size = "1024x1024"
            };

            HttpResponse response = await _httpService.GetResponseFromUrl(
                _dallEApiUrl, Method.Post, "Received a failed response from the Dall-E API.", headers, data);

            if (response is { IsSuccessStatusCode: true, Content: not null })
            {
                dynamic? imageUrl = JsonConvert.DeserializeObject<dynamic>(response.Content)?["data"][0]["url"];
                string imageUrlString = imageUrl?.ToString() ?? "";

                if (string.IsNullOrEmpty(imageUrlString))
                {
                    const string msg = "Could not deserialize image URL from DALL-E API!";
                    Program.Log($"{nameof(DallEAsync)}: {msg}", LogLevel.Error);
                    return Tuple.Create(success, msg);
                }
                Program.Log($"{nameof(DallEAsync)}: Generated image URL: {imageUrlString}", LogLevel.Information);
                success = true;
                return Tuple.Create(success, $"Here is your generated image: {imageUrlString}");
            }
            else
            {
                return Tuple.Create(success, response.Content ?? "Unknown error.");
            }
        }
    }
}
