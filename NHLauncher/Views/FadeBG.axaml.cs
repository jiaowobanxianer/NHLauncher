using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using NHLauncher.ViewModels;
using System;

namespace NHLauncher.Views;

public partial class FadeBG : UserControl
{

    private readonly DispatcherTimer _timer;
    public FadeBG()
    {
        InitializeComponent();
        var vm = new FadeBGViewModel();
        DataContext = vm;
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(30) // √ø30∫¡√Î√Î«–ªª“ª¥Œ
        };
        _timer.Tick += OnTimerTick;
        _timer.Start();
    }
    private void Border_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            (this.Parent as Window)?.BeginMoveDrag(e);
        }
    }
    private void OnTimerTick(object? sender, EventArgs e)
    {
        var d = DataContext as FadeBGViewModel;
        d?.NextIMG();
    }
}