using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace KannaBot.Scripts.Commands
{
    public class AdminCmd : ModuleBase
    {
        [RequireUserPermission(GuildPermission.Administrator)]
        [Command("purge")]
        [Summary("Purge an amount of commands from a channel.")]
        public async Task Purge(int amount)
        {
            var guild = Program.GetGuild(Context.Guild.Id);
            if (!guild.ModulePurge) return;
            var msgs = await Context.Channel.GetMessagesAsync(amount).Flatten();
            foreach (IMessage msg in msgs)
            {
                await msg.DeleteAsync();
            }
            await Context.Channel.SendMessageAsync($"Deleted **{amount}** messages.");
        }

        [Command("config")]
        [Summary("Change a configuration variable.")]
        [RequireUserPermission(GuildPermission.BanMembers)]
        public async Task Config(string variable, [Remainder]string value)
        {
            var guild = Program.GetGuild(Context.Guild.Id);
            if (!guild.ModuleConfig) return;
            if (!guild.Config.ContainsKey(variable))
            {
                await ReplyAsync($"No variable named: *{variable}*");
                return;
            }
            value.CleanString();
            //Console.WriteLine("Value: " + value);
            if (string.IsNullOrEmpty(value)) return;
            if (variable == "prefix")
            {
                char c;
                var validChar = char.TryParse(value, out c);
                if (!validChar)
                {
                    await Context.Channel.SendMessageAsync("Prefix must be a char.");
                    return;
                }
            }
            object v = value;
            try
            {
                switch (value)
                {
                    case "true":
                        v = true;
                        break;
                    case "false":
                        v = false;
                        break;
                }

                guild.Config[variable] = v;
                guild.Save();
                await Context.Channel.SendMessageAsync($"Set {variable} to **{v.ToString()}**.");
            }
            catch(Exception e)
            {
                await Context.Channel.SendMessageAsync("Error: " + e.Message);
                return;
            }
        }

        [Command("musicChannel")]
        [Summary("Set the music channel.")]
        [RequireUserPermission(GuildPermission.BanMembers)]
        public async Task MusicChannel(ulong id)
        {
            var guild = Program.GetGuild(Context.Guild.Id);
            if (!guild.ModuleConfig) return;
            guild.MusicChannelId = id;
            await Context.Channel.SendMessageAsync($"Set the *Music Channel* to **{id}**.");
            guild.Save(); 
        }

        [Command("musicTextChannel")]
        [Summary("Set the music text channel.")]
        [RequireUserPermission(GuildPermission.BanMembers)]
        public async Task MusicTextChannel(ulong id)
        {
            var guild = Program.GetGuild(Context.Guild.Id);
            if (!guild.ModuleConfig) return;
            guild.MusicChannelTextId = id;
            await Context.Channel.SendMessageAsync($"Set the *Music Text Channel* to **{id}**.");
            guild.Save();
        }

        [Command("click")]
        [Summary("CLICK.")]
        [RequireUserPermission(GuildPermission.BanMembers)]
        public async Task React()
        {
            //if(Program.Presence != null)
            //Program.Presence.OnClick();
        }
    }
}
