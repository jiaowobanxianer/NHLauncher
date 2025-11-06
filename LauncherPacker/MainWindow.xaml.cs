using LauncherHotupdate.Core;
using Microsoft.Win32;
using Newtonsoft.Json;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace LauncherPacker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static readonly RoutedCommand SaveCommand = new RoutedCommand();
        public bool upLoading = false;
        private static readonly HttpClient httpClient = new HttpClient()
        {
            MaxResponseContentBufferSize = 1024L * 1024L * 1024L,
            Timeout = System.TimeSpan.FromMinutes(10)
        };
        private static readonly int batchSize = 1;
        public MainWindow()
        {
            InitializeComponent();

            CurrentProject = new PackerProject();
            LauncherPackerSetting.GetPackerSetting();
            // 创建保存快捷键绑定
            KeyBinding saveBinding = new KeyBinding(
                SaveCommand,
                new KeyGesture(Key.S, ModifierKeys.Control)
            );
            InputBindings.Add(saveBinding);

            // 绑定命令到 SaveProject 方法
            CommandBindings.Add(new CommandBinding(SaveCommand, SaveProject));
        }
        public PackerProject CurrentProject;
        public PackerProject? LoadedProject { get; set; }

        // 按钮点击事件处理方法，用于触发文件夹选择
        private void SelectFolderButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new OpenFolderDialog
                {
                    Title = "请选择文件夹",
                    Multiselect = false // 不允许选择多个文件夹
                };

                bool? result = dialog.ShowDialog();

                if (result == true)
                {
                    FolderPathTextBox.Text = dialog.FolderName;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"选择文件夹时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CreateProject(object? sender, RoutedEventArgs? e)
        {
            try
            {
                if (CheckUnsavedChanges())
                {
                    var res = MessageBox.Show("当前有未保存的更改，是否继续？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (res == MessageBoxResult.No)
                    {
                        return;
                    }
                }

                if (string.IsNullOrWhiteSpace(FolderPathTextBox.Text))
                {
                    MessageBox.Show("请先选择项目文件夹。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var dialog = new SaveFileDialog
                {
                    Title = "保存Packer项目文件",
                    Filter = "Packer Project Files (*.packerproj)|*.packerproj",
                    FileName = "NewProject.packerproj",
                };

                bool? result = dialog.ShowDialog();

                if (result == true)
                {
                    CurrentProject.PackerProjectFilePath = dialog.FileName;
                    File.WriteAllText(dialog.FileName, JsonConvert.SerializeObject(CurrentProject));
                    MessageBox.Show("项目已成功创建并保存。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                LoadProject(CurrentProject);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"创建项目时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CheckUnsavedChanges()
        {
            if (LoadedProject == null)
            {
                return false;
            }

            return LoadedProject.ProjectPath != CurrentProject.ProjectPath ||
                   LoadedProject.ProjectRemoteUrl != CurrentProject.ProjectRemoteUrl;
        }
        private void LoadProject(PackerProject project)
        {
            CurrentProject = project;
            LoadedProject = project;
            FolderPathTextBox.Text = CurrentProject.ProjectPath;
            RemoteURLText.Text = CurrentProject.ProjectRemoteUrl;
        }
        private void OpenProject(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new OpenFileDialog
                {
                    Title = "请选择Packer项目文件",
                    Multiselect = false, // 不允许选择多个文件夹
                    Filter = "Packer Project Files (*.packerproj)|*.packerproj"
                };

                var result = dialog.ShowDialog();

                if (result == true)
                {
                    string filePath = dialog.FileName;
                    string jsonContent = File.ReadAllText(filePath);
                    var project = JsonConvert.DeserializeObject<PackerProject>(jsonContent) ?? throw new Exception("反序列化错误");
                    project.PackerProjectFilePath = filePath;
                    LoadProject(project);
                    MessageBox.Show("项目已成功打开。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开项目文件时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show($"保存项目时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GenerateManifestButton_Click(object sender, RoutedEventArgs e)
        {
            string folderPath = FolderPathTextBox.Text;

            if (string.IsNullOrWhiteSpace(folderPath))
            {
                MessageBox.Show("请先选择一个文件夹。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            try
            {
                ManifestGenerator.GenerateManifest(folderPath, Path.Combine(folderPath, "manifest.json"));
                MessageBox.Show("清单已成功生成。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"生成清单时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
            if (string.IsNullOrWhiteSpace(CurrentProject.ProjectPath))
            {
                MessageBox.Show("请先选择项目文件夹。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(CurrentProject.ProjectRemoteUrl))
            {
                MessageBox.Show("请先设置远程服务器地址。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string manifestPath = Path.Combine(CurrentProject.ProjectPath, "manifest.json");
            if (!File.Exists(manifestPath))
            {
                MessageBox.Show("manifest.json 不存在，请先生成清单。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            string currentFile = "";
            try
            {
                var setting = LauncherPackerSetting.GetPackerSetting();
                string hotUpdateApi = setting?.hotUpdateApi ?? throw new Exception("配置文件中未定义 hotUpdateApi");
                string apiKey = setting?.apiKey ?? throw new Exception("配置文件中未定义 apiKey");

                var downloader = new LauncherDownloader();
                var localManifest = downloader.LoadLocalManifest(manifestPath);
                var remoteManifest = await downloader.LoadRemoteManifestAsync(hotUpdateApi, CurrentProject.ProjectRemoteUrl);
                var differ = localManifest?.GetDifferenceFile(remoteManifest);

                if (differ == null || differ.Count == 0)
                {
                    MessageBox.Show("没有检测到差异文件，无需上传。", "信息", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                UploadButton.IsEnabled = false;
                // 差异文件 + manifest
                var filesToUpload = Directory.GetFiles(CurrentProject.ProjectPath, "*.*", SearchOption.AllDirectories)
                    .Where(f =>
                    {
                        string relPath = Path.GetRelativePath(CurrentProject.ProjectPath, f)
                                             .Replace("\\", "___").Replace("/", "___");
                        bool isManifest = Path.GetFileName(f).Equals("manifest.json", StringComparison.OrdinalIgnoreCase);
                        bool isDiffer = differ.Any(d => d.Path.Replace("\\", "___").Replace("/", "___") == relPath);
                        return isManifest || isDiffer;
                    })
                    .ToList();
                var totalFiles = filesToUpload.Count;
                var currentFileIndex = 0;

                for (int batchStart = 0; batchStart < filesToUpload.Count; batchStart += batchSize)
                {
                    var batchFiles = filesToUpload.Skip(batchStart).Take(batchSize).ToList();

                    using var content = new MultipartFormDataContent();
                    content.Add(new StringContent(apiKey), "apiKey");
                    content.Add(new StringContent(CurrentProject.ProjectRemoteUrl), "targetPath");
                    content.Add(new StringContent("Upload"), "cmd");

                    var fileStreams = new List<FileStream>();
                    try
                    {
                        foreach (var file in batchFiles)
                        {
                            string relativePath = Path.GetRelativePath(CurrentProject.ProjectPath, file)
                                                     .Replace("\\", "___").Replace("/", "___");
                            currentFile = file;
                            var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                            fileStreams.Add(fs);

                            var fileContent = new StreamContent(fs);
                            fileContent.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("form-data")
                            {
                                Name = "files",
                                FileName = relativePath // 保留你的 ___ 规则
                            };
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
                    finally
                    {
                        // 释放流
                        foreach (var fs in fileStreams)
                        {
                            try { fs.Dispose(); } catch (Exception ex) { MessageBox.Show($"{ex.Message}"); }
                        }
                    }
                }

                UploadButton.IsEnabled = true;
                MessageBox.Show($"上传完成，总文件 {filesToUpload.Count} 个，差异文件 {differ.Count} 个。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"上传过程中发生错误: {ex.Message}，文件{currentFile}处出错", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                UploadButton.IsEnabled = true;
            }
            finally
            {
                UploadButton.IsEnabled = true;
            }
        }

    }
}