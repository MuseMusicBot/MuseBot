using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Victoria;

namespace MusicBot.Helpers
{
    public class EmbedHelper
    {
        private readonly DiscordSocketClient discord;

        public EmbedHelper(DiscordSocketClient _discord)
        {
            discord = _discord;
        }

        public ValueTask<Embed> BuildMusicEmbed(LavaPlayer player, Color color, bool paused = false)
        {
            string footerText = string.Format(AudioHelper.FooterText,
                                              player.Queue.Count,
                                              player.Queue.Count switch { 1 => "", _ => "s" },
                                              $"{Program.Volume}%");

            Embed embed = new EmbedBuilder
            {
                Color = color,
                Author = new EmbedAuthorBuilder
                {
                    IconUrl = discord.CurrentUser.GetAvatarUrl(),
                    Name = $"[{player.Track.Duration.ToTimecode()}] - {player.Track.Title}",
                    Url = player.Track.Url
                },
                ImageUrl = paused ? "https://i.imgur.com/F6ujxuo.gif" : YouTubeHelper.GetYtThumbnail(player.Track.Url) ?? player.Track.FetchArtworkAsync().Result, // If not youtube, have Victoria fetch the artwork
                Footer = new EmbedFooterBuilder { Text = footerText }
            }.Build();

            return new ValueTask<Embed>(embed);
        }

        public ValueTask<Embed> BuildDefaultEmbed()
        {
            var embed = new EmbedBuilder
            {
                Color = Color.DarkTeal,
                Title = "Nothing currently playing",
                Description = "This ain't Hydra. Please stop asking.",
                ImageUrl = "https://i.imgur.com/K4dWciL.jpg",
                Footer = new EmbedFooterBuilder { Text = "Prefix for this server is: m?" }
            }.Build();

            return new ValueTask<Embed>(embed);
        }

        public ValueTask<Embed> BuildMessageEmbed(Color color, string text)
            => new ValueTask<Embed>(new EmbedBuilder
            {
                Color = color,
                Description = text
            }.Build());

        public ValueTask<Embed> BuildErrorEmbed(string title, string error)
            => new ValueTask<Embed>(new EmbedBuilder
            {
                Color = Color.DarkRed,
                Title = title,
                Description = error
            }.Build());

        public ValueTask<Embed> BuildJokeEmbed(LavaPlayer player, bool paused = false)
        {
            string footerText = string.Format(AudioHelper.FooterText,
                                              player.Queue.Count,
                                              player.Queue.Count switch { 1 => "", _ => "s" },
                                              $"{Program.Volume}%",
                                              paused ? " | Song paused" : "",
                                              "");

            Embed embed = new EmbedBuilder
            {
                Color = Color.DarkTeal,
                Author = new EmbedAuthorBuilder
                {
                    IconUrl = discord.CurrentUser.GetAvatarUrl(),
                    Name = $"[69:69] - Rick Astley - Never Gonna Give You Up (Video)",
                    Url = "https://youtu.be/dQw4w9WgXcQ"
                },
                ImageUrl = "https://img.youtube.com/vi/dQw4w9WgXcQ/maxresdefault.jpg", // If not youtube, have Victoria fetch the artwork
                Footer = new EmbedFooterBuilder { Text = footerText }
            }.Build();

            return new ValueTask<Embed>(embed);
        }
    }
}
