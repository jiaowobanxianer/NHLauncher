using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Threading;
using NHLauncher.Other;
using NHLauncher.ViewModels;
using NHLauncher.Views;
using System;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
namespace NHLauncher;

public partial class App : Application
{
    private CancellationTokenSource? _pipeCts;
    private bool _isTrayIconInitialized = false;
    private TrayIcon? _trayIcon;
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            desktop.MainWindow = new MainWindow();
            desktop.Exit += Desktop_Exit;
            InitializeTrayIcon(desktop);
            StartPipeListener();
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView();
        }
        base.OnFrameworkInitializationCompleted();
    }

    private void Desktop_Exit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        if (_trayIcon != null)
        {
            _trayIcon.Dispose();
            _trayIcon = null;
        }
    }

    private async void StartPipeListener()
    {
        _pipeCts = new CancellationTokenSource();
        try
        {
            while (!_pipeCts.IsCancellationRequested)
            {
                // 创建命名管道服务端
                using var server = new NamedPipeServerStream("NHLauncher_SingleInstance_Pipe", PipeDirection.In);
                await server.WaitForConnectionAsync(_pipeCts.Token);

                using var reader = new StreamReader(server);
                var message = await reader.ReadToEndAsync();

                if (message == "WAKEUP")
                {
                    // 关键：切换回 UI 线程执行显示逻辑，彻底解决透明框
                    Dispatcher.UIThread.Post(() =>
                    {
                        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                        {
                            var win = desktop.MainWindow;
                            if (win != null)
                            {
                                win.Show();
                                win.Activate();
                                win.WindowState = WindowState.Normal;
                                // 强制触发一次重绘
                                win.InvalidateVisual();
                            }
                        }
                    });
                }
            }
        }
        catch { /* 忽略退出时的异常 */ }
    }
    private void InitializeTrayIcon(IClassicDesktopStyleApplicationLifetime desktop)
    {
        if (_isTrayIconInitialized) return; // 防止重复初始化
        _trayIcon = new TrayIcon
        {
            Icon = new WindowIcon(ImageHelper.LoadAssetStreamFromResource("xrf.ico")), // 路径要确认
            ToolTipText = "NHLauncher",
            IsVisible = true,
        };
        var menu = new NativeMenu();

        var showItem = new NativeMenuItem("Show");
        showItem.Click += (sender, args) =>
        {
            desktop.MainWindow?.Show();
            desktop.MainWindow?.Activate();
        };
        menu.Items.Add(showItem);

        var exitItem = new NativeMenuItem("Exit");
        exitItem.Click += (sender, args) =>
        {
            desktop.Shutdown();
        };
        menu.Items.Add(exitItem);

        _trayIcon.Menu = menu;

        // 可选：支持单击托盘图标（注意 macOS 上可能不触发 Clicked）
        _trayIcon.Clicked += (s, e) =>
        {
            desktop?.MainWindow?.Show();
            desktop?.MainWindow?.Activate();
        };
        _isTrayIconInitialized = true;
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}