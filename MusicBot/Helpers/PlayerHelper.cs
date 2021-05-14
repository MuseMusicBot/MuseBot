using Victoria;
using Victoria.Enums;
using Discord;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace MusicBot.Helpers
{
    //Uiharu did a thing! :D
    public class PlayerHelper
    {
        private readonly DiscordSocketClient discord;
        private readonly LavaNode node;
        private readonly AudioHelper audioHelper;
        private readonly EmbedHelper embedHelper;
        private IGuild Guild
        {
            get
            {
                try
                {
                    return discord.GetGuild(Program.BotConfig.GuildId);
                }
                catch
                {
                    return null;
                }
            }
        }

        private LavaPlayer Player
        {
            get
            {
                return node.GetPlayer(Guild);
            }
        }

        public PlayerHelper(DiscordSocketClient client, LavaNode lavaNode, AudioHelper ah, EmbedHelper eh)
        {
            discord = client;
            node = lavaNode;
            audioHelper = ah;
            embedHelper = eh;
        }
        public async Task PauseResumeAsync(bool paused = true)
        {
            var player = node.GetPlayer(Guild);

            if (paused)
            {
                await player.ResumeAsync();
                return;
            }
            await player.PauseAsync();
        }

        public async Task PreviousAsync()
        {
            var player = node.GetPlayer(discord.GetGuild(Program.BotConfig.GuildId));
            await player.SeekAsync(TimeSpan.FromSeconds(0));
            await Task.CompletedTask;
        }

        public async Task NextTrackAsync(bool empty = true)
        {
            var player = node.GetPlayer(discord.GetGuild(Program.BotConfig.GuildId));
            if (empty)
            {
                await player.StopAsync();
            }
            else
            {
                await player.SkipAsync();
            }
            await Task.CompletedTask;
        }

        public async Task LoopAsync(bool loop = true)
        {
            var player = node.GetPlayer(discord.GetGuild(Program.BotConfig.GuildId));
            audioHelper.RepeatFlag = !audioHelper.RepeatFlag;
            audioHelper.RepeatTrack = audioHelper.RepeatFlag switch
            {
                true => player.Track,
                false => null
            };
            await Task.CompletedTask;
        }

        public async Task ShuffleAsync()
        {
            var player = node.GetPlayer(discord.GetGuild(Program.BotConfig.GuildId));
            player.Queue.Shuffle();
            await Task.CompletedTask;
        }

        public async Task EjectAsync()
        {
            var player = node.GetPlayer(discord.GetGuild(Program.BotConfig.GuildId));
            player.Queue.Clear();
            await node.LeaveAsync(player.VoiceChannel);
            await Task.CompletedTask;
        }
    }
}
