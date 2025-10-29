
using System;
using System.IO;
using Newtonsoft.Json;
using LauncherHotupdate.Core;
using System.Collections.Generic;
using NHLauncher.ViewModels;
using System.Collections.ObjectModel;
using System.Linq;

namespace NHLauncher.Other
{

    public static class SettingHelper
    {
        private static readonly string SettingFile = Path.Combine(AppContext.BaseDirectory, "setting.json");

        public static List<LauncherSetting> LoadOrCreateSetting()
        {
            List<LauncherSetting> setting;

            if (File.Exists(SettingFile))
            {
                try
                {
                    var json = File.ReadAllText(SettingFile);
                    setting = JsonConvert.DeserializeObject<List<LauncherSetting>>(json) ?? new List<LauncherSetting>();
                }
                catch
                {
                    // 文件损坏或解析失败时，使用默认设置
                    setting = new List<LauncherSetting>();
                }
            }
            else
            {
                // 文件不存在，创建默认设置
                setting = new List<LauncherSetting>();
                SaveSetting(setting);
            }

            return setting;
        }

        public static void SaveSetting(List<LauncherSetting> setting)
        {
            var json = JsonConvert.SerializeObject(setting, Formatting.Indented);
            File.WriteAllText(SettingFile, json);
        }
        public static void SaveSetting(List<LauncherSettingWrapper> setting)
        {
            var s = setting.ConvertAll(x => x.Setting);
            var json = JsonConvert.SerializeObject(s, Formatting.Indented);
            File.WriteAllText(SettingFile, json);
        }
        public static void SaveSetting(ObservableCollection<LauncherSettingWrapper> setting)
        {
            var s = setting.ToList().ConvertAll(x => x.Setting);
            var json = JsonConvert.SerializeObject(s, Formatting.Indented);
            File.WriteAllText(SettingFile, json);
        }
    }

}
