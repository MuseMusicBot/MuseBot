using System.Net.Http;
using System.Text.RegularExpressions;

namespace MusicBot.Helpers
{
    class YouTubeHelper
    {
        /// <summary>
        /// Grabs YouTube thumbnail
        /// </summary>
        /// <param name="url">YouTube url to get thumbnail from</param>
        /// <returns>Link to thumbnail</returns>
        public static string GetYtThumbnail(string url)
        {
            const string baseUrl = "https://img.youtube.com/vi/";
            const string maxRes = "/maxresdefault.jpg";
            const string hqDef = "/hqdefault.jpg";
            Regex r = new(@"https?:\/\/(?:www\.)?(?:youtube|youtu)\.(?:com|be)\/?(?:watch\?v=)?(?<id>[A-z0-9_-]{1,11})", RegexOptions.Compiled);
            var match = r.Match(url);

            if (!match.Success)
            {
                return null;
            }

            string urlWithId = baseUrl + match.Groups["id"].Value;

            using HttpClient httpClient = new();
            using var resp = httpClient.GetAsync(urlWithId + maxRes).Result;
            return urlWithId + (resp.IsSuccessStatusCode ? maxRes : hqDef);
        }
    }
}
