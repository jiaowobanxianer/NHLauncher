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
            //���һ�����ã���ֱֹ���޸�ԭʼ����
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
            // ���������ﱣ�浽�����ļ��������Ҫ
            if (DataContext != null) {
                SettingHelper.SaveSetting(ViewModel.GetUnderlyingSetting()); // �������õ��ļ�
            }
            Close(ViewModel); // �����޸ĺ������
        }

        private void CancelButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            //�ָ�
            ViewModel.Platform = setting.Platform;
            ViewModel.ProjectId = setting.ProjectId;
            ViewModel.AppName = setting.AppName;
            ViewModel.ServerBaseUrl = setting.ServerBaseUrl;

            Close(ViewModel); // ȡ��
        }
    }
}
