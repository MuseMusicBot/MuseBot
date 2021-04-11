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

                
                await Program.message.ModifyAsync(x =>
                {
                    x.Embed = embed;
                    x.Content = $"__**Queue List:**__\n{(queue == "" ? "No songs in queue, join a voice channel to get started." : queue)}";
                });

            };

            // TODO: Fix default embed showing up after skipping
            Node.OnTrackEnded += async (args) =>
            {
                if (!args.Reason.ShouldPlayNext())
                {
                    var embed = BuildDefaultEmbed();
                    await Program.message.ModifyAsync(x =>
                    {
                        x.Content = "__**Queue List:**__\nNo songs in queue, join a voice channel to get started.";
                        x.Embed = embed;
                    });   
                    return;
                }

                var player = args.Player;

                if (!player.Queue.TryDequeue(out var track) && player.Queue.Count == 0)
                {
                    var embed = BuildDefaultEmbed();
                    await Program.message.ModifyAsync(x =>
                    {
                        x.Content = "__**Queue List:**__\nNo songs in queue, join a voice channel to get started.";
                        x.Embed = embed;
                    });
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

            /* TODO: Fix and make sure it doesn't throw */
            //var thumb = player.Track.FetchArtworkAsync().Result;
            //using var client = new HttpClient();
            //var img = System.Drawing.Image.FromStream(client.GetStreamAsync(new Uri(thumb)).Result);

            //if (img.Width == 120 && img.Height == 90)
            //{
            //    thumb = YouTubeHelper.GetYtThumbnail(player.Track.Url);
            //}

            //img.Dispose();

            var embed = new EmbedBuilder
                {
                    Color = Discord.Color.DarkBlue,
                    Author = new EmbedAuthorBuilder
                    {
                        IconUrl = icon,
                        Name = string.Format("[{0:d2}{1:d2}:{2:d2}] - {3}", length.Hours > 0 ? $"{length.Hours}:" : "", length.Minutes, length.Seconds, player.Track.Title),
                        Url = $"{player.Track.Url}"
                    },
                    ImageUrl = thumb,
                    Footer = new EmbedFooterBuilder { Text = $"{player.Queue.Count} song{(player.Queue.Count > 1 ? "s" : "")} in queue | Volume: {Program.Volume}%" }

                }.Build();
                return embed;
            }

        public Embed BuildDefaultEmbed()
        {
            var embed = new EmbedBuilder
            {
                Color = Discord.Color.DarkTeal,
                Title = "No songs currently playing",
                Description = "No, this is not Hydra. Please stop asking.",
                ImageUrl = "https://i.imgur.com/ce9UMue.jpg",
                Footer = new EmbedFooterBuilder { Text = "Prefix for this server is: m?" }
            }.Build();

            return embed;
        }

        public Task<string> UpdateEmbedQueue(LavaPlayer player)
        {
            IEnumerable<int> Range = Enumerable.Range(0, 1500);
            StringBuilder sb = new StringBuilder();
            var q = player.Queue.ToList();
            int idx = 1;

            if (q.Count == 0)
            {
                return Task.FromResult("");
            }

            foreach (var p in q)
            {
                var s = $"{idx++}. {p.Title} [{p.Duration.ToTimecode()}]";

                if (Range.Contains(s.Length + sb.Length + 2))
                {
                    sb.AppendLine(s);
                }
                else
                {
                    sb.AppendLine($"And **{q.Count - q.IndexOf(p)}** more...");
                    break;
                }
            }

            return Task.FromResult(sb.ToString());

        }

        public async Task QueueTracksToPlayer(LavaPlayer player, Victoria.Responses.Rest.SearchResponse search)
        {
            _ = Task.Run(async () =>
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
            });

            await Task.CompletedTask;
        }
    }
}