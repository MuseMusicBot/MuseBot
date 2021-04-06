using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using MusicBot.Services;
using MusicBot.Helpers;
using System;
using System.Threading.Tasks;
using Victoria;

namespace MusicBot
{
    class Program
    {
        private DiscordSocketClient discord;
        //private Task LavalinkTask;
        public static ushort Volume = 25;

        public static int count = 0;
        public static int endcount = 0;
        public static IUserMessage message;

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


            services.GetRequiredService<LoggingService>();
            await services.GetRequiredService<CommandHandlerService>().InitializeAsync(services);
            var config = services.GetRequiredService<LavaConfig>();
            config.Authorization = "youshallnotpass";
            await discord.LoginAsync(TokenType.Bot, token);
            await discord.StartAsync();

            discord.Ready += async () =>
            {
                var node = services.GetRequiredService<LavaNode>();
                if (!node.IsConnected)
                {
                    await node.ConnectAsync();
                }
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

                discord.GetGuild(192866874185220098).GetTextChannel(message.Channel.Id).DeleteAsync();
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

                discord.GetGuild(192866874185220098).GetTextChannel(message.Channel.Id).DeleteAsync();
            };

            await Task.Delay(-1);

        }

        private IServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                .AddSingleton(discord)
                .AddSingleton<CommandService>()
                .AddSingleton<LavaNode>()
                .AddSingleton<LavaConfig>()
                .AddSingleton<AudioHelper>()
                .AddSingleton<MessageHelper>()
                .AddSingleton<CommandHandlerService>()
                .AddSingleton<Commands.TestCommands>()
                .AddLogging()
                .AddSingleton<LoggingService>()
                .BuildServiceProvider();
        }
    }
}
