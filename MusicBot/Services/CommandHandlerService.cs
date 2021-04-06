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
            this.discord.GuildAvailable += GuildAvailable;
        }

        // TODO: Create m?setup command instead of On Guild Available
        private async Task GuildAvailable(SocketGuild arg)
        {
            if (arg.TextChannels.Where(x => x.Name == "test-music").Any())
            {
                return;
            }

            var channel = await discord.GetGuild(arg.Id).CreateTextChannelAsync("test-music", x =>
            {
                var c = discord.GetGuild(arg.Id).CategoryChannels;
                x.CategoryId = c.Where(y => y.Name.Contains("general", StringComparison.OrdinalIgnoreCase)).First()?.Id;
                x.Topic = "Music Bot";
            });

            List<IRole> roles = arg.Roles.Where(x => x.Name == "Senpai" || x.Name == "Bots").Cast<IRole>().ToList();
            foreach (var role in roles)
            {
                OverwritePermissions overwritePermissions = new OverwritePermissions(readMessageHistory: PermValue.Allow, sendMessages: PermValue.Allow, viewChannel: PermValue.Allow, manageChannel: PermValue.Allow, addReactions: PermValue.Allow);
                await channel.AddPermissionOverwriteAsync(role, overwritePermissions);
            }

            List<IRole> denyRoles = arg.Roles.Where(x => x.Name == "Kohai" || x.Name == "Newbie").Cast<IRole>().ToList();
            denyRoles.Add(arg.EveryoneRole);

            foreach (var role in denyRoles)
            {
                OverwritePermissions overwritePermissions = new OverwritePermissions(readMessageHistory: PermValue.Deny, viewChannel: PermValue.Deny);

                await channel.AddPermissionOverwriteAsync(role, overwritePermissions);
            }

            Program.message = await channel.SendMessageAsync("test message");
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

            if (context.Guild.GetTextChannel(message.Channel.Id).Name != "test-music")
            {
                return;
            }

            if ((message.Author as IGuildUser)?.VoiceChannel == null)
            {
                var newMsg = await context.Channel.SendMessageAsync("You must be in a voice channel to add songs");

                await message.DeleteAsync();
                await newMsg.DeleteAsync();
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
