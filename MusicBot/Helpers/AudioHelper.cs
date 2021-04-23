using Discord;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Victoria;
using Victoria.Enums;

namespace MusicBot.Helpers
{
    public class AudioHelper
    {
        private LavaNode Node { get; set; }
        private readonly EmbedHelper embedHelper;
        public const string NoSongsInQueue = "​__**Queue List:**__\nNo songs in queue, join a voice channel to get started.";
        public const string QueueMayHaveSongs = "__**Queue List:**__\n{0}";
        public const string FooterText = "{0} song{1} in queue | Volume: {2}";
        public SpotifyClient Spotify;
        public bool RepeatFlag { get; set; } = false;
        public LavaTrack RepeatTrack { get; set; }

        public AudioHelper(LavaNode lavanode, EmbedHelper eh)
        {
            Node = lavanode;
            embedHelper = eh;

            // TODO: Make SpotifyClient own class
            var config = SpotifyClientConfig.CreateDefault();
            var request = new ClientCredentialsRequest(Program.BotConfig.SpotifyClientId, Program.BotConfig.SpotifySecret);
            var response = new OAuthClient(config).RequestToken(request);
            var spotify = new SpotifyClient(config.WithToken(response.Result.AccessToken));
            Spotify = spotify;

            Node.OnTrackStarted += async (args) =>
            {
                var player = args.Player;

                var queue = await UpdateEmbedQueue(player);
                var embed = await embedHelper.BuildMusicEmbed(player, Color.DarkTeal);

                var content = queue switch
                {
                    "" => NoSongsInQueue,
                    _ => string.Format(QueueMayHaveSongs, queue)
                };


                await Program.BotConfig.BotEmbedMessage.ModifyAsync(x =>
                {
                    x.Embed = embed;
                    x.Content = content;
                });

            };

            Node.OnTrackEnded += async (args) =>
            {
                var player = args.Player;

                if (RepeatFlag)
                {
                    await player.PlayAsync(RepeatTrack);
                    return;
                }

                RepeatFlag = false;

                if (!args.Reason.ShouldPlayNext())
                {
                    return;
                }

                if (!player.Queue.TryDequeue(out var track) && player.Queue.Count == 0)
                {
                    var embed = await embedHelper.BuildDefaultEmbed();
                    await Program.BotConfig.BotEmbedMessage.ModifyAsync(x =>
                    {
                        x.Content = NoSongsInQueue;
                        x.Embed = embed;
                    });
                    return;
                }

                await args.Player.PlayAsync(track);
            };
        }

        public ValueTask<string> UpdateEmbedQueue(LavaPlayer player)
        {
            StringBuilder sb = new StringBuilder();
            var q = player.Queue.ToList();
            int idx = 1;

            if (q.Count == 0)
            {
                return new ValueTask<string>("");
            }

            foreach (var p in q)
            {
                var s = $"{idx++}. {p.Title} [{p.Duration.ToTimecode()}]";

                if (s.Length + sb.Length + 2 <= 1500)
                {
                    sb.AppendLine(s);
                }
                else
                {
                    sb.AppendLine($"And **{q.Count - q.IndexOf(p)}** more...");
                    break;
                }
            }

            return new ValueTask<string>(sb.ToString());

        }

        public async Task QueueTracksToPlayer(LavaPlayer player, Victoria.Responses.Rest.SearchResponse search, TimeSpan? startTime = null, IGuildUser requester = null)
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
                    //Pause flag needed!
                    newQueue = await UpdateEmbedQueue(player);
                    var emebed = await embedHelper.BuildMusicEmbed(player, Color.DarkTeal);
                    await Program.BotConfig.BotEmbedMessage.ModifyAsync(x =>
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

                    if (startTime == null)
                    {
                        await player.PlayAsync(newTrack);
                    }
                    else
                    {
                        await player.PlayAsync(newTrack, startTime.Value, newTrack.Duration);
                    }
                }
            });

            await Task.CompletedTask;
        }

        public async Task QueueSpotifyToPlayer(LavaPlayer player, List<string> spotifyTracks)
        {
            _ = Task.Run(async () =>
            {
                string newQueue;
                int startIdx = 0;

                if (player.PlayerState == PlayerState.Connected || player.PlayerState == PlayerState.Stopped)
                {
                    var node = await Node.SearchYouTubeAsync(spotifyTracks[0]);
                    if (node.LoadStatus != LoadStatus.NoMatches || node.LoadStatus != LoadStatus.LoadFailed)
                        await player.PlayAsync(node.Tracks[0]);
                    startIdx = 1;
                }

                if (spotifyTracks.Count - startIdx > 0)
                {
                    var lavaTracks = spotifyTracks.Skip(startIdx).OrderedParallel(async e =>
                    {
                        int i = 0;
                        int maxRetries = 3;
                        Victoria.Responses.Rest.SearchResponse node;
                        do
                        {
                            node = await Node.SearchYouTubeAsync(e);
                            i++;
                        } while ((node.LoadStatus == LoadStatus.NoMatches || node.LoadStatus == LoadStatus.LoadFailed) && i <= maxRetries);

                        return (node.LoadStatus == LoadStatus.NoMatches || node.LoadStatus == LoadStatus.LoadFailed) ? null : node.Tracks.FirstOrDefault();
                    });

                    await Program.BotConfig.BotEmbedMessage.ModifyAsync(x => { x.Content = string.Format(QueueMayHaveSongs, "Loading..."); }).ConfigureAwait(false);

                    foreach (var track in lavaTracks)
                    {
                        player.Queue.Enqueue(track);
                    }
                }

                newQueue = await UpdateEmbedQueue(player).ConfigureAwait(false);
                var embed = await embedHelper.BuildMusicEmbed(player, Color.DarkGreen).ConfigureAwait(false);
                var content = newQueue switch
                {
                    "" => NoSongsInQueue,
                    _ => string.Format(QueueMayHaveSongs, newQueue)
                };

                await Program.BotConfig.BotEmbedMessage.ModifyAsync(x =>
                {
                    x.Embed = embed;
                    x.Content = content;
                }).ConfigureAwait(false);
            });
            await Task.CompletedTask;
        }
    }
}