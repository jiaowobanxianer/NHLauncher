using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using LauncherHotupdate.Core;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace NHLauncher.ViewModels
{
    internal partial class SettingViewModel : ViewModelBase, IProgress<double>
    {
        private Window? owner;
        private LauncherSetting? setting;
        private static readonly string SettingFile = Path.Combine(AppContext.BaseDirectory, "updater.json");

        [ObservableProperty]
        public bool updating;

        [ObservableProperty]
        private int downloadProgress;

        public SettingViewModel(Window owner)
        {
            this.owner = owner;
        }
        public async Task CheckUpdateCommand()
        {
            if (Updating) return;
            if (!File.Exists(SettingFile))
            {
                var msgBox = MessageBoxManager.GetMessageBoxStandard("错误", "未找到 updater.json 文件", ButtonEnum.Ok, Icon.Error);
                await msgBox.ShowAsPopupAsync(owner!);
                return;
            }

            try
            {
                var json = File.ReadAllText(SettingFile);
                setting = JsonConvert.DeserializeObject<LauncherSetting>(json) ?? LauncherSetting.CreateInstance();

                var updater = new LauncherUpdater(setting);
                var localManifestPath = Path.Combine(AppContext.BaseDirectory, "manifest.json");
                var local = JsonConvert.DeserializeObject<Manifest>(File.ReadAllText(localManifestPath));
                var remote = await updater.GetRemoteManifestAsync();
                var canUpdate = updater.HasUpdate(local!, remote!);

                if (!canUpdate)
                {
                    var noUpdateBox = MessageBoxManager.GetMessageBoxStandard("提示", "当前已是最新版本。", ButtonEnum.Ok, Icon.Info);
                    await noUpdateBox.ShowAsPopupAsync(owner!);
                    return;
                }

                var resultBox = MessageBoxManager.GetMessageBoxStandard(
                    "发现更新",
                    "检测到有新版本，是否下载？",
                    ButtonEnum.YesNo,
                    Icon.Question);

                var res = await resultBox.ShowAsPopupAsync(owner!);
                if (res != ButtonResult.Yes) return;

                Updating = true;
                DownloadProgress = 0;

                await updater.UpdateAsync(this, null); // this 实现 IProgress<double>

                Updating = false;
                File.Copy(Path.Combine(AppContext.BaseDirectory, setting.ManifestFile), localManifestPath,true);
                var restartBox = MessageBoxManager.GetMessageBoxStandard(
                    "更新完成",
                    "更新已完成，是否重启程序？",
                    ButtonEnum.YesNo,
                    Icon.Info);

                var restartRes = await restartBox.ShowAsPopupAsync(owner!);

                if (restartRes == ButtonResult.Yes)
                {
                    string updaterExe = Path.Combine(AppContext.BaseDirectory, "NHLauncher.Updater.exe");
                    if (File.Exists(updaterExe))
                    {
                        string tempUpdateDir = Path.Combine(AppContext.BaseDirectory, setting.LocalPath);
                        string mainExeName = "NHLauncher.Desktop.exe"; // 主程序名字

                        Process.Start(new ProcessStartInfo
                        {
                            FileName = updaterExe,
                            ArgumentList = { $"{AppContext.BaseDirectory}", $"{tempUpdateDir}", $"{mainExeName}"
                            },
                            WorkingDirectory = AppContext.BaseDirectory,
                            UseShellExecute = true
                        });
                        owner?.Close();
                        Environment.Exit(10086);
                    }
                    else
                    {
                        var missingBox = MessageBoxManager.GetMessageBoxStandard("错误", "找不到 NHLauncher.Updater.exe", ButtonEnum.Ok, Icon.Error);
                        await missingBox.ShowAsPopupAsync(owner!);
                    }
                }

            }
            catch (Exception ex)
            {
                Updating = false;
                var failBox = MessageBoxManager.GetMessageBoxStandard(
                    "更新失败",
                    $"更新过程中出现错误:\n{ex.Message}\n请稍后重试。",
                    ButtonEnum.Ok,
                    Icon.Error);

                await failBox.ShowAsPopupAsync(owner!);
            }
        }

        public void CancelCommand()
        {
            if (Updating) return;
            owner?.Close();
        }

        /// <summary>
        /// IProgress<double> 实现，更新下载进度
        /// </summary>
        /// <param name="value">百分比 0~100</param>
        public void Report(double value)
        {
            DownloadProgress = (int)value;
        }
    }
}
