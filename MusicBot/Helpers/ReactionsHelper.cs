using Discord;
using Discord.WebSocket;
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
        private readonly IEmote[] Emojis = { new Emoji("⏯️"), new Emoji("⏹️"), new Emoji("⏭️"), new Emoji("🔁"), new Emoji("🔀") };
        private enum EmojiStates
        {
            PlayPause,
            Stop,
            Skip,
            Repeat,
            Shuffle
        };

        public ReactionsHelper(DiscordSocketClient client, LavaNode lavaNode, AudioHelper ah)
        {
			//Uiharu was here. :)
            discord = client;
            node = lavaNode;
            audioHelper = ah;
            discord.ReactionAdded += OnReactionAdded;
        }

        private async Task OnReactionAdded(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction)
        {
            var msg = await message.GetOrDownloadAsync();
            if (channel.Id != Program.BotConfig.ChannelId && msg.Id != Program.BotConfig.MessageId && !Emojis.Contains(reaction.Emote))
            {
                await Task.CompletedTask;
                return;
            }

            _ = Task.Run(async () =>
            {
                EmojiStates currentState = (EmojiStates)Array.IndexOf(Emojis, reaction.Emote);

                if (reaction.UserId == 196067262069735434)
                {
                    await Task.CompletedTask;
                    return;
                }

                await msg.RemoveReactionAsync(reaction.Emote, reaction.User.Value, options: new RequestOptions { RetryMode = RetryMode.RetryRatelimit });

                if (!node.HasPlayer(discord.GetGuild(Program.BotConfig.GuildId)))
                {
                    return;
                }

                var player = node.GetPlayer(discord.GetGuild(Program.BotConfig.GuildId));

                switch (currentState)
                {
                    case EmojiStates.PlayPause:
                        if (player.PlayerState == PlayerState.Paused)
                        {
                            await player.ResumeAsync();
                        }
                        else if (player.PlayerState == PlayerState.Playing)
                        {
                            await player.PauseAsync();
                        }
                        break;
                    case EmojiStates.Stop:
                        if (player.PlayerState == PlayerState.Playing ||
                            player.PlayerState == PlayerState.Paused)
                        {
                            await player.StopAsync();
                        }
                        break;
                    case EmojiStates.Skip:
                        if (player.PlayerState == PlayerState.Playing ||
                            player.PlayerState == PlayerState.Paused)
                        {
                            await player.SkipAsync();
                        }
                        break;
                    // TODO: Make flag instead of seeking
                    case EmojiStates.Repeat:
                        if (player.PlayerState == PlayerState.Playing ||
                            player.PlayerState == PlayerState.Paused)
                        {
                            await player.SeekAsync(TimeSpan.FromSeconds(0));
                        }
                        break;
                    case EmojiStates.Shuffle:
                        if (player.PlayerState == PlayerState.Playing ||
                            player.PlayerState == PlayerState.Paused)
                        {
                            if (player.Queue.Count < 2)
                            {
                                break;
                            }

                            player.Queue.Shuffle();
                            string newQueue = await audioHelper.UpdateEmbedQueue(player);
                            await Program.message.ModifyAsync(x => x.Content = string.Format(AudioHelper.QueueMayHaveSongs, newQueue));
                        }
                        break;
                    default:
                        return;
                }
            });
        }
    }
}
