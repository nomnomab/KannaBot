using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using KannaBot.Scripts.Services;
using Microsoft.Extensions.DependencyInjection;

namespace KannaBot.Scripts
{
    class Program
    {
        /// <summary>
        /// All guilds on the bot
        /// </summary>
        private static readonly Dictionary<ulong, GuildInfo> Guilds = new Dictionary<ulong, GuildInfo>();

        // Client Variables
        public static DiscordSocketClient Client = new DiscordSocketClient();
        public static readonly CommandService Commands = new CommandService();
        private IServiceProvider _services;
        //public static DiscordPresence Presence;
        
        // Emojis
        public readonly static Dictionary<string, Emoji> Emojis = new Dictionary<string, Emoji>();

        // Services Variables
        private readonly AudioService _audioService = new AudioService();

        public static void Main(string[] args) => new Program().MainAsync(args).GetAwaiter().GetResult();
        
        public static bool HasGuild(ulong id) => Guilds.ContainsKey(id);
        public  static bool HasGuild(IGuild guild) => Guilds.ContainsKey(guild.Id);
        public static  bool HasGuild(SocketMessage message) => HasGuild(message.Channel);
        public  static bool HasGuild(ISocketMessageChannel channel) => HasGuild(channel as ITextChannel);
        public static  bool HasGuild(IMessageChannel channel) => HasGuild(channel as ITextChannel);
        public static  bool HasGuild(ITextChannel channel) => Guilds.ContainsKey(channel.GuildId);
        public  static GuildInfo GetGuild(ulong id) => Guilds[id];
        public static  GuildInfo GetGuild(IGuild guild) => Guilds[guild.Id];
        public  static GuildInfo GetGuild(SocketMessage message) => GetGuild(message.Channel);
        public static  GuildInfo GetGuild(ISocketMessageChannel channel) => GetGuild(channel as ITextChannel);
        public  static GuildInfo GetGuild(IMessageChannel channel) => GetGuild(channel as ITextChannel);
        public  static GuildInfo GetGuild(ITextChannel channel) => Guilds[channel.GuildId];

        private async Task MainAsync(string[] args)
        {   
            Client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Info
            });
            
            // emojis
            Emojis.Add("reverse", new Emoji("\U000025C0"));
            Emojis.Add("play", new Emoji("\U000025B6"));
            Emojis.Add("1", new Emoji("\U00000031\U000020E3"));
            Emojis.Add("2", new Emoji("\U00000032\U000020E3"));
            Emojis.Add("3", new Emoji("\U00000033\U000020E3"));
            Emojis.Add("4", new Emoji("\U00000034\U000020E3"));
            Emojis.Add("5", new Emoji("\U00000035\U000020E3"));
            Emojis.Add("6", new Emoji("\U00000036\U000020E3"));
            
            // settings
            SettingsParser.ReloadJson();
            var token = SettingsParser.GetField("token");
            // create services
            _services = new ServiceCollection()
                .AddSingleton<AudioService>()
                .AddSingleton(_audioService)
                .BuildServiceProvider();

            // events
            Client.Ready += Client_Ready;
            Client.Log += Log;
            Client.ReactionAdded += Client_Reaction_Added;
            
            await InstallCommands();
            await Client.LoginAsync(Discord.TokenType.Bot, token);
            await Client.StartAsync();

            // dont let the bot suddenly stop
            await Task.Delay(-1);
        }

        private async Task Client_Reaction_Added(Cacheable<IUserMessage, ulong> arg1, ISocketMessageChannel arg2, SocketReaction arg3)
        {
            // check for users
            if (arg3.UserId == Client.CurrentUser.Id) return;
            var guild = GetGuild(arg2);
            if (arg2.Id != guild.MusicChannelTextId) return;
            var waitingUser = guild.WaitingUsers.FirstOrDefault(user => user.Id == arg3.UserId);
            if (waitingUser != null)
            {
                var emote = arg3.Emote;
                if (!waitingUser.Reactions.ContainsKey(emote))
                {
                    return;
                }

                var action = waitingUser.Reactions[arg3.Emote];
                action.Invoke();
                return;
            }
            // check for emoji handlers
            var emojiHandler = guild.EmojiHandlers.FirstOrDefault(h => arg3.Emote.Name == h.Emoji.Name && arg1.Id == h.MsgId);
            emojiHandler?.Action();
        }

        private async Task InstallCommands()
        {
            Client.MessageReceived += HandleCommand;
            await Commands.AddModulesAsync(Assembly.GetEntryAssembly());
        }

        #region Events
        private static async Task Client_Ready()
        {
            foreach(IGuild guild in Client.Guilds) Guilds.Add(guild.Id, new GuildInfo().Load(guild.Id));
            //Presence = new DiscordPresence("427910168315953172");
            SetGame("cute music!");     
        }

        public static async void Log(string msg, LogSeverity severity = LogSeverity.Info)
        {
            //StackTrace stack = new StackTrace(new StackFrame(true));
            await Log(new LogMessage(severity, "Class", msg));
        }

        private static Task Log(LogMessage message)
        {
            Console.WriteLine(message.ToString());
            return Task.CompletedTask;
        }

        private async Task HandleCommand(SocketMessage messageParam)
        {
            var info = GetGuild(messageParam.Channel);
            if (!(messageParam is SocketUserMessage message)) return;

            if (messageParam.Channel.Id != info.MusicChannelTextId)
            {
                if (info.MusicChannelTextId == 0 || Client.GetChannel(info.MusicChannelTextId) == null)
                {
                }
                else return;
            }

            var waitingUser = info.GetWaitingUser(messageParam.Author.Id);
            if (waitingUser != null)
            {
                if (messageParam.Content != info.Prefix + "cancel")
                {
                    int i;
                    bool b = int.TryParse(messageParam.Content, out i);
                    if (!b)
                    {
                        await messageParam.Channel.SendMessageAsync(messageParam.Content + " is not a valid number.");
                        return;
                    }

                    i--;
                    if (i < 0 || i > 6)
                    {
                        await messageParam.Channel.SendMessageAsync("Enter a value from 1 to 6");
                        return;
                    }

                    await _audioService.PlaySearchSong(((IGuildChannel) messageParam.Channel).Guild,
                        messageParam.Channel, messageParam.Author, i);
                    info.WaitingUsers.Remove(waitingUser);
                    return;
                }
                else
                {
                    await messageParam.Channel.SendMessageAsync(
                        messageParam.Author.Username + " canceled the song selection.");
                    info.WaitingUsers.Remove(waitingUser);
                }

                return;
            }

            if (message.Author.Id == Client.CurrentUser.Id) return;
            
            var argPos = 0;
            if (!(message.HasCharPrefix(info.Prefix, ref argPos) || message.HasMentionPrefix(Client.CurrentUser, ref argPos))) return;
            var context = new CommandContext(Client, message);
            var result = await Commands.ExecuteAsync(context, argPos, _services);
            if (!result.IsSuccess)
                await context.Channel.SendMessageAsync(result.ErrorReason);
        }
        
        #endregion

        public static async void SetGame(string game)
        {
            await Client.SetGameAsync(game);
        }
    }
}
