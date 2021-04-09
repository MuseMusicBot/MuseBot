using Discord;
using System;
using System.Collections.Generic;
using System.Text;
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
    }
}
