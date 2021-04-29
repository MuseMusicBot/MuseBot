using Discord.Commands;
using MusicBot.Helpers;
using System;
using System.Linq;
using System.Threading.Tasks;

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
        public async Task HelpAsync([Remainder] string command = null)
        {
            // TODO: Get command by name
            var cmds = commands.Commands.OrderBy(x => x.Name);

            if (command == null)
            {
                var s = string.Join("\n", cmds.Select(x => $"`{x.Name}`{(x.Aliases.Count - 1 == 0 ? "" : " - [**" + string.Join(", ", x.Aliases.Skip(1)) + "**]")}: {x.Summary}"));
                var embed = await embedHelper.BuildMessageEmbed(s);
                await Context.Channel.SendAndRemove(embed: embed, timeout: 30000);
                return;
            }

            var selectedCmd = cmds.Where(x => x.Name.ToLower() == command || x.Aliases.Contains(command, StringComparer.OrdinalIgnoreCase))?.Select(x => $"`{x.Name}`{(x.Aliases.Count - 1 == 0 ? "" : " - [**" + string.Join(", ", x.Aliases.Skip(1)) + "**]")}: {x.Summary}").FirstOrDefault();

            if (selectedCmd != null)
            {
                var embed = await embedHelper.BuildMessageEmbed(selectedCmd);
                await Context.Channel.SendAndRemove(embed: embed, timeout: 15000);
                return;
            }

            var error = await embedHelper.BuildErrorEmbed("Invalid command", $"Command `{command}` is invalid.\nUse `{Program.BotConfig.Prefix}help` to see all commands");
            await Context.Channel.SendAndRemove(embed: error, timeout: 15000);
        }


    }
}
