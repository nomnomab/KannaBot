using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace KannaBot.Scripts
{
    public class NomDiscord
    {
        public bool IsRunning;
        public string DefaultConfig;
        public string DefaultGame;

        public readonly DiscordSocketClient Bot = 
            new DiscordSocketClient();
        public readonly CommandService Commands =
            new CommandService();
        public readonly Dictionary<ulong, GuildInfo> Guilds =
            new Dictionary<ulong, GuildInfo>();

        public IServiceProvider Services;

        private readonly string token;

        private NomDiscord()
        {
            IsRunning = false;
            DefaultConfig = "settings";
        }
        
        public async void Start(string config, object[] services)
        {
            // get settings
            SettingsParser.ReloadJson(config + ".json");
            // get token
            var token = SettingsParser.GetField("token");
            // services
            ServiceCollection collection = new ServiceCollection();
            foreach (object o in services)
                collection.AddSingleton(o);
            Services = collection.BuildServiceProvider();
            // events
            Bot.Log += BotOnLog;
            Bot.Ready += BotOnReady;
            Bot.MessageReceived += BotOnMessageReceived;
            // setup
            await InstallCommands();
            // start
            await Bot.LoginAsync(TokenType.Bot, token);
            await Bot.StartAsync();
            // stop bot
            await Task.Delay(-1);
        }

        private async Task InstallCommands() => await Commands.AddModulesAsync(Assembly.GetEntryAssembly());

        private async Task BotOnMessageReceived(SocketMessage socketMessage)
        {
            var info = GetGuild(socketMessage);
            if (!(socketMessage is SocketUserMessage message)) return;
            if (socketMessage.Channel.Id != info.MusicChannelTextId) return;
            var argPos = 0;
            if (!(message.HasCharPrefix(info.Prefix, ref argPos) ||
                  message.HasMentionPrefix(Bot.CurrentUser, ref argPos))) return;
            var context = new CommandContext(Bot, message);
            var result = await Commands.ExecuteAsync(context, argPos, Services);
            if (!result.IsSuccess)
                await context.Channel.SendMessageAsync(result.ErrorReason);
        }

        private Task BotOnLog(LogMessage logMessage)
        {
            //Console.WriteLine($"[{logMessage.Severity}] {logMessage.Message}");
            SetGame(DefaultGame);
            return Task.CompletedTask;
        }

        private Task BotOnReady()
        {
            foreach(IGuild guild in Bot.Guilds)
                Guilds.Add(guild.Id, new GuildInfo().Load(guild.Id));
            SetGame(DefaultGame);
            return Task.CompletedTask;
        }

        public bool HasGuild(IGuild guild) => Guilds.ContainsKey(guild.Id);
        public bool HasGuild(SocketMessage message) => HasGuild(message.Channel);
        public bool HasGuild(ISocketMessageChannel channel) => HasGuild(channel as ITextChannel);
        public bool HasGuild(ITextChannel channel) => Guilds.ContainsKey(channel.GuildId);
        public GuildInfo GetGuild(IGuild guild) => Guilds[guild.Id];
        public GuildInfo GetGuild(SocketMessage message) => GetGuild(message.Channel);
        public GuildInfo GetGuild(ISocketMessageChannel channel) => GetGuild(channel as ITextChannel);
        public GuildInfo GetGuild(ITextChannel channel) => Guilds[channel.GuildId];
        
        public async void SetGame(string game) => await Bot.SetGameAsync(game);
    }
}