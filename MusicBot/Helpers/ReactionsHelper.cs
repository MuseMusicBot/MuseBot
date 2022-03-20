using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MusicBot.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using Victoria;
using Victoria.Enums;

namespace MusicBot.Helpers
{
    public class ReactionsHelper
    {
        private readonly DiscordSocketClient discord;
        private readonly LavaNode node;
        private readonly AudioHelper audioHelper;
        private readonly EmbedHelper embedHelper;
        public static readonly IEmote[] Emojis = { new Emoji("⏮"), new Emoji("⏯️"), new Emoji("⏭️"), new Emoji("🔂"), new Emoji("🔀"), new Emoji("⏏️") };
        private enum EmojiStates
        {
            Previous,
            PlayPause,
            Next,
            Loop,
            Shuffle,
            Eject
        };

        public ReactionsHelper(DiscordSocketClient client, LavaNode lavaNode, AudioHelper ah, EmbedHelper eh)
        {
            //Uiharu was here. :)
            discord = client;
            node = lavaNode;
            audioHelper = ah;
            embedHelper = eh;
            discord.ReactionAdded += OnReactionAdded;
        }

        /// <summary>
        /// On Reaction handler 
        /// </summary>
        /// <param name="message">Cacheable struct from Discord</param>
        /// <param name="messageChannel">SocketChannel reaction occurred in</param>
        /// <param name="reaction">Reaction added</param>
        /// <returns></returns>
        private async Task OnReactionAdded(Cacheable<IUserMessage, ulong> message, Cacheable<IMessageChannel, ulong> messageChannel, SocketReaction reaction)
        {
            var channel = await messageChannel.GetOrDownloadAsync();

            if (!(channel is IGuildChannel guildChannel))
            {
                return;
            }
            var msg = await message.GetOrDownloadAsync();

            if (channel.Id != Program.BotConfig.ChannelId && msg.Id != Program.BotConfig.MessageId && !Emojis.Contains(reaction.Emote))
            {
                await Task.CompletedTask;
                return;
            }

            _ = Task.Run(async () =>
            {
                EmojiStates currentState = (EmojiStates)Array.IndexOf(Emojis, reaction.Emote);

                if (reaction.UserId == discord.CurrentUser.Id)
                {
                    await Task.CompletedTask;
                    return;
                }

                await msg.RemoveReactionAsync(reaction.Emote, reaction.User.Value, options: new RequestOptions { RetryMode = RetryMode.RetryRatelimit });

                try
                {
                    if (!node.HasPlayer(guildChannel.Guild))
                    {
                        return;
                    }
                }
                catch
                {
                    var error = await embedHelper.BuildErrorEmbed("Guild ID Error", $"Your Guild ID in **{ConfigHelper.ConfigName}** is most likely incorrect.");
                    await channel.SendAndRemove(embed: error, timeout: 15000);
                }

                var player = node.GetPlayer(guildChannel.Guild);

                if (!(player.PlayerState == PlayerState.Playing || player.PlayerState == PlayerState.Paused) && currentState != EmojiStates.Eject)
                {
                    return;
                }

                switch (currentState)
                {
                    case EmojiStates.Previous:
                        {
                            await player.PreviousAsync();
                            break;
                        }
                    case EmojiStates.PlayPause:
                        {
                            await player.PauseResumeAsync(embedHelper, player.PlayerState == PlayerState.Paused);
                            break;
                        }
                    case EmojiStates.Next:
                        {
                            await player.NextTrackAsync(embedHelper);
                            break;
                        }
                    case EmojiStates.Loop:
                        {
                            await player.LoopAsync(audioHelper, embedHelper, channel);
                            break;
                        }
                    case EmojiStates.Shuffle:
                        {
                            await player.ShuffleAsync(audioHelper, embedHelper, channel);
                            break;
                        }
                    case EmojiStates.Eject:
                        {
                            try
                            {
                                await node.EjectAsync(embedHelper, guildChannel.Guild);
                            }
                            catch { }
                            break;
                        }

                    default:
                        return;
                }
            });
        }
    }
}
