using Discord;
using Microsoft.Extensions.Logging;
using MusicBot.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
        }

        public static TimeSpan ToTimeSpan(this string timecode)
        {
            if (!TimeSpan.TryParse(timecode, out var res))
            {
                return default;
            }

            return res;
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

        public static IEnumerable<T1> OrderedParallel<T, T1>(this IEnumerable<T> list, Func<T, T1> action)
        {
            var unorderedResult = new ConcurrentBag<(long, T1)>();
            Parallel.ForEach(list, (o, state, i) =>
            {
                unorderedResult.Add((i, action.Invoke(o)));
            });
            var ordered = unorderedResult.OrderBy(o => o.Item1);
            return ordered.Select(o => o.Item2);
        }
    }
}
