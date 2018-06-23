using Discord;
using KannaBot.Scripts.Services;
using YoutubeExplode.Models;

namespace KannaBot.Scripts
{
    public class Embeds
    {
        public static Embed GetNowPlaying(VideoItem video, GuildInfo guild, bool defaultDuration = false)
        {
            var builder = new EmbedBuilder()
                .WithTitle("Now Playing")
                .WithUrl(video.Video.GetUrl())
                .WithColor(new Color(0xBB4FFF))
                .WithThumbnailUrl(video.Video.Thumbnails.HighResUrl)
//                .WithAuthor(author =>
//                {
//                    author
//                        .WithName(Program.Client.CurrentUser.Username)
//                        .WithIconUrl(Program.Client.CurrentUser.GetAvatarUrl());
//                })
                .AddField("Title", video.Video.Title)
                .AddField("Duration", !defaultDuration ? (guild.CurrentDuration + "/" + guild.SongDuration) : guild.SongDuration)
                .AddField("Requested By", video.AuthorUsername);
            return builder.Build();
        }
        
        public static Embed GetNowPlayingNull()
        {
            var builder = new EmbedBuilder()
                .WithTitle("Now Playing")
                .WithColor(new Color(0xBB4FFF))
//                .WithAuthor(author =>
//                {
//                    author
//                        .WithName(Program.Client.CurrentUser.Username)
//                        .WithIconUrl(Program.Client.CurrentUser.GetAvatarUrl());
//                })
                .WithDescription("*Nothing playing.*");
            return builder.Build();
        }

        public static Embed GetRetrievedSongs(string description, char prefix)
        {
            var builder = new EmbedBuilder()
                .WithTitle("Retrieved Songs")
                .WithColor(new Color(0xBB4FFF))
//                .WithAuthor(author =>
//                {
//                    author
//                        .WithName(Program.Client.CurrentUser.Username)
//                        .WithIconUrl(Program.Client.CurrentUser.GetAvatarUrl());
//                })
                .WithFooter($"To select: Type your number\n To cancel: {prefix}cancel");
            builder.WithDescription(description);
            return builder.Build();
        }

        public static Embed GetQueue(string songs)
        {
            var builder = new EmbedBuilder()
                .WithTitle("Queue")
                .WithColor(new Color(0xBB4FFF))
//                .WithAuthor(author =>
//                {
//                    author
//                        .WithName(Program.Client.CurrentUser.Username)
//                        .WithIconUrl(Program.Client.CurrentUser.GetAvatarUrl());
//                })
                .WithDescription(songs);
            return builder.Build();
        }
    }
}