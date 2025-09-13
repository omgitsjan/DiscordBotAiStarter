using System.Diagnostics;
using DiscordBot.Interfaces;
using DiscordBot.Services;
using DiscordBot.Wrapper;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;
using RestSharp;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using Timer = System.Timers.Timer;

namespace DiscordBot
{
    /// <summary>
    /// Program class: Main entry point for the Discord Bot.
    /// Handles application setup, dependency injection,
    /// Discord client lifecycle, and status updates.
    /// </summary>
    public class Program
    {
        private string? _discordToken;

        /// <summary>
        /// The Discord client instance for bot communication.
        /// </summary>
        public DiscordClient? Client { get; private set; }

        /// <summary>
        /// Global logger instance used throughout the application.
        /// </summary>
        public static ILogger? Logger { get; private set; }

        /// <summary>
        /// Global helper service for various utility functions.
        /// </summary>
        public static IHelperService? HelperService { get; private set; }

        /// <summary>
        /// Crypto service for price lookup.
        /// </summary>
        public static ICryptoService? CryptoService { get; private set; }

        /// <summary>
        /// Application entry point (async).
        /// </summary>
        public static Task Main() => new Program().MainAsync();

        /// <summary>
        /// Main application logic: initializes configuration, DI, Discord client, services, and starts the bot.
        /// </summary>
        public async Task MainAsync()
        {
            try
            {
                // Setup NLog from configuration file
                LogManager.Setup().LoadConfigurationFromFile("nlog.config");

                // Load application configuration from file and environment
                IConfigurationRoot configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddEnvironmentVariables()
                    .Build();

                // Setup dependency injection
                await using ServiceProvider services = ConfigureServices(configuration);

                Logger = services.GetRequiredService<ILogger<Program>>();
                HelperService = services.GetService<IHelperService>();
                CryptoService = services.GetService<ICryptoService>();

                Log("Starting DiscordBot...", LogLevel.Information);
                Log("Initializing services...", LogLevel.Information);

                // Retrieve and validate Discord token
                _discordToken = configuration["DiscordBot:Token"];
                if (string.IsNullOrWhiteSpace(_discordToken))
                {
                    Log("❌ Discord token is missing! Please check appsettings.json or your environment variables.", LogLevel.Critical);
                    Environment.Exit(1);
                    return;
                }
                Log($"Token loaded (ends with: ...{_discordToken[^4..]})", LogLevel.Debug);

                // Configure the Discord client
                DiscordConfiguration discordConfig = new()
                {
                    Token = _discordToken,
                    TokenType = TokenType.Bot,
                    Intents = DiscordIntents.AllUnprivileged | DiscordIntents.GuildMessages,
                    LoggerFactory = services.GetService<ILoggerFactory>(),
                    AutoReconnect = true
                };
                Client = new DiscordClient(discordConfig);

                // Register slash commands using the DI context
                var slashExt = Client.UseSlashCommands(new SlashCommandsConfiguration
                {
                    Services = services
                });
                slashExt.RegisterCommands<SlashCommands>();

                // Enable interactivity extension
                Client.UseInteractivity(new InteractivityConfiguration
                {
                    Timeout = TimeSpan.FromMinutes(1)
                });

                // Optionally register event handlers for diagnostics
                Client.Ready += OnClientReady;
                Client.GuildAvailable += OnGuildAvailable;

                Log("🚀 Connecting to Discord...", LogLevel.Information);
                await Client.ConnectAsync();

                StartStatusRotation();

                Log("✅ Bot is online!", LogLevel.Information);
                // Block the main thread
                await Task.Delay(-1);
            }
            catch (Exception ex)
            {
                Log($"❌ Fatal error: {ex.Message}", LogLevel.Critical);
                Environment.Exit(1);
            }
        }

