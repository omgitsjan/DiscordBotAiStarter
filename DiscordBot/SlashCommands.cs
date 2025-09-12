using DiscordBot.Interfaces;
using DiscordBot.Wrapper;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using System.Threading.Tasks;

namespace DiscordBot
{
    /// <summary>
    /// Defines available Slash Commands for the Discord bot.
    /// Uses rich responses and Discord best practices for user experience.
    /// </summary>
    public class SlashCommands(ISlashCommandsService slashCommandsService) : ApplicationCommandModule
    {
        /// <summary>
        /// Basic ping: checks if the bot is online and displays current latency.
        /// </summary>
        [SlashCommand("ping", "Check if the bot is online and view the latency.")]
        public async Task PingSlashCommand(InteractionContext ctx)
        {
            var context = new InteractionContextWrapper(ctx);
            await slashCommandsService.PingSlashCommandAsync(context);
        }

        /// <summary>
        /// Send a prompt to ChatGPT and receive a generated response.
        /// </summary>
        [SlashCommand("chatgpt", "Send a prompt to ChatGPT and get an AI-powered reply.")]
        public async Task ChatSlashCommand(
            InteractionContext ctx,
            [Option("prompt", "Your question or prompt for the ChatGPT AI.")]
            string text)
        {
            var context = new InteractionContextWrapper(ctx);
            await slashCommandsService.ChatSlashCommandAsync(context, text);
        }

        /// <summary>
        /// Generates an image using OpenAI DALL-E based on the given prompt.
        /// </summary>
        [SlashCommand("dall-e", "Generate an image with DALL-E from your description.")]
        public async Task ImageSlashCommand(
            InteractionContext ctx,
            [Option("prompt", "Describe how the generated image should look.")]
            string text)
        {
            var context = new InteractionContextWrapper(ctx);
            await slashCommandsService.ImageSlashCommandAsync(context, text);
        }

        /// <summary>
        /// Creates a shared Watch2Gether room for users.
        /// </summary>
        [SlashCommand("watch2gether", "Create a Watch2Gether room for you and your friends.")]
        public async Task Watch2GetherSlashCommand(
            InteractionContext ctx,
            [Option("video-url", "An optional video URL to start playing immediately.")]
            string url = "")
        {
            var context = new InteractionContextWrapper(ctx);
            await slashCommandsService.Watch2GetherSlashCommandAsync(context, url);
        }

        /// <summary>
        /// Fetches and displays the current weather for the specified city.
        /// </summary>
        [SlashCommand("weather", "Get the current weather for a specified city.")]
        public async Task WeatherSlashCommand(
            InteractionContext ctx,
            [Option("city", "The city to retrieve weather data for.")]
            string city)
        {
            var context = new InteractionContextWrapper(ctx);
            await slashCommandsService.WeatherSlashCommandAsync(context, city);
        }

        /// <summary>
        /// Gets the current price of a specified cryptocurrency.
        /// </summary>
        [SlashCommand("crypto", "Get the price for a specific cryptocurrency symbol.")]
        public async Task CryptoSlashCommand(
            InteractionContext ctx,
            [Option("symbol", "The cryptocurrency symbol, e.g. BTC (default: BTC).")]
            string symbol = "BTC",
            [Option("currency", "The comparison currency, e.g. USDT (default: USDT).")]
            string currency = "USDT")
        {
            var context = new InteractionContextWrapper(ctx);
            await slashCommandsService.CryptoSlashCommandAsync(context, symbol, currency);
        }
    }
}
