using Avalonia.Controls;
using Avalonia.Threading;
using LauncherHotupdate.Core;
using NHLauncher.Other;
using NHLauncher.ViewModels;
using System;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
namespace NHLauncher.Views;

public partial class MainView : UserControl
{
    private readonly DispatcherTimer _timer;
    private SettingWindow? currrentSettingWindow = null;
    private LauncherSetting setting;
    public MainView()
    {
        InitializeComponent();
        // 初始化定时器
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(30) // 每30毫秒秒切换一次
        };
        _timer.Tick += OnTimerTick;
        _timer.Start();
        setting = SettingHelper.LoadOrCreateSetting();
        var vm = new MainViewModel(setting);
        vm.OnError += async (msg) =>
        {
            await MessageBoxManager
                .GetMessageBoxStandard("Error", msg)
                .ShowAsync();
        };
        vm.OnStart += () =>
        {
            //隐藏到系统托盘
            (this.Parent as Window)?.Hide();
        };
        vm.OnUpdate += () =>
        {

        };
        DataContext = vm;
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        var d = DataContext as MainViewModel;
        d?.NextIMG();
    }
    private void Border_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            (this.Parent as Window)?.BeginMoveDrag(e);
        }
    }

    private void BTN_Close_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        (this.Parent as Window)?.Close();
    }
    private void BTN_Set_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (currrentSettingWindow != null && currrentSettingWindow.IsVisible)
        {
            currrentSettingWindow.Activate();
            return;
        }
        else
        {
            currrentSettingWindow = new SettingWindow(setting);
            currrentSettingWindow.Closed += (s, e) => currrentSettingWindow = null;
            currrentSettingWindow.Show();
        }
    }
    private void Hyperlink_PointerPressed1(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        OpenUrl("https://rule.tencent.com/rule/46a15f24-e42c-4cb6-a308-2347139b1201");
    }
    private void OpenUrl(string url)
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });
    }
}