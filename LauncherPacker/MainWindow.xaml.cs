using Launcher.Shared;
using LauncherHotupdate.Core;
using Microsoft.Win32;
using Newtonsoft.Json;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Collections.Generic;

namespace LauncherPacker
{
    public partial class MainWindow : Window
    {
        public static readonly RoutedCommand SaveCommand = new RoutedCommand();
        private LauncherUpdateManager manager;
        public PackerProject CurrentProject;
        public PackerProject? LoadedProject { get; set; }
        private bool upLoading = false;

        private static readonly HttpClient httpClient = new HttpClient()
        {
            MaxResponseContentBufferSize = 1024L * 1024L * 1024L,
            Timeout = System.TimeSpan.FromMinutes(10)
        };

        private const int batchSize = 1;

        public MainWindow()
        {
            InitializeComponent();
            CurrentProject = new PackerProject();
            LauncherPackerSetting.GetPackerSetting();

            // 快捷键绑定
            InputBindings.Add(new KeyBinding(SaveCommand, new KeyGesture(Key.S, ModifierKeys.Control)));
            CommandBindings.Add(new CommandBinding(SaveCommand, SaveProject));

            manager = new LauncherUpdateManager();
            Change2Upload(null, null);
            
        }

        #region 文件夹与项目操作

        private void SelectFolderButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new OpenFolderDialog
                {
                    Title = "请选择文件夹",
                    Multiselect = false
                };

