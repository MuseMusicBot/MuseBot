﻿using Discord;
using Discord.Commands;
using Microsoft.Extensions.Logging;
using MusicBot.Helpers;
using MusicBot.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
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
        private readonly ILogger logger;

        private static Dictionary<string, double[]> EqBands = new()
        {
            { "earrape", new[] { 1, 1, 1, 1, -0.25, -0.25, -0.25, -0.25, -0.25, -0.25, -0.25, 1, 1, 1, 1 } },
            { "bass", new[] { 0.10, 0.10, 0.05, 0.05, 0.05, -0.05, -0.05, 0, -0.05, -0.05, 0, 0.05, 0.05, 0.10, 0.10 }},
            { "pop", new[] { -0.01, -0.01, 0, 0.01, 0.02, 0.05, 0.07, 0.10, 0.07, 0.05, 0.02, 0.01, 0, -0.01, -0.01 }},
            { "off", null }
        };

        #region ctor
        public MuseCommands(AudioHelper ah, LavaNode lavaNode, EmbedHelper eh, LoggingService loggingService)
        {
            node = lavaNode;
            audioHelper = ah;
            embedHelper = eh;
            logger = loggingService.Loggers[LoggingService.LoggerEntries.Commands];
        }
        #endregion

        #region setup
        [Command("setup", RunMode = RunMode.Async)]
        [Summary("Setups the song request channel.")]
        public async Task Setup()
        {
            if (Context.Guild.TextChannels.Any(x => x.Id == Program.BotConfig.ChannelId))
            {
                var error = await embedHelper.BuildErrorEmbed("Could not create channel", $"<#{Program.BotConfig.ChannelId}> already exists.");
                await Context.Channel.SendAndRemove(embed: error, timeout: 15000);
                return;
            }

            var channel = await Context.Guild.CreateTextChannelAsync("muse-song-requests", x =>
            {
                var c = Context.Guild.CategoryChannels;
                x.CategoryId = c.FirstOrDefault(y => y.Name.Contains("general", StringComparison.OrdinalIgnoreCase))?.Id;
                x.Topic = "Music Bot";
            });

            await channel.SendFileAsync("muse-banner.png", "");

            var msg = await channel.SendMessageAsync(AudioHelper.NoSongsInQueue, embed: await EmbedHelper.BuildDefaultEmbed());
            await msg.AddReactionsAsync(ReactionsHelper.Emojis, new RequestOptions { RetryMode = RetryMode.RetryRatelimit });

            var config = Program.BotConfig;
            config.GuildId = Context.Guild.Id;
            config.ChannelId = channel.Id;
            config.MessageId = msg.Id;
            config.BotEmbedMessage = msg;
            ConfigHelper.UpdateConfigFile(config);
            Program.BotConfig = config;
        }
        #endregion

        #region play
        [Command("play", RunMode = RunMode.Async)]
        [Summary("Plays a song.")]
        public async Task Play([Remainder] string query)
        {
            LavaPlayer player;
            if (!node.HasPlayer(Context.Guild))
            {
                try
                {
                    if ((Context.User as IGuildUser)?.VoiceChannel == null)
                    {
                        return;
                    }

                    await node.JoinAsync((Context.User as IGuildUser)?.VoiceChannel, Context.Channel as ITextChannel);
                    player = node.GetPlayer(Context.Guild);
                    await player.UpdateVolumeAsync(Program.BotConfig.Volume);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Failed to create player");
                }
            }

            player = node.GetPlayer(Context.Guild);

            var search = await node.SearchAsync(query);
            if (search.LoadStatus is LoadStatus.LoadFailed or LoadStatus.NoMatches)
            {
                await Context.Channel.SendMessageAsync($"{search.Exception}");
                return;
            }

            await audioHelper.QueueTracksToPlayer(player, search);
        }
        #endregion

        #region pause
        [Command("pause")]
        [Alias("p")]
        [Summary("Pauses the current playing song.")]
        public async Task Pause()
        {
            if (!node.HasPlayer(Context.Guild))
            {
                return;
            }

            var player = node.GetPlayer(Context.Guild);

            switch (player.PlayerState)
            {
                case PlayerState.Paused:
                    return;
                case PlayerState.Playing:
                    await player.PauseResumeAsync(embedHelper, false, context: Context);
                    break;
            }
        }
        #endregion

        #region resume
        [Command("resume")]
        [Alias("r")]
        [Summary("Resumes the current playing song.")]
        public async Task Resume()
        {
            if (!node.HasPlayer(Context.Guild))
            {
                return;
            }

            var player = node.GetPlayer(Context.Guild);

            switch (player.PlayerState)
            {
                case PlayerState.Playing:
                    return;
                case PlayerState.Paused:
                    await player.PauseResumeAsync(embedHelper, true, context: Context);
                    break;
            }
        }
        #endregion

        #region seek
        [Command("seek", RunMode = RunMode.Async)]
        [Alias("s")]
        [Summary("Seeks to a specific part of the current song.")]
        public async Task Seek(string seekTime = null)
        {
            if (!node.HasPlayer(Context.Guild))
            {
                return;
            }

            var player = node.GetPlayer(Context.Guild);

            if (player.PlayerState is not (PlayerState.Playing or PlayerState.Paused))
            {
                return;
            }

            var pos = player.Track.Position;
            var len = player.Track.Duration;

            if (seekTime == null && player.PlayerState is PlayerState.Playing or PlayerState.Paused)
            {
                var msg = await embedHelper.BuildMessageEmbed($"Current Position: {pos.ToTimecode()}/{len.ToTimecode()}");
                await Context.Channel.SendAndRemove(embed: msg, timeout: 5000);
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
                var msg = await embedHelper.BuildMessageEmbed($"You can only seek up to {len.ToTimecode()}");
                await Context.Channel.SendAndRemove(embed: msg, timeout: 5000);
                return;
            }

            else
            {
                await player.SeekAsync(seek);
                var msg = await embedHelper.BuildMessageEmbed($"Seeked to `{seek.ToTimecode()}`.");
                await Context.Channel.SendAndRemove(embed: msg, timeout: 5000);
                return;
            }
        }
        #endregion

        #region shuffle
        [Command("shuffle", RunMode = RunMode.Async)]
        [Summary("Shuffles the entire queue.")]
        public async Task Shuffle()
        {
            if (node.TryGetPlayer(Context.Guild, out var player) &&
                player.PlayerState is PlayerState.Playing or PlayerState.Paused)
            {
                await player.ShuffleAsync(audioHelper, embedHelper, context: Context);
            }
        }
        #endregion

        #region clear
        [Command("clear", RunMode = RunMode.Async)]
        [Alias("clearqueue")]
        [Summary("Clears the entire queue.")]
        public async Task Clear()
        {
            if (!node.HasPlayer(Context.Guild))
            {
                return;
            }

            var player = node.GetPlayer(Context.Guild);
            player.Queue.Clear();
            if (Context.Guild.TextChannels.Any(x => x.Id == Program.BotConfig.ChannelId))
            {
                var embed = await embedHelper.BuildMusicEmbed(player, Color.DarkTeal);
                await Program.BotConfig.BotEmbedMessage.ModifyAsync(x => { x.Content = AudioHelper.NoSongsInQueue; x.Embed = embed; });
            }
            var msg = await embedHelper.BuildMessageEmbed("Queue cleared");
            await Context.Channel.SendAndRemove(embed: msg);
        }
        #endregion

        #region move
        [Command("move", RunMode = RunMode.Async)]
        [Alias("mv")]
        [Summary("Moves a specified song to the top queue.")]
        public async Task MoveQueue(int indexToMove = 0)
        {
            if (!node.HasPlayer(Context.Guild))
            {
                return;
            }

            var player = node.GetPlayer(Context.Guild);
            var queue = player.Queue.ToList();

            if (indexToMove == 0)
            {
                var msg = await embedHelper.BuildMessageEmbed("Please specify a track to move.");
                await Context.Channel.SendAndRemove(embed: msg, timeout: 5000);
                return;
            }
            if (queue.Count == 0)
            {
                var msg = await embedHelper.BuildMessageEmbed("Nothing in queue to remove.");
                await Context.Channel.SendAndRemove(embed: msg);
                return;
            }
            if (indexToMove > queue.Count)
            {
                var msg = await embedHelper.BuildMessageEmbed("Invalid track number.");
                await Context.Channel.SendAndRemove(embed: msg);
                return;
            }

            --indexToMove;

            LavaTrack trackToMove = queue.ElementAt(indexToMove);
            queue.RemoveAt(indexToMove);
            player.Queue.Clear();

            player.Queue.Enqueue(trackToMove);

            foreach (var p in queue)
            {
                player.Queue.Enqueue(p);
            }

            if (Context.Guild.TextChannels.Any(x => x.Id == Program.BotConfig.ChannelId))
            {
                string newQueue = await AudioHelper.GetNewEmbedQueueString(player);
                await Program.BotConfig.BotEmbedMessage.ModifyAsync(x => x.Content = newQueue);
            }

            var msg2 = await embedHelper.BuildMessageEmbed($"**{trackToMove.Title}** moved to position 1.");
            await Context.Channel.SendAndRemove(embed: msg2);
        }
        #endregion

        #region volume
        [Command("volume", RunMode = RunMode.Async)]
        [Alias("vol")]
        [Summary("Changes the output of the bot's volume.")]
        public async Task SetVolume(ushort? vol = null)
        {
            //If user sets volume when Bot is not in a voice channel
            if (!node.HasPlayer(Context.Guild))
            {
                if (vol == null)
                {
                    var msg = await embedHelper.BuildMessageEmbed($"Volume is at `{Program.BotConfig.Volume}%`.");
                    await Context.Channel.SendAndRemove(embed: msg);
                    return;
                }
                else
                {
                    var msg = await embedHelper.BuildMessageEmbed("The bot must be in a voice channel to change volume.");
                    await Context.Channel.SendAndRemove(embed: msg);
                    return;
                }
            }

            var player = node.GetPlayer(Context.Guild);

            //If user does not specify a volume
            if (vol == null)
            {
                var msg = await embedHelper.BuildMessageEmbed($"Current volume is at `{player.Volume}%`.");
                await Context.Channel.SendAndRemove(embed: msg);
                return;
            }
            //If user sets the volume to the same level as it currently is
            if (vol == Program.BotConfig.Volume)
            {
                var msg = await embedHelper.BuildMessageEmbed($"Volume is already set to `{player.Volume}%`.");
                await Context.Channel.SendAndRemove(embed: msg);
                return;
            }
            //If user sets the volume below 1 or above 150
            if (vol is > 150 or < 1)
            {
                var msg = await embedHelper.BuildMessageEmbed("Volume can only be set between 0 - 150 inclusively");
                await Context.Channel.SendAndRemove(embed: msg);
                return;
            }
            //If user sets the volume when music is stopped (not paused)
            if (player.PlayerState == PlayerState.Stopped)
            {
                var config2 = Program.BotConfig;
                config2.Volume = vol.Value;
                Program.BotConfig = config2;
                var msg = await embedHelper.BuildMessageEmbed($"Volume is now set to `{Program.BotConfig.Volume}%`.");
                await Context.Channel.SendAndRemove(embed: msg);
                return;
            }

            var config = Program.BotConfig;
            config.Volume = vol.Value;
            Program.BotConfig = config;
            await player.UpdateVolumeAsync(vol.Value);

            if (Context.Guild.TextChannels.Any(x => x.Id == Program.BotConfig.ChannelId))
            {
                var embed = await embedHelper.BuildMusicEmbed(player, Color.DarkTeal, player.PlayerState == PlayerState.Paused);
                await Program.BotConfig.BotEmbedMessage.ModifyAsync(x => x.Embed = embed);
            }

            var volmsg = await embedHelper.BuildMessageEmbed($"Volume is now set to `{Program.BotConfig.Volume}%`.");
            await Context.Channel.SendAndRemove(embed: volmsg);
        }
        #endregion

        #region skip
        [Command("skip", RunMode = RunMode.Async)]
        [Summary("Skips the current playing song.")]
        public async Task Skip()
        {
            if (!node.HasPlayer(Context.Guild))
            {
                return;
            }

            var player = node.GetPlayer(Context.Guild);

            if (player.PlayerState is PlayerState.Playing or PlayerState.Paused)
            {
                await player.NextTrackAsync(embedHelper, context: Context);
            }
        }
        #endregion

        #region stop
        [Command("stop", RunMode = RunMode.Async)]
        [Summary("Stops playing and clears the queue.")]
        public async Task Stop()
        {
            if (!node.HasPlayer(Context.Guild))
            {
                return;
            }

            var player = node.GetPlayer(Context.Guild);
            player.Queue.Clear();
            if (Context.Guild.TextChannels.Any(x => x.Id == Program.BotConfig.ChannelId))
            {
                var embed = await EmbedHelper.BuildDefaultEmbed();
                await Program.BotConfig.BotEmbedMessage.ModifyAsync(x => { x.Content = AudioHelper.NoSongsInQueue; x.Embed = embed; });
            }
            await player.StopAsync();
        }
        #endregion

        #region replay
        [Command("replay", RunMode = RunMode.Async)]
        [Alias("restart")]
        [Summary("Restarts the current playing song.")]
        public async Task RestartTrack()
        {
            var player = node.GetPlayer(Context.Guild);
            if (player.PlayerState != PlayerState.Playing)
            {
                return;
            }
            await player.PreviousAsync();
            var msg = await embedHelper.BuildMessageEmbed("Let's run it one more time!");
            await Context.Channel.SendAndRemove(embed: msg);
        }
        #endregion

        #region disconnect
        [Command("disconnect", RunMode = RunMode.Async)]
        [Alias("d", "dc", "leave")]
        [Summary("Disconnects the bot from the voice channel.")]
        public async Task Disconnect()
        {
            await node.EjectAsync(embedHelper, Context.Guild, Context);
        }
        #endregion

        #region remove
        [Command("remove", RunMode = RunMode.Async)]
        [Summary("Removes a specific song from the queue.")]
        public async Task Remove(int indexToMove = 0)
        {
            if (!node.HasPlayer(Context.Guild))
            {
                return;
            }

            var player = node.GetPlayer(Context.Guild);
            var queue = player.Queue.ToList();

            if (indexToMove == 0)
            {
                var msg = await embedHelper.BuildMessageEmbed("Please specify a track to remove.");
                await Context.Channel.SendAndRemove(embed: msg);
                return;
            }
            if (queue.Count == 0)
            {
                var msg = await embedHelper.BuildMessageEmbed("Nothing in queue to remove.");
                await Context.Channel.SendAndRemove(embed: msg);
                return;
            }
            if (indexToMove > queue.Count)
            {
                var msg = await embedHelper.BuildMessageEmbed("Invalid track number.");
                await Context.Channel.SendAndRemove(embed: msg);
                return;
            }

            --indexToMove;

            LavaTrack trackToRemove = queue.ElementAt(indexToMove) as LavaTrack;
            queue.RemoveAt(indexToMove);
            player.Queue.Clear();

            foreach (var p in queue)
            {
                player.Queue.Enqueue(p);
            }

            if (Context.Guild.TextChannels.Any(x => x.Id == Program.BotConfig.ChannelId))
            {
                string newQueue = await AudioHelper.GetNewEmbedQueueString(player);
                await Program.BotConfig.BotEmbedMessage.ModifyAsync(x => x.Content = newQueue);
            }

            var msg2 = await embedHelper.BuildMessageEmbed($"**{trackToRemove.Title}** has been removed.");
            await Context.Channel.SendAndRemove(embed: msg2);
        }
        #endregion

        #region equalizer
        [Command("equalizer", RunMode = RunMode.Async)]
        [Alias("eq")]
        [Summary("Applies EQ to the bot.")]
        public async Task Equalizer([Remainder] string eq = null)
        {
            if (!node.HasPlayer(Context.Guild))
            {
                return;
            }

            var player = node.GetPlayer(Context.Guild);

            if (eq == null)
            {
                var eqmsg = await embedHelper.BuildMessageEmbed((EQHelper.CurrentEQ == "Off") ? "No EQ applied." : $"Current EQ is: `{EQHelper.CurrentEQ}`");
                await Context.Channel.SendAndRemove(embed: eqmsg);
                return;
            }

            var textInfo = CultureInfo.InvariantCulture.TextInfo;

            if (!EqBands.ContainsKey(eq))
            {
                var sb = new StringBuilder();

                var keys = EqBands.Keys.ToList();

                for (var i = 0; i < keys.Count; i++)
                {
                    keys[i] = $"`{keys[i]}`";
                }

                await Context.Channel.SendAndRemove(
                    embed: await embedHelper.BuildMessageEmbed($"Valid EQ modes: {string.Join(", ", keys)}"),
                    timeout: 6000);
            }

            var bands = EQHelper.BuildEQ(EqBands[eq]);

            EQHelper.CurrentEQ = textInfo.ToTitleCase(eq);
            await player.EqualizerAsync(bands);
            var msg = await embedHelper.BuildMessageEmbed((EQHelper.CurrentEQ == "Off") ? "EQ turned off" : $"`{EQHelper.CurrentEQ}`: working my magic!");
            await Context.Channel.SendAndRemove(embed: msg, timeout: 5000);
        }
        #endregion

        #region spotify
        [Command("spotify", RunMode = RunMode.Async)]
        [Summary("Plays a song from Spotify.")]
        public async Task Spotify([Remainder] string url)
        {
            var player = node.GetPlayer(Context.Guild);

            if (!node.HasPlayer(Context.Guild))
            {
                await node.JoinAsync((Context.User as IGuildUser)?.VoiceChannel, Context.Channel as ITextChannel);
                await player.UpdateVolumeAsync(Program.BotConfig.Volume);
            }

            var tracks = await audioHelper.SearchSpotify(Context.Channel, url);
            if (tracks != null)
            {
                await audioHelper.QueueSpotifyToPlayer(player, tracks);
            }
        }
        #endregion

        #region requester
        [Command("requester", RunMode = RunMode.Async)]
        [Alias("req")]
        [Summary("Shows the user that requested the current song.")]
        public async Task Requester()
        {
            if (!node.TryGetPlayer(Context.Guild, out var player))
            {
                return;
            }

            if (player.Track is not MuseTrack track)
            {
                return;
            }
            
            var embed = await embedHelper.BuildMessageEmbed($"Requested by: `{track.Requester.Nickname ?? track.Requester.Username + "#" + track.Requester.Discriminator}`");
            await Context.Channel.SendAndRemove(embed: embed);
        }
        #endregion

        #region regen
        [Command("regenreactions", RunMode = RunMode.Async)]
        [Alias("regen", "rr")]
        [Summary("Regenerates the reaction buttons.")]
        [RequireUserPermission(ChannelPermission.ManageChannels)]
        public async Task RegenerateReactions()
        {
            var msg = Program.BotConfig.BotEmbedMessage;
            await msg.RemoveAllReactionsAsync(new RequestOptions { RetryMode = RetryMode.RetryRatelimit });
            await msg.AddReactionsAsync(ReactionsHelper.Emojis, new RequestOptions { RetryMode = RetryMode.RetryRatelimit });

            await Context.Channel.SendAndRemove(embed: await embedHelper.BuildMessageEmbed("Regenerated reactions."));
        }
        #endregion

        #region 24/7
        [Command("24/7", RunMode = RunMode.Async)]
        [Alias("247")]
        [Summary("Toggle bot to stay in channel 24/7.")]
        public async Task StayConnected()
        {
            audioHelper.StayFlag = !audioHelper.StayFlag;
            var embed = await embedHelper.BuildMessageEmbed($"24/7 is now `{(audioHelper.StayFlag ? "enabled" : "disabled")}`");
            await Context.Channel.SendAndRemove(embed: embed);
        }
        #endregion

        #region Loop
        [Command("loop", RunMode = RunMode.Async)]
        [Summary("Loops the currently playing song.")]
        public async Task Loop()
        {
            if (node.TryGetPlayer(Context.Guild, out var player) && player.PlayerState == PlayerState.Playing ||
                player.PlayerState == PlayerState.Paused)
            {
                await player.LoopAsync(audioHelper, embedHelper, Context.Channel);
            }
        }
        #endregion

        #region Prefix
        [Command("prefix", RunMode = RunMode.Async)]
        [Summary("Changes the bot's prefix")]
        public async Task Prefix([Remainder] string prefix = null)
        {
            switch (prefix)
            {
                case null:
                {
                    var current = await embedHelper.BuildMessageEmbed($"Current prefix is: **{Program.BotConfig.Prefix}**");
                    await Context.Channel.SendAndRemove(embed: current);
                    return;
                }
                case "default":
                    prefix = "m?";
                    break;
            }

            var config = Program.BotConfig;
            config.Prefix = prefix;
            ConfigHelper.UpdateConfigFile(config);
            Program.BotConfig = config;

            if (Context.Guild.TextChannels.Any(x => x.Id == Program.BotConfig.ChannelId))
            {
                var embed = await EmbedHelper.BuildDefaultEmbed();
                await Program.BotConfig.BotEmbedMessage.ModifyAsync(x => { x.Embed = embed; });
            }

            var msg = await embedHelper.BuildMessageEmbed($"Prefix has been changed to: **{Program.BotConfig.Prefix}**");
            await Context.Channel.SendAndRemove(embed: msg);
        }
        #endregion
    }
}
