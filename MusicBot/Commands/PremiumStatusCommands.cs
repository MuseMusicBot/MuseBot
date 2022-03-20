using Discord;
using Discord.Commands;
using MusicBot.Helpers;
using System.Threading.Tasks;

namespace MusicBot.Commands
{
    public class PremiumStatusCommands : ModuleBase<SocketCommandContext>
    {
        [Command("premiumstatus", RunMode = RunMode.Async)]
        [Alias("premium")]
        [Summary("Shows the server premium status.")]
        public async Task Premium()
        {
            var embed = new EmbedBuilder
            {
                Color = Discord.Color.DarkerGrey,
                Title = "Premium status",
                Description = "Your Subscription: `Free Forever`\n[Purchase](https://i.imgur.com/z4lM0Yj.gif) | [Manage](https://youtu.be/moZtoMP7HAA)",
                ImageUrl = "https://i.imgur.com/aq7yRAn.gif",
                Footer = new EmbedFooterBuilder { Text = "♡ Hydra" }
            }.Build();

            await Context.Channel.SendAndRemove(embed: embed, timeout: 15000);
        }

    }
}
