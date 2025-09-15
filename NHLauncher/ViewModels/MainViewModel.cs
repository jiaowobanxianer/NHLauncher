using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using LauncherHotupdate.Core;
using NHLauncher.Other;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;
namespace NHLauncher.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    public Bitmap imgAvatar;
    private List<Bitmap> images = new();
    private int currentIdx = 0;
    [ObservableProperty]
    public Bitmap? imageFromBinding;
    [ObservableProperty]
    public int downloadProgress;
    [ObservableProperty]
    public bool downloading;
    [ObservableProperty]
    public bool canUpdate;
    [ObservableProperty]
    public List<string> logMessages = new List<string>();
    private LauncherSetting _setting;
    public event Action<string>? OnError;
    public event Action? OnUpdate;
    public event Action? OnStart;
    public MainViewModel()
    {
        _setting = SettingHelper.LoadOrCreateSetting();
        // 假设图片文件名是连续的，动态生成路径
        var imageUris = GenerateImageUris("avares://NHLauncher/Assets/LoginBG/", "合成 1_", 4, 237, ".png");

        // 加载所有图片
        foreach (var uri in imageUris)
        {
            var bitmap = ImageHelper.LoadFromResource(uri);
            if (bitmap != null)
            {
                images.Add(bitmap);
            }
        }
        ImageFromBinding = images[currentIdx];
        ImgAvatar = ImageHelper.LoadFromResource(new Uri("avares://NHLauncher/Assets/avatar.jpg"));
        OnError += (msg) => LogMessages.Add(msg);
    }
    public MainViewModel(LauncherSetting setting)
    {
        _setting = setting;
        // 假设图片文件名是连续的，动态生成路径
        var imageUris = GenerateImageUris("avares://NHLauncher/Assets/LoginBG/", "合成 1_", 4, 237, ".png");

        // 加载所有图片
        foreach (var uri in imageUris)
        {
            var bitmap = ImageHelper.LoadFromResource(uri);
            if (bitmap != null)
            {
                images.Add(bitmap);
            }
        }
        ImageFromBinding = images[currentIdx];
        ImgAvatar = ImageHelper.LoadFromResource(new Uri("avares://NHLauncher/Assets/avatar.jpg"));
        OnError += (msg) => LogMessages.Add(msg);
    }
    public void NextIMG()
    {
        if (currentIdx < images.Count - 1)
        {
            currentIdx++;
        }
        else
            currentIdx = 0;
        ImageFromBinding = images[currentIdx];
    }
    public void StartCommand()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = Path.Combine(_setting.LocalPath, _setting.AppName),
                WorkingDirectory = AppContext.BaseDirectory,
                UseShellExecute = true
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
    private List<Uri> GenerateImageUris(string basePath, string prefix, int startIndex, int endIndex, string extension)
    {
        var uris = new List<Uri>();
        for (int i = startIndex; i < endIndex; i++)
        {
            var fileName = $"{prefix}{i:D5}{extension}";
            uris.Add(new Uri($"{basePath}{fileName}"));
        }
        return uris;
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

