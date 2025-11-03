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
        private readonly LauncherSetting _setting;
        private readonly ObservableCollection<LauncherSettingWrapper> _settings;
        private readonly MainViewModel _mainViewModel;
        private readonly LauncherSettingWrapper? _currentWrapper;

        public SettingViewModel ViewModel { get; }

        public SettingWindow()
        {
            InitializeComponent();
            _setting = new LauncherSetting();
            ViewModel = new SettingViewModel(_setting);
            DataContext = ViewModel;
        }

        public SettingWindow(ObservableCollection<LauncherSettingWrapper> settings, MainViewModel vm)
        {
            InitializeComponent();
            _setting = new LauncherSetting();
            _settings = settings;
            _mainViewModel = vm;
            ViewModel = new SettingViewModel(_setting);
            DataContext = ViewModel;
        }

        public SettingWindow(ObservableCollection<LauncherSettingWrapper> settings, LauncherSettingWrapper current, MainViewModel vm)
        {
            InitializeComponent();
            _setting = new LauncherSetting();
            _settings = settings;
            _mainViewModel = vm;
            _currentWrapper = current;

            // 复制旧配置内容
            CopySetting(current.Setting, _setting);

            ViewModel = new SettingViewModel(_setting);
            DataContext = ViewModel;
        }

        /// <summary>
        /// 从 source 复制到 target。
        /// </summary>
        private static void CopySetting(LauncherSetting source, LauncherSetting target)
        {
            target.Platform = source.Platform;
            target.ProjectId = source.ProjectId;
            target.AppName = source.AppName;
            target.ServerBaseUrl = source.ServerBaseUrl;
        }

        private void SaveButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            // 检查项目 ID 是否冲突
            if (_currentWrapper == null && _settings.Any(x => x.Setting.ProjectId == _setting.ProjectId))
            {
                _mainViewModel.LogMessages.Add("存在相同的项目ID，请修改后重试。");
                return;
            }

            var newWrapper = new LauncherSettingWrapper(
                _mainViewModel.SelectSetting,
                _mainViewModel.DeleteSetting,
                _mainViewModel.ModifySetting,
                _setting);

            if (_currentWrapper != null)
            {
                // 修改现有项：直接替换而非删除再插入
                int index = _settings.IndexOf(_currentWrapper);
                if (index >= 0)
                    _settings[index] = newWrapper;
            }
            else
            {
                // 新增项
                _settings.Add(newWrapper);
            }

            SettingHelper.SaveSetting(_settings);
            Close(ViewModel);
        }

        private void CancelButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Close(ViewModel);
        }
    }
}
