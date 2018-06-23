using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using YoutubeExplode;
using YoutubeExplode.Models;
using YoutubeSearch;

namespace KannaBot.Scripts.Services
{
    public class AudioService
    {
        private readonly int _maxMinutesForVideo = 12;
    
        private CancellationTokenSource _cancelToken = new CancellationTokenSource();

        public void SkipSong()
        {
            _cancelToken.Cancel();
            _cancelToken.Dispose();
        }

        public async Task PlaySearchSong(IGuild guildObj, IMessageChannel channel, IUser user, int number)
        {
            var guild = Program.GetGuild(guildObj.Id);
            var waitingUser = guild.GetWaitingUser(user.Id);
            var obj = waitingUser.Data as SearchVideoItem[];
            var path = obj[number].Url;
            await SendAudioAsync(guildObj, channel, user, path);
        }

        public async Task ResumeQueue(IGuild guildObj, IUser user)
        {
            var state = (user as IVoiceState);
            await PlayNext(guildObj.Id, state);
        }

        public async Task SendAudioAsync(IGuild guildObj, IMessageChannel channel, IUser user, string path)
        {
            var g = Program.GetGuild(guildObj);
            if (g.MessageChannel == null) g.MessageChannel = channel;
            var guild = Program.GetGuild(guildObj.Id);

            var youtube = new YoutubeClient();
            bool playlist = false;
            var id = string.Empty;
            try
            {
                if (path.Contains("playlist?list") || path.Contains("&list"))
                {
                    //Console.WriteLine("playlist gotten");
                    id = YoutubeClient.ParsePlaylistId(path);
                    playlist = true;
                }
                else id = YoutubeClient.ParseVideoId(path);
            }
            catch (Exception e)
            {
                // invalid id
                // find videos
                var items = new VideoSearch();
                var videos = new List<SearchVideoItem>();
                var sb = new StringBuilder();
                var index = 0;
                foreach (var item in items.SearchQuery(path, 1))
                {
                    if (index > 5) break;
                    var itemId = YoutubeClient.ParseVideoId(item.Url);
                    videos.Add(new SearchVideoItem(
                        item.Url,
                        itemId,
                        item.Title,
                        item.Duration));
                    sb.AppendLine($"{Program.Emojis[(index+1).ToString()].Name} {item.Title} ({item.Duration})");
                    index++;
                }

                var embed = Embeds.GetRetrievedSongs(sb.ToString(), guild.Prefix);
                var msg = await g.MessageChannel.SendMessageAsync(string.Empty, embed: embed);
                WaitingUser u = new WaitingUser(user.Id, guild.Id, msg.Id, WaitingUser.TypeOfWait.SearchingSong, videos.ToArray());
                guild.WaitingUsers.Add(u);
                return;
            }

            if (playlist) await ParsePlaylist(guild, channel, user, path, youtube, id);
            else await ParseVideo(guild, channel, user, path, youtube, id);
            ;    }

        private async Task ParsePlaylist(GuildInfo guild, IMessageChannel channel, IUser user, string path, YoutubeClient youtube, string id)
        {
            var playlist = await youtube.GetPlaylistAsync(id);
            int added = 0;
            int couldntAdd = 0;
            var maxSongs = int.Parse(guild.Config["MaxSongsPerUser"].ToString());
            foreach (Video video in playlist.Videos)
            {
                var songs = guild.Songs;
                if (video.Duration.TotalMinutes > _maxMinutesForVideo || (int)video.Duration.TotalMinutes == 0 ||
                    (!((IGuildUser) user).GuildPermissions.BanMembers && songs.ToArray().Count(x => x.AuthorId == user.Id) == maxSongs))
                {
                    couldntAdd++;
                    continue;
                }
            
                AddVideo(guild, channel, user, video, false);
                added++;
            }

            string message = "";
            message += $"Added {added} video(s) to the Queue.";
            if(couldntAdd > 0)
                message += $"\nCould not add {couldntAdd} video(s) to the Queue.";

            await channel.SendMessageAsync(message);
        }

        private async Task ParseVideo(GuildInfo guild, IMessageChannel channel, IUser user, string path, YoutubeClient youtube, string id)
        {
            try
            {
                var video = await youtube.GetVideoAsync(id);
                AddVideo(guild, channel, user, video);
            }
            catch (YoutubeExplode.Exceptions.VideoUnavailableException uE)
            {
                await channel.SendMessageAsync(uE.Reason);
            }
        }

