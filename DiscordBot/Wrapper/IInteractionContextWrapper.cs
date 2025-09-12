using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using System.Threading.Tasks;

namespace DiscordBot.Wrapper
{
    /// <summary>
    /// Interface for an abstraction of the DSharpPlus BaseContext for use in commands and testing.
    /// </summary>
    public interface IInteractionContextWrapper
    {
        /// <summary>
        /// The Discord channel this interaction is using.
        /// </summary>
        DiscordChannel Channel { get; }

        /// <summary>
        /// The Discord user performing this interaction.
        /// </summary>
        DiscordUser User { get; }

        /// <summary>
        /// Allows test code to inject fake user/channel for isolated testing.
        /// </summary>
        void SetUpForTesting(DiscordChannel? discordChannel, DiscordUser? discordUser);

        /// <summary>
        /// Sends an interaction response (e.g. deferred, immediate, etc.).
        /// </summary>
        Task CreateResponseAsync(InteractionResponseType type, DiscordInteractionResponseBuilder? builder = null);

        /// <summary>
        /// Deletes the original interaction response, if allowed.
        /// </summary>
        Task DeleteResponseAsync();

        /// <summary>
        /// Edits the original interaction response (e.g. for deferred responses).
        /// </summary>
        Task EditResponseAsync(DiscordWebhookBuilder builder);

        /// <summary>
        /// Sends a simple message to the underlying Discord channel.
        /// </summary>
        Task<DiscordMessage> SendMessageAsync(string content, DiscordEmbed? embed = null);
    }
}
