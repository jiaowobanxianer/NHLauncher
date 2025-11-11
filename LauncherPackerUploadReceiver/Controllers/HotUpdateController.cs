using Launcher.Shared;
using LauncherPakcerUploadReceiver.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LauncherPakcerUploadReceiver.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class HotUpdateController : ControllerBase
    {
        private readonly string _apiKey;
        private readonly LauncherDbContext _db;

        public HotUpdateController(IOptions<UploadSettings> options, LauncherDbContext db)
        {
            _apiKey = options.Value.ApiKey;
            _db = db;
        }

        [RequestSizeLimit(2_000_000_000)]
        [HttpPost]
        public async Task<IActionResult> HandleCommand(
            [FromForm] string cmd,
            [FromForm] string? targetPath,
            [FromForm] List<IFormFile>? files,
            [FromForm] string? apiKey,
            [FromForm] string? userName,
            [FromForm] string? password,
            [FromForm] string? projectNames,
            [FromForm] string? isFree)
        {
            if (string.IsNullOrWhiteSpace(cmd))
                return BadRequest("缺少 cmd 参数");

            return cmd.ToLowerInvariant() switch
            {
                "register" => await HandleRegisterAsync(userName, password, apiKey),
                "login" => await HandleLoginAsync(userName, password),
                "logout" => await HandleLogoutAsync(),
                "verify" => await HandleVerifyAsync(),
                "getprojects" => await HandleGetProjectsAsync(),
                "givepower" => await HandleGivePowerAsync(userName, projectNames, apiKey),
                "getmanifest" => await GetProjectManifestAsync(targetPath ?? "", apiKey ?? ""),
                "upload" => await UploadAsync(targetPath ?? "", files, apiKey, isFree ?? ""),
                "download" => await DownloadAsync(targetPath ?? ""),
                _ => BadRequest($"未知命令: {cmd}")
            };
        }

        #region 用户注册 / 登录 / 权限

        private async Task<IActionResult> HandleRegisterAsync(string? userName, string? password, string? apiKey)
        {
            if (apiKey != _apiKey)
                return Unauthorized("无权限注册");

            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
                return BadRequest("用户名或密码不能为空");

            if (await _db.Users.AnyAsync(u => u.UserName == userName))
                return Conflict("用户名已存在");

            var (serverHash, salt) = PasswordHelper.HashPassword(password);

            var user = new UserAccount
            {
                UserName = userName,
                PasswordHash = serverHash,
                Salt = salt,
                CreatedAt = DateTime.UtcNow
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            return Ok(new { message = "注册成功" });
        }

        private async Task<IActionResult> HandleLoginAsync(string? userName, string? clientHash)
        {
            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(clientHash))
                return BadRequest("用户名或密码不能为空");

            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserName == userName);
            if (user == null) return Unauthorized("用户名不存在");
            if (!PasswordHelper.VerifyPassword(clientHash, user.PasswordHash, user.Salt))
                return Unauthorized("用户名或密码错误");

            string token = Guid.NewGuid().ToString("N");
            var expire = DateTime.UtcNow.AddDays(7);

            _db.UserSessions.Add(new UserSession { Token = token, UserId = user.Id, ExpireUtc = expire });
            await _db.SaveChangesAsync();

            Response.Cookies.Append("LauncherAuth", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = false,
                SameSite = SameSiteMode.Strict,
                Expires = expire
            });

            return Ok(new { message = "登录成功", user = userName, expireUtc = expire });
        }

        private async Task<IActionResult> HandleGivePowerAsync(string? userName, string? projectNames, string? apiKey)
        {
            if (apiKey != _apiKey) return Unauthorized("无权限操作");
            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(projectNames))
                return BadRequest("用户名或项目名不能为空");

            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserName == userName);
            if (user == null) return NotFound("用户不存在");

            var projects = projectNames.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var projectName in projects)
            {
                var project = await _db.Projects.FirstOrDefaultAsync(p => p.ProjectName == projectName);
                if (project == null) return NotFound($"找不到项目 {projectName}");

                user.AccessibleProjectIds ??= new List<int>();
                if (!user.AccessibleProjectIds.Contains(project.Id))
                    user.AccessibleProjectIds.Add(project.Id);
            }

            _db.Users.Update(user);
            await _db.SaveChangesAsync();

            return Ok(new { message = $"成功给用户 {userName} 添加项目权限", project = projectNames });
        }

        private async Task<IActionResult> HandleGetProjectsAsync()
        {
            var user = await GetUserFromTokenAsync();
            if (user == null) return Unauthorized("未登录或登录已过期");

            var projects = await _db.Projects
                .Where(p => user.AccessibleProjectIds.Contains(p.Id))
                .Select(p => new { p.Id, p.ProjectName, p.TargetPath })
                .ToListAsync();

            return Ok(new { message = projects.Count > 0 ? $"共 {projects.Count} 个项目" : "该用户暂无项目", projects });
        }

        private async Task<IActionResult> HandleLogoutAsync()
        {
            if (Request.Cookies.TryGetValue("LauncherAuth", out var token))
            {
                var session = await _db.UserSessions.FirstOrDefaultAsync(s => s.Token == token);
                if (session != null) _db.UserSessions.Remove(session);
                Response.Cookies.Delete("LauncherAuth");
                await _db.SaveChangesAsync();
            }
            return Ok(new { message = "已登出" });
        }

        private async Task<IActionResult> HandleVerifyAsync()
        {
            var user = await GetUserFromTokenAsync();
            return user != null ? Ok(new { valid = true, user = user.UserName }) : Unauthorized(new { valid = false });
        }

        private async Task<UserAccount?> GetUserFromTokenAsync()
        {
            if (!Request.Cookies.TryGetValue("LauncherAuth", out var token)) return null;

            var session = await _db.UserSessions.Include(s => s.User).FirstOrDefaultAsync(s => s.Token == token);
            if (session == null || session.ExpireUtc <= DateTime.UtcNow)
            {
                if (session != null) _db.UserSessions.Remove(session);
                await _db.SaveChangesAsync();
                return null;
            }

            return session.User;
        }

        #endregion

        #region 文件操作

        private async Task<IActionResult> GetProjectManifestAsync(string targetPath, string apiKey = "")
        {
            if (apiKey != _apiKey)
            {
                var user = await GetUserFromTokenAsync();
                if (user == null) return Unauthorized("未登录或登录已过期");
                if (!(await IsUserAuthorizedForPathAsync(user, targetPath))) return Forbid("无权访问该项目");
            }

            if (targetPath.Contains("..")) return BadRequest("非法路径");

            await Task.Yield();
            string manifestFilePath = Path.Combine(GetPath(ref targetPath), "manifest.json");
            if (System.IO.File.Exists(manifestFilePath))
                return Ok(new { message = System.IO.File.ReadAllText(manifestFilePath) });

            return Ok(new { message = "该项目未上传 manifest" });
        }

        private async Task<IActionResult> UploadAsync(string targetPath, List<IFormFile>? files, string? apiKey, string? isFree)
        {
            if (apiKey != _apiKey) return Unauthorized("无权限");
            if (files == null || files.Count == 0) return BadRequest("没有文件上传");
            if (targetPath.Contains("..")) return BadRequest("非法路径");

            string basePath = GetPath(ref targetPath);

            foreach (var file in files)
            {
                string restoredPath = file.FileName.Replace("___", Path.DirectorySeparatorChar.ToString());
                string filePath = Path.Combine(basePath, restoredPath);
                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

                using var stream = new FileStream(filePath, FileMode.Create);
                await file.CopyToAsync(stream);
            }

            var free = string.IsNullOrEmpty(isFree);
            // 注册项目
            var project = await _db.Projects.FirstOrDefaultAsync(p => p.TargetPath == targetPath);
            if (project == null)
            {
                var projectName = targetPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).First();
                project = new Project { ProjectName = projectName, TargetPath = targetPath, IsFreeAccess = free };
                _db.Projects.Add(project);
            }
            else
            {
                // 如果项目已存在则允许更新是否免费
                project.IsFreeAccess = free;
                _db.Projects.Update(project);
            }
            await _db.SaveChangesAsync();

            float totalMB = files.Sum(f => f.Length) / 1024f / 1024f;
            return Ok(new { message = $"上传成功，共 {files.Count} 个文件，总大小 {totalMB:F2} MB", count = files.Count });
        }

        private async Task<IActionResult> DownloadAsync(string targetPath)
        {
            var user = await GetUserFromTokenAsync();
            if (user == null) return Unauthorized("未登录或登录已过期");
            if (!await IsUserAuthorizedForPathAsync(user, targetPath)) return Forbid("无权访问该项目");

            if (string.IsNullOrWhiteSpace(targetPath) || targetPath.Contains("..")) return BadRequest("非法路径");

            targetPath = targetPath.Replace("\\", Path.DirectorySeparatorChar.ToString())
                                   .Replace("/", Path.DirectorySeparatorChar.ToString());

            string filePath = GetPath(ref targetPath);
            if (!System.IO.File.Exists(filePath)) return NotFound("文件不存在");

            var memory = new MemoryStream();
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                await stream.CopyToAsync(memory);
            }

            memory.Position = 0;
            return File(memory, "application/octet-stream", Path.GetFileName(filePath));
        }

        private async Task<bool> IsUserAuthorizedForPathAsync(UserAccount user, string targetPath)
        {
            targetPath = targetPath
                .Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar)
                .TrimStart(Path.DirectorySeparatorChar);

            // 免费项目任何人都能访问
            var freeProjects = await _db.Projects
                .Where(p => p.IsFreeAccess)
                .ToListAsync();

            if (freeProjects.Any(p => targetPath.StartsWith(
                    p.TargetPath.Replace('/', Path.DirectorySeparatorChar)
                                .Replace('\\', Path.DirectorySeparatorChar)
                                .TrimStart(Path.DirectorySeparatorChar),
                    StringComparison.OrdinalIgnoreCase)))
                return true;

            if (user.AccessibleProjectIds == null || user.AccessibleProjectIds.Count == 0)
                return false;

            var projects = await _db.Projects
                .Where(p => user.AccessibleProjectIds.Contains(p.Id))
                .ToListAsync();

            return projects.Any(p =>
                targetPath.StartsWith(
                    p.TargetPath.Replace('/', Path.DirectorySeparatorChar)
                                .Replace('\\', Path.DirectorySeparatorChar)
                                .TrimStart(Path.DirectorySeparatorChar),
                    StringComparison.OrdinalIgnoreCase));
        }


        private static string GetPath(ref string targetPath)
        {
            string parentDir = Directory.GetParent(Program.ContentRootPath!)!.FullName;
            targetPath = targetPath.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string basePath = Path.Combine(parentDir, targetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(basePath)!);
            return basePath;
        }

        #endregion
    }

    public class UploadSettings
    {
        public string ApiKey { get; set; } = string.Empty;
    }
}
