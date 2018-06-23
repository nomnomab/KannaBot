using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;
using Discord;

namespace KannaBot.Scripts.Services
{
    public class Rule34Service
    {
        private XmlDocument lastDoc;
        private List<string> watchList = new List<string>();

        public async Task<Embed> GetRandomImage(ulong id, params string[] tags)
        {
            var guild = Program.GetGuild(id);
            var imagesPerPage = int.Parse(guild.Config["r34_img_per_page"].ToString());
            var pageLimit = int.Parse(guild.Config["r34_page_limit"].ToString());
            //Console.WriteLine(imagesPerPage + "/" + pageLimit);
            GrabDocument(0, tags);

            var maxCount = int.Parse(lastDoc.GetElementsByTagName("posts")[0].Attributes["count"].Value);

            while (true)
            {
                try
                {
                    var random = new Random().Next(0, maxCount > imagesPerPage ? imagesPerPage : maxCount);
                    //Console.WriteLine(random);
                    var randomPage = new Random().Next(0, (maxCount / imagesPerPage) > pageLimit ? pageLimit : maxCount / imagesPerPage);
                    //Console.WriteLine(randomPage);
                    GrabDocument(randomPage, tags);

                    var posts = lastDoc.GetElementsByTagName("post");
                    var post = posts[random];

                    var link = post.Attributes["file_url"].Value;

                    var builder = new EmbedBuilder()
                        .WithColor(new Color(0xA14027))
                        .WithFooter(tags.ToArrayString("+"))
                        .WithDescription(link)
                        .WithImageUrl(link)
                        .WithAuthor(author =>
                        {
                            author
                            .WithName(Program.Client.CurrentUser.Username)
                            .WithIconUrl(Program.Client.CurrentUser.GetAvatarUrl());
                        });

                    var embed = builder.Build();
                    return embed;
                }
                catch(Exception e) { continue; }
                break;
            }
        }

        private void GrabDocument(int page, params string[] tags)
        {
            lastDoc = new XmlDocument();
            lastDoc.Load(GetUrl(page, tags));
        }

        private static string GetUrl(int p, params string[] tags)
        {
            var ts = new List<string>();
            foreach (var t in tags) ts.AddRange(t.Split(' '));
            tags = ts.ToArray();
            const string api = "/index.php?page=dapi&s=post&q=index";
            const string url = "https://rule34.xxx";
            const string tag = "&tags=";
            var tagString = tags.ToArrayString("+");
            const string page = "&pid=";
            tagString = tagString.Replace(' ', '_');
            //Console.WriteLine(url + api + tag + tagString + page + p);
            return url + api + tag + tagString + page + p;
        }
    }
}
