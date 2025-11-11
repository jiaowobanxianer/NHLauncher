using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using Avalonia.Controls;
using System.Threading.Tasks;
using LauncherHotupdate.Core;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Launcher.Shared;

namespace NHLauncher.ViewModels
{
    public partial class LoginViewModel : ViewModelBase
    {
        [ObservableProperty] private string? userName = "";

        [ObservableProperty] private string? password = "";
        [ObservableProperty] private bool loginInProgress;
        public ContentControl? owner;
        public Action<string>? OnLoginSuccess { get; set; }
        public Action? OnCancel { get; set; }
        public LauncherUpdateManager? manager;

        public LoginViewModel()
        {
        }

        public async Task LoginCommand()
        {
            // 这里你可以替换为真实验证逻辑，比如服务器验证
            if (string.IsNullOrWhiteSpace(UserName) || string.IsNullOrWhiteSpace(Password))
            {
                // 可以用 MessageBox 或通知提示
                var box = MessageBoxManager.GetMessageBoxStandard(
                     "错误",
                     "用户名或密码不能为空。",
                     ButtonEnum.Ok,
                     Icon.Error);
                await box.ShowAsPopupAsync(owner!);
                return;
            }
            LoginInProgress = true;
            var str = await manager!.LoginAsync(UserName!, Password!);
            LoginInProgress = false;
            if (string.IsNullOrEmpty(str.Item1) && !string.IsNullOrEmpty(str.Item2))
            {
                Console.WriteLine($"登录成功: {UserName}");
                OnLoginSuccess?.Invoke(UserName);
            }
            else
            {
                var box = MessageBoxManager.GetMessageBoxStandard(
                     "错误",
                     str.Item1 ?? "登陆失败：未知错误！",
                     ButtonEnum.Ok,
                     Icon.Error);
                await box.ShowAsPopupAsync(owner!);
            }
            (owner as Window)?.Close();
        }

        public void CancelCommand()
        {
            OnCancel?.Invoke();
            (owner as Window)?.Close();
        }
    }
}
