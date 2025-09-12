using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using System.Threading.Tasks;

namespace DiscordBot.Wrapper
{
    /// <summary>
    /// Wraps a DSharpPlus context for dependency injection or unit test scenarios.
    /// Exposes only relevant functionality for command/Admin logic.
    /// </summary>
    public class InteractionContextWrapper(BaseContext context) : IInteractionContextWrapper
    {
        private DiscordChannel? _discordChannel;
        private DiscordUser? _discordUser;

        /// <summary>
        /// Allows test code to inject fake user/channel for isolated testing.
        /// </summary>
        public void SetUpForTesting(DiscordChannel? discordChannel, DiscordUser? discordUser)
        {
            _discordChannel = discordChannel;
            _discordUser = discordUser;
        }

        /// <summary>
        /// The Discord channel this interaction is using.
        /// </summary>
        public DiscordChannel Channel => _discordChannel ?? context.Channel;

        /// <summary>
        /// The Discord user performing this interaction.
        /// </summary>
        public DiscordUser User => _discordUser ?? context.User;

        /// <summary>
        /// Sends an interaction response (e.g. deferred, immediate, etc.).
        /// </summary>
        public Task CreateResponseAsync(InteractionResponseType type, DiscordInteractionResponseBuilder? builder = null)
        {
            return context.CreateResponseAsync(type, builder);
        }

        /// <summary>
        /// Deletes the original interaction response, if allowed.
        /// </summary>
        public Task DeleteResponseAsync()
        {
            return context.DeleteResponseAsync();
        }

        /// <summary>
        /// Sends a message to the command's channel (outside interaction).
        /// </summary>
        public Task<DiscordMessage> SendMessageAsync(string content, DiscordEmbed? embed = null)
        {
            return context.Channel.SendMessageAsync(content, embed);
        }

        /// <summary>
        /// Edits the original interaction response (e.g. for deferred responses).
        /// </summary>
        /// <param name="builder">Webhook builder for content, embeds, etc.</param>
        public Task EditResponseAsync(DiscordWebhookBuilder builder)
        {
            return context.EditResponseAsync(builder);
        }
    }
}
