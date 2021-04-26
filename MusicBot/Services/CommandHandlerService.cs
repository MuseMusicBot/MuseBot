﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MusicBot.Helpers;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Victoria;
using Victoria.Enums;

namespace MusicBot.Services
{
    public class CommandHandlerService
    {
        private readonly DiscordSocketClient discord;
        private readonly CommandService commands;
        private IServiceProvider provider;
        private readonly AudioHelper ah;
        private readonly LavaNode node;
        public EmbedHelper embedHelper;

        public CommandHandlerService(DiscordSocketClient discord, CommandService commands, IServiceProvider provider, AudioHelper audioHelper, LavaNode lavanode, EmbedHelper eh)
        {
            this.discord = discord;
            this.commands = commands;
            this.provider = provider;
            ah = audioHelper;
            node = lavanode;
            embedHelper = eh;

            this.discord.MessageReceived += MessageReceived;
        }

        private async Task MessageReceived(SocketMessage socketMessage)
        {
            if (!(socketMessage is SocketUserMessage message))
                return;

            if (message.Source != MessageSource.User)
                return;

            if (message.Author.IsBot)
                return;

            var context = new SocketCommandContext(discord, message);

            int argPos = 0;

            if (message.Content != "m?setup" && message.Channel.Id != Program.BotConfig.ChannelId)
            {
                if (message.HasStringPrefix("m?", ref argPos))
                {
                    _ = Task.Run(async () =>
                    {
                        await message.DeleteAsync();
                        var msg = await embedHelper.BuildMessageEmbed(Color.Orange, $"This command is restrcited to <#{Program.BotConfig.ChannelId}>.");
                        await (await context.Channel.SendMessageAsync(embed: msg)).RemoveAfterTimeout(15000);
                    });
                }
                return;
            }

            if (message.Content != "m?premium" && (message.Author as IGuildUser)?.VoiceChannel == null)
            {
                _ = Task.Run(async () =>
                {
                    await message.DeleteAsync();
                    var msg = await embedHelper.BuildMessageEmbed(Color.Orange, "You have to be in a voice channel.");
                    await (await context.Channel.SendMessageAsync(embed: msg)).RemoveAfterTimeout();
                });
                return;
            }

            if (!message.HasStringPrefix("m?", ref argPos))
            {
                _ = Task.Run(async () =>
                {
                    await ah.SearchForTrack(context, message.Content);
                });

                await message.DeleteAsync();
                return;
            }

            var result = await commands.ExecuteAsync(context, argPos, provider);

            if (result.Error.HasValue &&
                result.Error.Value == CommandError.UnknownCommand)
                return;
            if (result.Error.HasValue &&
                result.Error.Value != CommandError.UnknownCommand)
                // TODO Look at custom parse errors
                await context.Channel.SendMessageAsync(result.ToString());
            _ = Task.Run(async () =>
            {
                await message.DeleteAsync();
            });
        }

        public async Task InitializeAsync(IServiceProvider provider)
        {
            this.provider = provider;
            await commands.AddModulesAsync(Assembly.GetEntryAssembly(), provider);
        }
    }
}
