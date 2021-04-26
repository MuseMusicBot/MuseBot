using System.Net;
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
            Regex r = new Regex(@"https?:\/\/(?:www\.)?(?:youtube|youtu)\.(?:com|be)\/?(?:watch\?v=)?(?<id>[A-z0-9_-]{1,11})", RegexOptions.Compiled);
            var match = r.Match(url);
            
            if (!match.Success)
            {
                return null;
            }

            string hdthumb = baseUrl + match.Groups["id"].Value + maxRes;
            try
            {
                // Making sure to dispose of the webclient after
                using var wc = new WebClient();
                _ = wc.DownloadData(hdthumb);
                return hdthumb;
            }
            catch
            {
                return baseUrl + match.Groups["id"].Value + hqDef;
            }
        }
    }
}
