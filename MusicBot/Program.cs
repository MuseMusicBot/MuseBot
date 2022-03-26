using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MusicBot.Helpers;
using MusicBot.Services;
using System;
using System.IO;
using System.Threading.Tasks;
using Victoria;

// TODO: Possibly add interactive services?
// TODO: Update Victoria
namespace MusicBot
{
    class Program
    {
        private DiscordSocketClient discord;
        public static ConfigHelper.Config BotConfig { get; set; }

        static void Main()
            => new Program().MainAsync().GetAwaiter().GetResult();

        private async Task MainAsync()
        {
            if (!File.Exists(ConfigHelper.ConfigName))
            {
                ConfigHelper.CreateConfigFile();
            }

            BotConfig = ConfigHelper.LoadConfigFile();

            discord = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Info,
                GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.GuildMessageReactions | GatewayIntents.GuildVoiceStates
            });

            var services = ConfigureServices();
            var loggingService = services.GetRequiredService<LoggingService>();
            await services.GetRequiredService<CommandHandlerService>().InitializeAsync(services);
            services.GetRequiredService<ReactionsHelper>();
            await discord.LoginAsync(TokenType.Bot, BotConfig.Token);
            await discord.StartAsync();

            discord.GuildAvailable += OnGuildAvaiable;

            discord.Ready += async () =>
            {
                _ = Task.Run(async () =>
                {
                    var node = services.GetRequiredService<LavaNode>();

                    if (!node.IsConnected)
                    {
                        await node.ConnectAsync();
                    }
                });
                //Sets Listening activity
                await discord.SetGameAsync("music", type: ActivityType.Listening);
            };

            // Voice state updated handler
            // user -> SocketUser
            // before -> VoiceState before
            // after -> VoiceState after
            // discord.UserVoiceStateUpdated += async (user, before, after) =>
            // {
            //     if (user.Id == discord.CurrentUser.Id && before.VoiceChannel == null)
            //     {
            //         try
            //         {
            //             var node = services.GetRequiredService<LavaNode>();
            //             var player = node.GetPlayer((user as IGuildUser).Guild);
            //             await player.UpdateVolumeAsync(BotConfig.Volume);
            //         }
            //         catch { }
            //     }
            // };

            // Trap Ctrl+C
            Console.CancelKeyPress += (s, e) =>
            {
                BotExit(services);
            };

            // Trap process exit
            AppDomain.CurrentDomain.ProcessExit += (s, e) =>
            {
                BotExit(services);
            };

            await Task.Delay(-1);
        }

        /// <summary>
        /// Bot exit function fired when application is closing or ctrl+c is pressed
        /// </summary>
        /// <param name="services"><see cref="IServiceProvider"> to get services from</param>
        private void BotExit(IServiceProvider services)
        {
            var node = services.GetRequiredService<LavaNode>();
            var embedHelper = services.GetRequiredService<EmbedHelper>();
            foreach (var player in node.Players)
            {
                try
                {
                    node.LeaveAsync(player.VoiceChannel);
                    BotConfig.BotEmbedMessage.ModifyAsync(async (x) =>
                    {
                        x.Content = AudioHelper.NoSongsInQueue;
                        x.Embed = await embedHelper.BuildDefaultEmbed();
                    });
                }
                catch (Exception e)
                {
                    var logging = services.GetRequiredService<LoggingService>();
                    var logger = logging.Loggers[LoggingService.LoggerEntries.Discord];

                    logger.LogError(e, "");
                }

                BotConfig.BotEmbedMessage.ModifyAsync(async (x) =>
                {
                    x.Content = AudioHelper.NoSongsInQueue;
                    x.Embed = await embedHelper.BuildDefaultEmbed();
                });

                ConfigHelper.UpdateConfigFile(BotConfig);
            }
        }

        /// <summary>
        /// On Guild Available event handler
        /// </summary>
        /// <param name="guild">Guild that has become available</param>
        /// <returns>void</returns>
        private async Task OnGuildAvaiable(SocketGuild guild)
        {
            // Check if BotConfig has been loaded yet
            // Will not fire if setup has not been performed
            if (BotConfig.GuildId != 0 && BotConfig.ChannelId != 0 && BotConfig.MessageId != 0 && BotConfig.BotEmbedMessage == null)
            {
                var config = BotConfig;
                config.BotEmbedMessage = await guild.GetTextChannel(config.ChannelId).GetMessageAsync(config.MessageId) as IUserMessage;
                BotConfig = config;
            }
        }

        /// <summary>
        /// Creates the Dependency Injection for this bot
        /// </summary>
        /// <returns>DI for bot</returns>
        private IServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                .AddSingleton(discord)
                .AddSingleton<CommandService>()
                .AddLavaNode(x =>
                {
                    x.Authorization = BotConfig.LavalinkPassword;
                    x.Hostname = BotConfig.LavalinkHost;
                    x.Port = (ushort)BotConfig.LavalinkPort;
                    x.LogSeverity = LogSeverity.Debug;
                })
                .AddSingleton<EmbedHelper>()
                .AddSingleton<AudioHelper>()
                .AddSingleton<CommandHandlerService>()
                .AddSingleton<ReactionsHelper>()
                .AddSingleton<Commands.MuseCommands>()
                .AddSingleton<Commands.HelpCommands>()
                .AddLogging()
                .AddSingleton<LoggingService>()
                .BuildServiceProvider();
        }
    }
}
