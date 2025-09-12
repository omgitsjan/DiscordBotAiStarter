using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using DiscordBot.Interfaces;
using DiscordBot.Models;
using DiscordBot.Wrapper;
using DSharpPlus;
using DSharpPlus.Entities;
using System;
using System.Threading.Tasks;

namespace DiscordBot.Services
{
    /// <summary>
    /// Implements the logic for all slash commands, including integration
    /// with external APIs for AI, weather, cryptocurrency, and more.
    /// </summary>
    public partial class SlashCommandsService(
        IWatch2GetherService watch2GetherService,
        IOpenWeatherMapService openWeatherMapService,
        IOpenAiService openAiService,
        ICryptoService cryptoService)
        : ISlashCommandsService
    {
        /// <summary>
        /// Responds with latency by pinging google.com and returns a nicely formatted message.
        /// </summary>
        public async Task PingSlashCommandAsync(IInteractionContextWrapper ctx)
        {
            // Indicate to Discord that the bot is working
            await ctx.CreateResponseAsync(
                InteractionResponseType.DeferredChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent("Pinging..."));

            long latency = -1;
            try
            {
                using var ping = new Ping();
                var reply = ping.Send("google.com");
                latency = reply?.RoundtripTime ?? -1;
            }
            catch
            {
                // Could log more here if needed.
            }

            var embed = new DiscordEmbedBuilder
            {
                Title = latency >= 0 ? "🏓 Pong!" : "❓ Pong?",
                Description = latency >= 0
                    ? $"Latency is: {latency} ms"
                    : "Failed to measure latency (network error?)",
                Url = "https://github.com/omgitsjan/DiscordBotAI",
                Timestamp = DateTimeOffset.UtcNow,
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = "omgitsjan/DiscordBot",
                    IconUrl = "https://avatars.githubusercontent.com/u/42674570?v=4"
                }
            };

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed)); // Edit the deferred response
            Program.Log(
                $"Command '{nameof(PingSlashCommandAsync)}' executed by {ctx.User.Username} ({ctx.User.Id}).");
        }

        /// <summary>
        /// Processes a prompt via the ChatGPT API and returns the AI's reply as an embed.
        /// </summary>
        public async Task ChatSlashCommandAsync(IInteractionContextWrapper ctx, string text)
        {
            await ctx.CreateResponseAsync(
                InteractionResponseType.DeferredChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent("Sending request to ChatGPT API..."));

            (bool success, string? message) = await openAiService.ChatGptAsync(text);

            var embed = new DiscordEmbedBuilder
            {
                Title = "ChatGPT",
                Description = message,
                Timestamp = DateTimeOffset.UtcNow,
                Author = new DiscordEmbedBuilder.EmbedAuthor
                {
                    Name = ctx.User.Username,
                    IconUrl = ctx.User.AvatarUrl
                },
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = "Powered by OpenAI",
                    IconUrl = "https://seeklogo.com/images/O/open-ai-logo-8B9BFEDC26-seeklogo.com.png"
                }
            };

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
            if (!success) Program.Log(message);
            Program.Log(
                $"Command '{nameof(ChatSlashCommandAsync)}' executed by {ctx.User.Username} ({ctx.User.Id}). Input: {text}");
        }

        /// <summary>
        /// Sends a prompt to DALL-E to generate an image and respond with an embed containing the image.
        /// </summary>
        public async Task ImageSlashCommandAsync(IInteractionContextWrapper ctx, string text)
        {
            await ctx.CreateResponseAsync(
                InteractionResponseType.DeferredChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent("Sending request to DALL-E API..."));

            (bool success, string message) = await openAiService.DallEAsync(text);

            string url = HttpRegex().Match(message).ToString();
            var embed = new DiscordEmbedBuilder
            {
                Title = "DALL-E",
                Description = message,
                ImageUrl = url,
                Timestamp = DateTimeOffset.UtcNow,
                Author = new DiscordEmbedBuilder.EmbedAuthor
                {
                    Name = ctx.User.Username,
                    IconUrl = ctx.User.AvatarUrl
                },
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = "Powered by OpenAI",
                    IconUrl = "https://seeklogo.com/images/O/open-ai-logo-8B9BFEDC26-seeklogo.com.png"
                }
            };

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
            if (!success) Program.Log(message);
            Program.Log(
                $"Command '{nameof(ImageSlashCommandAsync)}' executed by {ctx.User.Username} ({ctx.User.Id}). Input: {text}");
        }

        /// <summary>
        /// Creates a Watch2Gether room, responds with an embed containing the room link.
        /// </summary>
        public async Task Watch2GetherSlashCommandAsync(IInteractionContextWrapper ctx, string url)
        {
            await ctx.CreateResponseAsync(
                InteractionResponseType.DeferredChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent("Requesting Watch2Gether room..."));

            (bool success, string? message) = await watch2GetherService.CreateRoom(url);

            var embed = new DiscordEmbedBuilder
            {
                Title = "Watch2Gether Room!",
                Description = success
                    ? $"Your room is ready: {message}"
                    : message ?? "Error creating room.",
                Author = new DiscordEmbedBuilder.EmbedAuthor
                {
                    Name = ctx.User.Username,
                    IconUrl = ctx.User.AvatarUrl
                },
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = "Watch2Gether",
                    IconUrl = "https://w2g.tv/assets/256.f5817612.png"
                },
                Timestamp = DateTimeOffset.UtcNow
            };

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
            Program.Log(
                $"Command '{nameof(Watch2GetherSlashCommandAsync)}' executed by {ctx.User.Username} ({ctx.User.Id}).");
        }

        /// <summary>
        /// Gets weather for a city and replies with either an embed or error message.
        /// </summary>
        public async Task WeatherSlashCommandAsync(IInteractionContextWrapper ctx, string city)
        {
            await ctx.CreateResponseAsync(
                InteractionResponseType.DeferredChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent("Fetching weather data..."));

            (bool success, string message, WeatherData? weather) = await openWeatherMapService.GetWeatherAsync(city);

            if (success && weather != null)
            {
                var embed = new DiscordEmbedBuilder
                {
                    Title = $"Weather in {weather.City} - {weather.Temperature:F2}°C",
                    Description = message,
                    Author = new DiscordEmbedBuilder.EmbedAuthor
                    {
                        Name = ctx.User.Username,
                        IconUrl = ctx.User.AvatarUrl
                    },
                    Footer = new DiscordEmbedBuilder.EmbedFooter
                    {
                        Text = "Weather data by OpenWeatherMap",
                        IconUrl = "https://openweathermap.org/themes/openweathermap/assets/img/logo_white_cropped.png"
                    },
                    Timestamp = DateTimeOffset.UtcNow
                };
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
            }
            else
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(message));
            }

            Program.Log(
                $"Command '{nameof(WeatherSlashCommandAsync)}' executed by {ctx.User.Username} ({ctx.User.Id}). City: {city}");
        }

        /// <summary>
        /// Fetches and displays the price for a given cryptocurrency.
        /// </summary>
        public async Task CryptoSlashCommandAsync(IInteractionContextWrapper ctx, string symbol = "BTC", string physicalCurrency = "USDT")
        {
            symbol = symbol.ToUpperInvariant();

            await ctx.CreateResponseAsync(
                InteractionResponseType.DeferredChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent($"Requesting {symbol} from ByBit API..."));

            (bool success, string? message) = await cryptoService.GetCryptoPriceAsync(symbol, physicalCurrency);

            if (success)
            {
                var embed = new DiscordEmbedBuilder
                {
                    Title = $"{symbol} - {physicalCurrency} | ${message}",
                    Description = $"Price of {symbol} in {physicalCurrency}: ${message}",
                    Author = new DiscordEmbedBuilder.EmbedAuthor
                    {
                        Name = ctx.User.Username,
                        IconUrl = ctx.User.AvatarUrl
                    },
                    Footer = new DiscordEmbedBuilder.EmbedFooter
                    {
                        Text = "Data provided by ByBit",
                        IconUrl = "https://seeklogo.com/images/B/bybit-logo-4C31FD6A08-seeklogo.com.png"
                    },
                    Timestamp = DateTimeOffset.UtcNow
                };
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
            }
            else
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(message));
            }

            Program.Log(
                $"Command '{nameof(CryptoSlashCommandAsync)}' executed by {ctx.User.Username} ({ctx.User.Id}). Symbol: {symbol}");
        }

        [GeneratedRegex(@"http(s)?://([\w-]+\.)+[\w-]+(/[\w- ./?%&=]*)?")]
        private static partial Regex HttpRegex();
    }
}
