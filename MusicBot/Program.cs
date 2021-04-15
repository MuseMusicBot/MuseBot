using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using MusicBot.Services;
using MusicBot.Helpers;
using System;
using System.Threading.Tasks;
using Victoria;
using System.IO;
using Microsoft.Extensions.Logging;

namespace MusicBot
{
    class Program
    {
        private DiscordSocketClient discord;
        //private Task LavalinkTask;
        public static ushort Volume = 5;

        public static int count = 0;
        public static int endcount = 0;
        public static IUserMessage message;
        public const string testConfig = "testConfig.txt";
        private ILogger victoriaLogger;

        static void Main(string[] args) =>
            new Program().MainAsync(args).GetAwaiter().GetResult();

        private async Task MainAsync(string[] args)
        {
            string token = "MTk2MDY3MjYyMDY5NzM1NDM0.V23X3g.5d7fjXNoohw1w2N3smANm8WYmQE";

            discord = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Info,
                GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.GuildMessageReactions | GatewayIntents.GuildVoiceStates
            });


            var services = ConfigureServices();

            //LavalinkTask = Task.Factory.StartNew(() =>
            //{
            //    var logging = services.GetRequiredService<LoggingService>();
            //    var logger = logging.CreateLogger("LavaLink");
            //    Process p = new Process
            //    {
            //        StartInfo = new ProcessStartInfo
            //        {
            //            FileName = "java.exe",
            //            WorkingDirectory = @"C:\\tools",
            //            Arguments = "-jar lavalink.jar",
            //            RedirectStandardOutput = true,
            //            RedirectStandardError = true
            //        }
            //    };

            //    if (!p.Start())
            //    {
            //        p.Kill();
            //        Console.WriteLine("Lavalink did not start properly, stopping execution.");
            //        Environment.Exit(255);
            //    }

            //    while (!p.HasExited)
            //    {
            //        logger.LogInformation(p.StandardOutput.ReadToEnd());
            //    }


            //    Task.Delay(-1);
            //});


            var loggingService = services.GetRequiredService<LoggingService>();
            await services.GetRequiredService<CommandHandlerService>().InitializeAsync(services);
            var config = services.GetRequiredService<LavaConfig>();
            config.Authorization = "youshallnotpass";
            var node = services.GetRequiredService<LavaNode>();

            victoriaLogger = loggingService.CreateLogger("Victoria");
            node.OnLog += LavaNodeOnLog;

            await discord.LoginAsync(TokenType.Bot, token);
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

                    message = await discord.GetGuild(guildId).GetTextChannel(chnlId).GetMessageAsync(msgId) as IUserMessage;
                }

                //Sets Listening activity
                await discord.SetGameAsync("music", type:ActivityType.Listening);
            };

            // trap ctrl+c
            Console.CancelKeyPress += (s, e) =>
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
            victoriaLogger.Log(
            LoggingService.LogLevelFromSeverity(message.Severity),
            0,
            message,
            message.Exception,
            (_1, _2) => message.ToString(prependTimestamp: false));

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
                .AddSingleton<Commands.MuseCommands>()
                .AddLogging()
                .AddSingleton<LoggingService>()
                .BuildServiceProvider();
        }
    }
}
