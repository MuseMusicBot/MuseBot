using System.Text.RegularExpressions;
using System.Net;

namespace MusicBot.Helpers
{
    class YouTubeHelper
    {
        public static string GetYtThumbnail(string url)
        {
            string baseUrl = "https://img.youtube.com/vi/";
            string jpg = "/maxresdefault.jpg";
            Regex r = new Regex(@"https?:\/\/(?:www.)?(?:youtube|youtu)\.(?:com|be)\/?(?:watch\?v=)?(?<id>[A-z0-9_-]{1,11})", RegexOptions.Compiled);
            var match = r.Match(url);
            string hdthumb = baseUrl + match.Groups["id"].Value + jpg;

            try
            {
                byte[] imageData = new WebClient().DownloadData(hdthumb);
                return hdthumb;
            }
            catch
            {
                string sdthumb = baseUrl + match.Groups["id"].Value + jpg.Replace("/maxresdefault.jpg", "/hqdefault.jpg");
                return sdthumb;
            }
        }
    }
}
