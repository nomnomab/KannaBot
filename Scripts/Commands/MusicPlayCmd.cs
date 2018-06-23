using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using KannaBot.Scripts.Services;
using YoutubeExplode.Models;

namespace KannaBot.Scripts.Commands
{
    //[Group("music")]
    public class MusicPlayCmd : ModuleBase
    {
        private readonly AudioService _service;
        private const int QueuePageSize = 5;

        public MusicPlayCmd(AudioService service) => _service = service;

        private async Task NotInTextChannel()
        {
            
        }

        private async Task<bool> InVoiceChannel(GuildInfo guild)
        {
            var noVoiceChannel = (Context.User is IVoiceState state &&
                    (state.VoiceChannel == null || state.VoiceChannel.Id != guild.MusicChannelId));
            if (!noVoiceChannel) return true;
            await Context.Channel.SendMessageAsync("You must be in the Music Voice Channel.");
            return false;

        }

        private async Task JoinCmd(GuildInfo guild)
        {
            if (!(Context.User is IVoiceState state) || state.VoiceChannel == null)
            {
                await ReplyAsync("You must be in a voicechannel.");
                return;
            }
            if (state.VoiceChannel.Id != guild.MusicChannelId)
            {
                await ReplyAsync("You must be in the Music Channel.");
                return;
            }
            
            await guild.JoinAudio(Context.Guild, state.VoiceChannel);
        }

        [Command("stop", RunMode = RunMode.Async)]
        [Alias("s", "quit")]
        [Summary("Have the bot leave the connected voicechannel.")]
        [RequireUserPermission(GuildPermission.BanMembers)]
        public async Task LeaveCmd()
        {
            var guild = Program.GetGuild(Context.Guild.Id);
            if (!guild.ModuleMusic) return;
            if (Context.Channel.Id != guild.MusicChannelTextId) return;
            //if (!await InVoiceChannel(guild)) return;
            await guild.LeaveAudio(Context.Guild);
            //guild.Songs.Clear();
            guild.CurrentPlaying = null;
            guild.IsPlaying = false;
            guild.AudioChannel = null;
            guild.MessageChannel = null;
            await Context.Channel.SendMessageAsync("Playback ended.");
            Program.SetGame("cute music!");
        }

        [Command("play", RunMode = RunMode.Async)]
        [Alias("p", "request", "pl", "add")]
        [Summary("Play a song or search for one.")]
        public async Task PlayCmd([Remainder] string song)
        {
            var guild = Program.GetGuild(Context.Guild.Id);
            if ((bool) guild.Config["MusicQueueDisabled"])
            {
                await Context.Channel.SendMessageAsync("The Queue is currently not accepting new songs.");
                return;
            }
            if (!guild.ModuleMusic) return;
            if (Context.Channel.Id != guild.MusicChannelTextId) return;
            if (!await InVoiceChannel(guild))
            {
                Console.WriteLine("Not in voice channel");
                return;
            }
            await JoinCmd(guild);
            var state = (Context.User as IVoiceState);
            if (state != null && (state.VoiceChannel == null || state.VoiceChannel.Id != guild.MusicChannelId)) return;
            await _service.SendAudioAsync(Context.Guild, Context.Channel, Context.User, song);
        }
        
        [Command("resume", RunMode = RunMode.Async)]
        [Alias("r")]
        [Summary("Resume the playlist.")]
        public async Task ResumeCmd()
        {
            var guild = Program.GetGuild(Context.Guild.Id);
            if (!guild.ModuleMusic) return;
            if (Context.Channel.Id != guild.MusicChannelTextId) return;
            if (!await InVoiceChannel(guild)) return;
            if (guild.Songs.Count == 0)
            {
                await Context.Channel.SendMessageAsync("No songs in the queue.");
                return;
            }

            if (guild.AudioChannel == null || guild.AudioChannel.ConnectionState == ConnectionState.Disconnected) guild.IsPlaying = false;
            if (guild.IsPlaying)
            {
                await Context.Channel.SendMessageAsync("Already playing the queue.");
                return;
            }
            var state = (Context.User as IVoiceState);
            await JoinCmd(guild);
            if (state != null && (state.VoiceChannel == null || state.VoiceChannel.Id != guild.MusicChannelId)) return;
            await _service.ResumeQueue(Context.Guild, Context.User);
        }

