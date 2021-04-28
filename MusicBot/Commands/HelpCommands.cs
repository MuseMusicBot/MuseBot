using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Victoria;
using Victoria.Enums;
using MusicBot.Helpers;

namespace MusicBot.Commands
{
    public class HelpCommands : ModuleBase<SocketCommandContext>
    {
        private readonly CommandService commands;
        private readonly EmbedHelper embedHelper;
        public HelpCommands(CommandService commandService, EmbedHelper eh)
        {
            commands = commandService;
            embedHelper = eh;
        }

        [Command("help", RunMode = RunMode.Async)]
        [Summary("Lists all the commands")]
        public async Task HelpAsync([Remainder] string commandOrModule = null)
        {
            // TODO: Get command by name
            var cmds = commands.Commands.OrderBy(x => x.Name);

            var s = string.Join("\n", cmds.Select(x => $"`{x.Name}` - [**{string.Join(", ", x.Aliases)}**]: {x.Summary}"));
            var embed = await embedHelper.BuildMessageEmbed(s);
            await Context.Channel.SendAndRemove(embed: embed, timeout: 30000);
        }


    }
}