                if (dialog.ShowDialog() == true)
                {
                    FolderPathTextBox.Text = dialog.FolderName;
                }
            }
            catch (Exception ex)
            {
                ShowError("选择文件夹时出错", ex);
            }
        }

        private void CreateProject(object? sender, RoutedEventArgs? e)
        {
            try
            {
                if (CheckUnsavedChanges() && MessageBox.Show("当前有未保存的更改，是否继续？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
                    return;

                if (string.IsNullOrWhiteSpace(FolderPathTextBox.Text))
                {
                    MessageBox.Show("请先选择项目文件夹。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var dialog = new SaveFileDialog
                {
                    Title = "保存Packer项目文件",
                    Filter = "Packer Project Files (*.packerproj)|*.packerproj",
                    FileName = "NewProject.packerproj"
                };

                if (dialog.ShowDialog() == true)
                {
                    CurrentProject.PackerProjectFilePath = dialog.FileName;
                    File.WriteAllText(dialog.FileName, JsonConvert.SerializeObject(CurrentProject));
                    MessageBox.Show("项目已成功创建并保存。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadProject(CurrentProject);
                }
            }
            catch (Exception ex)
            {
                ShowError("创建项目时出错", ex);
            }
        }

        private bool CheckUnsavedChanges()
        {
            return LoadedProject != null &&
                   (LoadedProject.ProjectPath != CurrentProject.ProjectPath ||
                    LoadedProject.ProjectRemoteUrl != CurrentProject.ProjectRemoteUrl);
        }

        private void LoadProject(PackerProject project)
        {
            CurrentProject = project;
            LoadedProject = project;
            FolderPathTextBox.Text = project.ProjectPath;
            RemoteURLText.Text = project.ProjectRemoteUrl;
        }

        private void OpenProject(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new OpenFileDialog
                {
                    Title = "请选择Packer项目文件",
                    Multiselect = false,
                    Filter = "Packer Project Files (*.packerproj)|*.packerproj"
                };

                if (dialog.ShowDialog() == true)
                {
                    string filePath = dialog.FileName;
                    var project = JsonConvert.DeserializeObject<PackerProject>(File.ReadAllText(filePath))
                                  ?? throw new Exception("反序列化错误");
                    project.PackerProjectFilePath = filePath;
                    LoadProject(project);
                    MessageBox.Show("项目已成功打开。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                ShowError("打开项目文件时出错", ex);
            }
        }

        private void SaveProject(object? sender, RoutedEventArgs? e)
        {
            try
            {
                if (string.IsNullOrEmpty(FolderPathTextBox.Text))
                {
                    MessageBox.Show("请先选择项目文件夹。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (LoadedProject == null)
                {
                    CreateProject(null, null);
                }
                else
                {
                    File.WriteAllText(CurrentProject.PackerProjectFilePath ?? "", JsonConvert.SerializeObject(CurrentProject));
                    MessageBox.Show("项目已成功保存。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                ShowError("保存项目时出错", ex);
            }
        }

        #endregion

        #region 界面切换

        private void HideAll()
        {
            Power.Visibility = Visibility.Hidden;
            Register.Visibility = Visibility.Hidden;
            Uploader.Visibility = Visibility.Hidden;
        }

        private void Change2Upload(object? sender, RoutedEventArgs? e)
        {
            HideAll();
            Uploader.Visibility = Visibility.Visible;
            FuncNameText.Text = "上传";
        }

        private void Change2Power(object? sender, RoutedEventArgs? e)
        {
            HideAll();
            Power.Visibility = Visibility.Visible;
            FuncNameText.Text = "赋权";
        }

        private void Change2Register(object? sender, RoutedEventArgs? e)
        {
            HideAll();
            Register.Visibility = Visibility.Visible;
            FuncNameText.Text = "注册";
        }

        #endregion

        #region 用户操作 (注册/授权)

        private async void GivePower(object sender, RoutedEventArgs e)
        {
            string hotUpdateApi, apiKey;
            GetConfig(out hotUpdateApi, out apiKey);

            var projectNames = ProjectNameText.Text?.Trim();
            var userName = UserNameText.Text?.Trim();

            if (string.IsNullOrWhiteSpace(projectNames) || string.IsNullOrWhiteSpace(userName))
            {
                MessageBox.Show("用户名或项目名不能为空！");
                return;
            }

            var form = new MultipartFormDataContent
            {
                { new StringContent("givepower"), "cmd" },
                { new StringContent(userName), "userName" },
                { new StringContent(projectNames), "projectNames" },
                { new StringContent(apiKey), "apiKey" }
            };

            await SendPostRequest(hotUpdateApi, form, $"成功给用户 {userName} 添加项目 {projectNames}");
        }

        private async void RegisterCommand(object sender, RoutedEventArgs e)
        {
            string hotUpdateApi, apiKey;
            GetConfig(out hotUpdateApi, out apiKey);

            var userName = UserNameRegisterText.Text?.Trim();
            var password = PasswordText?.Text?.Trim();

            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("用户名或密码不能为空！");
                return;
            }

            var clientHash = PasswordHelper.HashPasswordClient(password);

            var form = new MultipartFormDataContent
            {
                { new StringContent("register"), "cmd" },
                { new StringContent(userName), "userName" },
                { new StringContent(clientHash), "password" },
                { new StringContent(apiKey), "apiKey" }
            };

            await SendPostRequest(hotUpdateApi, form, $"注册成功！用户名：{userName}");
        }

        private async Task SendPostRequest(string url, MultipartFormDataContent form, string successMessage)
        {
            try
            {
                var response = await httpClient.PostAsync(url, form);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show(successMessage);
                }
                else
                {
                    MessageBox.Show($"操作失败：{content}");
                }
            }
            catch (Exception ex)
            {
                ShowError("请求异常", ex);
            }
        }

        #endregion

        #region Manifest & Upload

        private void GenerateManifestButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(FolderPathTextBox.Text))
                {
                    MessageBox.Show("请先选择一个文件夹。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                ManifestGenerator.GenerateManifest(FolderPathTextBox.Text, Path.Combine(FolderPathTextBox.Text, "manifest.json"));
                MessageBox.Show("清单已成功生成。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ShowError("生成清单时出错", ex);
            }
        }

        private void FolderPathTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            CurrentProject.ProjectPath = FolderPathTextBox.Text;
        }

        private void RemoteURLText_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            CurrentProject.ProjectRemoteUrl = RemoteURLText.Text;
        }

        private async void Upload(object sender, RoutedEventArgs e)
        {
            if (!ValidateUpload(out string manifestPath)) return;
            if(string.IsNullOrEmpty(CurrentProject.ProjectRemoteUrl) || string.IsNullOrEmpty(CurrentProject.ProjectPath))
            {
                MessageBox.Show("请输入远程路径和项目路径", "信息", MessageBoxButton.OK, MessageBoxImage.Information);
            }    
            string currentFile = "";
            try
            {
                GetConfig(out var hotUpdateApi, out var apiKey);

                var downloader = new LauncherDownloader();
                var localManifest = downloader.LoadLocalManifest(manifestPath);
                var remoteManifest = await downloader.LoadRemoteManifestAsync(hotUpdateApi, CurrentProject.ProjectRemoteUrl!, apiKey);

                var differ = localManifest?.GetDifferenceFile(remoteManifest);
                if (differ == null || differ.Count == 0)
                {
                    MessageBox.Show("没有检测到差异文件，无需上传。", "信息", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                UploadButton.IsEnabled = false;

                var filesToUpload = Directory.GetFiles(CurrentProject.ProjectPath!, "*.*", SearchOption.AllDirectories)
                    .Where(f =>
                    {
                        string relPath = Path.GetRelativePath(CurrentProject.ProjectPath!, f).Replace("\\", "___");
                        return Path.GetFileName(f).Equals("manifest.json", System.StringComparison.OrdinalIgnoreCase) ||
                               differ.Any(d => d.Path.Replace("\\", "___") == relPath);
                    })
                    .ToList();

                int totalFiles = filesToUpload.Count, currentFileIndex = 0;

                for (int batchStart = 0; batchStart < filesToUpload.Count; batchStart += batchSize)
                {
                    var batchFiles = filesToUpload.Skip(batchStart).Take(batchSize).ToList();
                    using var content = new MultipartFormDataContent();
                    content.Add(new StringContent(apiKey), "apiKey");
                    content.Add(new StringContent(CurrentProject.ProjectRemoteUrl!), "targetPath");
                    content.Add(new StringContent("Upload"), "cmd");
                    content.Add(new StringContent((IsFreeCheckBox.IsChecked ?? false) ? "" : "123"), "isFree");

                    using var fileStreams = new DisposableList<FileStream>();
                    foreach (var file in batchFiles)
                    {
                        string relativePath = Path.GetRelativePath(CurrentProject.ProjectPath!, file).Replace("\\", "___");
                        currentFile = file;
                        var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        fileStreams.Add(fs);

                        var fileContent = new StreamContent(fs);
                        fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data") { Name = "files", FileName = relativePath };
                        content.Add(fileContent);
                    }

                    var response = await httpClient.PostAsync(hotUpdateApi, content);
                    string respMsg = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        MessageBox.Show($"文件上传失败: {response.ReasonPhrase}\n{respMsg}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    currentFileIndex += batchFiles.Count;
                    var percent = MathF.Ceiling((float)currentFileIndex / totalFiles * 100);
                    progressBar.Value = percent;
                    ProgressTextBlock.Text = $"上传进度：{percent}%      文件:{currentFileIndex}/{totalFiles}";
                }

                MessageBox.Show($"上传完成，总文件 {filesToUpload.Count} 个，差异文件 {differ.Count} 个。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (System.Exception ex)
            {
                ShowError($"上传过程中发生错误，文件{currentFile}处出错", ex);
            }
            finally
            {
                UploadButton.IsEnabled = true;
            }
        }

        private bool ValidateUpload(out string manifestPath)
        {
            manifestPath = Path.Combine(CurrentProject.ProjectPath!, "manifest.json");
            if (string.IsNullOrWhiteSpace(CurrentProject.ProjectPath))
            {
                MessageBox.Show("请先选择项目文件夹。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (string.IsNullOrWhiteSpace(CurrentProject.ProjectRemoteUrl))
            {
                MessageBox.Show("请先设置远程服务器地址。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (!File.Exists(manifestPath))
            {
                MessageBox.Show("manifest.json 不存在，请先生成清单。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            return true;
        }

        #endregion

        #region 辅助方法

        private static void GetConfig(out string hotUpdateApi, out string apiKey)
        {
            var setting = LauncherPackerSetting.GetPackerSetting();
            hotUpdateApi = setting?.hotUpdateApi ?? throw new System.Exception("配置文件中未定义 hotUpdateApi");
            apiKey = setting?.apiKey ?? throw new System.Exception("配置文件中未定义 apiKey");
        }

        private static void ShowError(string message, System.Exception ex)
        {
            MessageBox.Show($"{message}: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private class DisposableList<T> : List<T>, System.IDisposable where T : System.IDisposable
        {
            public void Dispose()
            {
                foreach (var item in this) item.Dispose();
                Clear();
            }
        }

        #endregion
    }
}
