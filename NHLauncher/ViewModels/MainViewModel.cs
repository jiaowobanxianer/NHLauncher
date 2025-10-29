using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using LauncherHotupdate.Core;
using NHLauncher.Other;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;
namespace NHLauncher.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    public Bitmap imgAvatar;
    [ObservableProperty]
    public int downloadProgress;
    [ObservableProperty]
    public bool downloading;
    [ObservableProperty]
    public bool canUpdate;
    [ObservableProperty]
    public string title = "NHLauncher";
    public ObservableCollection<string> LogMessages { get; } = new ObservableCollection<string>();
    private LauncherSetting _setting;
    public event Action<string>? OnError;
    public event Action? OnUpdate;
    public event Action? OnStart;
    public MainViewModel()
    {
        _setting = SettingHelper.LoadOrCreateSetting();
        ImgAvatar = ImageHelper.LoadFromResource(new Uri("avares://NHLauncher/Assets/avatar.png"));
        OnError += (msg) => LogMessages.Add(msg);
    }
    public MainViewModel(LauncherSetting setting)
    {
        _setting = setting;
        title = _setting.ProjectId;
        ImgAvatar = ImageHelper.LoadFromResource(new Uri("avares://NHLauncher/Assets/avatar.png"));
        OnError += (msg) => LogMessages.Add(msg);
    }
    public void StartCommand()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = Path.Combine(_setting.LocalPath, _setting.AppName),
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
        try
        {
            if (Downloading) return;
            OnUpdate?.Invoke();
            LauncherUpdater updater = new LauncherUpdater(_setting);
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
        try
        {
            LauncherUpdater updater = new LauncherUpdater(_setting);
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

