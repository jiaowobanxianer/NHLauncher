using LauncherPakcerUploadReceiver.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LauncherPackerUploadReceiver.Services
{
    public class SessionCleanupService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<SessionCleanupService> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromMinutes(10);

        public SessionCleanupService(IServiceProvider services, ILogger<SessionCleanupService> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("SessionCleanupService 启动");

            // 启动时迁移数据库
            using (var scope = _services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<LauncherDbContext>();
                await db.Database.MigrateAsync(stoppingToken);
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _services.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<LauncherDbContext>();

                    var now = DateTime.UtcNow;
                    var count = await db.UserSessions
                                        .Where(s => s.ExpireUtc <= now)
                                        .ExecuteDeleteAsync(stoppingToken);

                    if (count > 0)
                        _logger.LogInformation("清理了 {Count} 个过期 session", count);
                    else
                        _logger.LogDebug("没有需要清理的 session");
                }
                catch (OperationCanceledException)
                {
                    // 停止信号，退出循环
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "SessionCleanupService 清理 session 出错");
                }

                try
                {
                    await Task.Delay(_interval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            _logger.LogInformation("SessionCleanupService 停止");
        }
    }
}
