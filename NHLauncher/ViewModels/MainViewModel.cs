using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Launcher.Shared;
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
        public enum LauncherButtonState
        {
            Download,
            Launch,
            Update
        }

        [ObservableProperty] private LauncherButtonState currentButtonState;
        [ObservableProperty] private Bitmap? imgAvatar;
        [ObservableProperty] private int downloadProgress;
        [ObservableProperty] private string downloadText = "开始下载";
        [ObservableProperty] private int currentSettingIndex = -1;
        [ObservableProperty] private bool downloading;
        [ObservableProperty] private string? title = "NHLauncher";
        [ObservableProperty] private string? userName;
        [ObservableProperty] private bool isLoggedIn;

        public ObservableCollection<string> LogMessages { get; } = new();
        public ObservableCollection<LauncherSettingWrapper> Settings { get; } = new ObservableCollection<LauncherSettingWrapper>();

        public event Action<string>? OnError;
        public event Action? OnUpdate;
        public event Action? OnStart;

        private UserControl? owner;
        private SettingWindow? currrentSettingWindow = null;
        private LauncherUpdateManager manager;

        public ICommand LauncherButtonCommand => new RelayCommand(async () => await OnLauncherButtonClick());

        public MainViewModel(UserControl? owner)
        {
            this.owner = owner;
            manager = new LauncherUpdateManager();
            Dispatcher.UIThread.InvokeAsync(InitLogin);
            OnError += msg => LogMessages.Add(msg);
        }

        public MainViewModel(UserControl? owner, List<LauncherSetting> settings)
        {
            this.owner = owner;
            Settings = new ObservableCollection<LauncherSettingWrapper>(ConvertSetting(settings));

            if (settings.Count > 0)
            {
                CurrentSettingIndex = Math.Clamp(CurrentSettingIndex, 0, settings.Count - 1);
                var current = Settings[CurrentSettingIndex];
                Title = current.ProjectId;
                ImgAvatar = current.AppIcon;
            }
            manager = new LauncherUpdateManager();
            OnError += msg => LogMessages.Add(msg);
        }

        private IEnumerable<LauncherSettingWrapper> ConvertSetting(List<LauncherSetting> settings)
            => settings.ConvertAll(x => new LauncherSettingWrapper(SelectSetting, DeleteSetting, ModifySetting, RepairSetting, OpenFolder, x));

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
                var localPath = Combine(AppContext.BaseDirectory, item!.Setting.LocalPath);
                Directory.Delete(localPath, true);
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
                var localPath = Combine(AppContext.BaseDirectory, setting.Setting.LocalPath)
                                .Replace('/', Path.DirectorySeparatorChar)
                                .Replace('\\', Path.DirectorySeparatorChar);
                if (Directory.Exists(localPath))
                    Process.Start("explorer.exe", localPath);
                else
                    OnError?.Invoke("应用目录不存在。");
            }
        }

        private async Task InitLogin()
        {
            var loginData = await manager.IsLoggedIn();
            if (loginData.Item1)
            {
                UserName = loginData.Item2 ?? "";
                IsLoggedIn = true;
            }

            if (IsLoggedIn)
            {
                var projectsData = await manager.GetProjectsAsync();
                if (projectsData.error != null)
                {
                    LogMessages.Add($"获取项目列表失败：{projectsData.error}");
                    return;
                }
                else
                {
                    var projects = projectsData.projects;
                    GenRuntimeProjects(projects);
                }
            }
            else
            {
                foreach (var item in ConvertSetting(SettingHelper.LoadOrCreateSetting()))
                {
                    Settings.Add(item);
                }
            }
        }

        public async Task RepairSetting(string projectId)
        {
            LogMessages.Add("开始修复应用...");
            try
            {
                var setting = Settings.FirstOrDefault(x => x.ProjectId == projectId);
                Downloading = true;
                var updater = new LauncherUpdater(setting!.Setting, manager);
                await updater.UpdateAllAsync(new ProgressReport(this), DownloadCallback);
                LogMessages.Add("修复完成，点击启动按钮启动。");
                _ = UpdateButtonState(setting);
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
            if (Settings.Count > 0 && newValue >= 0 && newValue < Settings.Count)
            {
                var current = Settings[newValue];
                Title = current.ProjectId;
                ImgAvatar = current.AppIcon;
                _ = UpdateButtonState(current);
            }
        }
        public async Task CheckUpdate()
        {
            if (!TryGetCurrentSetting(out var current)) return;

            try
            {
                var updater = new LauncherUpdater(current.Setting, manager);
                bool hasUpdate = await updater.HasUpdateAsync();

                string localFile = Path.Combine(AppContext.BaseDirectory, current.Setting.LocalPath, current.Setting.AppName)
                                    .Replace('/', Path.DirectorySeparatorChar)
                                    .Replace('\\', Path.DirectorySeparatorChar);
                if (!File.Exists(localFile))
                {
                    CurrentButtonState = LauncherButtonState.Download;
                    return;
                }
                if (hasUpdate)
                {
                    CurrentButtonState = LauncherButtonState.Update;
                }
                else
                {

                    CurrentButtonState = LauncherButtonState.Launch;
                }
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex.Message);
            }
        }

        private async Task OnLauncherButtonClick()
        {
            try
            {
                if (!TryGetCurrentSetting(out var current)) return;

                switch (CurrentButtonState)
                {
                    case LauncherButtonState.Download:
                        Downloading = true;
                        var updater1 = new LauncherUpdater(current.Setting, manager);
                        await updater1.UpdateAllAsync(new ProgressReport(this), DownloadCallback);
                        break;
                    case LauncherButtonState.Update:
                        Downloading = true;
                        var updater2 = new LauncherUpdater(current.Setting, manager);
                        await updater2.UpdateAsync(new ProgressReport(this), DownloadCallback);
                        break;
                    case LauncherButtonState.Launch:
                        try
                        {
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = Path.Combine(current.Setting.LocalPath, current.Setting.AppName),
                                WorkingDirectory = AppContext.BaseDirectory,
                                UseShellExecute = true
                            });
                            OnStart?.Invoke();
                        }
                        catch (Exception ex)
                        {
                            OnError?.Invoke(ex.Message);
                        }
                        break;
                }

                // 更新按钮状态
                _ = UpdateButtonState(current);
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

        private async Task UpdateButtonState(LauncherSettingWrapper setting)
        {
            string localFile = Path.Combine(AppContext.BaseDirectory, setting.Setting.LocalPath, setting.Setting.AppName)
                                .Replace('/', Path.DirectorySeparatorChar)
                                .Replace('\\', Path.DirectorySeparatorChar);

            if (!File.Exists(localFile))
            {
                CurrentButtonState = LauncherButtonState.Download;
            }
            else
            {
                try
                {
                    var updater = new LauncherUpdater(setting.Setting, manager);
                    bool hasUpdate = await updater.HasUpdateAsync();
                    if (hasUpdate)
                    {
                        CurrentButtonState = LauncherButtonState.Update;
                    }
                    else
                    {
                        CurrentButtonState = LauncherButtonState.Launch;
                    }
                }
                catch
                {
                    CurrentButtonState = LauncherButtonState.Launch;
                }
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

        public void MinimalCommand()
        {
            owner?.GetWindow()?.Hide();
        }

        public void LoginCommand()
        {
            var loginWindow = new LoginWindow(new LoginViewModel()
            {
                manager = manager,
                OnLoginSuccess = async (account) =>
                {
                    try
                    {
                        LogMessages.Add($"欢迎回来，{account}！");
                        UserName = account;
                        IsLoggedIn = true;

                        var (err, projects) = await manager.GetProjectsAsync();
                        if (err != null)
                        {
                            LogMessages.Add($"获取项目列表失败：{err}");
                            return;
                        }

                        bool flowControl = GenRuntimeProjects(projects);
                        if (!flowControl) return;
                    }
                    catch (Exception ex)
                    {
                        OnError?.Invoke($"加载项目时出错：{ex.Message}");
                    }
                }
            });

            loginWindow.Show();
        }

        private bool GenRuntimeProjects(List<Project>? projects, bool clearLoacal = false)
        {
            if (clearLoacal)
                Settings?.Clear();
            if (projects == null || projects.Count == 0) return false;

            foreach (var p in projects)
            {
                if (Settings?.Any(x => x.ProjectId == p.ProjectName) ?? false)
                    continue;

                var setting = LauncherSetting.CreateInstance();
                setting.ProjectId = p.ProjectName;
                setting.API = manager.Endpoint;
                setting.RemotePath = p.TargetPath;
                setting.UseCdn = false;

                Settings?.Add(new LauncherSettingWrapper(
                    SelectSetting,
                    DeleteSetting,
                    ModifySetting,
                    RepairSetting,
                    OpenFolder,
                    setting
                ));
                SettingHelper.SaveSetting(Settings!);
            }

            if (Settings?.Count > 0)
                CurrentSettingIndex = 0;

            return true;
        }

        public async Task LogoutCommand()
        {
            var str = await manager.LogoutAsync();
            if (str != "成功")
            {
                OnError?.Invoke(str);
            }
            else
            {
                UserName = "";
                IsLoggedIn = false;
            }
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
                LogMessages.Add($"未知下载命令: {commandFile}");
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
