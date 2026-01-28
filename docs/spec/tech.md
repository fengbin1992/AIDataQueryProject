# AIDataQuery 技术栈文档

## 项目类型

前后端分离的企业级 Web 应用程序，采用 RESTful API 架构。

---

## 核心技术

### 后端技术栈

#### 主要语言与框架
- **语言**: C# 12
- **运行时**: .NET 8.0 LTS
- **Web 框架**: ASP.NET Core Web API
- **ORM**: Entity Framework Core 8.0

#### 关键依赖

| 包名 | 版本 | 用途 |
|------|------|------|
| Microsoft.AspNetCore.Authentication.JwtBearer | 8.0.x | JWT 认证 |
| Microsoft.EntityFrameworkCore | 8.0.x | ORM 框架 |
| Microsoft.EntityFrameworkCore.Sqlite | 8.0.x | SQLite 数据库驱动 |
| Microsoft.Data.SqlClient | 5.x | SQL Server 数据库驱动 |
| Swashbuckle.AspNetCore | 6.x | Swagger/OpenAPI 文档 |
| BCrypt.Net-Next | 4.x | 密码加密 |
| Serilog.AspNetCore | 8.x | 结构化日志 |
| AutoMapper | 12.x | 对象映射 |
| FluentValidation.AspNetCore | 11.x | 请求验证 |
| ClosedXML | 0.102.x | Excel 导出 |

### 前端技术栈

#### 主要框架
- **框架**: Vue 3.4+
- **构建工具**: Vite 5.x
- **语言**: TypeScript 5.x
- **状态管理**: Pinia 2.x
- **路由**: Vue Router 4.x

#### UI 组件库
- **组件库**: Element Plus 2.x
- **图标库**: @element-plus/icons-vue
- **CSS 预处理器**: SCSS

#### 关键依赖

| 包名 | 版本 | 用途 |
|------|------|------|
| vue | ^3.4 | 前端框架 |
| element-plus | ^2.5 | UI 组件库 |
| pinia | ^2.1 | 状态管理 |
| vue-router | ^4.2 | 路由管理 |
| axios | ^1.6 | HTTP 客户端 |
| monaco-editor | ^0.45 | SQL 代码编辑器 |
| @vueuse/core | ^10.x | Vue 组合式工具集 |
| dayjs | ^1.11 | 日期处理 |
| file-saver | ^2.0 | 文件下载 |

---

## 应用架构

### 整体架构

```
┌─────────────────────────────────────────────────────────────────┐
│                          客户端浏览器                            │
│                     (Vue 3 + Element Plus)                      │
└───────────────────────────────┬─────────────────────────────────┘
                                │ HTTP/HTTPS
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│                      ASP.NET Core Web API                       │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐              │
│  │ Controllers │  │ Middleware  │  │   Filters   │              │
│  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘              │
│         │                │                │                      │
│         └────────────────┼────────────────┘                      │
│                          ▼                                       │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │                    Application Services                      ││
│  │  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐       ││
│  │  │  Auth    │ │  Query   │ │ Template │ │  User    │       ││
│  │  │ Service  │ │ Service  │ │ Service  │ │ Service  │       ││
│  │  └──────────┘ └──────────┘ └──────────┘ └──────────┘       ││
│  └─────────────────────────────────────────────────────────────┘│
│                          │                                       │
│                          ▼                                       │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │                     Infrastructure                           ││
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐       ││
│  │  │ EF Core      │  │ Dynamic DB   │  │ External     │       ││
│  │  │ (SQLite)     │  │ Connection   │  │ Services     │       ││
│  │  └──────────────┘  └──────────────┘  └──────────────┘       ││
│  └─────────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────────┘
         │                      │
         ▼                      ▼
   ┌───────────┐     ┌─────────────────────┐
   │  SQLite   │     │  SQL Server 实例    │
   │  (配置库)  │     │  (业务数据库)       │
   └───────────┘     └─────────────────────┘
```

### 后端三层架构

