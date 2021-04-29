using Discord;
using Discord.WebSocket;
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
    public static class Extensions
    {
        public static string ToTimecode(this TimeSpan ts)
        {
            var hours = ts.Hours;
            var min = ts.Minutes;
            var sec = ts.Seconds;

            return $"{(hours > 0 ? $"{hours:d2}:" : "")}{min:d2}:{sec:d2}";
        }

        public static async Task RemoveAfterTimeout(this IMessage message, int timeout = 10000)
        {
            await Task.Delay(timeout);
            await message.DeleteAsync(new RequestOptions { RetryMode = RetryMode.RetryRatelimit });
            await Task.CompletedTask;
        }

        public static TimeSpan ToTimeSpan(this string timecode)
        {
            if (!TimeSpan.TryParse(timecode, out var res))
            {
                return default;
            }

            return res;
        }

        public static async Task SendAndRemove<T>(this T channel, string content = null, Embed embed = null, int timeout = 10000) where T: IMessageChannel
        {
            var msg = await channel.SendMessageAsync(text: content, embed: embed);
            await msg.RemoveAfterTimeout(timeout);
            await Task.CompletedTask;
        }

        public static MuseTrack CreateMuseTrack(this LavaTrack track, IGuildUser requester)
        {
            return new MuseTrack(track, requester);
        }

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

        public static bool IsUri(this string url, out Uri uri)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out uri);
        }

        public static IEnumerable<T1> OrderedParallel<T, T1>(this IEnumerable<T> list, Func<T, Task<T1>> action)
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
