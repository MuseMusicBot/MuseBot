using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MusicBot.Helpers;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Victoria;
using Victoria.Enums;
using System.Text.RegularExpressions;

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

            string channelId = "";

            try
            {
                channelId = File.ReadLines(Program.testConfig).ElementAt(1);
            }
            catch { }

            int argPos = 0;

            //Channel id needs to be fixed, I have no idea how to make it work. For now hardcoded.
            // ulong parse the channel id - Devin
            if (message.Content != "m?setup" && context.Guild.GetTextChannel(message.Channel.Id).Id != ulong.Parse(channelId))
            {
                if (message.HasStringPrefix("m?", ref argPos))
                {
                    _ = Task.Run(async () =>
                    {
                        await message.DeleteAsync();
                        var msg = await embedHelper.BuildMessageEmbed(Color.Orange, $"This command is restrcited to <#{channelId}>.");
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
                    Victoria.Responses.Rest.SearchResponse search = await node.SearchAsync(message.Content);
                    if (search.LoadStatus == LoadStatus.LoadFailed || search.LoadStatus == LoadStatus.NoMatches)
                    {
                        var msg = await embedHelper.BuildErrorEmbed($"The link `{message.Content}` failed to load.", "Is this a private video or playlist? Double check if the resource is available for public viewing or not region locked.");
                        Regex r = new Regex(@"(?<vid>(?:https?:\/\/)?(?:www\.)?youtube\.com\/watch\?v=[\w-]{11})(?:&list=.+)?");
                        if (r.Match(message.Content).Success)
                        {
                            string type = r.Match(message.Content).Groups["vid"].Value;
                            search = await node.SearchAsync(type);
                            if (search.LoadStatus == LoadStatus.LoadFailed || search.LoadStatus == LoadStatus.NoMatches)
                            {
                                await message.DeleteAsync();
                                await (await context.Channel.SendMessageAsync(embed: msg)).RemoveAfterTimeout(15000);
                                return;
                            }
                        }
                        else
                        {
                            await message.DeleteAsync();
                            await (await context.Channel.SendMessageAsync(embed: msg)).RemoveAfterTimeout(15000);
                            return;
                        }
                    }

                    if (!node.HasPlayer(context.Guild))
                    {
                        await node.JoinAsync((context.User as IGuildUser).VoiceChannel, context.Channel as ITextChannel);
                    }

                    await ah.QueueTracksToPlayer(node.GetPlayer(context.Guild), search);
                    _ = Task.Run(async () =>
                    {
                        await message.DeleteAsync();
                    });
                    return;
                });
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
