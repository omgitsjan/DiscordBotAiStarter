using DiscordBot.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;

namespace DiscordBot.Services
{
    /// <summary>
    /// Service for retrieving cryptocurrency prices via a configurable HTTP API.
    /// </summary>
    public class CryptoService(IHttpService httpService, IConfiguration configuration) : ICryptoService
    {
        /// <summary>
        /// Base URL for the ByBit API (set via configuration).
        /// </summary>
        private string? _byBitApiUrl;

        /// <summary>
        /// Retrieves the current price for a specified cryptocurrency and currency pair.
        /// </summary>
        /// <param name="symbol">The cryptocurrency symbol, e.g. BTC (default: BTC).</param>
        /// <param name="physicalCurrency">The comparison currency, e.g. USDT (default: USDT).</param>
        /// <returns>Tuple indicating success and the price or error message.</returns>
        public async Task<Tuple<bool, string>> GetCryptoPriceAsync(string symbol = "BTC", string physicalCurrency = "USDT")
        {
            _byBitApiUrl = configuration["ByBit:ApiUrl"] ?? string.Empty;
            symbol = symbol.ToUpperInvariant();

            if (string.IsNullOrEmpty(_byBitApiUrl))
            {
                const string errorMessage = "No ByBit API URL configured. Please contact the developer to add a valid API URL.";
                Program.Log($"{nameof(GetCryptoPriceAsync)}: {errorMessage}", LogLevel.Error);
                return Tuple.Create(false, errorMessage);
            }

            string requestUrl = $"{_byBitApiUrl}{symbol}{physicalCurrency}";
            Program.Log($"Requesting: {requestUrl}", LogLevel.Debug);

            HttpResponse response = await httpService.GetResponseFromUrl(requestUrl);
            if (!response.IsSuccessStatusCode)
            {
                return Tuple.Create(false, response.Content ?? "API error (no content).");
            }

            try
            {
                JObject json = JObject.Parse(response.Content ?? "{}");
                string? priceString = json["result"]?["list"]?[0]?["lastPrice"]?.Value<string>();
                bool success = priceString != null;

                if (!success)
                    priceString = $"Could not fetch price for {symbol}.";

                Program.Log($"{nameof(GetCryptoPriceAsync)}: {priceString} (success={success})", LogLevel.Information);
                return Tuple.Create(success, priceString ?? throw new Exception("No response string."));
            }
            catch (JsonReaderException ex)
            {
                Program.Log($"{nameof(GetCryptoPriceAsync)}: JSON parse error: {ex.Message}", LogLevel.Error);
                return Tuple.Create(false, $"Could not fetch price for {symbol} (invalid API response).");
            }
            catch (Exception ex)
            {
                Program.Log($"{nameof(GetCryptoPriceAsync)}: Unexpected error: {ex.Message}", LogLevel.Error);
                return Tuple.Create(false, $"Could not fetch price for {symbol} (unexpected error).");
            }
        }
    }
}
