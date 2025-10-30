using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using NHLauncher.Other;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NHLauncher.ViewModels
{
    public partial class FadeBGViewModel : ViewModelBase
    {
        private int currentIdx = 0;
        private static List<Uri> imageUris = new();
        private static Dictionary<int, Bitmap> cache = new(); // 缓存当前/附近的图片
        private const int CacheSize = 3; // 前一张、当前、后一张

        [ObservableProperty]
        public Bitmap? imageFromBinding;

        public FadeBGViewModel()
        {
            if (imageUris.Count == 0)
            {
                imageUris = GenerateImageUris(
                    "avares://NHLauncher/Assets/LoginBG/",
                    "合成 1_",
                    4, 237,
                    ".png");
            }

            LoadImage(currentIdx); // 初始加载第一张
        }

        private List<Uri> GenerateImageUris(string basePath, string prefix, int startIndex, int endIndex, string extension)
        {
            var uris = new List<Uri>();
            for (int i = startIndex; i < endIndex; i++)
            {
                var fileName = $"{prefix}{i:D5}{extension}";
                uris.Add(new Uri($"{basePath}{fileName}"));
            }
            return uris;
        }

        private void LoadImage(int index)
        {
            if (!cache.ContainsKey(index))
            {
                var bitmap = ImageHelper.LoadFromResource(imageUris[index]);
                if (bitmap != null)
                {
                    cache[index] = bitmap;
                }
            }

            ImageFromBinding = cache[index];

            CleanupCache(index);
        }

        private void CleanupCache(int centerIndex)
        {
            var keysToKeep = new HashSet<int> { centerIndex };
            if (centerIndex > 0) keysToKeep.Add(centerIndex - 1);
            if (centerIndex < imageUris.Count - 1) keysToKeep.Add(centerIndex + 1);

            var keysToRemove = new List<int>();
            foreach (var key in cache.Keys)
            {
                if (!keysToKeep.Contains(key))
                    keysToRemove.Add(key);
            }

            foreach (var key in keysToRemove)
            {
                var bmp = cache[key];
                cache.Remove(key);

                // 延迟释放，避免 UI 渲染过程中访问空引用
                Avalonia.Threading.Dispatcher.UIThread.Post(async() =>
                {
                    await Task.Delay(1000);
                    bmp?.Dispose();
                },
                    Avalonia.Threading.DispatcherPriority.Background);
            }
        }

        public void NextIMG()
        {
            currentIdx = (currentIdx + 1) % imageUris.Count;
            LoadImage(currentIdx);
        }

        public void PrevIMG()
        {
            currentIdx = (currentIdx - 1 + imageUris.Count) % imageUris.Count;
            LoadImage(currentIdx);
        }
    }
}
