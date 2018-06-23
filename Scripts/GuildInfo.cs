using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using Newtonsoft.Json;
using YoutubeExplode.Models;

namespace KannaBot.Scripts
{
    [System.Serializable]
    public class GuildInfo
    {
        public ulong Id;
        public char Prefix => ((string)Config["prefix"])[0];
        public readonly Dictionary<string, object> Config = new Dictionary<string, object>()
        {
            { "prefix", "."  },
            { "debug", false },
            { "r34_img_per_page", 42 },
            { "r34_page_limit", 100 },
            { "ModuleConfig", true },
            { "ModuleHelp", true },
            { "ModuleInvite", false },
            { "ModuleMusic", true },
            { "ModulePurge", false },
            { "ModuleR34", false },
            { "MusicQueueDisabled", false },
            { "MaxSongsPerUser" , 3 }
        };
        public bool ModuleConfig => (bool)Config["ModuleConfig"];
        public bool ModuleHelp => (bool)Config["ModuleHelp"];
        public bool ModuleInvite => (bool)Config["ModuleInvite"];
        public bool ModuleMusic => (bool)Config["ModuleMusic"];
        public bool ModulePurge => (bool)Config["ModulePurge"];
        public bool ModuleR34 => (bool)Config["ModuleR34"];
        public ulong MusicChannelId, MusicChannelTextId;
        
        [NonSerialized]
        public IAudioClient AudioChannel;
        [NonSerialized]
        public IMessageChannel MessageChannel;
        [NonSerialized]
        public VideoItem CurrentPlaying;
        private TimeSpan currentDuration => DateTime.Now.Subtract(SongStartedAt);
        [NonSerialized]
        public DateTime SongStartedAt;
        [NonSerialized]
        public bool IsPlaying;
        [NonSerialized]
        public readonly QueueCollection<VideoItem> Songs = new QueueCollection<VideoItem>();
        [NonSerialized]
        //public readonly Dictionary<ulong, SearchVideoItem[]> UserWaits = new Dictionary<ulong, SearchVideoItem[]>();
        public readonly List<WaitingUser> WaitingUsers = new List<WaitingUser>();
        [NonSerialized]
        public readonly List<ulong> UsersVoteSkipped = new List<ulong>();
        public readonly List<ulong> UsersVoteShuffle = new List<ulong>();
        public readonly List<SavedSong> FavoritedSongs = new List<SavedSong>();
        public readonly List<EmojiHandler> EmojiHandlers = new List<EmojiHandler>();
        
        public string CurrentDuration
        {
            get
            {
                // currentDuration.Hours + ":" + currentDuration.Minutes + ":" + currentDuration.Seconds
                var hours = currentDuration.Hours.ToString();
                var minutes = currentDuration.Minutes.ToString();
                var seconds = currentDuration.Seconds.ToString();
                return ParseDateTime(hours, minutes, seconds);
            }
        }

        public string SongDuration
        {
            get
            {
                // CurrentPlaying.Video.Duration.Hours + ":" + CurrentPlaying.Video.Duration.Minutes + ":" + CurrentPlaying.Video.Duration.Seconds;
                var hours = CurrentPlaying.Video.Duration.Hours.ToString();
                var minutes = CurrentPlaying.Video.Duration.Minutes.ToString();
                var seconds = CurrentPlaying.Video.Duration.Seconds.ToString();
                return ParseDateTime(hours, minutes, seconds);
            }
        }

        public GuildInfo()
        {
            Songs.OnChanged += OnSongsChanged;
        }

        private GuildInfo(GuildInfo info)
        {
            Id = info.Id;
            Config = new Dictionary<string, object>(info.Config);
            MusicChannelId = info.MusicChannelId;
            MusicChannelTextId = info.MusicChannelTextId;
            FavoritedSongs = info.FavoritedSongs;
            Songs = info.Songs;
            Console.WriteLine(info.Id + ", " + Prefix);
        }

        private void OnSongsChanged()
        {
            //if(AudioChannel != null) Save();
        }

        public void SetSongs(params VideoItem[] items)
        {
            Songs.Clear();
            foreach(VideoItem item in items) Songs.Enqueue(item);
        }
        
        public GuildInfo Save()
        {
            if (!Directory.Exists(Environment.CurrentDirectory + "/Saves/")) Directory.CreateDirectory(Environment.CurrentDirectory + "/Saves/");
            var path = Environment.CurrentDirectory + "/Saves/" + Id + ".save";
            var output = JsonConvert.SerializeObject(this, Formatting.Indented, new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            });
            File.WriteAllText(path, output);
            return this;
        }

