using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using LauncherHotupdate.Core;
using NHLauncher.Other;
using NHLauncher.ViewModels;

namespace NHLauncher
{
    public partial class SettingWindow : Window
    {
        private LauncherSetting setting;
        public SettingViewModel ViewModel { get; }
        public SettingWindow()
        {
            InitializeComponent();
            setting = SettingHelper.LoadOrCreateSetting();
            ViewModel = new SettingViewModel(setting);
        }
        public SettingWindow(LauncherSetting settings)
        {
            InitializeComponent();
            //深拷贝一份设置，防止直接修改原始设置
            setting = new LauncherSetting
            {
                ProjectId = settings.ProjectId,
                Platform = settings.Platform,
                AppName = settings.AppName,
                ServerBaseUrl = settings.ServerBaseUrl
            };
            ViewModel = new SettingViewModel(settings);
            DataContext = ViewModel;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void SaveButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            // 可以在这里保存到本地文件，如果需要
            if (DataContext != null) {
                SettingHelper.SaveSetting(ViewModel.GetUnderlyingSetting()); // 保存设置到文件
            }
            Close(ViewModel); // 返回修改后的设置
        }

        private void CancelButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            //恢复
            ViewModel.Platform = setting.Platform;
            ViewModel.ProjectId = setting.ProjectId;
            ViewModel.AppName = setting.AppName;
            ViewModel.ServerBaseUrl = setting.ServerBaseUrl;

            Close(ViewModel); // 取消
        }
    }
}
