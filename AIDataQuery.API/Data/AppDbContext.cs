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
    public DbSet<ConfigQuery> ConfigQueries => Set<ConfigQuery>();
    public DbSet<ConfigQueryParameter> ConfigQueryParameters => Set<ConfigQueryParameter>();
    public DbSet<ConfigQueryParamPreset> ConfigQueryParamPresets => Set<ConfigQueryParamPreset>();
    public DbSet<ConfigQueryFolder> ConfigQueryFolders => Set<ConfigQueryFolder>();

    // 数据安全相关
    public DbSet<SensitiveMaskingRule> SensitiveMaskingRules => Set<SensitiveMaskingRule>();
    public DbSet<SensitiveFieldMark> SensitiveFieldMarks => Set<SensitiveFieldMark>();

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

        // ConfigQuery
        modelBuilder.Entity<ConfigQuery>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.SqlContent).IsRequired();
            entity.Property(e => e.IsPublic).HasDefaultValue(false);
            entity.Property(e => e.SortOrder).HasDefaultValue(0);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.HasIndex(e => e.CreatedBy);
            entity.HasIndex(e => e.IsPublic);
            entity.HasIndex(e => e.FolderId);

            entity.HasOne(e => e.Connection)
                .WithMany()
                .HasForeignKey(e => e.ConnectionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Creator)
                .WithMany()
                .HasForeignKey(e => e.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Folder)
                .WithMany(f => f.ConfigQueries)
                .HasForeignKey(e => e.FolderId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ConfigQueryParameter
        modelBuilder.Entity<ConfigQueryParameter>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ParamName).HasMaxLength(50).IsRequired();
            entity.Property(e => e.ParamLabel).HasMaxLength(100).IsRequired();
            entity.Property(e => e.ParamType).HasMaxLength(20).IsRequired();
            entity.Property(e => e.IsRequired).HasDefaultValue(true);
            entity.Property(e => e.DefaultValue).HasMaxLength(500);
            entity.Property(e => e.Placeholder).HasMaxLength(200);
            entity.Property(e => e.ValidationRule).HasMaxLength(200);
            entity.Property(e => e.SortOrder).HasDefaultValue(0);
            entity.Property(e => e.ConditionGroup).HasMaxLength(50);
            entity.HasIndex(e => new { e.ConfigQueryId, e.ParamName }).IsUnique();

            entity.HasOne(e => e.ConfigQuery)
                .WithMany(q => q.Parameters)
                .HasForeignKey(e => e.ConfigQueryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ConfigQueryParamPreset
        modelBuilder.Entity<ConfigQueryParamPreset>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.ParamValues).IsRequired();
            entity.Property(e => e.IsDefault).HasDefaultValue(false);
            entity.HasIndex(e => e.ConfigQueryId);

            entity.HasOne(e => e.ConfigQuery)
                .WithMany(q => q.Presets)
                .HasForeignKey(e => e.ConfigQueryId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Creator)
                .WithMany()
                .HasForeignKey(e => e.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ConfigQueryFolder
        modelBuilder.Entity<ConfigQueryFolder>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.SortOrder).HasDefaultValue(0);
            entity.HasIndex(e => e.CreatedBy);

            entity.HasOne(e => e.Creator)
                .WithMany()
                .HasForeignKey(e => e.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // SensitiveMaskingRule
        modelBuilder.Entity<SensitiveMaskingRule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.FieldPattern).HasMaxLength(500).IsRequired();
            entity.Property(e => e.MaskConfig).HasMaxLength(1000);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Priority).HasDefaultValue(0);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.HasIndex(e => new { e.IsActive, e.Priority });
        });

        // SensitiveFieldMark
        modelBuilder.Entity<SensitiveFieldMark>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TableName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.FieldName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.MaskConfig).HasMaxLength(1000);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.HasIndex(e => new { e.ConnectionId, e.TableName, e.FieldName }).IsUnique();

            entity.HasOne(e => e.Connection)
                .WithMany()
                .HasForeignKey(e => e.ConnectionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Marker)
                .WithMany()
                .HasForeignKey(e => e.MarkedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