        [Command("skip", RunMode = RunMode.Async)]
        [Summary("Skips the current song.")]
        public async Task SkipCmd()
        {
            var guild = Program.GetGuild(Context.Guild.Id);
            if (!guild.ModuleMusic) return;
            if (Context.Channel.Id != guild.MusicChannelTextId) return;
            if (!await InVoiceChannel(guild)) return;
            if (!guild.IsPlaying)
            {
                await Context.Channel.SendMessageAsync("*Nothing playing*");
                return;
            }

            if (guild.CurrentPlaying.AuthorId == Context.Message.Author.Id)
            {
                guild.UsersVoteSkipped.Clear();
                await Context.Channel.SendMessageAsync("Skipping song.");
                if(guild.Songs.Count == 0)
                {
                    await LeaveCmd();
                    return;
                }
                _service.SkipSong();
                return;
            }
            
            if (guild.UsersVoteSkipped.Contains(Context.User.Id))
            {
                await Context.Channel.SendMessageAsync(Context.User.Mention + ", you have already voted");
                return;
            }
            guild.UsersVoteSkipped.Add(Context.User.Id);
            if (Context.User is IVoiceState state)
            {
                var users = await state.VoiceChannel.GetUsersAsync(CacheMode.AllowDownload).Flatten();
                var currentNumber = users.Count() / 2 + 1;
                if(guild.UsersVoteSkipped.Count >= currentNumber)
                {
                    guild.UsersVoteSkipped.Clear();
                    await Context.Channel.SendMessageAsync("Skipping song.");
                    if(guild.Songs.Count == 0)
                    {
                        await LeaveCmd();
                        return;
                    }
                    _service.SkipSong();
                    return;
                }
                await Context.Channel.SendMessageAsync($"**Skip Requested** {currentNumber - guild.UsersVoteSkipped.Count} more votes needed.");
            }
        }

        [Command("remove", RunMode = RunMode.Async)]
        [Summary("Removes a song or songs from the queue.")]
        [RequireUserPermission(GuildPermission.BanMembers)]
        public async Task RemoveSongsCmd([Remainder]string numbers)
        {
            var guild = Program.GetGuild(Context.Guild.Id);
            if (!guild.ModuleMusic) return;
            if (Context.Channel.Id != guild.MusicChannelTextId) return;
            if (!guild.IsPlaying)
            {
                await Context.Channel.SendMessageAsync("*Nothing playing*");
                return;
            }

            if (guild.Songs.Count == 0)
            {
                await Context.Channel.SendMessageAsync("No songs in the queue.");
                return;
            }
            
            try
            {
                string[] split = numbers.Split(' ');
                int[] indexes = new int[split.Length];
                for (int i = 0; i < split.Length; i++)
                {
                    indexes[i] = int.Parse(split[i]);
                }
                var songs = guild.Songs.ToArray();
                var newSongs = songs.Where((t, i) => i != indexes[i]).ToList();
                guild.SetSongs(newSongs.ToArray());
                await Context.Channel.SendMessageAsync($"Removed {songs.Length - newSongs.Count} songs from the Queue.");
                return;
            }
            catch (Exception e)
            {
                throw e;
            }
        }
        
