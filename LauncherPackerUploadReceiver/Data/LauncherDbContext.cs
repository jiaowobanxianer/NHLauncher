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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ---------------- UserAccount ----------------
            modelBuilder.Entity<UserAccount>(entity =>
            {
                entity.HasKey(u => u.Id);
                entity.Property(u => u.Id)
                      .ValueGeneratedOnAdd(); // ← 关键：告诉 EF Core 这是自增

                entity.Property(e => e.UserName)
                      .HasColumnType("varchar(255)")
                      .IsUnicode(false)
                      .HasMaxLength(255);

                entity.HasIndex(e => e.UserName)
                      .IsUnique();

                // 存储可访问项目 ID 列表为 JSON
                entity.Property(e => e.AccessibleProjectIds)
                      .HasConversion(
                          v => JsonConvert.SerializeObject(v),
                          v => JsonConvert.DeserializeObject<List<int>>(v) ?? new List<int>()
                      );
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
