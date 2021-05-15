using Victoria;
using Victoria.Enums;
using Discord;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace MusicBot.Helpers
{
    //Uiharu did a thing! :D
    public static class PlayerHelper
    {
        // TODO: Take a bool
        public static async Task PauseResumeAsync(this LavaPlayer player, EmbedHelper embedHelper, bool pause)
        {
            var embed = await embedHelper.BuildMusicEmbed(player, Color.DarkTeal, pause);
            await Program.BotConfig.BotEmbedMessage.ModifyAsync(x => x.Embed = embed);

            if (!pause)
            {
                await player.ResumeAsync();
                return;
            }
            await player.PauseAsync();
        }

        public static async Task PreviousAsync(this LavaPlayer player)
        {
            await player.SeekAsync(TimeSpan.Zero);
        }

        public static async Task NextTrackAsync(this LavaPlayer player, EmbedHelper embedHelper)
        {
            if (player.Queue.Count >= 2)
            {
                await player.SkipAsync();
                return;
            }

            await player.StopAsync();
            var embed = await embedHelper.BuildDefaultEmbed();
            await Program.BotConfig.BotEmbedMessage.ModifyAsync(x => { x.Content = AudioHelper.NoSongsInQueue; x.Embed = embed; });
        }

        public static async Task LoopAsync(this LavaPlayer player, AudioHelper audioHelper, EmbedHelper embedHelper, ISocketMessageChannel channel)
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

        public static async Task ShuffleAsync(this LavaPlayer player, AudioHelper audioHelper, EmbedHelper embedHelper, ISocketMessageChannel channel)
        {
            if (player.Queue.Count < 2)
            {
                return;
            }

            player.Queue.Shuffle();
            string newQueue = await audioHelper.GetNewEmbedQueueString(player);
            await Program.BotConfig.BotEmbedMessage.ModifyAsync(x => x.Content = string.Format(AudioHelper.QueueMayHaveSongs, newQueue));
            var msg = await embedHelper.BuildMessageEmbed("Queue shuffled");
            await channel.SendAndRemove(embed: msg);
        }

        public static async Task EjectAsync(this LavaNode node, IGuild guild, EmbedHelper embedHelper)
        {
            var embed = await embedHelper.BuildDefaultEmbed();
            await Program.BotConfig.BotEmbedMessage.ModifyAsync(x => { x.Content = AudioHelper.NoSongsInQueue; x.Embed = embed; });
            var player = node.GetPlayer(guild);
            player.Queue.Clear();
            await node.LeaveAsync(player.VoiceChannel);
        }
    }
}
