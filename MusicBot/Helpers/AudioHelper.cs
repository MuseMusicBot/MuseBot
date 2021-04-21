using Discord;
using SpotifyAPI.Web;
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
        public const string FooterText = "{0} song{1} in queue | Volume: {2}{3}{4}";
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

        public async Task QueueTracksToPlayer(LavaPlayer player, Victoria.Responses.Rest.SearchResponse search, IGuildUser requester = null)
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

        public async Task QueueSpotifyToPlayer(LavaPlayer player, List<string> spotifyTracks)
        {
            _ = Task.Run(async () =>
            {
                string newQueue;
                var lavaTracks = spotifyTracks.OrderedParallel(async e =>
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

                if (player.PlayerState == PlayerState.Playing || player.PlayerState == PlayerState.Playing)
                {
                    foreach (var track in lavaTracks)
                    {
                        player.Queue.Enqueue(await track);
                    }

                    newQueue = await UpdateEmbedQueue(player);

                    var embed = await embedHelper.BuildMusicEmbed(player, Color.DarkTeal);

                    await Program.message.ModifyAsync(x =>
                    {
                        x.Content = string.Format(QueueMayHaveSongs, newQueue);
                        x.Embed = embed;
                    });
                }
                else
                {
                    //This seems to be running multiple times?
                    await Program.message.ModifyAsync(x => { x.Content = string.Format(QueueMayHaveSongs, "Loading..."); });

                    await player.PlayAsync(await lavaTracks.ElementAt(0));

                    foreach (var track in lavaTracks.Skip(1))
                    {
                        if (track != null)
                            player.Queue.Enqueue(await track);
                    }

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