using LauncherPakcerUploadReceiver.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LauncherPakcerUploadReceiver
{
    // 设计时 DbContext 工厂，用于 EF CLI 生成迁移
    public class LauncherDbContextFactory : IDesignTimeDbContextFactory<LauncherDbContext>
    {
        public LauncherDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<LauncherDbContext>();

            // 临时使用 SQLite 文件，本地可生成迁移
            optionsBuilder.UseSqlite("Data Source=design_time.db");

            return new LauncherDbContext(optionsBuilder.Options);
        }
    }
}
