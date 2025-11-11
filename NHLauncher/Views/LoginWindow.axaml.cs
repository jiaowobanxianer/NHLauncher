using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using NHLauncher.ViewModels;

namespace NHLauncher;

public partial class LoginWindow : Window
{
    public LoginWindow(LoginViewModel vm)
    {
        InitializeComponent();
        vm.owner = this;
        DataContext = vm;
    }
}