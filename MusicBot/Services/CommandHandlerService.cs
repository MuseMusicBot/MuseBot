using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MusicBot.Helpers;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace MusicBot.Services
{
    public class CommandHandlerService
    {
        private readonly DiscordSocketClient discord;
        private readonly CommandService commands;
        private IServiceProvider provider;
        private readonly AudioHelper ah;
        public EmbedHelper embedHelper;

        public CommandHandlerService(DiscordSocketClient discord, CommandService commands, IServiceProvider provider, AudioHelper audioHelper, EmbedHelper eh)
        {
            this.discord = discord;
            this.commands = commands;
            this.provider = provider;
            ah = audioHelper;
            embedHelper = eh;

            this.discord.MessageReceived += MessageReceived;
        }

        private async Task MessageReceived(SocketMessage socketMessage)
        {
            if (socketMessage is not SocketUserMessage message)
                return;

            if (message.Source != MessageSource.User)
                return;

            if (message.Author.IsBot)
                return;

            var context = new SocketCommandContext(discord, message);

            int argPos = 0;

            string prefix   = Program.BotConfig.Prefix;
            ulong chId      = Program.BotConfig.ChannelId;

            if (context.Guild.TextChannels.Any(x => x.Id == chId) && message.Channel.Id != chId)
            {
                
                if (message.HasStringPrefix(prefix, ref argPos))
                {
                    _ = Task.Run(async () =>
                    {
                        await message.DeleteAsync();
                        var msg = await embedHelper.BuildMessageEmbed($"This command is restricted to <#{chId}>.");
                        await context.Channel.SendAndRemove(embed: msg, timeout: 15000);
                    });
                }
                return;
            }

            if (message.Content != $"{prefix}premium" && (message.Author as IGuildUser)?.VoiceChannel == null)
            {
                _ = Task.Run(async () =>
                {
                    await message.DeleteAsync();
                    var msg = await embedHelper.BuildMessageEmbed("You have to be in a voice channel.");
                    await context.Channel.SendAndRemove(embed: msg);
                });
                return;
            }

            if (!message.HasStringPrefix(prefix, ref argPos) && context.Guild.TextChannels.Any(x => x.Id == chId))
            {
                _ = Task.Run(async () =>
                {
                    await ah.SearchForTrack(context, message.Content);
                });

                await message.DeleteAsync();
                return;
            }

            var result = await commands.ExecuteAsync(context, argPos, provider);

            if (result.Error is CommandError.UnknownCommand)
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
