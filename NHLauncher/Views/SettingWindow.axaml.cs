using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace NHLauncher;

public partial class SettingWindow : Window
{
    public SettingWindow()
    {
        InitializeComponent();
        var vm = new ViewModels.SettingViewModel(this);
        DataContext = vm;
    }
}