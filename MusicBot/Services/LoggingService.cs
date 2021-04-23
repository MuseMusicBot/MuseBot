using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using MusicBot.Helpers;
using System;
using System.Threading.Tasks;
using Victoria;

namespace MusicBot.Services
{
    public class LoggingService
    {
        private readonly DiscordSocketClient discord;
        private readonly CommandService commandService;
        private readonly ILoggerFactory logger;
        private readonly ILogger commandLogger;
        private readonly ILogger discordLogger;
        private readonly ILogger victoriaLogger;

        public LoggingService(DiscordSocketClient client, LavaNode lavaNode, CommandService cmdService, ILoggerFactory loggerFactory)
        {
            discord = client;
            commandService = cmdService;
            logger = ConfigureLogging(loggerFactory);
            discordLogger = logger.CreateLogger("Discord");
            commandLogger = logger.CreateLogger("Commands");
            victoriaLogger = logger.CreateLogger("Victoria");

            discord.Log += LogDiscord;
            commandService.Log += LogCommandService;
            lavaNode.OnLog += LavaNodeOnLog;
        }

        public ILogger CreateLogger(string loggerName)
            => logger.CreateLogger(loggerName);

        private ILoggerFactory ConfigureLogging(ILoggerFactory loggerFactory)
        {
            loggerFactory = LoggerFactory.Create(x => x.AddConsole());
            return loggerFactory;
        }

        public static LogLevel LogLevelFromSeverity(LogSeverity severity)
            => (LogLevel)Math.Abs((int)severity - 5);

        private Task LogDiscord(LogMessage message)
        {
            discordLogger.LogMessage(message);
            return Task.CompletedTask;
        }

        private Task LavaNodeOnLog(LogMessage message)
        {
            victoriaLogger.LogMessage(message);
            return Task.CompletedTask;
        }

        private Task LogCommandService(LogMessage message)
        {
            if (message.Exception is CommandException cmd)
            {
                // Sends error as a message to original channel
                var _ = cmd.Context.Channel.SendMessageAsync($"Error: {cmd.Message}");
            }

            commandLogger.LogMessage(message);
            return Task.CompletedTask;
        }

    }
}
