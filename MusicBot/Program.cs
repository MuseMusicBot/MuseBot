using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using MusicBot.Services;
using MusicBot.Helpers;
using System;
using System.Threading.Tasks;
using System.IO;
using Victoria;
using Microsoft.Extensions.Logging;

namespace MusicBot
{
    class Program
    {
        private DiscordSocketClient discord;
        public static ushort Volume = 5;
        public static IUserMessage message;
        public const string testConfig = "testConfig.txt";
        private ILogger victoriaLogger;
        public static ConfigHelper.Config BotConfig { get; set; }

        static void Main()
            => new Program().MainAsync().GetAwaiter().GetResult();

        private async Task MainAsync()
        {
            if (!int.TryParse(ProcessHelper.GetJavaVersion(), out int javaVer))
            {
                Console.WriteLine("Couldn't get the Java version installed.\n" +
                    "Maybe Java is not installed or in PATH?");
                return;
            }

            if (javaVer < 11)
            {
                Console.WriteLine("Java version 11 or greater required.");
                return;
            }

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
            var config = services.GetRequiredService<LavaConfig>();
            config.Authorization = "youshallnotpass";
            var node = services.GetRequiredService<LavaNode>();

            victoriaLogger = loggingService.CreateLogger("Victoria");
            node.OnLog += LavaNodeOnLog;

            await discord.LoginAsync(TokenType.Bot, BotConfig.Token);
            await discord.StartAsync();

            discord.Ready += async () =>
            {
                var node = services.GetRequiredService<LavaNode>();

                if (!node.IsConnected)
                {
                    await node.ConnectAsync();
                }

                if (File.Exists(testConfig))
                {
                    var msgIds = (await File.ReadAllLinesAsync(testConfig));
                    var guildId = ulong.Parse(msgIds[0]);
                    var chnlId = ulong.Parse(msgIds[1]);
                    var msgId = ulong.Parse(msgIds[2]);

                    var config = BotConfig;
                    config.GuildId = guildId;
                    config.ChannelId = chnlId;
                    config.MessageId = msgId;

                    BotConfig = config;

                    message = await discord.GetGuild(guildId).GetTextChannel(chnlId).GetMessageAsync(msgId) as IUserMessage;
                }

                //Sets Listening activity
                await discord.SetGameAsync("music", type: ActivityType.Listening);
            };

            // trap ctrl+c
            Console.CancelKeyPress += (s, e) =>
            {
                var node = services.GetRequiredService<LavaNode>();
                var embedHelper = services.GetRequiredService<EmbedHelper>();
                foreach (var player in node.Players)
                {
                    try
                    {
                        node.LeaveAsync(player.VoiceChannel);
                    }
                    catch { }
                }

                Program.message.ModifyAsync(async (x) =>
                {
                    x.Content = AudioHelper.NoSongsInQueue;
                    x.Embed = await embedHelper.BuildDefaultEmbed();
                });
            };

            // trap process exit
            AppDomain.CurrentDomain.ProcessExit += (s, e) =>
            {
                var node = services.GetRequiredService<LavaNode>();
                foreach (var player in node.Players)
                {
                    try
                    {
                        node.LeaveAsync(player.VoiceChannel);
                    }
                    catch { }
                }
            };

            await Task.Delay(-1);

        }

        private Task LavaNodeOnLog(LogMessage message)
        {
            victoriaLogger.LogMessage(message);
            return Task.CompletedTask;
        }

        private IServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                .AddSingleton(discord)
                .AddSingleton<CommandService>()
                .AddSingleton<LavaNode>()
                .AddSingleton<LavaConfig>()
                .AddSingleton<EmbedHelper>()
                .AddSingleton<AudioHelper>()
                .AddSingleton<CommandHandlerService>()
                .AddSingleton<ReactionsHelper>()
                .AddSingleton<Commands.MuseCommands>()
                .AddLogging()
                .AddSingleton<LoggingService>()
                .BuildServiceProvider();
        }
    }
}
