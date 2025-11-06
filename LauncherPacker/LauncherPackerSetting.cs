using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LauncherPacker
{
    public class LauncherPackerSetting
    {
        public string? baseURL;
        public string? hotUpdateApi;
        public string? apiKey;
        public static LauncherPackerSetting GetPackerSetting()
        {
            if(File.Exists(AppContext.BaseDirectory + "LauncherPackerSetting.json"))
            {
                var json = File.ReadAllText(AppContext.BaseDirectory + "LauncherPackerSetting.json");
                return JsonConvert.DeserializeObject<LauncherPackerSetting>(json)?? CreateDefaultSetting();
            }
            else
            {
                var defaultSetting = CreateDefaultSetting();
                return defaultSetting;
            }
        }
        private static LauncherPackerSetting CreateDefaultSetting()
        {
            var defaultSetting = new LauncherPackerSetting
            {
                baseURL = "http://example.com/",
                hotUpdateApi = "http://example.com/api/upload",
                apiKey = "your_api_key_here"
            };
            File.WriteAllText(AppContext.BaseDirectory + "LauncherPackerSetting.json", JsonConvert.SerializeObject(defaultSetting));
            return defaultSetting;
        }
    }
}