        /// <summary>
        /// Called when the bot has connected to Discord and is ready.
        /// </summary>
        private Task OnClientReady(DiscordClient sender, DSharpPlus.EventArgs.ReadyEventArgs args)
        {
            Log($"Bot is ready as {sender.CurrentUser.Username}#{sender.CurrentUser.Discriminator}", LogLevel.Information);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Called when a guild (server) becomes available.
        /// </summary>
        private Task OnGuildAvailable(DiscordClient sender, DSharpPlus.EventArgs.GuildCreateEventArgs args)
        {
            Log($"Connected to guild: {args.Guild.Name} ({args.Guild.MemberCount} members)", LogLevel.Debug);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Periodically rotates the bot status (crypto price, date, uptime, user count, dev excuse, branding).
        /// </summary>
        private void StartStatusRotation()
        {
            int statusIndex = 0;
            var timer = new Timer(20000); // 20s per status
            timer.Elapsed += async (_, _) =>
            {
                try
                {
                    if (Client == null) return;
                    DiscordActivity activity = statusIndex switch
                    {
                        0 => await GetCryptoStatusAsync(),
                        1 => new DiscordActivity($"Date: {DateTime.UtcNow:dd.MM.yyyy HH:mm}", ActivityType.Watching),
                        2 => new DiscordActivity($"Time: {DateTime.UtcNow:HH:mm} UTC", ActivityType.Watching),
                        3 => new DiscordActivity($"Uptime: {GetUptimeString()}", ActivityType.Watching),
                        4 => new DiscordActivity($"Available to '{GetTotalMemberCount()}' Users", ActivityType.Watching),
                        5 => await GetExcuseStatusAsync(),
                        _ => new DiscordActivity("omgitsjan/DiscordBotAI | janpetry.de", ActivityType.Watching)
                    };
                    await Client.UpdateStatusAsync(activity);
                    statusIndex = (statusIndex + 1) % 7;
                }
                catch (Exception ex)
                {
                    Log($"Status update error: {ex.Message}", LogLevel.Warning);
                }
            };
            timer.AutoReset = true;
            timer.Enabled = true;
        }

        /// <summary>
        /// Builds a DiscordActivity with the current BTC/USDT price.
        /// </summary>
        private static async Task<DiscordActivity> GetCryptoStatusAsync()
        {
            if (CryptoService != null)
            {
                var (success, price) = await CryptoService.GetCryptoPriceAsync("BTC", "USDT");
                if (success)
                {
                    return new DiscordActivity($"BTC: ${(price.Length > 110 ? price[..110] : price)}", ActivityType.Watching);
                }
                else
                {
                    return new DiscordActivity("Failed to fetch BTC Price...", ActivityType.Watching);
                }
            }
            return new DiscordActivity("CryptoService not available", ActivityType.Watching);
        }

        /// <summary>
        /// Builds a DiscordActivity with a random developer excuse.
        /// </summary>
        private static async Task<DiscordActivity> GetExcuseStatusAsync()
        {
            if (HelperService != null)
            {
                var excuse = await HelperService.GetRandomDeveloperExcuseAsync();
                return new DiscordActivity($"Excuse: {(excuse.Length > 110 ? excuse[..110] : excuse)}", ActivityType.ListeningTo);
            }
            return new DiscordActivity("No excuse available", ActivityType.ListeningTo);
        }

        /// <summary>
        /// Returns the formatted process uptime.
        /// </summary>
        private static string GetUptimeString()
        {
            var uptime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime();
            return $"{uptime.Days}d {uptime.Hours}h {uptime.Minutes}m";
        }

        /// <summary>
        /// Returns the total member count across all connected guilds.
        /// </summary>
        private int GetTotalMemberCount()
        {
            return Client?.Guilds?.Sum(g => g.Value.MemberCount) ?? 0;
        }

        /// <summary>
        /// Centralized logging: logs to NLog and console (fallback).
        /// </summary>
        internal static void Log(string? msg, LogLevel logLevel = LogLevel.Information)
        {
            if (Logger != null)
            {
                Logger.Log(logLevel, "{Message}", msg);
            }
            else
            {
                var timestamp = DateTime.Now.ToString("HH:mm:ss");
                var level = logLevel.ToString().ToUpper().PadRight(11);
                Console.WriteLine($"[{timestamp}] [{level}] {msg}");
            }
        }

        /// <summary>
        /// Configures the dependency injection container and registers all required services.
        /// </summary>
        /// <param name="configuration">The application configuration root.</param>
        /// <returns>A ServiceProvider containing all registered dependencies.</returns>
        private static ServiceProvider ConfigureServices(IConfiguration configuration)
        {
            return new ServiceCollection()
                .AddSingleton(configuration)
                .AddSingleton<IHttpService, HttpService>()
                .AddSingleton<IWatch2GetherService, Watch2GetherService>()
                .AddSingleton<IOpenWeatherMapService, OpenWeatherMapService>()
                .AddSingleton<IOpenAiService, OpenAiService>()
                .AddSingleton<ICryptoService, CryptoService>()
                .AddSingleton<IHelperService, HelperService>()
                .AddSingleton<IInteractionContextWrapper, InteractionContextWrapper>()
                .AddSingleton<ISlashCommandsService, SlashCommandsService>()
                .AddSingleton<SlashCommands>()
                .AddSingleton<IRestClient>(_ => new RestClient())
                .AddLogging(builder => builder.AddNLog())
                .BuildServiceProvider();
        }

        /// <summary>
        /// Returns true if the application was built in Debug mode.
        /// </summary>
        private static bool IsDebug()
        {
#if DEBUG
            return true;
#else
            return false;
#endif
        }
    }
}