```
┌─────────────────────────────────────────────────────────────────┐
│                      表现层 (Presentation Layer)                 │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │                      Controllers                             ││
│  │  - 接收 HTTP 请求，返回响应                                    ││
│  │  - 参数验证、请求/响应映射                                     ││
│  │  - 调用服务层处理业务逻辑                                      ││
│  └─────────────────────────────────────────────────────────────┘│
└───────────────────────────────┬─────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│                    业务逻辑层 (Business Logic Layer)             │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │                       Services                               ││
│  │  - 实现核心业务逻辑                                           ││
│  │  - 事务管理、数据验证                                         ││
│  │  - 直接使用 DbContext 进行数据操作                            ││
│  └─────────────────────────────────────────────────────────────┘│
└───────────────────────────────┬─────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│                      数据访问层 (Data Access Layer)              │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │              EF Core DbContext + Entities                    ││
│  │  - 数据库上下文配置                                           ││
│  │  - 实体模型定义                                               ││
│  │  - 数据库迁移管理                                             ││
│  └─────────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────────┘
```

**目录结构：**

```
AIDataQuery.API/
├── Controllers/          # 表现层 - API 控制器
│   ├── AuthController
│   ├── QueryController
│   ├── TemplateController
│   └── UserController
│
├── Services/             # 业务逻辑层 - 业务服务
│   ├── Interfaces/       # 服务接口
│   │   ├── IAuthService
│   │   ├── IQueryService
│   │   ├── ITemplateService
│   │   └── IUserService
│   ├── AuthService
│   ├── QueryService
│   ├── TemplateService
│   └── UserService
│
├── Data/                 # 数据访问层 - EF Core
│   ├── AppDbContext.cs   # 数据库上下文
│   ├── DbInitializer.cs  # 数据初始化
│   └── Migrations/       # 数据库迁移
│
├── Models/               # 数据模型
│   ├── Entities/         # 数据库实体
│   └── DTOs/             # 数据传输对象
│
├── Infrastructure/       # 基础设施（横切关注点）
│   ├── Security/         # 安全相关
│   └── Extensions/       # 扩展方法
│
└── Middleware/           # 中间件
    ├── ExceptionMiddleware
    └── RequestLoggingMiddleware
```

### 前端架构

```
src/
├── views/                # 页面视图
│   ├── Login/
│   ├── Query/
│   ├── Template/
│   └── Admin/
│
├── components/           # 可复用组件
│   ├── common/           # 通用组件
│   ├── query/            # 查询相关组件
│   └── layout/           # 布局组件
│
├── stores/               # Pinia 状态管理
│   ├── user.ts
│   ├── query.ts
│   └── theme.ts
│
├── services/             # API 服务封装
│   ├── auth.ts
│   ├── query.ts
│   └── template.ts
│
├── utils/                # 工具函数
│   ├── request.ts        # Axios 封装
│   ├── storage.ts        # 本地存储
│   └── sql-parser.ts     # SQL 解析
│
├── styles/               # 样式文件
│   ├── themes/           # 主题配置
│   └── variables.scss    # SCSS 变量
│
└── types/                # TypeScript 类型定义
    ├── api.d.ts
    └── models.d.ts
```

---

## 数据存储

### 系统配置数据库 (SQLite)

用于存储系统配置、用户、权限、**业务数据库连接字符串**等数据。

```
数据库文件: app_data.db
位置: ./data/app_data.db
```

**数据表：**
- Users - 用户表
- UserPlatformPermissions - 用户平台权限
- Platforms - 平台表
- DatabaseConnections - 数据库连接配置（**连接字符串 AES 加密存储**）
- QueryTemplates - 查询模板
- TemplateModules - 模板模块
- QueryLogs - 查询日志

### 业务数据库连接管理

业务数据库（SQL Server）的连接字符串**存储在 SQLite 配置库的 DatabaseConnections 表中**，而非配置文件：

```
┌─────────────────────────────────────────────────────────────────┐
│                     连接字符串管理流程                            │
├─────────────────────────────────────────────────────────────────┤
│  1. 管理员通过管理界面添加数据库连接                               │
│  2. 连接字符串使用 AES-256 加密后存储到 DatabaseConnections 表     │
│  3. 执行查询时，从数据库读取连接配置并解密                          │
│  4. 使用解密后的连接字符串建立 SQL Server 连接                     │
└─────────────────────────────────────────────────────────────────┘
```

**优势：**
- 连接字符串不存在于配置文件中，更安全
- 支持运行时动态添加/修改数据库连接
- 连接字符串加密存储，防止泄露

### 业务数据库 (SQL Server)

外部 SQL Server 数据库实例，通过动态连接访问。连接信息存储在 SQLite 的 `DatabaseConnections` 表中。

---

## 外部集成

### 协议
- **HTTP/REST**: 前后端通信
- **ADO.NET**: 动态数据库连接

