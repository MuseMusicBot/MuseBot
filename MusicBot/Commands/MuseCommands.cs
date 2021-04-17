using Discord;
using Discord.Commands;
using MusicBot.Helpers;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Victoria;
using Victoria.Enums;
using Victoria.Payloads;

namespace MusicBot.Commands
{
    public class MuseCommands : ModuleBase<SocketCommandContext>
    {
        private readonly LavaNode node;
        private readonly AudioHelper audioHelper;
        private readonly EmbedHelper embedHelper;

        #region ctor
        public MuseCommands(AudioHelper ah, LavaNode lavaNode, EmbedHelper eh)
        {
            node = lavaNode;
            audioHelper = ah;
            embedHelper = eh;
        }
        #endregion

        #region setup
        [Command("setup", RunMode = RunMode.Async)]
        public async Task Setup()
        {
            if (Context.Guild.TextChannels.Where(x => x.Name == "muse-song-requests").Any())
            {
                return;
            }

            var channel = await Context.Guild.CreateTextChannelAsync("muse-song-requests", x =>
            {
                var c = Context.Guild.CategoryChannels;
                x.CategoryId = c.Where(y => y.Name.Contains("general", StringComparison.OrdinalIgnoreCase)).First()?.Id;
                x.Topic = "Music Bot";
            });
            
            await channel.SendFileAsync("muse-banner.png","");

            var embed = await embedHelper.BuildDefaultEmbed();
            var msg = await channel.SendMessageAsync(AudioHelper.NoSongsInQueue, embed: embed);

            var guildId = Context.Guild.Id;
            var channelId = channel.Id;
            var msgId = msg.Id;

            File.WriteAllText(Program.testConfig, $"{guildId}\n{channelId}\n{msgId}\n");

            Program.message = msg;
        }
        #endregion

        #region play
        [Command("play", RunMode = RunMode.Async)]
        public async Task Play([Remainder] string query)
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
                await Context.Channel.SendMessageAsync($"{search.Exception}");
                return;
            }

            var player = node.GetPlayer(Context.Guild);

