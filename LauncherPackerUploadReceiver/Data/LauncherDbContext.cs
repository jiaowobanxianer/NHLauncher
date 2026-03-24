using Launcher.Shared;
using LauncherPakcerUploadReceiver.Controllers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Newtonsoft.Json;

namespace LauncherPakcerUploadReceiver.Data
{
    public class LauncherDbContext : DbContext
    {
        public DbSet<UserAccount> Users { get; set; } = null!;
        public DbSet<UserSession> UserSessions { get; set; } = null!;
        public DbSet<Project> Projects { get; set; } = null!; // 新增项目表

        public LauncherDbContext(DbContextOptions<LauncherDbContext> options)
            : base(options) { }
        // ⬇️ 添加这个方法，为 EF 设计时提供后备方案
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // 这里硬编码一个连接字符串，仅用于本地生成迁移脚本
                // 版本号请根据你的服务器 MySQL 实际版本填写（5.7 或 8.0）
                var connectionString = "Server=127.0.0.1;Database=launcherdb;User=launcherdb;Password=fdf5808f0cfe025a;Charset=utf8mb4;SslMode=None;";
                var serverVersion = new MySqlServerVersion(new Version(5, 7, 44));

                optionsBuilder.UseMySql(connectionString, serverVersion);
            }
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ---------------- UserAccount ----------------
            modelBuilder.Entity<UserAccount>(entity =>
            {
                entity.HasKey(u => u.Id);
                entity.Property(u => u.Id).ValueGeneratedOnAdd(); // 确保这行紧跟主键定义

                entity.Property(e => e.UserName)
                    .HasColumnType("varchar(255)")
                    .HasMaxLength(255)
                    .IsRequired();

                entity.HasIndex(e => e.UserName).IsUnique();

                // 统一配置 AccessibleProjectIds
                var comparer = new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<ICollection<int>>(
                    (c1, c2) => (c1 == null && c2 == null) || (c1 != null && c2 != null && c1.SequenceEqual(c2)),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToList());

                entity.Property(e => e.AccessibleProjectIds)
                    .HasConversion(
                        v => JsonConvert.SerializeObject(v),
                        v => JsonConvert.DeserializeObject<List<int>>(v) ?? new List<int>()
                    )
                    .Metadata.SetValueComparer(comparer);
            });

            // ---------------- UserSession ----------------
            modelBuilder.Entity<UserSession>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id)
          .ValueGeneratedOnAdd(); // 关键：自增
                entity.HasOne(s => s.User)
                      .WithMany(u => u.Sessions)
                      .HasForeignKey(s => s.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ---------------- Project ----------------
            modelBuilder.Entity<Project>(entity =>
            {
                entity.HasKey(p => p.Id); // 明确主键

                entity.Property(p => p.Id)
                      .ValueGeneratedOnAdd();

                entity.Property(p => p.ProjectName)
                      .HasColumnType("varchar(255)")
                      .IsUnicode(false)
                      .HasMaxLength(255)
                      .IsRequired();

                entity.Property(p => p.TargetPath)
                      .HasColumnType("varchar(500)")
                      .IsUnicode(false)
                      .IsRequired();
            });


            base.OnModelCreating(modelBuilder);
        }
    }
}
