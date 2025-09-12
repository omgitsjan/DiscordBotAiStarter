using DiscordBot.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace DiscordBot.Services
{
    /// <summary>
    /// Provides helper utilities, such as loading and returning random developer excuses.
    /// </summary>
    public class HelperService : IHelperService
    {
        private readonly List<string> _developerExcuses = [];
        private readonly ILogger<HelperService> _logger;

        /// <summary>
        /// Constructs the HelperService, loading developer excuses from the default location.
        /// </summary>
        public HelperService(ILogger<HelperService> logger)
        {
            _logger = logger;
            LoadDeveloperExcuses(null);
        }

        /// <summary>
        /// Constructs the HelperService, loading developer excuses from a custom file path.
        /// </summary>
        public HelperService(ILogger<HelperService> logger, string? excusesFilePath)
        {
            _logger = logger;
            LoadDeveloperExcuses(excusesFilePath);
        }

        /// <summary>
        /// Loads developer excuses from a JSON file. If no path is provided, the default file is used.
        /// </summary>
        /// <param name="excusesFilePath">Optional custom path to the excuses file.</param>
        private void LoadDeveloperExcuses(string? excusesFilePath)
        {
            string filePath = excusesFilePath ?? Path.Combine(Directory.GetCurrentDirectory(), "Data", "excuses.json");
            if (!File.Exists(filePath))
            {
                _logger.LogError("Developer excuses file not found at: {FilePath}", filePath);
                return;
            }

            try
            {
                string jsonContent = File.ReadAllText(filePath);
                JObject json = JObject.Parse(jsonContent);
                var excuses = json["en"]?.ToObject<List<string>>() ?? [];
                _developerExcuses.AddRange(excuses);
                if (_developerExcuses.Count == 0)
                    _logger.LogWarning("Developer excuses file loaded, but no excuses found in 'en' array.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading developer excuses from file: {FilePath}", filePath);
            }
        }

        /// <summary>
        /// Returns a random developer excuse from the loaded list.
        /// </summary>
        /// <returns>Random developer excuse as a string, or an error message if unavailable.</returns>
        public Task<string> GetRandomDeveloperExcuseAsync()
        {
            if (_developerExcuses.Count == 0)
            {
                return Task.FromResult("Could not fetch a developer excuse. Please check the configuration or file content.");
            }
            var random = new Random();
            int index = random.Next(_developerExcuses.Count);
            return Task.FromResult(_developerExcuses[index]);
        }
    }
}
