using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net.Http;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
namespace NHLauncher.Other
{

    public static class ImageHelper
    {
        public static string projectResourcePath = "avares://NHLauncher/Assets";
        public static Bitmap? DefaultMap
        {
            get
            {
                return LoadFromResource("avatar.png");
            }
        }
        public static Bitmap? LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath))
                return null;
            using (var stream = File.OpenRead(filePath))
            {
                return new Bitmap(stream);
            }

        }
        public static Bitmap LoadFromResource(Uri resourceUri)
        {
            return new Bitmap(AssetLoader.Open(resourceUri));
        }
        public static Bitmap LoadFromResource(string resourcePath)
        {
            return LoadFromResource(new Uri(Path.Combine(projectResourcePath, resourcePath)));
        }
        public static Stream LoadAssetStreamFromResource(Uri resourceUri)
        {
            return AssetLoader.Open(resourceUri);
        }
        public static Stream LoadAssetStreamFromResource(string resourcePath)
        {
            return LoadAssetStreamFromResource(new Uri(Path.Combine(projectResourcePath, resourcePath)));
        }
        public static async Task<Bitmap?> LoadFromWeb(Uri url)
        {
            using var httpClient = new HttpClient();
            try
            {
                var response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                var data = await response.Content.ReadAsByteArrayAsync();
                return new Bitmap(new MemoryStream(data));
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"An error occurred while downloading image '{url}' : {ex.Message}");
                return null;
            }
        }
    }

}
