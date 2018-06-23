using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.Commands;
using KannaBot.Scripts.Services;

namespace KannaBot.Scripts.Commands
{
    public class R34RandomImageCmd : ModuleBase
    {
        private readonly Rule34Service _service;

        private static readonly Dictionary<ulong, DateTime> usersOnCooldown = new Dictionary<ulong, DateTime>();
        const double waitTime = 3f;

        public R34RandomImageCmd(Rule34Service server)
        {
            _service = server;
        }

        [Command("r34")]
        [Summary("Gets a random Rule34 image.")]
        public async Task Get(string tag)
        {
            var guild = Program.GetGuild(Context.Guild.Id);
            if (!guild.ModuleR34) return;
            if (!Context.Channel.IsNsfw)
            {
                await ReplyAsync($"You must in a **NSFW** channel to use that command.");
                return;
            }
            if (usersOnCooldown.ContainsKey(Context.User.Id))
            {
                var time = Math.Round(Math.Abs(usersOnCooldown[Context.User.Id].Subtract(DateTime.Now).TotalSeconds), 2);
                if (time < waitTime)
                {
                    await ReplyAsync($"You must wait {waitTime - time} second(s) before using that command.");
                    return;
                }
                usersOnCooldown.Remove(Context.User.Id);
            }
            usersOnCooldown.Add(Context.User.Id, DateTime.Now);
            var result = await _service.GetRandomImage(Context.Guild.Id, tag);
            await Context.Channel.SendMessageAsync(string.Empty, embed: result);
        }
    }
}
