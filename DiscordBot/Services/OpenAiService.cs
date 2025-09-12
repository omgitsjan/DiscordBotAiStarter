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
    public class OpenAiService(IHttpService httpService, IConfiguration configuration) : IOpenAiService
    {
        /// <summary>
        /// Base URL for the ChatGPT API.
        /// </summary>
        private string? _chatGptApiUrl;

        /// <summary>
        /// Base URL for the DALL-E API.
        /// </summary>
        private string? _dallEApiUrl;

        /// <summary>
        /// API key to authenticate with OpenAI services.
        /// </summary>
        private string? _openAiApiKey;

        /// <summary>
        /// Sends a prompt to the ChatGPT API and returns the generated response.
        /// </summary>
        /// <param name="message">The user prompt for ChatGPT.</param>
        /// <returns>Tuple with success indicator and response or error message.</returns>
        public async Task<Tuple<bool, string>> ChatGptAsync(string message)
        {
            bool success = false;

            _openAiApiKey = configuration["OpenAi:ApiKey"] ?? string.Empty;
            _chatGptApiUrl = configuration["OpenAi:ChatGPTApiUrl"] ?? string.Empty;

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
                model = "gpt-3.5-turbo", // CHANGE IF YOU WANT A OTHER MODEL
                messages = new[] { new { role = "user", content = message } }
            };

            HttpResponse response = await httpService.GetResponseFromUrl(
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

            _openAiApiKey = configuration["OpenAi:ApiKey"] ?? string.Empty;
            _dallEApiUrl = configuration["OpenAi:DallEApiUrl"] ?? string.Empty;

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
                prompt = message,
                n = 1,
                size = "1024x1024"
            };

            HttpResponse response = await httpService.GetResponseFromUrl(
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
