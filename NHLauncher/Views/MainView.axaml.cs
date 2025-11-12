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
    public MainView()
    {
        InitializeComponent();
        var vm = new MainViewModel(this);
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
}