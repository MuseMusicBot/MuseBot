using Discord;
using Discord.Audio;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using SharpLink;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Victoria;
using Victoria.Enums;
using System.Net;
using System.IO;
using System.Drawing;
using System.Text;
using System.Net.Http;

namespace MusicBot.Helpers
{
    public class AudioHelper
    {
        private LavaNode Node { get; set; }
        private DiscordSocketClient _discord {get; set; }

        public AudioHelper(DiscordSocketClient discord, LavaNode lavanode)
        {
            Node = lavanode;
            this._discord = discord;

            Node.OnTrackStarted += async (args) =>
            {
                var player = args.Player;
                await player.UpdateVolumeAsync(Program.Volume);

                var queue = await UpdateEmbedQueue(player);
                var embed = BuildMusicEmbed(player);

                
                await Program.message.ModifyAsync((x) =>
                {
                    x.Embed = embed;
                    x.Content = $"__**Queue List:**__\n{(queue == "" ? "No songs in queue, join a voice channel to get started." : queue)}";
                });

            };

            Node.OnTrackEnded += async (args) =>
            {
                if (!args.Reason.ShouldPlayNext())
                {
                    return;
                }

                var player = args.Player;

                if (!player.Queue.TryDequeue(out var track))
                {
                    return;
                }

                await args.Player.PlayAsync(track);
            };
        }

        public Embed BuildMusicEmbed(LavaPlayer player)
        {
            var length = player.Track.Duration;
            var icon = _discord.CurrentUser.GetAvatarUrl();
            var thumb = YouTubeHelper.GetYtThumbnail(player.Track.Url);

            var embed = new EmbedBuilder
            {
                Color = Discord.Color.Blue,
                Author = new EmbedAuthorBuilder
                {
                    IconUrl = icon,
                    Name = string.Format("[{0:d2}{1:d2}:{2:d2}] - {3}", length.Hours > 0 ? $"{length.Hours}:" : "", length.Minutes, length.Seconds, player.Track.Title),
                    Url = $"{player.Track.Url}"
                },
                ImageUrl = thumb,
                Footer = new EmbedFooterBuilder { Text = $"{player.Queue.Count} songs in queue | Volume: {Program.Volume}%" }
            }.Build();

            return embed;
        }

        public Task<string> UpdateEmbedQueue(LavaPlayer player)
        {
            StringBuilder sb = new StringBuilder();
            var q = player.Queue.ToList();
            int idx = 1;

            if (q.Count == 0)
            {
                return Task.FromResult("");
            }

            foreach (var p in q)
            {
                sb.AppendLine($"{idx++}. {p.Title}");
            }

            return Task.FromResult(sb.ToString());
            
        }

        public async Task QueueTracksToPlayer(LavaPlayer player, Victoria.Responses.Rest.SearchResponse search)
        {
            List<LavaTrack> lavaTracks;
            string newQueue;
            if (search.LoadStatus == LoadStatus.PlaylistLoaded)
            {
                lavaTracks = search.Tracks.ToList();
            }
            else
            {
                lavaTracks = new List<LavaTrack>
                {
                    search.Tracks.First()
                };
            }

            if (player.PlayerState == PlayerState.Playing || player.PlayerState == PlayerState.Paused)
            {
                foreach (var track in lavaTracks)
                {
                    player.Queue.Enqueue(track);
                }
                newQueue = await UpdateEmbedQueue(player);
                var emebed = BuildMusicEmbed(player);
                await Program.message.ModifyAsync(x =>
                {
                    x.Content = $"__**Queue List:**__\n{newQueue}";
                    x.Embed = emebed;
                });
            }
            else
            {
                foreach (var track in lavaTracks)
                {
                    player.Queue.Enqueue(track);
                }

                _ = player.Queue.TryDequeue(out var newTrack);

                await player.PlayAsync(newTrack);
                newQueue = await UpdateEmbedQueue(player);
                var embed = BuildMusicEmbed(player);

                await Program.message.ModifyAsync(x =>
                {
                    x.Embed = embed;
                    x.Content = $"__**Queue List:**__\n{(newQueue == "" ? "No songs in queue, join a voice channel to get started." : newQueue)}";

                });
            }
        }
    }
}
