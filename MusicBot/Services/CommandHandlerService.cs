using Discord;
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
                    if (message.Content.IsUri())
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

                        Regex regex = new Regex(@"https?:\/\/(?:www\.)?(?:youtube|youtu)\.(?:com|be)\/?(?:watch\?v=)?(?:[A-z0-9_-]{1,11})(?:\?t=(?<time>\d+))?(&t=(?<time2>\d+)\w)?");
                        Match m = regex.Match(message.Content);
                        double time = m switch
                        {
                            _ when m.Groups["time"].Value != "" => double.Parse(m.Groups["time"].Value),
                            _ when m.Groups["time2"].Value != "" => double.Parse(m.Groups["time2"].Value),
                            _ => -1
                        };

                        if (!node.HasPlayer(context.Guild))
                        {
                            await node.JoinAsync((context.User as IGuildUser).VoiceChannel, context.Channel as ITextChannel);
                        }
                        TimeSpan? timeSpan = (time == -1) ? (TimeSpan?)null : TimeSpan.FromSeconds(time);

                        await ah.QueueTracksToPlayer(node.GetPlayer(context.Guild), search, timeSpan);
                        _ = Task.Run(async () =>
                        {
                            await message.DeleteAsync();
                        });
                        return;
                    }
                    else
                    {
                        if (!node.HasPlayer(context.Guild))
                        {
                            await node.JoinAsync((context.User as IGuildUser).VoiceChannel, context.Channel as ITextChannel);
                        }
                        Victoria.Responses.Rest.SearchResponse search = await node.SearchYouTubeAsync(message.Content.Trim());
                        await ah.QueueTracksToPlayer(node.GetPlayer(context.Guild), search);
                        _ = Task.Run(async () =>
                        {
                            await message.DeleteAsync();
                        });
                        return;
                    }
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
