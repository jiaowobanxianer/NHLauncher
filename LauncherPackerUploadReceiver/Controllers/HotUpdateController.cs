using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace LauncherPakcerUploadReceiver.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class HotUpdateController : ControllerBase
    {
        private readonly string _apiKey;

        public HotUpdateController(IOptions<UploadSettings> options)
        {
            _apiKey = options.Value.ApiKey;
        }
        [RequestSizeLimit(2_000_000_000)]
        [HttpPost]
        public async Task<IActionResult> HandleCommand(
            [FromForm] string cmd,
            [FromForm] string targetPath,
            [FromForm] List<IFormFile>? files,
            [FromForm] string? apiKey)
        {
            if (string.IsNullOrWhiteSpace(cmd))
                return BadRequest("缺少 cmd 参数");

            switch (cmd.ToLowerInvariant())
            {
                case "getmanifest":
                    return await GetProjectManifest(targetPath);

                case "upload":
                    return await Upload(targetPath, files, apiKey);

                case "download":
                    return await Download(targetPath);

                default:
                    return BadRequest($"未知命令: {cmd}");
            }
        }

        private async Task<IActionResult> GetProjectManifest(string targetPath)
        {
            if (targetPath.Contains(".."))
                return BadRequest("非法路径");

            targetPath = targetPath.Replace("\\", Path.DirectorySeparatorChar.ToString())
                                   .Replace("/", Path.DirectorySeparatorChar.ToString());

            await Task.Yield();
            var manifestFilePath = Path.Combine(GetPath(ref targetPath), "manifest.json");

            if (System.IO.File.Exists(manifestFilePath))
                return Ok(new { message = System.IO.File.ReadAllText(manifestFilePath) });

            return Ok(new { message = "该项目未上传manifest" });
        }

        private async Task<IActionResult> Upload(string targetPath, List<IFormFile>? files, string? apiKey)
        {
            if (apiKey != _apiKey)
                return Unauthorized("无权限");

            if (files == null || files.Count == 0)
                return BadRequest("没有文件上传");

            if (targetPath.Contains(".."))
                return BadRequest("非法路径");

            string basePath = GetPath(ref targetPath);

            foreach (var file in files)
            {
                string restoredPath = file.FileName.Replace("___", Path.DirectorySeparatorChar.ToString());
                string filePath = Path.Combine(basePath, restoredPath);
                string dir = Path.GetDirectoryName(filePath)!;
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                try
                {
                    using var stream = new FileStream(filePath, FileMode.Create);
                    await file.CopyToAsync(stream);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, $"保存文件 {file.FileName} 出错: {ex.Message}");
                }
            }

            float totalMB = files.Sum(f => f.Length) / 1024f / 1024f;
            Console.WriteLine($"保存了 {files.Count} 个文件到 {basePath}，总大小 {totalMB:F2} MB");

            return Ok(new
            {
                message = $"上传成功，共 {files.Count} 个文件，总大小 {totalMB:F2} MB",
                count = files.Count
            });
        }

        private async Task<IActionResult> Download(string targetPath)
        {
            if (string.IsNullOrWhiteSpace(targetPath) || targetPath.Contains(".."))
                return BadRequest("非法路径");

            targetPath = targetPath.Replace("\\", Path.DirectorySeparatorChar.ToString())
                                   .Replace("/", Path.DirectorySeparatorChar.ToString());

            string basePath = GetPath(ref targetPath);
            if (!System.IO.File.Exists(basePath))
                return NotFound("文件不存在");

            var memory = new MemoryStream();
            try
            {
                using var stream = new FileStream(basePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                await stream.CopyToAsync(memory);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"读取文件出错: {ex.Message}");
            }

            memory.Position = 0;
            string fileName = Path.GetFileName(basePath);
            return File(memory, "application/octet-stream", fileName);
        }

        private static string GetPath(ref string targetPath)
        {
            string parentDir = Directory.GetParent(Program.ContentRootPath)!.FullName;
            targetPath = targetPath.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string basePath = Path.Combine(parentDir, targetPath);

            if (!Directory.Exists(Path.GetDirectoryName(basePath)!))
                Directory.CreateDirectory(Path.GetDirectoryName(basePath)!);

            return basePath;
        }
    }

    public class UploadSettings
    {
        public string ApiKey { get; set; } = string.Empty;
    }
}
