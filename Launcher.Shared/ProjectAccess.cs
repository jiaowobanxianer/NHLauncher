using System;
using System.Collections.Generic;

namespace Launcher.Shared
{
    public class Project
    {
        public int Id { get; set; }
        public string ProjectName { get; set; } = string.Empty; // 游戏客户端名等
        public string TargetPath { get; set; } = string.Empty;  // 对应服务器路径
    }

    public class ProjectAccess
    {
        public int Id { get; set; }
        public int UserId { get; set; }

        public string ProjectName { get; set; } = string.Empty; // 例如 "GameClient"、"Launcher" 等
        public string TargetPath { get; set; } = string.Empty;  // 对应服务器上的存储路径

        public UserAccount User { get; set; } = null!;
    }
    public class UserAccount
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Salt { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // 只存项目 ID
        public ICollection<int> AccessibleProjectIds { get; set; } = new List<int>();

        public ICollection<UserSession> Sessions { get; set; } = new List<UserSession>();
    }


    public class UserSession
    {
        public int Id { get; set; }
        public string Token { get; set; } = string.Empty;
        public int UserId { get; set; }
        public UserAccount User { get; set; } = null!;
        public DateTime ExpireUtc { get; set; }
    }
}
