using Avalonia.Controls;
using Avalonia.Threading;
using LauncherHotupdate.Core;
using NHLauncher.Other;
using NHLauncher.ViewModels;
using System;
using MsBox.Avalonia;
using System.Collections.Generic;
using MsBox.Avalonia.Enums;
namespace NHLauncher.Views;

public partial class MainView : UserControl
{
    private SettingWindow? currrentSettingWindow = null;
    private List<LauncherSetting> setting;
    public MainView()
    {
        InitializeComponent();
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
            //Òþ²Øµ½ÏµÍ³ÍÐÅÌ
            this.GetWindow()?.Hide();
        };
        vm.OnUpdate += () =>
        {

        };
        vm.LogMessages.CollectionChanged += (e, s) => LogListBox.ScrollIntoView(LogListBox.ItemCount);
        DataContext = vm;
    }
    private void BTN_Close_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        this.GetWindow()?.Close();
    }
    private void BTN_Set_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        //if (currrentSettingWindow != null && currrentSettingWindow.IsVisible)
        //{
        //    currrentSettingWindow.Activate();
        //    return;
        //}
        //else
        //{
        //    currrentSettingWindow = new SettingWindow(Setting);
        //    currrentSettingWindow.Closed += (s, e) => currrentSettingWindow = null;
        //    currrentSettingWindow.Show();
        //}
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