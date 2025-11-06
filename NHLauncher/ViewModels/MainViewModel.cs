using Avalonia.Controls;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LauncherHotupdate.Core;
using NHLauncher.Other;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using static System.IO.Path;

namespace NHLauncher.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        [ObservableProperty] private Bitmap? imgAvatar;
        [ObservableProperty] private int downloadProgress;
        [ObservableProperty] private string downloadText = "开始下载";
        [ObservableProperty] private int currentSettingIndex;
        [ObservableProperty] private bool downloading;
        [ObservableProperty] private bool canUpdate;
        [ObservableProperty] private string? title = "NHLauncher";

        public ObservableCollection<string> LogMessages { get; } = new();
        public ObservableCollection<LauncherSettingWrapper> Settings { get; }

        public event Action<string>? OnError;
        public event Action? OnUpdate;
        public event Action? OnStart;

        private UserControl? owner;
        private SettingWindow? currrentSettingWindow = null;
        public MainViewModel()
            : this(null, SettingHelper.LoadOrCreateSetting()) { }

        public MainViewModel(UserControl owner, List<LauncherSetting> settings)
        {
            this.owner = owner;
            Settings = new ObservableCollection<LauncherSettingWrapper>(
                settings.ConvertAll(x => new LauncherSettingWrapper(SelectSetting, DeleteSetting, ModifySetting, RepairSetting, OpenFolder, x))
            );

            if (settings.Count > 0)
            {
                CurrentSettingIndex = Math.Clamp(CurrentSettingIndex, 0, settings.Count - 1);
                var current = Settings[CurrentSettingIndex];
                Title = current.ProjectId;
                ImgAvatar = current.AppIcon;
            }

            OnError += msg => LogMessages.Add(msg);
        }

        private bool TryGetCurrentSetting(out LauncherSettingWrapper setting)
        {
            if (CurrentSettingIndex >= 0 && CurrentSettingIndex < Settings.Count)
            {
                setting = Settings[CurrentSettingIndex];
                return true;
            }

            OnError?.Invoke("未选择有效的应用。");
            setting = null!;
            return false;
        }

        public void SelectSetting(string projectId)
        {
            int index = Settings.ToList().FindIndex(x => x.ProjectId == projectId);
            if (index >= 0) CurrentSettingIndex = index;
        }

        public void DeleteSetting(string projectId)
        {
            var item = Settings.FirstOrDefault(x => x.ProjectId == projectId);
            if (item != null)
            {
                Settings.Remove(item);
                SettingHelper.SaveSetting(Settings);
            }
        }

        public void ModifySetting(string projectId)
        {
            var setting = Settings.FirstOrDefault(x => x.ProjectId == projectId);
            if (setting != null)
                new CreateNewProfileWindow(Settings, setting, this).Show();
        }

        public void OpenFolder(string projectId)
        {
            var setting = Settings.FirstOrDefault(x => x.ProjectId == projectId);
            if (setting != null)
            {
                var localPath = Combine(AppContext.BaseDirectory, setting.Setting.LocalPath);
                if (Directory.Exists(localPath))
                    Process.Start("explorer.exe", localPath);
                else
                    OnError?.Invoke("应用目录不存在。");
            }
        }

        public async Task RepairSetting(string projectId)
        {
            LogMessages.Add("开始修复应用...");
            try
            {
                var setting = Settings.FirstOrDefault(x => x.ProjectId == projectId);
                var downloader = new LauncherDownloader();
                Downloading = true;
                await new LauncherUpdater(setting!.Setting).UpdateAsync(new ProgressReport(this), DownloadCallback);
                Downloading = false;
                LogMessages.Add("修复完成，点击启动按钮启动。");
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex.Message);
            }
            finally
            {
                Downloading = false;
            }
        }
        partial void OnCurrentSettingIndexChanged(int oldValue, int newValue)
        {
            if (newValue >= 0 && newValue < Settings.Count)
            {
                var current = Settings[newValue];
                Title = current.ProjectId;
                CanUpdate = false;
                ImgAvatar = current.AppIcon;
            }
        }

        public void AddAppCommand()
        {
            try
            {
                new CreateNewProfileWindow(Settings, this).Show();
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex.Message);
            }
        }

        public void StartCommand()
        {
            if (!TryGetCurrentSetting(out var current)) return;

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = Combine(current.Setting.LocalPath, current.Setting.AppName),
                    WorkingDirectory = AppContext.BaseDirectory,
                    UseShellExecute = true,
                    Verb = "runas"
                });
                OnStart?.Invoke();
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex.Message);
            }
        }
        public void MinimalCommand()
        {
            owner?.GetWindow()?.Hide();
        }
        public void SetCommand()
        {
            if (currrentSettingWindow != null && currrentSettingWindow.IsVisible)
            {
                currrentSettingWindow.Activate();
                return;
            }
            else
            {
                currrentSettingWindow = new SettingWindow();
                currrentSettingWindow.Closed += (s, e) => currrentSettingWindow = null;
                currrentSettingWindow.Show();
            }
        }
        public void CloseCommand()
        {
            Environment.Exit(10010);
        }
        public async void UpdateCommand()
        {
            if (!TryGetCurrentSetting(out var current)) return;
            if (Downloading) return;

            try
            {
                Downloading = true;
                OnUpdate?.Invoke();
                LogMessages.Add("开始下载更新...");

                var updater = new LauncherUpdater(current.Setting);
                await updater.UpdateAsync(new ProgressReport(this), DownloadCallback);

                LogMessages.Add("更新完成，点击启动按钮启动。");
                CheckUpdate();
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex.Message);
                DownloadText = "下载出错，请查看日志并反馈给管理员！";
            }
            finally
            {
                Downloading = false;
            }
        }

        private void DownloadCallback(string commandFile)
        {
            if (string.IsNullOrEmpty(commandFile)) return;

            char command = commandFile[0];
            string str = commandFile.Length > 1 ? commandFile[1..] : "";

            DownloadText = command switch
            {
                's' => $"已存在 {str}，跳过下载。",
                'd' => $"正在下载 {str}...",
                'c' => "下载完成！",
                _ => DownloadText
            };
            if (command != 'c' && command != 'd' && command != 's')
            {
                LogMessages.Add($"未知下载命令: {commandFile}");
            }
        }

        public async void CheckUpdate()
        {
            if (!TryGetCurrentSetting(out var current)) return;

            try
            {
                var updater = new LauncherUpdater(current.Setting);
                CanUpdate = await updater.HasUpdateAsync();

                string log = CanUpdate
                    ? "检测到新版本，点击更新按钮进行更新。"
                    : (await updater.GetRemoteManifestAsync() == null
                        ? "当前应用无远程版本，请检查 ProjectID 是否正确。"
                        : "当前已是最新版本。");

                LogMessages.Add(log);
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex.Message);
            }
        }

        public class ProgressReport : IProgress<double>
        {
            private readonly MainViewModel _viewModel;
            public ProgressReport(MainViewModel viewModel) => _viewModel = viewModel;

            public void Report(double value)
            {
                _viewModel.DownloadProgress = (int)value;
                if (value >= 100)
                    _viewModel.Downloading = false;
            }
        }
    }

    public class LauncherSettingWrapper : ObservableObject
    {
        public LauncherSetting Setting { get; }

        public string ProjectId => Setting.ProjectId;

        public Bitmap? AppIcon
        {
            get
            {
                var path = Combine(AppContext.BaseDirectory, Setting.LocalPath, Setting.AppIcon);
                return ImageHelper.LoadFromFile(path) ?? ImageHelper.DefaultMap;
            }
        }

        public ICommand SelectCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand ModifyCommand { get; }
        public ICommand RepairCommand { get; }
        public ICommand OpenFolderCommand { get; }

        public LauncherSettingWrapper(Action<string> select, Action<string> delete, Action<string> modify, Func<string, Task> repair, Action<string> openFolder, LauncherSetting setting)
        {
            Setting = setting;
            SelectCommand = new RelayCommand(() => select?.Invoke(setting.ProjectId));
            DeleteCommand = new RelayCommand(() => delete?.Invoke(setting.ProjectId));
            ModifyCommand = new RelayCommand(() => modify?.Invoke(setting.ProjectId));
            RepairCommand = new RelayCommand(() => repair?.Invoke(setting.ProjectId));
            OpenFolderCommand = new RelayCommand(() => openFolder?.Invoke(setting.ProjectId));
        }
    }
}
