using System;
using System.Collections.Generic;
using System.Text;

namespace LauncherHotupdate.Core
{
    public class Manifest
    {
        public string Version { get; set; } = string.Empty;
        public List<FileEntry> Files { get; set; } = new List<FileEntry>();

        public class FileEntry
        {
            public string Path { get; set; } = string.Empty;
            public long Size { get; set; }
            public string Hash { get; set; } = string.Empty;
        }
        public List<FileEntry> GetDifferenceFile(Manifest? other)
        {
            if (other == null)
            {
                return new List<FileEntry>(Files);
            }
            var diff = new List<FileEntry>();
            var otherFilesDict = new Dictionary<string, FileEntry>();
            foreach (var file in other.Files)
            {
                otherFilesDict[file.Path] = file;
            }
            foreach (var file in Files)
            {
                if (!otherFilesDict.TryGetValue(file.Path, out var otherFile) || otherFile.Hash != file.Hash)
                {
                    diff.Add(file);
                }
            }
            return diff;
        }
        public List<string> GetDifferenceFilePath(Manifest? other)
        {
            return GetDifferenceFile(other).ConvertAll(x => x.Path);
        }
    }
}
