
using System;
using System.IO;
using Newtonsoft.Json;
using LauncherHotupdate.Core;

namespace NHLauncher.Other
{

    public static class SettingHelper
    {
        private static readonly string SettingFile = Path.Combine(AppContext.BaseDirectory, "setting.json");

        public static LauncherSetting LoadOrCreateSetting()
        {
            LauncherSetting setting;

            if (File.Exists(SettingFile))
            {
                try
                {
                    var json = File.ReadAllText(SettingFile);
                    setting = JsonConvert.DeserializeObject<LauncherSetting>(json) ?? new LauncherSetting();
                }
                catch
                {
                    // 文件损坏或解析失败时，使用默认设置
                    setting = new LauncherSetting();
                }
            }
            else
            {
                // 文件不存在，创建默认设置
                setting = new LauncherSetting();
                SaveSetting(setting);
            }

            return setting;
        }

        public static void SaveSetting(LauncherSetting setting)
        {
            var json = JsonConvert.SerializeObject(setting, Formatting.Indented);
            File.WriteAllText(SettingFile, json);
        }
    }

}
