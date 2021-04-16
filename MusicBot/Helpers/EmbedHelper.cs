using System;
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
        private DiscordSocketClient discord;

        public EmbedHelper(DiscordSocketClient _discord)
        {
            discord = _discord;
        }

        public Task<Embed> BuildMusicEmbed(LavaPlayer player, Color color, string footer)
        {
            Embed embed = new EmbedBuilder
            {
                Color = color,
                Author = new EmbedAuthorBuilder
                {
                    IconUrl = discord.CurrentUser.GetAvatarUrl(),
                    Name = $"[{player.Track.Duration.ToTimecode()}] - {player.Track.Title}",
                    Url = player.Track.Url
                },
                ImageUrl = YouTubeHelper.GetYtThumbnail(player.Track.Url),
                Footer = new EmbedFooterBuilder { Text = footer }
            }.Build();

            return Task.FromResult(embed);
        }

        public Task<Embed> BuildDefaultEmbed()
        {
            var embed = new EmbedBuilder
            {
                Color = Color.DarkTeal,
                Title = "Nothing currently playing",
                Description = "This ain't Hydra. Please stop asking.",
                ImageUrl = "https://i.imgur.com/K4dWciL.jpg",
                Footer = new EmbedFooterBuilder { Text = "Prefix for this server is: m?" }
            }.Build();

            return Task.FromResult(embed);
        }

        public Task<Embed> BuildMessageEmbed(Color color, string text)
        {
            return Task.FromResult(new EmbedBuilder
            {
                Color = color,
                Description = text
            }.Build());
        }

        public Task<Embed> BuildErrorEmbed(string error)
        {
            return Task.FromResult(new EmbedBuilder
            {
                Color = Color.DarkRed,
                Description = error
            }.Build());
        }
    }
}
