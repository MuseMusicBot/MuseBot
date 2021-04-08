using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Reflection;
using System.Threading.Tasks;
using MusicBot.Helpers;
using Discord;
using System.Linq;
using System.Collections.Generic;
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
        private readonly MessageHelper mh;

        public CommandHandlerService(DiscordSocketClient discord, CommandService commands, IServiceProvider provider, AudioHelper audioHelper, LavaNode lavanode, MessageHelper messageHelper)
        {
            this.discord = discord;
            this.commands = commands;
            this.provider = provider;
            ah = audioHelper;
            mh = messageHelper;
            node = lavanode;

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

            if (message.Content != "m?setup" && context.Guild.GetTextChannel(message.Channel.Id).Name != "muse-song-requests")
            {
                return;
            }

            if ((message.Author as IGuildUser)?.VoiceChannel == null)
            {
                var embed = new EmbedBuilder
                {
                    Color = Discord.Color.Orange,
                    Description = "You have to be in a voice channel."
                }.Build();
                var newMsg = await context.Channel.SendMessageAsync(embed:embed);

                await message.DeleteAsync();
                //await newMsg.DeleteAsync();
                //await mh.RemoveMessageAfterTimeout(message, 10000);
                //await mh.RemoveMessageAfterTimeout(newMsg, 10000);
                return;
            }

            int argPos = 0;
            if (!message.HasStringPrefix("m?", ref argPos))
            {
                var search = await node.SearchAsync(message.Content);
                if (search.LoadStatus == LoadStatus.LoadFailed || search.LoadStatus == LoadStatus.NoMatches)
                {
                    await message.DeleteAsync();
                    return;
                }

                if (!node.HasPlayer(context.Guild))
                {
                    await node.JoinAsync((context.User as IGuildUser).VoiceChannel, context.Channel as ITextChannel);
                }

                await ah.QueueTracksToPlayer(node.GetPlayer(context.Guild), search);
                await message.DeleteAsync();
                return;
            }

            var result = await commands.ExecuteAsync(context, argPos, provider);

            if (result.Error.HasValue &&
                result.Error.Value == CommandError.UnknownCommand)
                    return;
            if (result.Error.HasValue &&
                result.Error.Value != CommandError.UnknownCommand)
                await context.Channel.SendMessageAsync(result.ToString());
            await message.DeleteAsync();
        }

        public async Task InitializeAsync(IServiceProvider provider)
        {
            this.provider = provider;
            await commands.AddModulesAsync(Assembly.GetEntryAssembly(), provider);
        }
    }
}
