using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace KannaBot.Scripts.Commands
{
    public class HelpCmd : ModuleBase
    {
        [Command("help")]
        [Summary("Shows all available commands for the user.")]
        public async Task Help()
        {
            var user = (IGuildUser)Context.User;
            var guild = Program.GetGuild(Context.Guild.Id);
            if (!guild.ModuleHelp) return;

            var modules = Program.Commands.Modules.OrderByDescending(x=>x.Name.EndsWith("Cmd")).ToArray();

            var sb = new StringBuilder();
            var listedAlready = new List<string>();
            sb.AppendLine("KannaBot 1.0 - Help\n");
            foreach (ModuleInfo module in modules)
            {
                if (module.Commands.Count == 0) continue;
                var isGroup = !module.Name.EndsWith("Cmd");
                // check modules
                switch (module.Name)
                {
                    case "HelpCmd":
                        if (!guild.ModuleHelp) continue;
                        break;
                    case "music":
                        if (!guild.ModuleMusic) continue;
                        break;
                    case "R34RandomImageCmd":
                        if (!guild.ModuleR34) continue;
                        break;
                    case "AdminCmd":
                        continue;
                }

                sb.AppendLine("");
                if (isGroup) sb.AppendLine(guild.Prefix + module.Name);
                foreach (CommandInfo cmd in module.Commands)
                {
                    var parameters = cmd.Parameters;
                    var line = "";
                    var spacerComplete = "";
                    for (int i = 0; i < 15; i++) spacerComplete += ' ';
                    var spacer = "";
                    var length = cmd.Name.Length + 1;
                    for (int i = 0; i < 15 - length - (isGroup ? 1 : 0); i++) spacer += ' ';
                    sb.AppendLine(
                    $"{(isGroup ? "  " : guild.Prefix.ToString())}{cmd.Name}" +
                    $"{spacer}" +
                    $"{cmd.Summary}");
                    if (parameters != null && parameters.Count > 0) sb.AppendLine($"{spacerComplete}--[Arguments] {cmd.Parameters.ToArray().ToArrayString(", ")}");
                    listedAlready.Add(module.Name);
                }
            }
            var msg = sb.ToString();
            await Context.Channel.SendMessageAsync($"```\n{msg}\n```");
        }
    }
}