        public GuildInfo Load(ulong id)
        {
            if (!Directory.Exists(Environment.CurrentDirectory + "/Saves/")) Directory.CreateDirectory(Environment.CurrentDirectory + "/Saves/");
            var path = Environment.CurrentDirectory + "/Saves/" + id + ".save";
            if (!File.Exists(path)) return new GuildInfo { Id = id }.Save();
            var json = File.ReadAllText(path);
            return new GuildInfo(JsonConvert.DeserializeObject<GuildInfo>(json));
        }

        public WaitingUser GetWaitingUser(ulong id) => WaitingUsers.FirstOrDefault(user => user.Id == id);
        
        private static string ParseDateTime(string hours, string minutes, string seconds)
        {
            hours = hours.Length == 1 ? $"0{hours}" : hours;
            minutes = minutes.Length == 1 ? $"0{minutes}" : minutes;
            seconds = seconds.Length == 1 ? $"0{seconds}" : seconds;
            return $"{hours}:{minutes}:{seconds}";
        }
        
        public async Task JoinAudio(IGuild guild, IVoiceChannel target)
        {
            var g = Program.GetGuild(guild);
            if (g.AudioChannel != null || target.Guild.Id != guild.Id) return;
            if (g.AudioChannel != null && g.AudioChannel.ConnectionState == ConnectionState.Connected) return;

            IAudioClient audioClient = null;

            try{ audioClient = await target.ConnectAsync(); }
            catch (Exception e) { return; }
            finally{ /*Console.WriteLine("Connected to audio channel.");*/ }

            g.AudioChannel = audioClient;
        }

        public async Task LeaveAudio(IGuild guild)
        {
            var g = Program.GetGuild(guild);
            await g.AudioChannel.StopAsync();
            CurrentPlaying = null;
            g.AudioChannel = null;
            g.MessageChannel = null;
        }
    }

    [System.Serializable]
    public class VideoItem
    {
        public readonly Video Video;
        public readonly ulong AuthorId;
        public readonly string AuthorUsername;

        public VideoItem(Video video, IUser user)
        {
            Video = video;
            AuthorId = user.Id;
            AuthorUsername = user.Username;
        }
    }

    
    [System.Serializable]
    public struct SearchVideoItem
    {
        public readonly string Url;
        public string Id;
        public string Title;
        public string Duration;

        public SearchVideoItem(
            string url,
            string id,
            string title,
            string duration)
        {
            Url = url;
            Id = id;
            Title = title;
            Duration = duration;
        }
    }

    public class WaitingUser
    {
        public ulong GuildId;
        public ulong MessageId;
        
        public ulong Id;
        public Dictionary<IEmote, Action> Reactions = new Dictionary<IEmote, Action>();
        
        public enum TypeOfWait
        {
            SearchingSong
        }

        public TypeOfWait Type;
        public object Data;

        public WaitingUser(ulong id, ulong guildId, ulong messageId, TypeOfWait type, object data)
        {
            Id = id;
            GuildId = guildId;
            MessageId = messageId;
            Type = type;
            Data = data;
        }
    }

    public class EmojiHandler
    {
        public IEmote Emoji;
        public Action Action;
        public ulong GuildId;
        public ulong Owner;
        public ulong MsgId;

        public EmojiHandler(ulong guildId, ulong owner, ulong msg, IEmote emote, Action action = null)
        {
            Emoji = emote;
            Action = action;
            GuildId = guildId;
            Owner = owner;
            MsgId = msg;
        }
    }

    public class QueueEmojiHandler : EmojiHandler
    {
        public int CurrentQueueIndex;
        
        public QueueEmojiHandler(ulong guildId, ulong owner, ulong msg, IEmote emote, Action action = null) : base(guildId, owner, msg, emote, action)
        {
        }

        public void Back(int amount)
        {
            CurrentQueueIndex -= amount;
            if (CurrentQueueIndex < 0) CurrentQueueIndex = 0;
        }
        
        public void Forward(int amount)
        {
            if (CurrentQueueIndex + amount >= Program.GetGuild(GuildId).Songs.Count) return;
            CurrentQueueIndex += amount;
            //Console.WriteLine("Forward: " + CurrentQueueIndex);
        }
    }

    [System.Serializable]
    public class SavedSong
    {
        public string Url;
        public string Title;
        public List<ulong> Users = new List<ulong>();
        
        public SavedSong(string url, string title)
        {
            Url = url;
            Title = title;
        }
    }

    public struct GuildVideo
    {
        public Video Video;
        public ulong Guild;
    }

    public class VideoItemSavable
    {
        
    }
}
