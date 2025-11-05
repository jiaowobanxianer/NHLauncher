using System.Diagnostics;

namespace NHLauncher.Updater
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            await Task.Delay(2000); // 等待主程序完全退出
            Console.WriteLine(args.Length);
            await Task.Delay(2000); // 等待主程序完全退出

            if (args.Length < 3)
            {
                return;
            }
            var destDir = args[0];
            var sourceDir = args[1];
            var launcherName = args[2];
            Console.WriteLine(destDir);
            Console.WriteLine(sourceDir);
            Console.WriteLine(launcherName);
            try
            {
                // 复制文件
                foreach (var filePath in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
                {
                    var relativePath = Path.GetRelativePath(sourceDir, filePath);
                    var destPath = Path.Combine(destDir, relativePath);
                    var destFileDir = Path.GetDirectoryName(destPath);
                    if (destPath.Contains("Games")) continue; //跳过Games文件夹
                    if (destPath.EndsWith("update_error.log")) continue; //跳过日志文件
                    if (!Directory.Exists(destFileDir))
                    {
                        Directory.CreateDirectory(destFileDir!);
                    }
                    File.Copy(filePath, destPath, true);
                }
            }
            catch (Exception ex)
            {
                // 记录错误日志
                var logPath = Path.Combine(destDir, "update_error.log");
                File.WriteAllText(logPath, ex.ToString());
            }

            // 删除临时更新文件夹
            try
            {
                Directory.Delete(sourceDir, true);
            }
            catch (Exception ex)
            {
                var logPath = Path.Combine(destDir, "update_error.log");
                File.WriteAllText(logPath, ex.ToString());
            }
            await Task.Delay(1000); // 确保文件操作完成
            Process.Start(Path.Combine(destDir, launcherName));
        }
    }
}
