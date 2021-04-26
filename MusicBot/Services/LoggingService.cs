﻿using Discord;
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

        /// <summary>
        /// ctor for Logging service
        /// </summary>
        /// <param name="client">DiscordSocketClient from DI</param>
        /// <param name="lavaNode">LavaNode from DI</param>
        /// <param name="cmdService">CommandService from DI</param>
        /// <param name="loggerFactory">ILoggerFactory from DI</param>
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

        /// <summary>
        /// Creates a new logger object from the logger factory
        /// </summary>
        /// <param name="loggerName">Logger name</param>
        /// <returns>new Logger</returns>
        public ILogger CreateLogger(string loggerName)
            => logger.CreateLogger(loggerName);

        /// <summary>
        /// Creates a new LoggerFactory
        /// </summary>
        /// <param name="loggerFactory">LoggerFactory from DI</param>
        /// <returns>Configured LoggerFactory</returns>
        private ILoggerFactory ConfigureLogging(ILoggerFactory loggerFactory)
        {
            loggerFactory = LoggerFactory.Create(x => x.AddConsole());
            return loggerFactory;
        }

        /// <summary>
        /// Coverts Discord Log Severity to MS Log Level
        /// </summary>
        /// <param name="severity">LogMessage severity</param>
        /// <returns>LogSevirity converted to LogLevel</returns>
        public static LogLevel LogLevelFromSeverity(LogSeverity severity)
            => (LogLevel)Math.Abs((int)severity - 5);

        /// <summary>
        /// Logs Discord log message
        /// </summary>
        /// <param name="message">Log Message to send to Logger</param>
        /// <returns></returns>
        private Task LogDiscord(LogMessage message)
        {
            discordLogger.LogMessage(message);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Logs Victoria log message
        /// </summary>
        /// <param name="message">Log Message to send to Logger</param>
        /// <returns></returns>
        private Task LavaNodeOnLog(LogMessage message)
        {
            victoriaLogger.LogMessage(message);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Logs CommandService log message
        /// </summary>
        /// <param name="message">Log Message to send to Logger</param>
        /// <returns></returns>
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
