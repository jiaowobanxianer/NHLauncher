using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NHLauncher.Other;
namespace NHLauncher.Views;

public partial class DragableBorder : Border
{
    object lo = new object(); 
    private bool _doubleTapLock = false;
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
        if (_doubleTapLock)
            return;

        _doubleTapLock = true;
        // �ӳٵ� UI �߳�ִ�У�ȷ����������ȫ����
        Dispatcher.UIThread.Post(() =>
        {
            lock (lo)
            {
                if (this.VisualRoot is not Window window)
                    return;

                window.WindowState = window.WindowState == WindowState.Maximized
                    ? WindowState.Normal
                    : WindowState.Maximized;

                e.Handled = true;
                _doubleTapLock = false;
            }
        }, DispatcherPriority.Input);
    }

}