        [Command("removerange", RunMode = RunMode.Async)]
        [Summary("Removes songs from a min and max index from the queue.")]
        [RequireUserPermission(GuildPermission.BanMembers)]
        public async Task RemoveSongsCmd(int min, int max)
        {
            var guild = Program.GetGuild(Context.Guild.Id);
            if (!guild.ModuleMusic) return;
            if (Context.Channel.Id != guild.MusicChannelTextId) return;
            if (!guild.IsPlaying)
            {
                await Context.Channel.SendMessageAsync("*Nothing playing*");
                return;
            }

            if (guild.Songs.Count == 0)
            {
                await Context.Channel.SendMessageAsync("No songs in the queue.");
                return;
            }

            int counter = 0;

            try
            {
                var songs = guild.Songs.ToArray();
                var newSongs = songs.Where((t, i) => i < min || i > max).ToList();
                guild.SetSongs(newSongs.ToArray());
                await Context.Channel.SendMessageAsync($"Removed {(songs.Length - newSongs.Count)} songs from the Queue.");
                return;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        [Command("favorite", RunMode = RunMode.Async)]
        [Alias("f")]
        [Summary("Favorites the current song playing.")]
        public async Task FavoriteSongCmd()
        {
            var guild = Program.GetGuild(Context.Guild.Id);
            if (!guild.ModuleMusic) return;
            if (Context.Channel.Id != guild.MusicChannelTextId) return;
            if (!await InVoiceChannel(guild)) return;
            if (!guild.IsPlaying)
            {
                await Context.Channel.SendMessageAsync("*Nothing playing*");
                return;
            }

            var song = guild.FavoritedSongs.FirstOrDefault(s => s.Url == guild.CurrentPlaying.Video.GetUrl());
            if (song == null)
            {
                song = new SavedSong(guild.CurrentPlaying.Video.GetUrl(), guild.CurrentPlaying.Video.Title);
                song.Users.Add(Context.Message.Author.Id);
                guild.FavoritedSongs.Add(song);
                guild.Save();
                await Context.Channel.SendMessageAsync($"**{Context.Message.Author.Username}** added **{song.Title}** to the Favorites.");
                return;
            }
            else
            {
                if (song.Users.Contains(Context.Message.Author.Id))
                {
                    await Context.Channel.SendMessageAsync("You have already favorited this song.");
                    return;
                }
                song.Users.Add(Context.Message.Author.Id);
                await Context.Channel.SendMessageAsync($"**{Context.Message.Author.Username}** favorited **{song.Title}**.");
            }
        }

        [Command("favorites", RunMode = RunMode.Async)]
        [Alias("fs")]
        [Summary("Shows all of the favorites.")]
        public async Task FavoritesCmd()
        {
            var guild = Program.GetGuild(Context.Guild.Id);
            if (!guild.ModuleMusic) return;
            if (Context.Channel.Id != guild.MusicChannelTextId) return;

            var favorites = guild.FavoritedSongs.OrderByDescending(song=>song.Users.Count()).ToArray();
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < favorites.Length; i++)
            {
                sb.AppendLine($"[{i+1}] {favorites[i].Users.Count} - {favorites[i].Title}");
            }

            if (favorites.Length == 0) sb.AppendLine("No favorites.");

            await Context.Channel.SendMessageAsync($"```{sb.ToString()}```");
        }

        [Command("forceskip", RunMode = RunMode.Async)]
        [Alias("fs")]
        [Summary("Force skips the current song.")]
        [RequireUserPermission(GuildPermission.BanMembers)]
        public async Task ForceSkipCmd()
        {
            var guild = Program.GetGuild(Context.Guild.Id);
            if (!guild.ModuleMusic) return;
            if (Context.Channel.Id != guild.MusicChannelTextId) return;
            if (!await InVoiceChannel(guild)) return;
            if (!guild.IsPlaying)
            {
                await Context.Channel.SendMessageAsync("*Nothing playing*");
                return;
            }
            guild.UsersVoteSkipped.Clear();
            await Context.Channel.SendMessageAsync("Skipping song.");
            if (guild.Songs.Count == 0)
            {
                await LeaveCmd();
                return;
            }
            _service.SkipSong();
        }

        [Command("shuffle", RunMode = RunMode.Async)]
        [Summary("Shuffles the queue.")]
        public async Task ShuffleCmd()
        {
            var guild = Program.GetGuild(Context.Guild.Id);
            if (!guild.ModuleMusic) return;
            if (Context.Channel.Id != guild.MusicChannelTextId) return;
            if (!guild.IsPlaying)
            {
                await Context.Channel.SendMessageAsync("*Nothing playing*");
                return;
            }

            if (guild.Songs.Count == 0)
            {
                await Context.Channel.SendMessageAsync("No songs in the Queue.");
                return;
            }
            
            List<SavedSong[]> songs = new List<SavedSong[]>();
            
            
        }

        [Command("cancel", RunMode = RunMode.Async)]
        [Summary("Cancel the current operation.")]
        public async Task CancelCmd()
        {
            var guild = Program.GetGuild(Context.Guild.Id);
            if (!guild.ModuleMusic) return;
            if (Context.Channel.Id != guild.MusicChannelTextId) return;
            if (!await InVoiceChannel(guild)) return;
            var user = guild.GetWaitingUser(Context.User.Id);
            if (user != null) guild.WaitingUsers.Remove(user);
        }

        [Command("clear", RunMode = RunMode.Async)]
        [Summary("Clear the current queue.")]
        [RequireUserPermission(GuildPermission.BanMembers)]
        public async Task ClearCmd()
        {
            var guild = Program.GetGuild(Context.Guild.Id);
            if (!guild.ModuleMusic) return;
            if (Context.Channel.Id != guild.MusicChannelTextId) return;
            //if (!await InVoiceChannel(guild)) return;
            if (guild.Songs.Count == 0)
            {
                await Context.Channel.SendMessageAsync("Nothing in the Queue to clear.");
                return;
            }
            guild.Songs.Clear();
            await Context.Channel.SendMessageAsync("Cleared the Queue.");
        }

        [Command("queue", RunMode = RunMode.Async)]
        [Alias("q")]
        [Summary("Check the current queue of songs.")]
        public async Task Queue()
        {
            var guild = Program.GetGuild(Context.Guild.Id);
            if (!guild.ModuleMusic) return;
            if (Context.Channel.Id != guild.MusicChannelTextId) return;
            //if (!await InVoiceChannel(guild)) return;
            //Console.WriteLine(Context.Channel.Id + "," + guild.MusicChannelTextId);
            var embed = Embeds.GetQueue(GetQueue(guild));
            var msg = await Context.Channel.SendMessageAsync(string.Empty, embed: embed);
            await msg.AddReactionAsync(Program.Emojis["reverse"]);
            await msg.AddReactionAsync(Program.Emojis["play"]);
            var handlerReverse = new QueueEmojiHandler(Context.Guild.Id, Context.User.Id, msg.Id, Program.Emojis["reverse"]);
            var handlerPlay = new QueueEmojiHandler(Context.Guild.Id, Context.User.Id, msg.Id, Program.Emojis["play"]);

            async void Left()
            {
                handlerPlay.Back(QueuePageSize);
                embed = Embeds.GetQueue(GetQueue(guild, handlerPlay.CurrentQueueIndex));
                await msg.ModifyAsync(properties => { properties.Embed = embed; });
            }

            async void Right()
            {
                handlerPlay.Forward(QueuePageSize);
                embed = Embeds.GetQueue(GetQueue(guild, handlerPlay.CurrentQueueIndex));
                await msg.ModifyAsync(properties => { properties.Embed = embed; });
            }

            handlerReverse.Action = Left;
            handlerPlay.Action = Right;
            
            guild.EmojiHandlers.Add(handlerReverse);
            guild.EmojiHandlers.Add(handlerPlay);
        }

//        [Command("lyrics", RunMode = RunMode.Async)]
//        public async Task LyricsCmd()
//        {
//            // 5ZtRuxxoeNQs61TJC7yNG9B6e8GJ6yX_rE1KSGI5MPpYlYnxG3wQIM1ZYVy_JgsB
//            string url = "https://api.genius.com/oauth/authorize";
//            string clientId = "qEsjDxLiQ85KnujcjNWzfO0mqaiJ7qx9N9cWyD3RnPaINcCZUr2JZjk3Nj80cYl9";
//            string clientSecret =
//                "GW17loif8X8Ion2PmnjAKbwfUdb_lB7EitZqjyHB2TPy_hOjF6EeIEcLwSE4aC67rGHNVXURyNn4Za2glhMBsw";
//            HttpWebRequest req = WebRequest.CreateHttp("");
//        }

        public string GetQueue(GuildInfo guild, int start = 0)
        {
            var sb = new StringBuilder();
            if (guild.Songs.Count == 0) return "*No songs in queue.*";
            var infos = guild.Songs.ToArray();
            int end = start + QueuePageSize;
            if (end >= guild.Songs.Count) end = guild.Songs.Count;
            for (int i = start; i < end; i++)
                sb.AppendLine($"[{i+1}] {infos[i].Video.Title} - {infos[i].Video.Duration} - ({infos[i].AuthorUsername})");
            try
            {
                float totalSongs = guild.Songs.Count;
                float totalPages = (totalSongs / QueuePageSize) + 1;
                var page = Math.Floor((totalPages * (start / (totalPages * QueuePageSize))));
                page++;
                sb.AppendLine($"Page {page} out of {Math.Floor(totalPages)} ({totalSongs} songs in queue)");
            }
            catch (Exception e)
            {
                throw e;
            }

            return sb.ToString();
        }

        [Command("playing", RunMode = RunMode.Async)]
        [Alias("pg")]
        [Summary("Check the current playing song.")]
        public async Task Playing()
        {
            var guild = Program.GetGuild(Context.Guild.Id);
            if (!guild.ModuleMusic) return;
            if (Context.Channel.Id != guild.MusicChannelTextId) return;
            //if (!await InVoiceChannel(guild)) return;
            var video = guild.CurrentPlaying;
            if (video != null)
            {
                var embed = Embeds.GetNowPlaying(video, guild);
                await Context.Channel.SendMessageAsync(string.Empty, embed: embed);
            }
            else
            {
                var embed = Embeds.GetNowPlayingNull();
                await Context.Channel.SendMessageAsync(string.Empty, embed: embed);
            }
        }
    }
}
