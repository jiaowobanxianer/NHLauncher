using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using LauncherHotupdate.Core;
using NHLauncher.Other;

namespace NHLauncher.ViewModels
{
    public partial class SettingViewModel : ViewModelBase
    {
        private LauncherSetting _setting;

        public SettingViewModel()
        {
            _setting = SettingHelper.LoadOrCreateSetting(); 
        }
        public SettingViewModel(LauncherSetting setting)
        {
            _setting = setting;  
        }
        public string ProjectId
        {
            get => _setting.ProjectId;
            set
            {
                if (_setting.ProjectId != value)
                {
                    _setting.ProjectId = value;
                    OnPropertyChanged(nameof(ProjectId));
                    OnPropertyChanged(nameof(AppName)); // AppName依赖ProjectId
                }
            }
        }

        public string Platform
        {
            get => _setting.Platform;
            set
            {
                if (_setting.Platform != value)
                {
                    _setting.Platform = value;
                    OnPropertyChanged(nameof(Platform));
                }
            }
        }

        public string AppName
        {
            get => _setting.AppName;
            set
            {
                if (_setting.AppName != value)
                {
                    _setting.AppName = value;
                    OnPropertyChanged(nameof(AppName));
                }
            }
        }

        public string ServerBaseUrl
        {
            get => _setting.ServerBaseUrl;
            set
            {
                if (_setting.ServerBaseUrl != value)
                {
                    _setting.ServerBaseUrl = value;
                    OnPropertyChanged(nameof(ServerBaseUrl));
                }
            }
        }
        public string API
        {
            get => _setting.ServerBaseUrl;
            set
            {
                if (_setting.ServerBaseUrl != value)
                {
                    _setting.ServerBaseUrl = value;
                    OnPropertyChanged(nameof(API));
                }
            }
        }
        public string LocalPath => _setting.LocalPath;
        public string ManifestFile => _setting.ManifestFile;

        public LauncherSetting GetUnderlyingSetting() => _setting;
    }
}