### 认证
- **JWT Bearer Token**: API 认证
- **BCrypt**: 密码加密

---

## 开发环境

### 构建与开发工具

#### 后端
- **IDE**: Visual Studio 2022 / VS Code / Rider
- **包管理**: NuGet
- **构建系统**: dotnet CLI / MSBuild

#### 前端
- **IDE**: VS Code + Volar 扩展
- **包管理**: pnpm (推荐) / npm / yarn
- **构建系统**: Vite

### 开发命令

```bash
# 后端
dotnet restore                    # 还原依赖
dotnet build                      # 编译
dotnet run                        # 运行
dotnet ef migrations add <Name>   # 添加迁移
dotnet ef database update         # 更新数据库

# 前端
pnpm install                      # 安装依赖
pnpm dev                          # 开发服务器
pnpm build                        # 生产构建
pnpm preview                      # 预览构建结果
```

### 代码质量工具

#### 后端
- **静态分析**: Roslyn Analyzers, SonarQube
- **格式化**: .editorconfig, dotnet format
- **测试框架**: xUnit, Moq, FluentAssertions

#### 前端
- **静态分析**: ESLint + @typescript-eslint
- **格式化**: Prettier
- **测试框架**: Vitest, Vue Test Utils
- **类型检查**: vue-tsc

### 版本控制
- **VCS**: Git
- **分支策略**: Git Flow
- **提交规范**: Conventional Commits

---

## 部署与分发

### 目标平台
- **操作系统**: Windows Server 2016+
- **Web 服务器**: IIS 10+ / Kestrel
- **运行时**: .NET 8.0 Runtime

### 部署方式
```
┌─────────────────────────────────────────┐
│            Windows Server               │
│  ┌─────────────────────────────────────┐│
│  │              IIS                    ││
│  │  ┌─────────────┐  ┌─────────────┐  ││
│  │  │ Vue SPA     │  │ .NET API    │  ││
│  │  │ (wwwroot)   │  │ (应用池)     │  ││
│  │  └─────────────┘  └─────────────┘  ││
│  └─────────────────────────────────────┘│
│          │                  │           │
│          └────────┬─────────┘           │
│                   ▼                     │
│           ┌─────────────┐               │
│           │   SQLite    │               │
│           └─────────────┘               │
└─────────────────────────────────────────┘
```

### 系统要求
- **CPU**: 2 核+
- **内存**: 4GB+
- **磁盘**: 10GB+
- **网络**: 内网可访问 SQL Server 实例

---

## 技术要求与约束

### 性能要求
| 指标 | 目标 |
|------|------|
| API 响应时间 | < 500ms (非查询) |
| 查询响应时间 | < 30s |
| 前端首屏加载 | < 3s |
| 内存占用 | < 500MB |

### 兼容性要求
- **浏览器**: Chrome 90+, Edge 90+, Firefox 90+
- **分辨率**: 1920x1080 及以上
- **SQL Server**: 2014+

### 安全要求
- HTTPS 加密传输
- JWT Token 签名验证
- SQL 注入防护
- XSS 防护
- CORS 配置

---

## 技术决策记录

### Decision 1: 选择 SQLite 作为配置数据库
**原因**:
- 轻量级，无需额外数据库服务器
- 便于部署和备份
- 配置数据量小，SQLite 性能足够

**权衡**:
- 不支持并发写入（可接受，配置修改频率低）

### Decision 2: 选择 Monaco Editor 作为 SQL 编辑器
**原因**:
- VS Code 同款编辑器，功能强大
- 支持语法高亮和智能提示
- 活跃的社区支持

**权衡**:
- 包体积较大（约 2MB），影响首屏加载

### Decision 3: 动态数据库连接而非多上下文
**原因**:
- 数据库连接配置动态变化
- 避免为每个连接创建 DbContext
- 更灵活的连接管理

**权衡**:
- 需要手动管理连接生命周期

---

## 已知限制

1. **SQLite 并发限制**
   - 影响: 高并发配置修改可能出现锁等待
   - 解决: 配置修改操作加锁，读操作无限制

2. **大数据量导出**
   - 影响: 导出大量数据时内存占用高
   - 解决: 流式导出，分页处理

3. **SQL Server 版本兼容**
   - 影响: 某些高级 SQL 语法在旧版本不支持
   - 解决: 提供版本检测和兼容性提示
