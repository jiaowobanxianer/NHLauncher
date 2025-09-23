using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using NHLauncher.Other;
namespace NHLauncher.Views;

public partial class DragableBorder : Border
{
    public DragableBorder()
    {
        InitializeComponent();
        DoubleTapped += OnDoubleTapped;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            this.GetWindow()?.BeginMoveDrag(e);
        }
    }
    private void OnDoubleTapped(object? sender, RoutedEventArgs e)
    {
        var window = this.GetWindow();
        if (window == null)
            return;

        // 双击切换最大化/正常
        window.WindowState = window.WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }
}