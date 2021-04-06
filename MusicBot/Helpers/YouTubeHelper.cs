using System.Text.RegularExpressions;

namespace MusicBot.Helpers
{
    class YouTubeHelper
    {
        public static string GetYtThumbnail(string url)
        {
            string baseUrl = "https://img.youtube.com/vi/";
            string jpg = "/hqdefault.jpg";
            Regex r = new Regex(@"https?:\/\/(?:www.)?(?:youtube|youtu)\.(?:com|be)\/?(?:watch\?v=)?(?<id>[A-z0-9_-]{1,11})", RegexOptions.Compiled);
            var match = r.Match(url);
            string newUrl = baseUrl + match.Groups["id"].Value + jpg;
            return newUrl;
        }
    }
}
