using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using NHLauncher.Other;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NHLauncher.ViewModels
{
    public partial class FadeBGViewModel: ViewModelBase
    {

        private int currentIdx = 0;
        private List<Bitmap> images = new();
        [ObservableProperty]
        public Bitmap imageFromBinding;
        public FadeBGViewModel()
        {
            // 假设图片文件名是连续的，动态生成路径
            var imageUris = GenerateImageUris("avares://NHLauncher/Assets/LoginBG/", "合成 1_", 4, 237, ".png");

            // 加载所有图片
            foreach (var uri in imageUris)
            {
                var bitmap = ImageHelper.LoadFromResource(uri);
                if (bitmap != null)
                {
                    images.Add(bitmap);
                }
            }
            ImageFromBinding = images[currentIdx];
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
        public void NextIMG()
        {
            if (currentIdx < images.Count - 1)
            {
                currentIdx++;
            }
            else
                currentIdx = 0;
            ImageFromBinding = images[currentIdx];
        }
    }

}