            await audioHelper.QueueTracksToPlayer(player, search);
        }
        #endregion

        #region pause
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
                var embed = await embedHelper.BuildMusicEmbed(player, Color.DarkTeal, $"{player.Queue.Count} song{player.Queue.Count switch { 1 => "", _ => "s" }} in queue | Volume: {Program.Volume}% | Song paused");
                await Program.message.ModifyAsync(x => x.Embed = embed);
            }
        }
        #endregion

        #region resume
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
                var embed = await embedHelper.BuildMusicEmbed(player, Color.DarkTeal, $"{player.Queue.Count} song{player.Queue.Count switch { 1 => "", _ => "s" }} in queue | Volume: {Program.Volume}%");
                await Program.message.ModifyAsync(x => x.Embed = embed);
            }

        }
        #endregion

        #region seek
        [Command("seek", RunMode = RunMode.Async)]
        [Alias("s")]
        public async Task Seek(string seekTime = null)
        {
            if (!node.HasPlayer(Context.Guild))
            {
                return;
            }

            var player = node.GetPlayer(Context.Guild);

            if (!(player.PlayerState == PlayerState.Playing || player.PlayerState == PlayerState.Paused))
            {
                return;
            }

            var pos = player.Track.Position;
            var len = player.Track.Duration;

            if (seekTime == null && (player.PlayerState == PlayerState.Playing || player.PlayerState == PlayerState.Paused))
            {
                var msg = await embedHelper.BuildMessageEmbed(Color.Orange, $"Current Position: {pos.ToTimecode()}/{len.ToTimecode()}");
                await (await Context.Channel.SendMessageAsync(embed: msg)).RemoveAfterTimeout(5000);
                return;
            }

            int seperators = seekTime.Split(":").Length;

            var seek = seperators switch
            {
                3 => seekTime.ToTimeSpan(),
                2 => ("00:" + seekTime).ToTimeSpan(),
                1 => ("00:00:" + seekTime).ToTimeSpan(),
                _ => default,
            };

            if (len.TotalMilliseconds - seek.TotalMilliseconds < 0)
            {
                var msg = await embedHelper.BuildMessageEmbed(Color.Orange, $"You can only seek up to {len.ToTimecode()}");
                await (await Context.Channel.SendMessageAsync(embed: msg)).RemoveAfterTimeout(5000);
                return;
            }

            else
            {
                await player.SeekAsync(seek);
                var msg = await embedHelper.BuildMessageEmbed(Color.Orange, $"Seeked to `{seek.ToTimecode()}`.");
                await (await Context.Channel.SendMessageAsync(embed: msg)).RemoveAfterTimeout(5000);
                return;
            }
        }
        #endregion

        #region shuffle
        [Command("shuffle", RunMode = RunMode.Async)]
        public async Task Shuffle()
        {
            var player = node.GetPlayer(Context.Guild);
            player.Queue.Shuffle();
            string newQueue = await audioHelper.UpdateEmbedQueue(player);
            await Program.message.ModifyAsync(x => x.Content = string.Format(AudioHelper.QueueMayHaveSongs, newQueue));
            var msg = await embedHelper.BuildMessageEmbed(Color.Orange, "Queue shuffled");
            await (await Context.Channel.SendMessageAsync(embed: msg)).RemoveAfterTimeout();
        }
        #endregion

        #region clear
        [Command("clear", RunMode = RunMode.Async)]
        [Alias("clearqueue")]
        public async Task Clear()
        {
            var player = node.GetPlayer(Context.Guild);
            player.Queue.Clear();
            var embed = await embedHelper.BuildMusicEmbed(player, Color.DarkTeal, $"{player.Queue.Count} song{player.Queue.Count switch { 1 => "", _ => "s" }} in queue | Volume: {Program.Volume}%");
            await Program.message.ModifyAsync(x => { x.Content = AudioHelper.NoSongsInQueue; x.Embed = embed; });
            var msg = await embedHelper.BuildMessageEmbed(Color.Orange, "Queue cleared");
            await (await Context.Channel.SendMessageAsync(embed: msg)).RemoveAfterTimeout();
        }
        #endregion

        #region move
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

            var msg = await embedHelper.BuildMessageEmbed(Color.Orange, $"**{trackToMove.Title}** moved to position 1.");
            await (await Context.Channel.SendMessageAsync(embed: msg)).RemoveAfterTimeout();
        }
        #endregion

        #region volume
        [Command("volume", RunMode = RunMode.Async)]
        [Alias("vol")]
        public async Task SetVolume(ushort? vol = null)
        {
            if (!node.HasPlayer(Context.Guild))
            {
                if (vol == null)
                {
                    var msg = await embedHelper.BuildMessageEmbed(Color.Orange, $"Volume is at `{Program.Volume}%`.");
                    await (await Context.Channel.SendMessageAsync(embed: msg)).RemoveAfterTimeout();
                    return;
                }
                else
                {
                    var msg = await embedHelper.BuildMessageEmbed(Color.Orange, "The bot must be in a voice channel to change volume.");
                    await (await Context.Channel.SendMessageAsync(embed: msg)).RemoveAfterTimeout();
                    return;
                }
            }

            var player = node.GetPlayer(Context.Guild);

            if (vol == null)
            {
                var msg = await embedHelper.BuildMessageEmbed(Color.Orange, $"Current volume is at `{player.Volume}%`.");
                await (await Context.Channel.SendMessageAsync(embed: msg)).RemoveAfterTimeout();
                return;
            }

            if (vol > 150 || vol < 1)
            {
                var msg = await embedHelper.BuildMessageEmbed(Color.Orange, "Volume can only be set between 0 - 150 inclusively");
                await (await Context.Channel.SendMessageAsync(embed: msg)).RemoveAfterTimeout();
                return;
            }

            if (player.PlayerState == PlayerState.Stopped)
            {
                Program.Volume = vol.Value;
                var msg = await embedHelper.BuildMessageEmbed(Color.Orange, $"Volume is now set to `{Program.Volume}%`.");
                await (await Context.Channel.SendMessageAsync(embed: msg)).RemoveAfterTimeout();
                return;
            }

            Program.Volume = vol.Value;
            await player.UpdateVolumeAsync(vol.Value);

            var embed = await embedHelper.BuildMusicEmbed(player, Color.DarkTeal, $"{player.Queue.Count} song{player.Queue.Count switch { 1 => "", _ => "s" }} in queue | Volume: {Program.Volume}%");
            await Program.message.ModifyAsync(x => x.Embed = embed);

            var volmsg = await embedHelper.BuildMessageEmbed(Color.Orange, $"Volume is now set to `{Program.Volume}%`.");
            await (await Context.Channel.SendMessageAsync(embed: volmsg)).RemoveAfterTimeout();
        }
        #endregion

        #region skip
        [Command("skip", RunMode = RunMode.Async)]
        public async Task Skip()
        {
            if (!node.HasPlayer(Context.Guild))
            {
                return;
            }

            var player = node.GetPlayer(Context.Guild);

            if (player.PlayerState == PlayerState.Playing || player.PlayerState == PlayerState.Paused)
            {
                if (player.Queue.Count >= 1)
                {
                    await player.SkipAsync();
                }
            }
        }
        #endregion

        #region stop
        [Command("stop", RunMode = RunMode.Async)]
        public async Task Stop()
        {
            if (!node.HasPlayer(Context.Guild))
            {
                return;
            }

            var player = node.GetPlayer(Context.Guild);
            player.Queue.Clear();
            var embed = await embedHelper.BuildDefaultEmbed();
            await Program.message.ModifyAsync(x => { x.Content = AudioHelper.NoSongsInQueue; x.Embed = embed; });
            await player.StopAsync();
        }
        #endregion

        #region restart
        [Command("restart", RunMode = RunMode.Async)]
        public async Task RestartTrack()
        {
            var player = node.GetPlayer(Context.Guild);
            if (player.PlayerState != PlayerState.Playing)
            {
                return;
            }
            await player.SeekAsync(TimeSpan.Zero);
            var msg = await embedHelper.BuildMessageEmbed(Color.Orange, "Let's run it one more time!");
            await (await Context.Channel.SendMessageAsync(embed: msg)).RemoveAfterTimeout();
        }
        #endregion

        #region disconnect
        [Command("disconnect", RunMode = RunMode.Async)]
        [Alias("d", "dc", "leave")]
        public async Task Disconnect()
        {
            if (!node.HasPlayer(Context.Guild))
            {
                return;
            }

            var player = node.GetPlayer(Context.Guild);
            player.Queue.Clear();
            var embed = await embedHelper.BuildDefaultEmbed();
            await Program.message.ModifyAsync(x => { x.Content = AudioHelper.NoSongsInQueue; x.Embed = embed; });
            await node.LeaveAsync(player.VoiceChannel);
        }
        #endregion

        #region equalizer
        [Command("equalizer", RunMode = RunMode.Async)]
        [Alias("eq")]
        public async Task Equalizer([Remainder]string eq = null)
        {
            if (!node.HasPlayer(Context.Guild))
            {
                await (await Context.Channel.SendMessageAsync(embed: await embedHelper.BuildErrorEmbed("Player is not in a voice channel"))).RemoveAfterTimeout(5000);
                return;
            }

            var player = node.GetPlayer(Context.Guild);

            if (eq == null)
            {
                var eqmsg = await embedHelper.BuildMessageEmbed(Color.Orange, (EQHelper.CurrentEQ == "Off") ? "No EQ applied." : $"Current EQ is: `{EQHelper.CurrentEQ}`");
                await (await Context.Channel.SendMessageAsync(embed: eqmsg)).RemoveAfterTimeout();
                return;
            }

            var textInfo = CultureInfo.InvariantCulture.TextInfo;
            EqualizerBand[] bands;
            switch (eq)
            {
                case "earrape":
                    bands = EQHelper.BuildEQ(new[] { 1, 1, 1, 1, -0.25, -0.25, -0.25, -0.25, -0.25, -0.25, -0.25, 1, 1, 1, 1 });
                    EQHelper.CurrentEQ = textInfo.ToTitleCase(eq);
                    break;

                case "bass":
                    bands = EQHelper.BuildEQ(new[] { 0.10, 0.10, 0.05, 0.05, 0.05, -0.05, -0.05, 0, -0.05, -0.05, 0, 0.05, 0.05, 0.10, 0.10 });
                    EQHelper.CurrentEQ = textInfo.ToTitleCase(eq);
                    break;

                case "pop":
                    bands = EQHelper.BuildEQ(new[] { -0.01, -0.01, 0, 0.01, 0.02, 0.05, 0.07, 0.10, 0.07, 0.05, 0.02, 0.01, 0, -0.01, -0.01 });
                    EQHelper.CurrentEQ = textInfo.ToTitleCase(eq);
                    break;

                case "off":
                    bands = EQHelper.BuildEQ(null);
                    EQHelper.CurrentEQ = textInfo.ToTitleCase(eq);
                    break;
                default:
                    await (await Context.Channel.SendMessageAsync(embed: await embedHelper.BuildMessageEmbed(Color.Orange, "Valid EQ modes: `earrape`, `bass`, `pop`, `off`"))).RemoveAfterTimeout(6000);
                    return;
            };

            await player.EqualizerAsync(bands);
            var msg = await embedHelper.BuildMessageEmbed(Color.Orange, (EQHelper.CurrentEQ == "Off") ? "EQ turned off" : $"`{EQHelper.CurrentEQ}`: working my magic!");
            await (await Context.Channel.SendMessageAsync(embed: msg)).RemoveAfterTimeout(5000);
        }
        #endregion

        // [Command("ping", RunMode = RunMode.Async)]
        // public async Task Ping()
        // {
        //     IUserMessage message;
        //     Stopwatch stopwatch;
        //     var latency = Context.Client.Latency;

        //     var tcs = new TaskCompletionSource<long>();
        //     var timeout = Task.Delay(TimeSpan.FromSeconds(30));

        //     Task TestMessageAsync(SocketMessage arg)
        //     {
        //         tcs.SetResult(stopwatch.ElapsedMilliseconds);
        //         return Task.CompletedTask;
        //     }

        //     stopwatch = Stopwatch.StartNew();
        //     message = await ReplyAsync($"{latency}ms");
        //     var init = stopwatch.ElapsedMilliseconds;

        //     Context.Client.MessageReceived += TestMessageAsync;
        //     var task = await Task.WhenAny(tcs.Task, timeout);
        //     Context.Client.MessageReceived -= TestMessageAsync;
        //     stopwatch.Stop();

        //     if (task == timeout)
        //     {
        //         await message.ModifyAsync(x => x.Content = $"{latency}ms, init: {init}ms, rtt: timed out");
        //     }
        //     else
        //     {
        //         var rtt = await tcs.Task;
        //         await message.ModifyAsync(x => x.Content = $"{latency}ms, init: {init}ms, rtt: {rtt}ms");
        //     }
        // }
    }
}
