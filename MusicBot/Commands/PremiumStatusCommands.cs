using Discord;
using Discord.Commands;
using System.Threading.Tasks;

namespace MusicBot.Commands
{
    public class PremiumStatusCommands : ModuleBase<SocketCommandContext>
    {
        [Command("premiumstatus", RunMode = RunMode.Async)]
        public async Task HelpAsync([Remainder] string commandOrModule = null)
        {
            var embed = new EmbedBuilder
            {
                Color = Discord.Color.DarkerGrey,
                Title = "Premium status",
                Description = "Your Subscription: `Free Forever`\n[Purchase](https://i.imgur.com/z4lM0Yj.gif) | [Manage](https://youtu.be/moZtoMP7HAA)",
                ImageUrl = "https://i.imgur.com/aq7yRAn.gif"
            }.Build();

            await Context.Channel.SendMessageAsync(embed: embed);
        }
        
    }
}
