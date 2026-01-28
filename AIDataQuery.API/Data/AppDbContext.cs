using Microsoft.EntityFrameworkCore;
using AIDataQuery.API.Models.Entities;

namespace AIDataQuery.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Platform> Platforms => Set<Platform>();
    public DbSet<DatabaseConnection> DatabaseConnections => Set<DatabaseConnection>();
    public DbSet<UserPlatformPermission> UserPlatformPermissions => Set<UserPlatformPermission>();
    public DbSet<UserConnectionPermission> UserConnectionPermissions => Set<UserConnectionPermission>();
    public DbSet<TemplateModule> TemplateModules => Set<TemplateModule>();
    public DbSet<QueryTemplate> QueryTemplates => Set<QueryTemplate>();
    public DbSet<QueryLog> QueryLogs => Set<QueryLog>();
    public DbSet<QueryTab> QueryTabs => Set<QueryTab>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.Property(e => e.Username).HasMaxLength(50).IsRequired();
            entity.Property(e => e.PasswordHash).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Nickname).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.ThemePreference).HasMaxLength(20).HasDefaultValue("auto");
        });

        // Platform
        modelBuilder.Entity<Platform>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.SortOrder).HasDefaultValue(0);
        });

        // DatabaseConnection
        modelBuilder.Entity<DatabaseConnection>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.PlatformCode).HasMaxLength(50).IsRequired();
            entity.Property(e => e.ConnectionString).HasMaxLength(1000).IsRequired();
            entity.Property(e => e.DatabaseType).HasMaxLength(20).HasDefaultValue("SqlServer");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.SortOrder).HasDefaultValue(0);

            entity.HasOne(e => e.Platform)
                .WithMany(p => p.Connections)
                .HasForeignKey(e => e.PlatformCode)
                .HasPrincipalKey(p => p.Code)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // UserPlatformPermission
        modelBuilder.Entity<UserPlatformPermission>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.PlatformCode }).IsUnique();
            entity.Property(e => e.PlatformCode).HasMaxLength(50).IsRequired();

            entity.HasOne(e => e.User)
                .WithMany(u => u.PlatformPermissions)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Platform)
                .WithMany(p => p.UserPermissions)
                .HasForeignKey(e => e.PlatformCode)
                .HasPrincipalKey(p => p.Code)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // UserConnectionPermission
        modelBuilder.Entity<UserConnectionPermission>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.ConnectionId }).IsUnique();

            entity.HasOne(e => e.User)
                .WithMany(u => u.ConnectionPermissions)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Connection)
                .WithMany(c => c.UserPermissions)
                .HasForeignKey(e => e.ConnectionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // TemplateModule
        modelBuilder.Entity<TemplateModule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Icon).HasMaxLength(50);
            entity.Property(e => e.SortOrder).HasDefaultValue(0);
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(e => e.Parent)
                .WithMany(e => e.Children)
                .HasForeignKey(e => e.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // QueryTemplate
        modelBuilder.Entity<QueryTemplate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.SqlContent).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsPublic).HasDefaultValue(false);
            entity.Property(e => e.SortOrder).HasDefaultValue(0);

            entity.HasOne(e => e.Module)
                .WithMany(m => m.Templates)
                .HasForeignKey(e => e.ModuleId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Creator)
                .WithMany(u => u.Templates)
                .HasForeignKey(e => e.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // QueryLog
        modelBuilder.Entity<QueryLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PlatformCode).HasMaxLength(50);
            entity.Property(e => e.DatabaseName).HasMaxLength(100);
            entity.Property(e => e.SqlContent).IsRequired();
            entity.Property(e => e.ErrorMessage).HasMaxLength(2000);
            entity.Property(e => e.ClientIp).HasMaxLength(50);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.UserId);

            entity.HasOne(e => e.User)
                .WithMany(u => u.QueryLogs)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // QueryTab
        modelBuilder.Entity<QueryTab>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.PlatformCode).HasMaxLength(50);
            entity.Property(e => e.SqlContent);
            entity.Property(e => e.SortOrder).HasDefaultValue(0);
            entity.HasIndex(e => e.UserId);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
