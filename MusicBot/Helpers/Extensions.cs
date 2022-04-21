using Discord;
using Microsoft.Extensions.Logging;
using MusicBot.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Victoria;

namespace MusicBot.Helpers
{
    /// <summary>
    /// Various extension methods
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Creates a timecode string from a <see cref="TimeSpan" />
        /// </summary>
        /// <param name="ts"><see cref="TimeSpan"/> to make a timecode string</param>
        /// <returns><see cref="TimeSpan"/> to timecode string</returns>
        public static string ToTimecode(this TimeSpan ts)
        {
            var hours = ts.Hours;
            var min = ts.Minutes;
            var sec = ts.Seconds;

            return $"{(hours > 0 ? $"{hours:d2}:" : "")}{min:d2}:{sec:d2}";
        }

        /// <summary>
        /// Removes <see cref="IMessage" /> after a timeout
        /// </summary>
        /// <param name="message">Message to remove</param>
        /// <param name="timeout">Timeout to remove message</param>
        /// <returns></returns>
        private static async Task RemoveAfterTimeout(this IDeletable message, int timeout = 10000)
        {
            await Task.Delay(timeout);
            await message.DeleteAsync(new RequestOptions { RetryMode = RetryMode.RetryRatelimit });
            await Task.CompletedTask;
        }

        /// <summary>
        /// String timecode to <see cref="TimeSpan"/>
        /// </summary>
        /// <param name="timecode">string to be converted</param>
        /// <returns><see cref="TimeSpan" /> of the timecode, default if bad parse</returns>
        public static TimeSpan ToTimeSpan(this string timecode)
        {
            return !TimeSpan.TryParse(timecode, out var res) ? default : res;
        }

        /// <summary>
        /// Sends message and removes it after timeout
        /// </summary>
        /// <typeparam name="T"><see cref="IMessageChannel" /> to send message from</typeparam>
        /// <param name="channel">Channel to send message from</param>
        /// <param name="content">Text content</param>
        /// <param name="embed">Embed</param>
        /// <param name="timeout">Timeout to remove message, default is 10 seconds</param>
        /// <returns></returns>
        public static async Task SendAndRemove<T>(this T channel, string content = null, Embed embed = null, int timeout = 10000) where T: IMessageChannel
        {
            var msg = await channel.SendMessageAsync(text: content, embed: embed);
            await msg.RemoveAfterTimeout(timeout);
            await Task.CompletedTask;
        }

        /// <summary>
        /// Creates <see cref="MuseTrack"/> from <see cref="LavaTrack"/>
        /// </summary>
        /// <param name="track"><see cref="LavaTrack"/> to make MuseTrack from</param>
        /// <param name="requester">Requester of the song</param>
        /// <returns></returns>
        public static MuseTrack CreateMuseTrack(this LavaTrack track, IGuildUser requester)
            => new MuseTrack(track, requester);

        /// <summary>
        /// Logs Message
        /// </summary>
        /// <param name="logger">ILogger to log message to</param>
        /// <param name="message">LogMessage</param>
        /// <returns></returns>
        public static Task LogMessage(this ILogger logger, LogMessage message)
        {
            logger.Log(
            LoggingService.LogLevelFromSeverity(message.Severity),
            0,
            message,
            message.Exception,
            (_1, _2) => message.ToString(prependTimestamp: false));

            return Task.CompletedTask;
        }

        /// <summary>
        /// Checks is string in a URI
        /// </summary>
        /// <param name="url">string to check</param>
        /// <param name="uri">Uri object from string</param>
        /// <returns><see langword="true" /> if string is URI, <see langword="false" /> otherwise</returns>
        public static bool IsUri(this string url, out Uri uri)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out uri);
        }

        /// <summary>
        /// Runs collection in parallel while maintaining order
        /// </summary>
        /// <typeparam name="T">Type of collection</typeparam>
        /// <typeparam name="T1">Type of new collection</typeparam>
        /// <param name="list">IEnumerable to perform action on</param>
        /// <param name="action">Action to perform</param>
        /// <returns><see cref="IEnumerable{T}" where T is T1/></returns>
        public static IEnumerable<T1> OrderedParallelAsync<T, T1>(this IEnumerable<T> list, Func<T, Task<T1>> action)
        {
            var unorderedResult = new ConcurrentBag<(long, T1)>();
            Parallel.ForEach(list, async (o, state, i) =>
            {
                unorderedResult.Add((i, await action.Invoke(o).ConfigureAwait(false)));
            });
            var ordered = unorderedResult.OrderBy(o => o.Item1);
            return ordered.Select(o => o.Item2);
        }
    }
}
