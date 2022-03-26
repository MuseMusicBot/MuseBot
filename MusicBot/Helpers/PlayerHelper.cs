using Victoria;
using Discord.Commands;
using Discord;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace MusicBot.Helpers
{
    //Uiharu did a thing! :D
    public static class PlayerHelper
    {
        public static async Task PauseResumeAsync(
            this LavaPlayer player,
            EmbedHelper embedHelper,
            bool paused,
            SocketCommandContext context = null)
        {
            if (paused)
            {
                await player.ResumeAsync();
            }
            else
            {
                await player.PauseAsync();
            }

            if (context != null && !context.Guild.TextChannels.Where(x => x.Id == Program.BotConfig.ChannelId).Any())
            {
                return;
            }

            var embed = await embedHelper.BuildMusicEmbed(player, Color.DarkTeal, !paused);
            await Program.BotConfig.BotEmbedMessage.ModifyAsync(x => x.Embed = embed);
        }

        public static async Task PreviousAsync(this LavaPlayer player)
        {
            await player.SeekAsync(TimeSpan.Zero);
        }

        public static async Task NextTrackAsync(
            this LavaPlayer player,
            EmbedHelper embedHelper,
            SocketCommandContext context = null)
        {
            if (player.Queue.Count >= 1)
            {
                await player.SkipAsync();
            }
            else
            {
                await player.StopAsync();
            }
            
            if (context != null && !context.Guild.TextChannels.Where(x => x.Id == Program.BotConfig.ChannelId).Any())
            {
                return;
            }
            var embed = await embedHelper.BuildDefaultEmbed();
            await Program.BotConfig.BotEmbedMessage.ModifyAsync(x => { x.Content = AudioHelper.NoSongsInQueue; x.Embed = embed; });
        }

        public static async Task LoopAsync(this LavaPlayer player, AudioHelper audioHelper, EmbedHelper embedHelper, IMessageChannel channel)
        {
            audioHelper.RepeatFlag = !audioHelper.RepeatFlag;
            audioHelper.RepeatTrack = audioHelper.RepeatFlag switch
            {
                true => player.Track,
                false => null
            };

            var embed = await embedHelper.BuildMessageEmbed($"Loop set to `{(audioHelper.RepeatFlag ? "enabled" : "disabled")}`");
            await channel.SendAndRemove(embed: embed, timeout: 5000);
        }

        public static async Task ShuffleAsync(
            this LavaPlayer player,
            AudioHelper audioHelper,
            EmbedHelper embedHelper,
            IMessageChannel channel = null,
            SocketCommandContext context = null)
        {
            if (player.Queue.Count < 2)
            {
                return;
            }

            player.Queue.Shuffle();

            if (context != null && !context.Guild.TextChannels.Where(x => x.Id == Program.BotConfig.ChannelId).Any())
            {
                return;
            }

            channel ??= context?.Channel;
            string newQueue = await audioHelper.GetNewEmbedQueueString(player);
            await Program.BotConfig.BotEmbedMessage.ModifyAsync(x => x.Content = string.Format(AudioHelper.QueueMayHaveSongs, newQueue));

            if (channel != null)
            {
                var msg = await embedHelper.BuildMessageEmbed("Queue shuffled");
                await channel.SendAndRemove(embed: msg);
            }
        }

        public static async Task EjectAsync(
            this LavaNode node,
            EmbedHelper embedHelper,
            IGuild guild,
            SocketCommandContext context = null)
        {
            if (!node.TryGetPlayer(guild, out var player))
            {
                return;
            }

            player.Queue.Clear();
            await node.LeaveAsync(player.VoiceChannel);

            if (context != null && !context.Guild.TextChannels.Where(x => x.Id == Program.BotConfig.ChannelId).Any())
            {
                return;
            }
            var embed = await embedHelper.BuildDefaultEmbed();
            await Program.BotConfig.BotEmbedMessage.ModifyAsync(x => { x.Content = AudioHelper.NoSongsInQueue; x.Embed = embed; });
        }
    }
}
