using Discord;
using Discord.WebSocket;
using System.Threading.Tasks;
using Victoria;

namespace MusicBot.Helpers
{
    /// <summary>
    /// Class to create commonly used embeds
    /// </summary>
    public class EmbedHelper
    {
        private readonly DiscordSocketClient discord;

        public EmbedHelper(DiscordSocketClient _discord)
        {
            discord = _discord;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="player"></param>
        /// <param name="color"></param>
        /// <param name="paused"></param>
        /// <returns></returns>
        public ValueTask<Embed> BuildMusicEmbed(LavaPlayer player, Color color, bool paused = false)
        {
            string footerText = string.Format(AudioHelper.FooterText,
                                              player.Queue.Count,
                                              player.Queue.Count switch { 1 => "", _ => "s" },
                                              $"{Program.BotConfig.Volume}%");

            string iUrl = paused ? "https://i.imgur.com/F6ujxuo.gif" : YouTubeHelper.GetYtThumbnail(player.Track.Url) ?? player.Track.FetchArtworkAsync().Result;

            Embed embed = new EmbedBuilder
            {
                Color = color,
                Author = new EmbedAuthorBuilder
                {
                    IconUrl = discord.CurrentUser.GetAvatarUrl(),
                    Name = $"[{player.Track.Duration.ToTimecode()}] - {player.Track.Title}",
                    Url = player.Track.Url
                },
                ImageUrl = iUrl,
                Footer = new EmbedFooterBuilder { Text = footerText }
            }.Build();

            return new ValueTask<Embed>(embed);
        }

        public ValueTask<Embed> BuildDefaultEmbed()
        {
            var embed = new EmbedBuilder
            {
                Color = Color.DarkTeal,
                Title = "Nothing is currently playing",
                Description = "This ain't Hydra. Please stop asking.",
                ImageUrl = "https://i.imgur.com/K4dWciL.jpg",
                Footer = new EmbedFooterBuilder { Text = $"Prefix for this server is: {Program.BotConfig.Prefix}" }
            }.Build();

            return new ValueTask<Embed>(embed);
        }

        public ValueTask<Embed> BuildMessageEmbed(string text)
            => new ValueTask<Embed>(new EmbedBuilder
            {
                Color = Color.Orange,
                Description = text
            }.Build());

        public ValueTask<Embed> BuildErrorEmbed(string title, string error)
            => new ValueTask<Embed>(new EmbedBuilder
            {
                Color = Color.DarkRed,
                Title = title,
                Description = error
            }.Build());

        public ValueTask<Embed> BuildTrackErrorEmbed(string error)
            => new ValueTask<Embed>(new EmbedBuilder
            {
                Color = Color.DarkRed,
                Title = "An error has occurred",
                Description = error
            }.Build());
    }
}
