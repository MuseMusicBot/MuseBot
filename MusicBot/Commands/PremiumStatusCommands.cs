﻿using Discord;
using Discord.Commands;
using System.Threading.Tasks;
using MusicBot.Helpers;

namespace MusicBot.Commands
{
    public class PremiumStatusCommands : ModuleBase<SocketCommandContext>
    {
        [Command("premiumstatus", RunMode = RunMode.Async)]
        [Alias("premium")]
        [Summary("Shows the server premium status.")]
        public async Task HelpAsync([Remainder] string commandOrModule = null)
        {
            var embed = new EmbedBuilder
            {
                Color = Discord.Color.DarkerGrey,
                Title = "Premium status",
                Description = "Your Subscription: `Free Forever`\n[Purchase](https://i.imgur.com/z4lM0Yj.gif) | [Manage](https://youtu.be/moZtoMP7HAA)",
                ImageUrl = "https://i.imgur.com/aq7yRAn.gif"
            }.Build();

            await Context.Channel.SendAndRemove(embed: embed, timeout:15000);
        }
        
    }
}
