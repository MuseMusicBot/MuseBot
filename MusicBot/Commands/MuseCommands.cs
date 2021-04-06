using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Victoria;
using Victoria.Enums;

namespace MusicBot.Commands
{
    public class MuseCommands : ModuleBase<SocketCommandContext>
    {
        private readonly LavaNode node;
        private readonly Helpers.AudioHelper audioHelper;

        public MuseCommands(Helpers.AudioHelper ah, LavaNode lavaNode)
        {
            node = lavaNode;
            audioHelper = ah;
        }

        [Command("play", RunMode = RunMode.Async)]
        public async Task TestPlay([Remainder]string query)
        {
            if (!node.HasPlayer(Context.Guild))
            {
                try
                {
                    if ((Context.User as IGuildUser)?.VoiceChannel == null)
                    {
                        return;
                    }

                    await node.JoinAsync((Context.User as IGuildUser)?.VoiceChannel, Context.Channel as ITextChannel);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }

            var search = await node.SearchAsync(query);
            if (search.LoadStatus == LoadStatus.LoadFailed || search.LoadStatus == LoadStatus.NoMatches)
            {
                return;
            }

            var player = node.GetPlayer(Context.Guild);

            await audioHelper.QueueTracksToPlayer(player, search);
        }

        [Command("pause")]
        [Alias("p")]
        public async Task Pause()
        {
            if (!node.HasPlayer(Context.Guild))
            {
                return;
            }

            var player = node.GetPlayer(Context.Guild);

            if (player.PlayerState == PlayerState.Paused)
            {
                return;
            }

            if (player.PlayerState == PlayerState.Playing)
            {
                await player.PauseAsync();
            }
        }

        [Command("resume")]
        [Alias("r")]
        public async Task Resume()
        {
            if (!node.HasPlayer(Context.Guild))
            {
                return;
            }

            var player = node.GetPlayer(Context.Guild);

            if (player.PlayerState == PlayerState.Playing)
            {
                return;
            }

            if (player.PlayerState == PlayerState.Paused)
            {
                await player.ResumeAsync();
            }

        }

        [Command("seek")]
        [Alias("s")]
        public async Task Seek(TimeSpan? seek = null)
        {
            if (!node.HasPlayer(Context.Guild))
            {
                return;
            }

            var player = node.GetPlayer(Context.Guild);

            if(!(player.PlayerState == PlayerState.Playing || player.PlayerState == PlayerState.Paused))
            {
                return;
            }

            var pos = player.Track.Position;
            var len = player.Track.Duration;

            if (seek == null && (player.PlayerState == PlayerState.Playing || player.PlayerState == PlayerState.Paused))
            {
                var embed = new EmbedBuilder
                {
                    Color = Color.Orange,
                    Description = $"Current Position: {audioHelper.TimeSpanToTimeCode(pos)}/{audioHelper.TimeSpanToTimeCode(len)}"
                }.Build();

                await Context.Channel.SendMessageAsync(embed: embed);
                return;
            }

            if (len.TotalMilliseconds - seek.Value.TotalMilliseconds < 0)
            {
                var embed = new EmbedBuilder
                {
                    Color = Color.Orange,
                    Description = $"You can only seek up to {audioHelper.TimeSpanToTimeCode(len)}"
                }.Build();

                await Context.Channel.SendMessageAsync(embed: embed);
                return;
            }

            await player.SeekAsync(seek.Value);

            //var player = llm.GetPlayer(Context.Guild.Id);
            //if (player == null)
            //{
            //    return;
            //}

            //if (seek == null)
            //{
            //    long playerPosition = player.CurrentPosition;
            //    TimeSpan ts = TimeSpan.FromSeconds(playerPosition);
            //    await Context.Channel.SendMessageAsync(string.Format("Current Position: {0}h {1}m {2}s", ts.TotalHours, ts.TotalMinutes, ts.TotalSeconds));
            //    return;
            //}

            //if (seek.Value < 0 || seek.Value > player.CurrentTrack.Length.TotalSeconds)
            //{
            //    await Context.Channel.SendMessageAsync($"Cannot seek to that position. Valid max position is `{player.CurrentTrack.Length.TotalSeconds}`.");
            //    return;
            //}

            //if (player.CurrentTrack.IsSeekable)
            //{
            //    await player.SeekAsync((int)seek.Value);
            //}
        }

        [Command("move", RunMode = RunMode.Async)]
        [Alias("mv")]
        public async Task MoveQueue(int indexToMove)
        {
            if (!node.HasPlayer(Context.Guild))
            {
                return;
            }

            var player = node.GetPlayer(Context.Guild);
            var queue = player.Queue.ToList();


            if (indexToMove < 1 || indexToMove > queue.Count)
                return;

            --indexToMove;

            LavaTrack trackToMove = queue.ElementAt(indexToMove);
            queue.RemoveAt(indexToMove);
            player.Queue.Clear();

            player.Queue.Enqueue(trackToMove);

            foreach (var p in queue)
            {
                player.Queue.Enqueue(p);
            }

            string newQueue = await audioHelper.UpdateEmbedQueue(player);
            await Program.message.ModifyAsync(x => x.Content = newQueue);
        }

        [Command("volume", RunMode = RunMode.Async)]
        [Alias("vol")]
        public async Task SetVolume(ushort? vol = null)
        {
            if (!node.HasPlayer(Context.Guild))
            {
                if (vol ==null)
                {
                    var embed = new EmbedBuilder
                    {
                        Color = Discord.Color.Orange,
                        Description = $"Volume is at `{Program.Volume}%`."
                    }.Build();
                    await Context.Channel.SendMessageAsync(embed:embed);
                    return;
                }
                else
                {
                    var embed = new EmbedBuilder
                    {
                        Color = Discord.Color.Orange,
                        Description = "You must be in a voice channel to change volume."
                    }.Build();
                    await Context.Channel.SendMessageAsync(embed:embed);
                    return;
                }
            }

            var player = node.GetPlayer(Context.Guild);

            if (vol == null)
            {
                var embed = new EmbedBuilder
                {
                    Color = Discord.Color.Orange,
                    Description = $"Current volume is at `{player.Volume}%`."
                }.Build();
                await Context.Channel.SendMessageAsync(embed:embed);
                return;
            }

            if (vol > 150)
            {
                var embed = new EmbedBuilder
                {
                    Color = Discord.Color.Orange,
                    Description = "Volume can only be set between 0 - 150"
                }.Build();
                await Context.Channel.SendMessageAsync(embed:embed);
                return;
            }

            Program.Volume = vol.Value;
            await player.UpdateVolumeAsync(vol.Value);

            await Program.message.ModifyAsync(x => x.Embed = audioHelper.BuildMusicEmbed(player));
        }

        // [Command("nowplaying")]
        // [Alias("np")]
        // public async Task NowPlaying()
        // {
        //     string s = await ah.NowPlaying();

        //     if (s == "")
        //     {
        //         return;
        //     }

        //     await Context.Channel.SendMessageAsync($"**Now Playing**: {s}");
        // }

        [Command("skip")]
        public async Task Skip()
        {
            if (!node.HasPlayer(Context.Guild))
            {
                return;
            }

            var player = node.GetPlayer(Context.Guild);

            if (player.PlayerState == PlayerState.Playing || player.PlayerState == PlayerState.Paused)
            {
                if (player.Queue.Count > 1)
                {
                    await player.SkipAsync();
                }
            }
        }

        [Command("queue")]
        [Alias("q")]
        public async Task DisplayQueue()
        {
            StringBuilder sb = new StringBuilder();
            if (!node.HasPlayer(Context.Guild))
            {
                return;
            }

            var player = node.GetPlayer(Context.Guild);
            var q = player.Queue.ToList();

            if (q.Count == 0)
            {
                return;
            }

            int idx = 1;
            foreach (var i in q)
            {
                sb.AppendLine($"{idx++}. {i.Title}");
            }

            await Context.Channel.SendMessageAsync(sb.ToString());
        }

        [Command("stop", RunMode = RunMode.Async)]
        public async Task Stop()
        {
            if (!node.HasPlayer(Context.Guild))
            {
                return;
            }

            var player = node.GetPlayer(Context.Guild);
            await player.StopAsync();
        }

        [Command("disconnect", RunMode = RunMode.Async)]
        [Alias("d", "dc", "leave")]
        public async Task Disconnect()
        {
            if (!node.HasPlayer(Context.Guild))
            {
                return;
            }

            var player = node.GetPlayer(Context.Guild);
            await node.LeaveAsync(player.VoiceChannel);
        }

        [Command("ping", RunMode = RunMode.Async)]
        public async Task Ping()
        {
            IUserMessage message;
            Stopwatch stopwatch;
            var latency = Context.Client.Latency;

            var tcs = new TaskCompletionSource<long>();
            var timeout = Task.Delay(TimeSpan.FromSeconds(30));

            Task TestMessageAsync(SocketMessage arg)
            {
                tcs.SetResult(stopwatch.ElapsedMilliseconds);
                return Task.CompletedTask;
            }

            stopwatch = Stopwatch.StartNew();
            message = await ReplyAsync($"{latency}ms");
            var init = stopwatch.ElapsedMilliseconds;

            Context.Client.MessageReceived += TestMessageAsync;
            var task = await Task.WhenAny(tcs.Task, timeout);
            Context.Client.MessageReceived -= TestMessageAsync;
            stopwatch.Stop();

            if (task == timeout)
            {
                await message.ModifyAsync(x => x.Content = $"{latency}ms, init: {init}ms, rtt: timed out");
            }
            else
            {
                var rtt = await tcs.Task;
                await message.ModifyAsync(x => x.Content = $"{latency}ms, init: {init}ms, rtt: {rtt}ms");
            }
        }
    }
}
