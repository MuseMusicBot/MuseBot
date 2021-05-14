using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Linq;
using System.Threading.Tasks;
using Victoria;
using Victoria.Enums;

namespace MusicBot.Helpers
{
    public class ReactionsHelper : ModuleBase<SocketCommandContext>
    {
        private readonly DiscordSocketClient discord;
        private readonly LavaNode node;
        private readonly AudioHelper audioHelper;
        private readonly EmbedHelper embedHelper;
        private readonly PlayerHelper playerHelper; //access to universal commands. -Uiharu
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

        public ReactionsHelper(DiscordSocketClient client, LavaNode lavaNode, AudioHelper ah, EmbedHelper eh, PlayerHelper ph)
        {
            //Uiharu was here. :)
            discord = client;
            node = lavaNode;
            audioHelper = ah;
            embedHelper = eh;
            playerHelper = ph;
            discord.ReactionAdded += OnReactionAdded;
        }

        /// <summary>
        /// On Reaction handler 
        /// </summary>
        /// <param name="message">Cacheable struct from Discord</param>
        /// <param name="channel">SocketChannel reaction occurred in</param>
        /// <param name="reaction">Reaction added</param>
        /// <returns></returns>
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

                if (reaction.UserId == discord.CurrentUser.Id)
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
                    case EmojiStates.Previous:
                        if (player.PlayerState == PlayerState.Playing ||
                            player.PlayerState == PlayerState.Paused)
                        {
                            //await player.SeekAsync(TimeSpan.FromSeconds(0));
                            await playerHelper.PreviousAsync();
                        }
                        break;
                    case EmojiStates.PlayPause:
                        if (player.PlayerState == PlayerState.Paused)
                        {
                            await playerHelper.PauseResumeAsync(false);
                            var embed = await embedHelper.BuildMusicEmbed(player, Color.DarkTeal);
                            await Program.BotConfig.BotEmbedMessage.ModifyAsync(x => x.Embed = embed);
                        }
                        else if (player.PlayerState == PlayerState.Playing)
                        {
                            await playerHelper.PauseResumeAsync(true);
                            var embed = await embedHelper.BuildMusicEmbed(player, Color.DarkTeal, true);
                            await Program.BotConfig.BotEmbedMessage.ModifyAsync(x => x.Embed = embed);
                        }
                        break;
                    case EmojiStates.Next:
                        if (player.PlayerState == PlayerState.Playing ||
                            player.PlayerState == PlayerState.Paused)
                        {
                            if (player.Queue.Count == 0)
                            {
                                var embed = await embedHelper.BuildDefaultEmbed();
                                await Program.BotConfig.BotEmbedMessage.ModifyAsync(x => { x.Content = AudioHelper.NoSongsInQueue; x.Embed = embed; });
                                await playerHelper.NextTrackAsync(true);
                            }
                            else
                            {
                                await playerHelper.NextTrackAsync(false);
                            }
                        }
                        break;
                    case EmojiStates.Loop:
                        if (player.PlayerState == PlayerState.Playing ||
                            player.PlayerState == PlayerState.Paused)
                        {
                            /*audioHelper.RepeatFlag = !audioHelper.RepeatFlag;
                            audioHelper.RepeatTrack = audioHelper.RepeatFlag switch
                            {
                                true => player.Track,
                                false => null
                            };*/
                            await playerHelper.LoopAsync();

                            var embed = await embedHelper.BuildMessageEmbed($"Loop set to `{(audioHelper.RepeatFlag ? "enabled" : "disabled")}`");
                            await channel.SendAndRemove(embed: embed, timeout: 5000);
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

                            await playerHelper.ShuffleAsync();
                            string newQueue = await audioHelper.GetNewEmbedQueueString(player);
                            await Program.BotConfig.BotEmbedMessage.ModifyAsync(x => x.Content = string.Format(AudioHelper.QueueMayHaveSongs, newQueue));
                            var msg = await embedHelper.BuildMessageEmbed("Queue shuffled");
                            await channel.SendAndRemove(embed: msg);
                        }
                        break;
                    case EmojiStates.Eject:
                        {
                            //player.Queue.Clear();
                            var embed = await embedHelper.BuildDefaultEmbed();
                            await Program.BotConfig.BotEmbedMessage.ModifyAsync(x => { x.Content = AudioHelper.NoSongsInQueue; x.Embed = embed; });

                            try
                            {
                                //await node.LeaveAsync(player.VoiceChannel);
                                await playerHelper.EjectAsync();
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
