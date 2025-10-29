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
using static System.Runtime.InteropServices.JavaScript.JSType;
namespace NHLauncher.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    public Bitmap imgAvatar;
    [ObservableProperty]
    public int downloadProgress;
    [ObservableProperty]
    public int currentSettingIndex;
    [ObservableProperty]
    public bool downloading;
    [ObservableProperty]
    public bool canUpdate;
    [ObservableProperty]
    public string title = "NHLauncher";
    public ObservableCollection<string> LogMessages { get; } = new ObservableCollection<string>();
    public ObservableCollection<LauncherSettingWrapper> Settings { get; }
    public event Action<string>? OnError;
    public event Action? OnUpdate;
    public event Action? OnStart;
    public MainViewModel()
    {
        Settings = new ObservableCollection<LauncherSettingWrapper>(SettingHelper.LoadOrCreateSetting().ConvertAll(x => new LauncherSettingWrapper(SelectSetting, 
            DeleteSetting, x)));
        ImgAvatar = ImageHelper.LoadFromResource(new Uri("avares://NHLauncher/Assets/avatar.png"));
        OnError += (msg) => LogMessages.Add(msg);
    }
    public MainViewModel(List<LauncherSetting> setting)
    {
        this.Settings = new ObservableCollection<LauncherSettingWrapper>(setting.ConvertAll(x => new LauncherSettingWrapper(SelectSetting, 
            DeleteSetting, x)));
        if (CurrentSettingIndex < 0 || CurrentSettingIndex >= setting.Count)
        {
            CurrentSettingIndex = 0;
        }
        else
            title = this.Settings[CurrentSettingIndex].ProjectId;
        ImgAvatar = ImageHelper.LoadFromResource(new Uri("avares://NHLauncher/Assets/avatar.png"));
        OnError += (msg) => LogMessages.Add(msg);
    }
    public void SelectSetting(string ProjectID)
    {
        if (Settings.Any(x => x.ProjectId == ProjectID))
        {
            CurrentSettingIndex = Settings.ToList().FindIndex(x => x.ProjectId == ProjectID);
        }
    }
    public void DeleteSetting(string ProjectID)
    {
        if (Settings.Any(x => x.ProjectId == ProjectID))
        {
            var index = Settings.ToList().FindIndex(x => x.ProjectId == ProjectID);
            Settings.RemoveAt(index);
            SettingHelper.SaveSetting(Settings);
        }
    }
    partial void OnCurrentSettingIndexChanged(int oldValue, int newValue)
    {
        Title = Settings[newValue].ProjectId;
        CanUpdate = false;
    }
    public void AddAppCommand()
    {
        var window = new SettingWindow(Settings, this);
        window.Show();
    }
    public void StartCommand()
    {
        if (CurrentSettingIndex < 0 || CurrentSettingIndex >= Settings.Count)
        {
            OnError?.Invoke("未选择有效的启动项。");
            return;
        }
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = Path.Combine(Settings[CurrentSettingIndex].Setting.LocalPath, Settings[CurrentSettingIndex].Setting.AppName),
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
    public async void UpdateCommand()
    {
        if (CurrentSettingIndex < 0 || CurrentSettingIndex >= Settings.Count)
        {
            OnError?.Invoke("未选择有效的应用。");
            return;
        }
        try
        {
            if (Downloading) return;
            OnUpdate?.Invoke();
            LauncherUpdater updater = new LauncherUpdater(Settings[CurrentSettingIndex].Setting);
            Downloading = true;
            LogMessages.Add("开始下载更新...");
            await updater.UpdateAsync(new ProgressReport(this));
            Downloading = false;
            LogMessages.Add("更新完成，点击启动按钮启动。");
            CheckUpdate();
        }
        catch (Exception ex)
        {
            OnError?.Invoke(ex.Message);
            Downloading = false;
        }
    }
    public async void CheckUpdate()
    {
        if (CurrentSettingIndex < 0 || CurrentSettingIndex >= Settings.Count)
        {
            OnError?.Invoke("未选择有效的应用。");
            return;
        }
        try
        {
            LauncherUpdater updater = new LauncherUpdater(Settings[CurrentSettingIndex].Setting);
            CanUpdate = await updater.HasUpdateAsync();

            if (CanUpdate)
            {
                LogMessages.Add("检测到新版本，点击更新按钮进行更新。");
            }
            else
            {
                LogMessages.Add("当前已是最新版本。");
            }
        }
        catch (Exception ex)
        {
            OnError?.Invoke(ex.Message);
        }
    }
    public class ProgressReport : IProgress<double>
    {
        private MainViewModel _viewModel;
        public ProgressReport(MainViewModel viewModel)
        {
            _viewModel = viewModel;
        }
        public void Report(double value)
        {
            _viewModel.DownloadProgress = (int)value;
            if (value >= 100)
            {
                _viewModel.Downloading = false;
            }
        }
    }
}

public class LauncherSettingWrapper : ObservableObject
{
    public LauncherSetting Setting { get; set; }

    public string ProjectId => Setting.ProjectId;

    public ICommand SelectCommand { get; }
    public ICommand DeleteCommand { get; }

    public LauncherSettingWrapper(Action<string> selectAction,Action<string> deleteAction, LauncherSetting setting)
    {
        Setting = setting;
        SelectCommand = new RelayCommand(() => selectAction?.Invoke(setting.ProjectId));
        DeleteCommand = new RelayCommand(() => deleteAction?.Invoke(setting.ProjectId));
    }
}