        private async void AddVideo(GuildInfo guild, IMessageChannel channel, IUser user, Video video, bool showAddMsg = true)
        {
            try
            {
                if (!((IGuildUser) user).GuildPermissions.BanMembers && video.Duration.TotalMinutes > _maxMinutesForVideo)
                {
                    await channel.SendMessageAsync($"Video is longer than {_maxMinutesForVideo} minutes");
                    return;
                }

                if ((int)video.Duration.TotalSeconds == 0)
                {
                    await channel.SendMessageAsync("Live streams are not supported.");
                    return;
                }

                var songs = guild.Songs;
                var maxSongs = int.Parse(guild.Config["MaxSongsPerUser"].ToString());
                //Console.WriteLine("Songs so far in queue from user: " + (songs.ToArray().Count(x => x.AuthorId == user.Id)));
                if (!((IGuildUser) user).GuildPermissions.BanMembers && songs.ToArray().Count(x => x.AuthorId == user.Id) == maxSongs)
                {
                    await guild.MessageChannel.SendMessageAsync($"Cannot add song, you already have {maxSongs} songs in the queue.");
                    return;
                }
                songs.Enqueue(new VideoItem(video, user));
                if (showAddMsg)
                    await guild.MessageChannel.SendMessageAsync(
                        $"**{video.Title}** added to the queue (Queue Count: {songs.Count}).");

                IVoiceState state = (user as IVoiceState);
                if (!guild.IsPlaying || songs.Count == 0) await PlayNext(guild.Id, state);
            }
            catch (Exception e)
            {
                //Console.WriteLine(e.Message);
                throw e;
            }
        }

        private async Task PlayNext(ulong id, IVoiceState state)
        {
            var guild = Program.GetGuild(id);
            var songs = guild.Songs;
            if (guild.IsPlaying || songs.Count == 0) return;
            //guild.Save();
            guild.IsPlaying = true;
            var video = songs.Dequeue();
            Program.GetGuild(id).UsersVoteSkipped.Clear();
        
            await guild.JoinAudio(Program.Client.GetGuild(id), state.VoiceChannel);

            _cancelToken = new CancellationTokenSource();
            _cancelToken.Token.ThrowIfCancellationRequested();

            guild.CurrentPlaying = video;
            Program.SetGame(video.Video.Title);

            var youtube = new YoutubeClient();
            var streamObj = await youtube.GetVideoMediaStreamInfosAsync(video.Video.Id);
            var streamurl = streamObj.Audio.First().Url;
            AudioOutStream lastStream = null;
            if (guild.AudioChannel != null)
            {
                try
                {
                    var embed = Embeds.GetNowPlaying(video, guild, true);
                    await guild.MessageChannel.SendMessageAsync(string.Empty, embed: embed);
                    using (var ffmpeg = CreateStream(streamurl))
                    using (var stream = guild.AudioChannel.CreatePCMStream(AudioApplication.Music, 48000))
                    {
                        try
                        {
                            lastStream = stream;
                            guild.SongStartedAt = DateTime.Now;
                            Program.Log($"Now Playing: {video.Video.Title} - {video.Video.Duration.ToString()} - [{video.AuthorUsername}]");
                            await ffmpeg.StandardOutput.BaseStream.CopyToAsync(stream, 81920, _cancelToken.Token);
                        }
                        catch (AggregateException e)
                        {
                        }
                        catch (OperationCanceledException e)
                        {
                        }
                        catch (Exception e)
                        {
                            throw e;
                        }
                        finally
                        {
                            CleanUpBeforeNext(stream, id, state);
                        }
                    }
                }
                catch (YoutubeExplode.Exceptions.VideoUnavailableException e)
                {
                    Program.GetGuild(id).IsPlaying = false;
                    CleanUpBeforeNext(lastStream, id, state);
                    return;
                }
                catch (Exception e)
                {
                    throw e;
                }
            }
        }

        private async void CleanUpBeforeNext(Stream stream, ulong id, IVoiceState state)
        {
            await stream.FlushAsync();
            var guild = Program.GetGuild(id);
            guild.IsPlaying = false;
            guild.CurrentPlaying = null;
            _cancelToken.Dispose();
            var songs = Program.GetGuild(id).Songs;
            if (songs.Count > 0)
            {
                await PlayNext(id, state);
                return;
            }
            else
            {
                Program.SetGame("cute music!");
                await guild.LeaveAudio(Program.Client.Guilds.First(x=>x.Id == id) as IGuild);
            }
        }

        private Process CreateStream(string path)
        {
            // -ss 00:01:00 -to 00:02:00
            // ffmpeg -i movie.mp4 -ss 00:00:03 -t 00:00:08 -async 1 -c copy cut.mp4 
            return Process.Start(new ProcessStartInfo
            {
                FileName = "ffmpeg.exe",
                Arguments = $"-hide_banner -loglevel panic -i \"{path}\"  -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true
            });
        }
    }
}