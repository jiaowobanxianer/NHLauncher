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
    private List<LauncherSetting> setting;
    public MainView()
    {
        InitializeComponent();
        setting = SettingHelper.LoadOrCreateSetting();
        var vm = new MainViewModel(this, setting);
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
    private void OpenUrl(string url)
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });
    }
}