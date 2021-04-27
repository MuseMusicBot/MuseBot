using Discord;
using Discord.WebSocket;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Victoria;
using Victoria.Enums;
using Discord.Commands;
using System.Text.RegularExpressions;

namespace MusicBot.Helpers
{
    public class AudioHelper
    {
        private LavaNode Node { get; set; }
        private readonly EmbedHelper embedHelper;
        private readonly ConcurrentDictionary<ulong, CancellationTokenSource> _disconnectTokens;
        public const string NoSongsInQueue = "​__**Queue List:**__\nNo songs in queue, join a voice channel to get started.";
        public const string QueueMayHaveSongs = "__**Queue List:**__\n{0}";
        public const string FooterText = "{0} song{1} in queue | Volume: {2}";
        public SpotifyClient Spotify { get; }
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
            _disconnectTokens = new ConcurrentDictionary<ulong, CancellationTokenSource>();

            Node.OnTrackStarted += async (args) =>
            {
                var player = args.Player;
                var queue = await UpdateEmbedQueue(player);
                var embed = await embedHelper.BuildMusicEmbed(player, Color.DarkTeal);

                //If for some reason Volume is set to 0 (100%) it will set to default volume
                if (player.Volume == 0)
                {
                    await player.UpdateVolumeAsync(Program.BotConfig.Volume);
                }

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
                if (!_disconnectTokens.TryGetValue(args.Player.VoiceChannel.Id, out var value))
                {
                    return;
                }
                if (value.IsCancellationRequested)
                {
                    return;
                }

                value.Cancel(true);
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

                    _ = InitiateDisconnectAsync(args.Player, TimeSpan.FromMinutes(15));
                    return;
                }

                await args.Player.PlayAsync(track);
            };

