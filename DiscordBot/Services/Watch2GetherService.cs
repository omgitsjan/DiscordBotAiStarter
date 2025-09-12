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
    /// Service for interacting with the Watch2Gether API to create shared video rooms.
    /// </summary>
    public class Watch2GetherService(IHttpService httpService, IConfiguration configuration) : IWatch2GetherService
    {
        /// <summary>
        /// API key for Watch2Gether (set via configuration).
        /// </summary>
        private string? _w2GApiKey;

        /// <summary>
        /// API endpoint for room creation.
        /// </summary>
        private string? _w2GCreateRoomUrl;

        /// <summary>
        /// Base URL used to present a created room to the user.
        /// </summary>
        private string? _w2GShowRoomUrl;

        /// <summary>
        /// Creates a Watch2Gether room for sharing a video.
        /// </summary>
        /// <param name="videoUrl">The URL of the video to be shared.</param>
        /// <returns>
        /// Tuple: (success/failure, message with room URL or error).
        /// </returns>
        public async Task<Tuple<bool, string?>> CreateRoom(string videoUrl)
        {
            // Load configuration and check all endpoints
            _w2GApiKey = configuration["Watch2Gether:ApiKey"] ?? string.Empty;
            _w2GCreateRoomUrl = configuration["Watch2Gether:CreateRoomUrl"] ?? string.Empty;
            _w2GShowRoomUrl = configuration["Watch2Gether:ShowRoomUrl"] ?? string.Empty;

            if (string.IsNullOrEmpty(_w2GApiKey) || string.IsNullOrEmpty(_w2GCreateRoomUrl) || string.IsNullOrEmpty(_w2GShowRoomUrl))
            {
                string configError = "Could not load necessary configuration, please provide a valid configuration.";
                Program.Log($"{nameof(CreateRoom)}: {configError}", LogLevel.Error);
                return Tuple.Create(false, configError);
            }

            var headers = new List<KeyValuePair<string, string>>
            {
                new("Content-Type", "application/json"),
                new("Accept", "application/json")
            };

            var data = new
            {
                w2g_api_key = _w2GApiKey,
                share = videoUrl
            };

            HttpResponse response = await httpService.GetResponseFromUrl(
                _w2GCreateRoomUrl,
                Method.Post,
                $"{nameof(CreateRoom)}: No response from Watch2Gether",
                headers,
                data);

            string? message = response.Content;

            if (response is { IsSuccessStatusCode: true, Content: not null })
            {
                try
                {
                    dynamic? responseObj = JsonConvert.DeserializeObject<dynamic>(response.Content);
                    message = _w2GShowRoomUrl + (string?)responseObj?.streamkey;
                }
                catch (Exception e)
                {
                    message = "Failed to deserialize response from Watch2Gether";
                    Program.Log($"{nameof(CreateRoom)}: {message} Error: {e.Message}", LogLevel.Error);
                }
            }

            if (response.IsSuccessStatusCode)
            {
                Program.Log($"{nameof(CreateRoom)}: Successfully created Watch2Gether room: {message}");
            }
            else
            {
                Program.Log($"{nameof(CreateRoom)}: Failed to create Watch2Gether room. Error: {message}", LogLevel.Error);
            }

            return Tuple.Create(response.IsSuccessStatusCode, message);
        }
    }
}
