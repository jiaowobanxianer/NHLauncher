using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace LauncherHotupdate.Core
{
    public static class ManifestGenerator
    {
        public static Manifest GenerateManifest(string folderPath, string outputFile)
        {
            if (!Directory.Exists(folderPath))
                throw new DirectoryNotFoundException(folderPath);

            var manifest = new Manifest
            {
                Version = DateTime.Now.ToString("yyyyMMdd-HHmmss")
            };

            foreach (var file in Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(folderPath, file).Replace("\\", "/");

                manifest.Files.Add(new Manifest.FileEntry
                {
                    Path = relativePath,
                    Size = new FileInfo(file).Length,
                    Hash = ComputeHash(file)
                });
            }

            var json = JsonConvert.SerializeObject(manifest);

            File.WriteAllText(outputFile, json);
            return manifest;
        }

        public static string ComputeHash(string filePath)
        {
            using var sha256 = SHA256.Create();
            using var stream = File.OpenRead(filePath);
            var hash = sha256.ComputeHash(stream);
            return string.Concat(hash.Select(b => b.ToString("X2")));
        }
    }
}
