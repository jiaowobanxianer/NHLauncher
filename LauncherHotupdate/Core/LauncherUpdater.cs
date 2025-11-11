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
        private readonly LauncherUpdateManager? updateManager;

        public LauncherUpdater(LauncherSetting settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public LauncherUpdater(LauncherSetting settings, LauncherUpdateManager updateManager)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.updateManager = updateManager;
            _downloader = new LauncherDownloader(updateManager.HttpClient);
        }
        #region 检查更新

        public async Task<bool> HasUpdateAsync()
        {
            if (!(await IsLoggedIn()))
            {
                throw new InvalidOperationException("请先登录。");
            }
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

        public List<Manifest.FileEntry>? GetDiff(Manifest? local, Manifest? remote)
            => _downloader.Compare(remote, local);

        public async Task<Manifest?> GetRemoteManifestAsync()
        {
            if (!(await IsLoggedIn()))
            {
                throw new InvalidOperationException("请先登录。");
            }
            return await _downloader.LoadRemoteManifestAsync(_settings.API, _settings.RemotePath);
        }
        private async Task<bool> IsLoggedIn()
        {
            return updateManager == null || (await updateManager.IsLoggedIn()).Item1;
        }
        #endregion

        #region 更新流程

        /// <summary>
        /// 自动判断来源并执行更新
        /// </summary>
        public async Task UpdateAsync(
            IProgress<double>? progress = null,
            Action<string>? callBack = null,
            CancellationToken token = default)
        {
            if (!(await IsLoggedIn()))
            {
                throw new InvalidOperationException("请先登录。");
            }
            await  UpdateInternalAsync(progress, callBack, async () =>
            {
                var remote = await GetRemoteManifestAsync();
                if (remote == null)
                {
                    Console.WriteLine("未获取到远程 manifest，更新终止。");
                    return null;
                }
                return remote;
            },
            () => Task.FromResult(_downloader.LoadLocalManifest(_settings.ManifestFile))
            , token);
        }
        public async Task UpdateAllAsync(
            IProgress<double>? progress = null,
            Action<string>? callBack = null,
            CancellationToken token = default)
        {
            if (!(await IsLoggedIn()))
            {
                throw new InvalidOperationException("请先登录。");
            }
            await UpdateInternalAsync(progress, callBack,
                async () =>
                {
                    return await GetRemoteManifestAsync();
                },
                () => Task.FromResult<Manifest?>(null)
                , token);
        }


        /// <summary>
        /// 内部通用更新逻辑
        /// </summary>
        private async Task UpdateInternalAsync(
    IProgress<double>? progress,
    Action<string>? callBack,
    Func<Task<Manifest?>>? getRemote,
    Func<Task<Manifest?>>? getLocal,
    CancellationToken token)
        {
            try
            {
                if (getRemote == null)
                {
                    return; // 远程为空直接返回
                }

                var remote = await getRemote();
                if (remote == null)
                {
                    return; // 远程 manifest 为 null，直接返回
                }

                var local = getLocal != null ? await getLocal() : null;

                var diff = GetDiff(local, remote) ?? new List<Manifest.FileEntry>();

                if (diff.Count > 0)
                {
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
                }
                SaveManifest(remote);
            }
            finally
            {
                callBack?.Invoke("c");
            }
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
