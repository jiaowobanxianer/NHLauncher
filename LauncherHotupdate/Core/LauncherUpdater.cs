using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LauncherHotupdate.Core
{
    public class LauncherUpdater
    {
        private readonly LauncherSetting _settings;
        private readonly LauncherDownloader _downloader = new LauncherDownloader();

        public LauncherUpdater(LauncherSetting settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        #region 检查更新

        public async Task<bool> HasUpdateAsync()
        {
            var remote = await GetRemoteManifestAsync();
            if (remote == null) return false;

            var local = _downloader.LoadLocalManifest(_settings.ManifestFile);
            var diff = _downloader.Compare(remote, local);
            return diff?.Count > 0;
        }

        public bool HasUpdate(Manifest local, Manifest remote)
            => _downloader.Compare(remote, local)?.Count > 0;

        public bool HasUpdate(Manifest? local, Manifest? remote, out List<Manifest.FileEntry>? diff)
        {
            diff = _downloader.Compare(remote, local);
            return diff?.Count > 0;
        }

        public List<Manifest.FileEntry>? GetDiff(Manifest local, Manifest remote)
            => _downloader.Compare(remote, local);

        public async Task<Manifest?> GetRemoteManifestAsync()
            => await _downloader.LoadRemoteManifestAsync(_settings.API, _settings.RemotePath);

        #endregion

        #region 更新流程

        /// <summary>
        /// 自动判断来源并执行更新
        /// </summary>
        public Task UpdateAsync(
            IProgress<double>? progress = null,
            Action<string>? callBack = null,
            CancellationToken token = default)
            => UpdateInternalAsync(progress, callBack, token);


        /// <summary>
        /// 内部通用更新逻辑
        /// </summary>
        private async Task UpdateInternalAsync(
            IProgress<double>? progress,
            Action<string>? callBack,
            CancellationToken token)
        {
            var remote = await GetRemoteManifestAsync();
            if (remote == null)
            {
                Console.WriteLine("未获取到远程 manifest，更新终止。");
                return;
            }

            var local = _downloader.LoadLocalManifest(_settings.ManifestFile);
            if (!HasUpdate(local, remote, out var diff) || diff == null || diff.Count == 0)
            {
                callBack?.Invoke("c");
                Console.WriteLine("暂无更新。");
                return;
            }

            // 执行文件下载
            switch (_settings.UseCdn)
            {
                case false:
                    await _downloader.DownloadFilesAsync(
                        _settings.API,
                        _settings.RemotePath,
                        _settings.LocalPath,
                        diff,
                        progress,
                        callBack,
                        token);
                    break;

                case true:
                    await _downloader.DownloadFilesFromStaticAsync(
                        _settings.ServerBaseUrl,
                        _settings.RemotePath,
                        _settings.LocalPath,
                        diff,
                        progress,
                        callBack,
                        token);
                    break;
            }

            // 保存新 manifest
            SaveManifest(remote);
        }

        #endregion

        #region 辅助方法

        private void SaveManifest(Manifest manifest)
        {
            try
            {
                var dir = Path.GetDirectoryName(_settings.ManifestFile);
                if (string.IsNullOrEmpty(dir)) return;

                Directory.CreateDirectory(dir);
                var json = JsonConvert.SerializeObject(manifest, Formatting.Indented);
                File.WriteAllText(_settings.ManifestFile, json, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"保存 manifest 失败：{ex.Message}");
            }
        }

        private enum UpdateSource
        {
            API,
            CDN
        }

        #endregion
    }
}
