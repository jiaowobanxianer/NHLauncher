using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace LauncherHotupdate.Core
{
    public class LauncherDownloader
    {
        private readonly HttpClient _httpClient;
        private const int BufferSize = 81920;

        public LauncherDownloader()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };

            _httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromHours(2)
            };
        }

        #region Manifest 加载

        public async Task<Manifest?> LoadRemoteManifestAsync(string api, string projectPath)
        {
            var form = new MultipartFormDataContent
            {
                { new StringContent(projectPath), "targetPath" },
                { new StringContent("getmanifest"), "cmd" }
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, api) { Content = form };
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"获取 manifest 失败: {await response.Content.ReadAsStringAsync()}");
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var wrapper = JsonConvert.DeserializeObject<MessageWrapper>(json);

            if (string.IsNullOrWhiteSpace(wrapper?.message) ||
                wrapper.message.Contains("该项目未上传manifest"))
                return null;

            return JsonConvert.DeserializeObject<Manifest>(wrapper.message);
        }

        public Manifest? LoadLocalManifest(string path)
        {
            return File.Exists(path)
                ? JsonConvert.DeserializeObject<Manifest>(File.ReadAllText(path))
                : null;
        }

        #endregion

        #region Manifest 比较

        public List<Manifest.FileEntry> Compare(Manifest? remote, Manifest? local)
        {
            if (remote == null) return new List<Manifest.FileEntry>();
            if (local == null) return remote.Files;

            DateTime remoteTime = ParseVersion(remote.Version, "远程");
            DateTime localTime = ParseVersion(local.Version, "本地");

            if (remoteTime <= localTime)
                return new List<Manifest.FileEntry>();

            return remote.Files
                .Where(r => !local.Files.Any(l => l.Path == r.Path && l.Hash == r.Hash))
                .ToList();
        }

        private static DateTime ParseVersion(string version, string tag)
        {
            if (DateTime.TryParseExact(version, "yyyyMMdd-HHmmss", null,
                System.Globalization.DateTimeStyles.None, out var time))
                return time;

            throw new FormatException($"{tag} manifest 版本号格式错误: {version}");
        }

        #endregion

        #region 文件下载主流程

        /// <summary>
        /// 使用 API 流下载（POST）
        /// </summary>
        public async Task DownloadFilesAsync(
            string apiUrl,
            string projectPath,
            string localPath,
            List<Manifest.FileEntry>? files,
            IProgress<double>? progress = null,
            Action<string>? callBack = null,
            CancellationToken token = default)
        {
            await DownloadInternalAsync(
                files,
                localPath,
                progress,
                callBack,
                token,
                async file =>
                {
                    var form = new MultipartFormDataContent
                    {
                        { new StringContent("download"), "cmd" },
                        { new StringContent($"{projectPath}/{file.Path}".Replace("\\", "/")), "targetPath" }
                    };
                    return await _httpClient.SendAsync(
                        new HttpRequestMessage(HttpMethod.Post, apiUrl) { Content = form },
                        HttpCompletionOption.ResponseHeadersRead,
                        token);
                });
        }

        /// <summary>
        /// 使用静态 CDN 下载（GET）
        /// </summary>
        public async Task DownloadFilesFromStaticAsync(
            string cdnBaseUrl,
            string projectPath,
            string localPath,
            List<Manifest.FileEntry>? files,
            IProgress<double>? progress = null,
            Action<string>? callBack = null,
            CancellationToken token = default)
        {
            await DownloadInternalAsync(
                files,
                localPath,
                progress,
                callBack,
                token,
                async file =>
                {
                    var url = $"{cdnBaseUrl.TrimEnd('/')}/{projectPath.TrimStart('/').TrimEnd('/')}/{file.Path.Replace("\\", "/")}";
                    return await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token);
                });
        }
        private async Task DownloadInternalAsync(
            List<Manifest.FileEntry>? files,
            string localPath,
            IProgress<double>? progress,
            Action<string>? callBack,
            CancellationToken token,
            Func<Manifest.FileEntry, Task<HttpResponseMessage>> getResponseAsync)
        {
            if (files == null || files.Count == 0)
            {
                callBack?.Invoke("c");
                return;
            }

            Directory.CreateDirectory(localPath);

            long totalSize = files.Sum(f => f.Size);
            long downloaded = 0;
            var buffer = new byte[BufferSize];

            foreach (var file in files)
            {
                string localFile = Path.Combine(localPath, file.Path);
                Directory.CreateDirectory(Path.GetDirectoryName(localFile)!);

                // ✅ 跳过已存在文件
                if (ShouldSkipFile(localFile, file))
                {
                    downloaded += file.Size;
                    progress?.Report((double)downloaded / totalSize * 100);
                    callBack?.Invoke($"s{file.Path}");
                    continue;
                }

                callBack?.Invoke($"d{file.Path}");

                var tempFile = localFile + ".tmp";

                try
                {
                    using var response = await getResponseAsync(file);

                    if (!response.IsSuccessStatusCode)
                        throw new Exception($"下载失败 ({response.StatusCode})");

                    await using var stream = await response.Content.ReadAsStreamAsync();
                    await using var fs = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, true);

                    int bytesRead;
                    long current = 0;
                    while ((bytesRead = await stream.ReadAsync(buffer.AsMemory(0, BufferSize), token)) > 0)
                    {
                        await fs.WriteAsync(buffer.AsMemory(0, bytesRead), token);
                        current += bytesRead;
                        progress?.Report((double)(downloaded + current) / totalSize * 100);
                    }

                    MoveToTargetFile(tempFile, localFile);
                }
                catch (Exception ex)
                {
                    if (File.Exists(tempFile)) File.Delete(tempFile);
                    throw new IOException($"下载 {file.Path} 出错: {ex.Message}", ex);
                }

                downloaded += file.Size;
                progress?.Report((double)downloaded / totalSize * 100);
            }

            callBack?.Invoke("c");
        }

        #endregion

        #region 工具方法

        private static bool ShouldSkipFile(string localFile, Manifest.FileEntry file)
        {
            if (!File.Exists(localFile)) return false;

            try
            {
                File.SetAttributes(localFile, FileAttributes.Normal);
                var info = new FileInfo(localFile);

                return info.Length == file.Size &&
                       ManifestGenerator.ComputeHash(localFile) == file.Hash;
            }
            catch
            {
                return false;
            }
        }

        private static void MoveToTargetFile(string tempFile, string targetFile)
        {
            if (File.Exists(targetFile))
            {
                File.SetAttributes(targetFile, FileAttributes.Normal);
                File.Delete(targetFile);
            }
            File.Move(tempFile, targetFile);
        }

        public class MessageWrapper
        {
            public string message { get; set; } = string.Empty;
        }

        #endregion
    }
}
