using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LauncherHotupdate.Core
{
    public class LauncherSetting
    {
        /// <summary>
        /// 应用项目ID
        /// </summary>
        public string ProjectId { get; set; } = "DemoProject";
        /// <summary>
        /// 应用平台，默认为Win
        /// </summary>
        public string Platform { get; set; } = "Win";
        private string? _appName;
        /// <summary>
        /// 应用程序文件名，默认为项目ID加.exe后缀
        /// </summary>
        public string AppName
        {
            get => _appName ?? $"{ProjectId}.exe";
            set => _appName = value;
        }
        /// <summary>
        /// 应用图标文件名，应与项目ID同名且为PNG格式且放在同级目录下
        /// </summary>
        public string AppIcon
        {
            get => $"{ProjectId}.png";
        }
        /// <summary>
        /// 是否使用CDN进行下载
        /// </summary>
        public bool UseCdn { get; set; } = false;
        /// <summary>
        /// CDN地址
        /// </summary>
        public string ServerBaseUrl { get; set; } = "http://Hotupdate.example.com";
        /// <summary>
        /// API,如果使用的是LauncherPakcerUploadReceiver则后缀为/HotUpdate
        /// </summary>
        public string API { get; set; } = "http://Hotupdate.example.com/HotUpdate";
        public string RemotePath { get; set; } = "";
        [JsonIgnore]
        public string LocalPath => Path.Combine("Games", RemotePath).Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        [JsonIgnore]
        public string ManifestFile => Path.Combine(LocalPath, "manifest.json");
        private LauncherSetting() { }
        public static LauncherSetting CreateInstance()
        {
            var template = Path.Combine(AppContext.BaseDirectory, "launcherSetting.template.json");
            try
            {
                if (File.Exists(template))
                    return JsonConvert.DeserializeObject<LauncherSetting>(File.ReadAllText(template)) ?? new LauncherSetting();
            }
            catch (Exception ex)
            {
                throw new Exception("解析 launcherSetting.template.json 失败,请检查模板格式", ex);
            }
            return new LauncherSetting();
        }
    }
}