            Node.OnTrackException += async (args) =>
            {
                var player = args.Player;
                var msg = await embedHelper.BuildTrackErrorEmbed($"[{player.Track.Title}]({player.Track.Url})\nVideo might still be processing, try again later.");
                await (await player.TextChannel.SendMessageAsync(embed: msg)).RemoveAfterTimeout(10000);
                //Works but might require some better debugging
                //Having whats below above the messageasync doesn't trigger it for some reason?
                if (player.Queue.Count == 0)
                {
                    //If no songs in queue it will stop playback to reset the embed
                    await player.StopAsync();
                    return;
                }
                //If queue has any songs it will skip to the next track
                await player.SkipAsync();
                return;
            };
        }

        private async Task InitiateDisconnectAsync(LavaPlayer player, TimeSpan timeSpan)
        {
            if (!_disconnectTokens.TryGetValue(player.VoiceChannel.Id, out var value))
            {
                value = new CancellationTokenSource();
                _disconnectTokens.TryAdd(player.VoiceChannel.Id, value);
            }
            else if (value.IsCancellationRequested)
            {
                _disconnectTokens.TryUpdate(player.VoiceChannel.Id, new CancellationTokenSource(), value);
                value = _disconnectTokens[player.VoiceChannel.Id];
            }

            var isCancelled = SpinWait.SpinUntil(() => value.IsCancellationRequested, timeSpan);
            if (isCancelled)
            {
                return;
            }

            await Node.LeaveAsync(player.VoiceChannel);
            var msg = await embedHelper.BuildMessageEmbed(Color.Orange, "Muse has disconnected due to inactivity.");
            await (await player.TextChannel.SendMessageAsync(embed: msg)).RemoveAfterTimeout(10000);
        }

        public async Task SearchForTrack(SocketCommandContext context, string query)
        {
            Victoria.Responses.Rest.SearchResponse search;
            Regex regex;
            Match match;

            if (!Node.TryGetPlayer(context.Guild, out var player))
            {
                await Node.JoinAsync((context.User as IGuildUser).VoiceChannel, context.Channel as ITextChannel);
                player = Node.GetPlayer(context.Guild);
            }

            if (!query.IsUri(out var uri))
            {
                search = await Node.SearchYouTubeAsync(query.Trim());
                await QueueTracksToPlayer(player, search, requester: context.User as IGuildUser);
                return;
            }

            if (uri.Host.ToLower() == "open.spotify.com")
            {
                var tracks = await SearchSpotify(context.Channel, query);

                if (tracks != null)
                {
                    await QueueSpotifyToPlayer(player, tracks);
                }
                return;
            }

            search = await Node.SearchAsync(query);

            if (search.LoadStatus == LoadStatus.LoadFailed || search.LoadStatus == LoadStatus.NoMatches)
            {
                var msg = await embedHelper.BuildErrorEmbed($"The link `{query}` failed to load.", "Is this a private video or playlist? Double check if the resource is available for public viewing or not region locked.");
                regex = new Regex(@"(?<vid>(?:https?:\/\/)?(?:www\.)?youtube\.com\/watch\?v=[\w-]{11})(?:&list=.+)?");
                match = regex.Match(query);

                if (!match.Success)
                {
                    await context.Channel.SendAndRemove(embed: msg, timeout: 15000);
                    return;
                }

                string type = match.Groups["vid"].Value;
                search = await Node.SearchYouTubeAsync(type);

                if (search.LoadStatus == LoadStatus.LoadFailed || search.LoadStatus == LoadStatus.NoMatches)
                {
                    await context.Channel.SendAndRemove(embed: msg, timeout: 15000);
                    return;
                }
            }

            regex = new Regex(@"https?:\/\/(?:www\.)?(?:youtube|youtu)\.(?:com|be)\/?(?:watch\?v=)?(?:[A-z0-9_-]{1,11})(?:\?t=(?<time>\d+))?(&t=(?<time2>\d+)\w)?");
            match = regex.Match(query);
            double time = match switch
            {
                _ when match.Groups["time"].Value != "" => double.Parse(match.Groups["time"].Value),
                _ when match.Groups["time2"].Value != "" => double.Parse(match.Groups["time2"].Value),
                _ => -1
            };
            TimeSpan? timeSpan = (time == -1) ? (TimeSpan?)null : TimeSpan.FromSeconds(time);
            await QueueTracksToPlayer(player, search, timeSpan, context.User as IGuildUser);
        }

        public async Task<List<string>> SearchSpotify(ISocketMessageChannel channel, string url)
        {
            Regex r = new Regex(@"https?:\/\/(?:open\.spotify\.com)\/(?<type>\w+)\/(?<id>[\w-]{22})(?:\?si=(?:[\w-]{22}))?");
            if (!r.Match(url).Success)
            {
                var msg = await embedHelper.BuildMessageEmbed(Color.Orange, "Invalid Spotify link.");
                var send = await channel.SendMessageAsync(embed: msg);
                await send.RemoveAfterTimeout(5000);
                return null;
            }

            string type = r.Match(url).Groups["type"].Value;
            string id = r.Match(url).Groups["id"].Value;
            List<string> tracks = new List<string>();

            switch (type)
            {
                case "album":
                    await foreach (var item in Spotify.Paginate((await Spotify.Albums.Get(id)).Tracks))
                    {
                        tracks.Add($"{item.Name} {string.Join(" ", item.Artists.Select(x => x.Name))}");
                    }
                    break;

                case "playlist":
                    var playlist = await Spotify.Playlists.Get(id);
                    await foreach (var item in Spotify.Paginate(playlist.Tracks))
                    {
                        if (item.Track is FullTrack track)
                        {
                            tracks.Add($"{track.Name} {string.Join(" ", track.Artists.Select(x => x.Name))}");
                        }
                    }
                    break;

                case "track":
                    var trackItem = await Spotify.Tracks.Get(id);
                    tracks.Add($"{trackItem.Name} {string.Join(" ", trackItem.Artists.Select(x => x.Name))}");
                    break;

                default:
                    var msg = await embedHelper.BuildMessageEmbed(Color.Orange, "Must be a `track`, `playlist`, or `album`.");
                    var send = await channel.SendMessageAsync(embed: msg);
                    await send.RemoveAfterTimeout(6000);
                    return null;
            }

            return tracks;
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
                    var emebed = await embedHelper.BuildMusicEmbed(player, Color.DarkTeal, player.PlayerState == PlayerState.Paused);
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

                await Program.BotConfig.BotEmbedMessage.ModifyAsync(x => { x.Content = string.Format(QueueMayHaveSongs, "Loading..."); }).ConfigureAwait(false);

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

                    SpinWait.SpinUntil(() => lavaTracks.Count() == spotifyTracks.Count - startIdx);

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