using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LauncherPacker
{
    public partial class PackerProject
    {
        public string? PackerProjectFilePath { get; set; }
        public string? ProjectPath { get; set; }
        public bool IsFree { get; set; }
        public string? ProjectRemoteUrl { get; set; }
    }
}
