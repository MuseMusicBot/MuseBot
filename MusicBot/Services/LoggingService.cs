using System;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using MusicBot.Helpers;

namespace MusicBot.Services
{
    public class LoggingService
    {
        private readonly DiscordSocketClient discord;
        private readonly CommandService commandService;
        private readonly ILoggerFactory logger;
        private readonly ILogger commandLogger;
        private readonly ILogger discordLogger;

        public LoggingService(DiscordSocketClient client, CommandService cmdService, ILoggerFactory loggerFactory)
        {
            discord = client;
            commandService = cmdService;
            logger = ConfigureLogging(loggerFactory);
            discordLogger = logger.CreateLogger("Discord");
            commandLogger = logger.CreateLogger("Commands");
            
            discord.Log += LogDiscord;
            commandService.Log += LogCommandService;
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

        private Task LogCommandService(LogMessage message)
        {
            if (message.Exception is CommandException cmd)
            {
                // Sends error as a message to original channel
                var _ = cmd.Context.Channel.SendMessageAsync($"Error: {cmd.Message}");
            }

            commandLogger.Log(
                LogLevelFromSeverity(message.Severity),
                0,
                message,
                message.Exception,
                (_1, _2) => message.ToString(prependTimestamp: false));
            return Task.CompletedTask;
        }

    }
}
