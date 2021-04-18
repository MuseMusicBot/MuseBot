using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Victoria;
using Victoria.Enums;
using Victoria.Responses.Rest;

namespace MusicBot.Helpers
{
    public class AudioHelper
    {
        private LavaNode Node { get; set; }
        private EmbedHelper embedHelper;
        public const string NoSongsInQueue = "​__**Queue List:**__\nNo songs in queue, join a voice channel to get started.";
        public const string QueueMayHaveSongs = "__**Queue List:**__\n{0}";
        public const string FooterText = "{0} song{1} in queue | Volume: {2}{3}{4}";

        public AudioHelper(LavaNode lavanode, EmbedHelper eh)
        {
            Node = lavanode;
            embedHelper = eh;

            Node.OnTrackStarted += async (args) =>
            {
                var player = args.Player;
                await player.UpdateVolumeAsync(Program.Volume);

                var queue = await UpdateEmbedQueue(player);
                var embed = await embedHelper.BuildMusicEmbed(player, Color.DarkTeal);

                var content = queue switch
                {
                    "" => NoSongsInQueue,
                    _ => string.Format(QueueMayHaveSongs, queue)
                };


                await Program.message.ModifyAsync(x =>
                {
                    x.Embed = embed;
                    x.Content = content;
                });

            };

            Node.OnTrackEnded += async (args) =>
            {
                if (!args.Reason.ShouldPlayNext())
                {
                    return;
                }

                var player = args.Player;

                if (!player.Queue.TryDequeue(out var track) && player.Queue.Count == 0)
                {
                    var embed = await embedHelper.BuildDefaultEmbed();
                    await Program.message.ModifyAsync(x =>
                    {
                        x.Content = NoSongsInQueue;
                        x.Embed = embed;
                    });
                    return;
                }

                await args.Player.PlayAsync(track);
            };
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

        public async Task QueueTracksToPlayer(LavaPlayer player, SearchResponse search, IGuildUser requester = null)
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
                    var emebed = await embedHelper.BuildMusicEmbed(player, Color.DarkTeal);
                    await Program.message.ModifyAsync(x =>
                    {
                        x.Content = string.Format(QueueMayHaveSongs, newQueue);
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
                    var embed = await embedHelper.BuildMusicEmbed(player, Color.DarkTeal);

                    var content = newQueue switch
                    {
                        "" => NoSongsInQueue,
                        _ => string.Format(QueueMayHaveSongs, newQueue)
                    };

                    await Program.message.ModifyAsync(x =>
                    {
                        x.Embed = embed;
                        x.Content = content;

                    });
                }
            });

            await Task.CompletedTask;
        }
    }
}