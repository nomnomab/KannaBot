using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using ImageSharp = SixLabors.ImageSharp;

namespace KannaBot.Scripts.Services
{
    public static class ImageGeneration
    {
        public static async Task<string> GenerateProfileCard(IUser user)
        {
            var path = Environment.CurrentDirectory + "/Images/";
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            var backgroundPath = path + "background.png";

            Image<Rgba32> profileCard = ImageSharp.Image.Load(backgroundPath);
            Image<Rgba32> avatar;
            Stream stream;
            string avatarUrl = user.GetAvatarUrl();
            ulong userId = user.Id;

            try
            {
                using(var http = new HttpClient())
                {
                    stream = await http.GetStreamAsync(new Uri(avatarUrl));
                }
                avatar = ImageSharp.Image.Load(stream);
            }
            catch(Exception e)
            {
                //Console.WriteLine(e.Message);
                throw e;
            }

            uint xp = 0;
            uint rank = 0;
            uint level = 0;
            double xpLow = 0;
            double xpHigh = 100;

            return "";
        }
    }
}
