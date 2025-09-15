using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using NHLauncher.Other;
using NHLauncher.ViewModels;
using NHLauncher.Views;
using System;
using System.Linq;
namespace NHLauncher;

public partial class App : Application
{
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
            _trayIcon = new TrayIcon
            {
                Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://NHLauncher/Assets/avalonia-logo.ico"))), // 路径要确认
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
                desktop.MainWindow?.Show();
                desktop.MainWindow?.Activate();
            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView();
        }

        base.OnFrameworkInitializationCompleted();
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