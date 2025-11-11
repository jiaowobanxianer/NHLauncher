using Avalonia.Data.Converters;
using NHLauncher.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NHLauncher.Other
{
    public class LauncherButtonStateToTextConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value switch
            {
                MainViewModel.LauncherButtonState.Download => "下载",
                MainViewModel.LauncherButtonState.Launch => "启动",
                MainViewModel.LauncherButtonState.Update => "更新",
                _ => "启动"
            };
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value switch
            {
                "下载"  => MainViewModel.LauncherButtonState.Download,
                "启动"  => MainViewModel.LauncherButtonState.Launch,
                "更新"  => MainViewModel.LauncherButtonState.Update,
                _ => MainViewModel.LauncherButtonState.Launch
            };
        }
    }

}
