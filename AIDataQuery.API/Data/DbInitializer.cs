using AIDataQuery.API.Models.Entities;
using AIDataQuery.API.Models.Enums;
using BCrypt.Net;

namespace AIDataQuery.API.Data;

public static class DbInitializer
{
    public static void Initialize(AppDbContext context)
    {
        context.Database.EnsureCreated();

        // Seed Platforms
        if (!context.Platforms.Any())
        {
            var platforms = new List<Platform>
            {
                new() { Code = "ERP_YYY_GXXQ", Name = "【正式】ERP系统-药约约-高新西区", SortOrder = 1 },
                new() { Code = "ERP_YYY_CZ", Name = "【正式】ERP系统-药约约-崇州", SortOrder = 2 },
                new() { Code = "ERP_HYYX_GXXQ", Name = "【正式】ERP系统-好药优选-高新西区", SortOrder = 3 },
                new() { Code = "ERP_YYY_TJ", Name = "【正式】ERP系统-药约約-天津", SortOrder = 4 }
            };
            context.Platforms.AddRange(platforms);
            context.SaveChanges();
        }

        // 注意：数据库连接需要管理员通过管理界面配置，连接字符串将加密存储在 DatabaseConnections 表中

        // Seed Database Connections
        if (!context.DatabaseConnections.Any())
        {
            var connections = new List<DatabaseConnection>
            {
                // ERP_HYYX_GXXQ - 好药优选-高新西区
                new() { Name = "【正式】ERP系统-好药优选-高新西区-ERP", PlatformCode = "ERP_HYYX_GXXQ", ConnectionString = "Data Source=10.16.16.3,51433;Initial Catalog=CR_V11_ERP;uid=pt;pwd=pt@hYs2022!;PERSIST SECURITY INFO=True;TrustServerCertificate=True;", DatabaseType = "SqlServer", SortOrder = 1 },
                new() { Name = "【正式】ERP系统-好药优选-高新西区-ERP_CX", PlatformCode = "ERP_HYYX_GXXQ", ConnectionString = "Data Source=10.16.16.3,51433;Initial Catalog=ERP_CX;uid=pt;pwd=pt@hYs2022!;PERSIST SECURITY INFO=True;TrustServerCertificate=True;", DatabaseType = "SqlServer", SortOrder = 2 },
                new() { Name = "【正式】ERP系统-好药优选-高新西区-WMS", PlatformCode = "ERP_HYYX_GXXQ", ConnectionString = "Data Source=10.16.16.25,1433;Initial Catalog=WMS;uid=pt;pwd=pt@hYs2022!;PERSIST SECURITY INFO=True;TrustServerCertificate=True;", DatabaseType = "SqlServer", SortOrder = 3 },
                new() { Name = "【正式】ERP系统-好药优选-高新西区-WMS_Mid", PlatformCode = "ERP_HYYX_GXXQ", ConnectionString = "Data Source=10.16.16.25,1433;Initial Catalog=MID;uid=pt;pwd=pt@hYs2022!;PERSIST SECURITY INFO=True;TrustServerCertificate=True;", DatabaseType = "SqlServer", SortOrder = 4 },

                // ERP_YYY_GXXQ - 药约约-高新西区
                new() { Name = "【正式】ERP系统-药约约-高新西区-ERP", PlatformCode = "ERP_YYY_GXXQ", ConnectionString = "Data Source=10.16.16.3,51433;Initial Catalog=CR_V11_ERP_XJND;uid=pt;pwd=pt@hYs2022!;PERSIST SECURITY INFO=True;TrustServerCertificate=True;", DatabaseType = "SqlServer", SortOrder = 1 },
                new() { Name = "【正式】ERP系统-药约约-高新西区-ERP_CX", PlatformCode = "ERP_YYY_GXXQ", ConnectionString = "Data Source=10.16.16.3,51433;Initial Catalog=ERP_CX_DSYB;uid=pt;pwd=pt@hYs2022!;PERSIST SECURITY INFO=True;TrustServerCertificate=True;", DatabaseType = "SqlServer", SortOrder = 2 },

                // ERP_YYY_CZ - 药约约-崇州
                new() { Name = "【正式】ERP系统-药约约-崇州-ERP", PlatformCode = "ERP_YYY_CZ", ConnectionString = "Data Source=171.221.200.189,63436;Initial Catalog=CR_V11_ERP_XJND;uid=sa;pwd=Hysyyl@123*;PERSIST SECURITY INFO=True;TrustServerCertificate=True;", DatabaseType = "SqlServer", SortOrder = 1 },
                new() { Name = "【正式】ERP系统-药约约-崇州-ERP_CX", PlatformCode = "ERP_YYY_CZ", ConnectionString = "Data Source=171.221.200.189,63436;Initial Catalog=ERP_CX_YYY;uid=sa;pwd=Hysyyl@123*;PERSIST SECURITY INFO=True;TrustServerCertificate=True;", DatabaseType = "SqlServer", SortOrder = 2 },

                // ERP_YYY_TJ - 药约約-天津
                new() { Name = "【正式】ERP系统-药约約-天津-ERP", PlatformCode = "ERP_YYY_TJ", ConnectionString = "Data Source=111.33.108.110,55164;Initial Catalog=CR_V11_ERP_TJY;uid=yyy;pwd=THMWW4xThAzy;PERSIST SECURITY INFO=True;TrustServerCertificate=True;", DatabaseType = "SqlServer", SortOrder = 1 },
                new() { Name = "【正式】ERP系统-药约約-天津-ERP_CX", PlatformCode = "ERP_YYY_TJ", ConnectionString = "Data Source=111.33.108.110,55164;Initial Catalog=ERP_CX;uid=yyy;pwd=THMWW4xThAzy;PERSIST SECURITY INFO=True;TrustServerCertificate=True;", DatabaseType = "SqlServer", SortOrder = 2 }
            };
            context.DatabaseConnections.AddRange(connections);
            context.SaveChanges();
        }

        // Seed Admin User
        if (!context.Users.Any())
        {
            var admin = new User
            {
                Username = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                Nickname = "管理员",
                Email = "admin@example.com",
                Role = UserRole.Admin,
                Status = UserStatus.Active
            };
            context.Users.Add(admin);
            context.SaveChanges();

            // Add all platform permissions for admin
            var allPlatformCodes = context.Platforms.Select(p => p.Code).ToList();
            foreach (var code in allPlatformCodes)
            {
                context.UserPlatformPermissions.Add(new UserPlatformPermission
                {
                    UserId = admin.Id,
                    PlatformCode = code
                });
            }
            context.SaveChanges();
        }

        // Seed Template Modules
        if (!context.TemplateModules.Any())
        {
            var modules = new List<TemplateModule>
            {
                new() { Name = "客户模块", SortOrder = 1 },
                new() { Name = "商品模块", SortOrder = 2 },
                new() { Name = "订单模块", SortOrder = 3 },
                new() { Name = "库存模块", SortOrder = 4 },
                new() { Name = "自定义模板", SortOrder = 5 }
            };
            context.TemplateModules.AddRange(modules);
            context.SaveChanges();

            // Add sub-modules for "客户模块"
            var customerModuleId = context.TemplateModules.First(m => m.Name == "客户模块").Id;
            var subModules = new List<TemplateModule>
            {
                new() { Name = "KEY查询", ParentId = customerModuleId, SortOrder = 1 },
                new() { Name = "基础查询", ParentId = customerModuleId, SortOrder = 2 },
                new() { Name = "区域查询", ParentId = customerModuleId, SortOrder = 3 },
                new() { Name = "发票查询", ParentId = customerModuleId, SortOrder = 4 },
                new() { Name = "发货查询", ParentId = customerModuleId, SortOrder = 5 },
                new() { Name = "经营范围查询", ParentId = customerModuleId, SortOrder = 6 },
                new() { Name = "证照查询", ParentId = customerModuleId, SortOrder = 7 }
            };
            context.TemplateModules.AddRange(subModules);
            context.SaveChanges();
        }
    }
}
