using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using LauncherHotupdate.Core;
using NHLauncher.Other;
using NHLauncher.ViewModels;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace NHLauncher
{
    public partial class SettingWindow : Window
    {
        private LauncherSetting setting;
        private ObservableCollection<LauncherSettingWrapper> settings;
        private MainViewModel _mainViewModel;
        public SettingViewModel ViewModel { get; }
        public SettingWindow()
        {
            InitializeComponent();
            setting = new LauncherSetting();
            ViewModel = new SettingViewModel(setting);
        }
        public SettingWindow(ObservableCollection<LauncherSettingWrapper> settings, MainViewModel vm)
        {
            InitializeComponent();
            //���һ�����ã���ֱֹ���޸�ԭʼ����
            this.settings = settings;
            setting = new LauncherSetting();
            _mainViewModel = vm;
            ViewModel = new SettingViewModel(setting);
            DataContext = ViewModel;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void SaveButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            // ���������ﱣ�浽�����ļ��������Ҫ
            if(settings.Any(x=>x.Setting.ProjectId == setting.ProjectId))
            {
                return;
            }
            settings.Add(new LauncherSettingWrapper(_mainViewModel.SelectSetting, _mainViewModel.DeleteSetting, setting));
            if (DataContext != null)
            {
                SettingHelper.SaveSetting(settings); // �������õ��ļ�
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
