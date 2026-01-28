# AIDataQuery 项目结构文档

## 目录组织

```
AIDataQuery/
├── docs/                                   # 文档目录
│   └── spec/                               # 规格说明文档
│       ├── product.md                      # 产品愿景
│       ├── requirements.md                 # 需求文档
│       ├── design.md                       # 设计文档
│       ├── tech.md                         # 技术栈文档
│       ├── structure.md                    # 项目结构文档
│       └── tasks.md                        # 任务清单
│
├── AIDataQuery.API/                        # 后端 Web API 项目（三层架构）
│   ├── Controllers/                        # 表现层 - API 控制器
│   │   ├── AuthController.cs               # 认证控制器
│   │   ├── QueryController.cs              # 查询控制器
│   │   ├── TemplateController.cs           # 模板控制器
│   │   ├── UserController.cs               # 用户管理控制器
│   │   ├── PlatformController.cs           # 平台管理控制器
│   │   └── QueryLogController.cs           # 查询日志控制器
│   │
│   ├── Services/                           # 业务逻辑层 - 业务服务
│   │   ├── Interfaces/                     # 服务接口
│   │   │   ├── IAuthService.cs
│   │   │   ├── IQueryService.cs
│   │   │   ├── ITemplateService.cs
│   │   │   ├── IUserService.cs
│   │   │   └── IPlatformService.cs
│   │   ├── AuthService.cs
│   │   ├── QueryService.cs
│   │   ├── TemplateService.cs
│   │   ├── UserService.cs
│   │   └── PlatformService.cs
│   │
│   ├── Data/                               # 数据访问层 - EF Core
│   │   ├── AppDbContext.cs                 # EF Core 上下文
│   │   ├── DbInitializer.cs                # 数据库初始化
│   │   └── Migrations/                     # EF 迁移文件
│   │
│   ├── Models/                             # 数据模型
│   │   ├── Entities/                       # 数据库实体
│   │   │   ├── User.cs
│   │   │   ├── Platform.cs
│   │   │   ├── DatabaseConnection.cs
│   │   │   ├── UserPlatformPermission.cs
│   │   │   ├── TemplateModule.cs
│   │   │   ├── QueryTemplate.cs
│   │   │   └── QueryLog.cs
│   │   │
│   │   ├── DTOs/                           # 数据传输对象
│   │   │   ├── Auth/
│   │   │   │   ├── LoginRequest.cs
│   │   │   │   ├── LoginResponse.cs
│   │   │   │   └── ChangePasswordRequest.cs
│   │   │   ├── User/
│   │   │   │   ├── UserDto.cs
│   │   │   │   ├── CreateUserRequest.cs
│   │   │   │   └── UpdateUserRequest.cs
│   │   │   ├── Query/
│   │   │   │   ├── QueryRequest.cs
│   │   │   │   ├── QueryResult.cs
│   │   │   │   └── ExportRequest.cs
│   │   │   ├── Template/
│   │   │   │   ├── TemplateDto.cs
│   │   │   │   ├── TemplateModuleDto.cs
│   │   │   │   └── CreateTemplateRequest.cs
│   │   │   └── Common/
│   │   │       ├── PagedResult.cs
│   │   │       └── ApiResponse.cs
│   │   │
│   │   └── Enums/                          # 枚举类型
│   │       ├── UserRole.cs
│   │       ├── UserStatus.cs
│   │       └── QueryStatus.cs
│   │
│   ├── Infrastructure/                     # 基础设施（横切关注点）
│   │   ├── Security/                       # 安全相关
│   │   │   ├── JwtTokenGenerator.cs        # JWT 生成器
│   │   │   ├── PasswordHasher.cs           # 密码加密
│   │   │   └── SqlValidator.cs             # SQL 验证器
│   │   │
│   │   ├── Encryption/                     # 加密相关
│   │   │   └── AesEncryptor.cs             # AES 加密器
│   │   │
│   │   └── Extensions/                     # 扩展方法
│   │       ├── ServiceCollectionExtensions.cs
│   │       └── StringExtensions.cs
│   │
│   ├── Middleware/                         # 中间件
│   │   ├── ExceptionMiddleware.cs          # 全局异常处理
│   │   └── RequestLoggingMiddleware.cs     # 请求日志
│   │
│   ├── Filters/                            # 过滤器
│   │   └── ValidateModelFilter.cs          # 模型验证
│   │
│   ├── Mappings/                           # AutoMapper 配置
│   │   └── MappingProfile.cs
│   │
│   ├── Validators/                         # FluentValidation 验证器
│   │   ├── LoginRequestValidator.cs
│   │   ├── CreateUserRequestValidator.cs
│   │   └── QueryRequestValidator.cs
│   │
│   ├── appsettings.json                    # 配置文件
│   ├── appsettings.Development.json        # 开发环境配置
│   ├── Program.cs                          # 程序入口
│   └── AIDataQuery.API.csproj              # 项目文件
│
├── AIDataQuery.Web/                        # 前端 Vue 项目
│   ├── public/                             # 静态资源
│   │   └── favicon.ico
│   │
│   ├── src/
│   │   ├── assets/                         # 资源文件
│   │   │   ├── images/                     # 图片
│   │   │   └── fonts/                      # 字体
│   │   │
│   │   ├── components/                     # 组件
│   │   │   ├── common/                     # 通用组件
│   │   │   │   ├── AppHeader.vue           # 头部导航
│   │   │   │   ├── AppSidebar.vue          # 侧边栏
│   │   │   │   ├── DataTable.vue           # 数据表格
│   │   │   │   ├── Pagination.vue          # 分页
│   │   │   │   └── ConfirmDialog.vue       # 确认对话框
│   │   │   │
│   │   │   ├── query/                      # 查询相关组件
│   │   │   │   ├── SqlEditor.vue           # SQL 编辑器
│   │   │   │   ├── QueryResult.vue         # 查询结果
│   │   │   │   ├── PlatformSelector.vue    # 平台选择器
│   │   │   │   └── DatabaseSelector.vue    # 数据库选择器
│   │   │   │
│   │   │   ├── template/                   # 模板相关组件
│   │   │   │   ├── TemplateTree.vue        # 模板树
│   │   │   │   ├── TemplateForm.vue        # 模板表单
│   │   │   │   └── ModuleForm.vue          # 模块表单
│   │   │   │
│   │   │   └── user/                       # 用户相关组件
│   │   │       ├── UserForm.vue            # 用户表单
│   │   │       └── PermissionForm.vue      # 权限表单
│   │   │
│   │   ├── views/                          # 页面视图
│   │   │   ├── login/
│   │   │   │   └── LoginView.vue           # 登录页
│   │   │   ├── query/
│   │   │   │   └── QueryView.vue           # 数据查询页
│   │   │   ├── template/
│   │   │   │   └── TemplateView.vue        # 模板管理页
│   │   │   ├── history/
│   │   │   │   └── HistoryView.vue         # 查询历史页
│   │   │   ├── admin/
│   │   │   │   ├── UserManage.vue          # 用户管理
│   │   │   │   ├── PlatformManage.vue      # 平台管理
│   │   │   │   └── ConnectionManage.vue    # 连接管理
│   │   │   └── error/
│   │   │       └── NotFound.vue            # 404 页面
│   │   │
│   │   ├── layouts/                        # 布局组件
│   │   │   ├── MainLayout.vue              # 主布局
│   │   │   └── BlankLayout.vue             # 空白布局
│   │   │
│   │   ├── router/                         # 路由配置
│   │   │   └── index.ts
│   │   │
│   │   ├── stores/                         # Pinia 状态管理
│   │   │   ├── user.ts                     # 用户状态
│   │   │   ├── query.ts                    # 查询状态
│   │   │   ├── template.ts                 # 模板状态
│   │   │   └── theme.ts                    # 主题状态
│   │   │
│   │   ├── services/                       # API 服务
│   │   │   ├── api.ts                      # Axios 实例
│   │   │   ├── auth.ts                     # 认证服务
│   │   │   ├── query.ts                    # 查询服务
│   │   │   ├── template.ts                 # 模板服务
│   │   │   ├── user.ts                     # 用户服务
│   │   │   └── platform.ts                 # 平台服务
│   │   │
│   │   ├── utils/                          # 工具函数
│   │   │   ├── storage.ts                  # 本地存储
│   │   │   ├── format.ts                   # 格式化工具
│   │   │   └── validate.ts                 # 验证工具
│   │   │
│   │   ├── styles/                         # 样式文件
│   │   │   ├── themes/                     # 主题
│   │   │   │   ├── light.scss              # 浅色主题
│   │   │   │   └── dark.scss               # 深色主题
│   │   │   ├── variables.scss              # SCSS 变量
│   │   │   ├── mixins.scss                 # SCSS 混入
│   │   │   └── global.scss                 # 全局样式
│   │   │
│   │   ├── types/                          # TypeScript 类型
│   │   │   ├── api.d.ts                    # API 类型
│   │   │   ├── models.d.ts                 # 模型类型
│   │   │   └── components.d.ts             # 组件类型
│   │   │
│   │   ├── App.vue                         # 根组件
│   │   └── main.ts                         # 入口文件
│   │
│   ├── index.html                          # HTML 模板
│   ├── vite.config.ts                      # Vite 配置
│   ├── tsconfig.json                       # TypeScript 配置
│   ├── package.json                        # 依赖配置
│   └── .env.development                    # 开发环境变量
│
├── tests/                                  # 测试目录
│   ├── AIDataQuery.API.Tests/              # 后端测试
│   │   ├── Services/                       # 服务测试
│   │   ├── Controllers/                    # 控制器测试
│   │   └── Infrastructure/                 # 基础设施测试
│   │
│   └── AIDataQuery.Web.Tests/              # 前端测试
│       ├── components/                     # 组件测试
│       └── stores/                         # 状态测试
│
├── data/                                   # 数据目录
│   └── app_data.db                         # SQLite 数据库文件
│
├── scripts/                                # 脚本目录
│   ├── init-db.sql                         # 数据库初始化脚本
│   └── seed-data.sql                       # 种子数据脚本
│
├── .gitignore                              # Git 忽略文件
├── .editorconfig                           # 编辑器配置
├── AIDataQuery.sln                         # 解决方案文件
└── README.md                               # 项目说明
```

---

## 命名规范

### 文件命名

| 类型 | 规范 | 示例 |
|------|------|------|
| **C# 文件** | PascalCase | `UserService.cs`, `QueryController.cs` |
| **接口文件** | I + PascalCase | `IUserService.cs` |
| **Vue 组件** | PascalCase | `SqlEditor.vue`, `AppHeader.vue` |
| **TypeScript 文件** | camelCase | `user.ts`, `storage.ts` |
| **样式文件** | kebab-case | `global.scss`, `dark.scss` |
| **测试文件** | 源文件名 + Tests | `UserServiceTests.cs` |

### 代码命名

| 类型 | 规范 | 示例 |
|------|------|------|
| **C# 类/接口** | PascalCase | `UserService`, `IQueryRepository` |
| **C# 方法** | PascalCase | `GetUsersAsync()`, `ExecuteQuery()` |
| **C# 私有字段** | _camelCase | `_userRepository`, `_logger` |
| **C# 常量** | PascalCase | `MaxQueryTimeout`, `DefaultPageSize` |
| **TS/JS 类** | PascalCase | `UserService` |
| **TS/JS 函数** | camelCase | `getUserById()`, `formatDate()` |
| **TS/JS 变量** | camelCase | `userName`, `isLoading` |
| **Vue 组件名** | PascalCase | `SqlEditor`, `QueryResult` |
| **CSS 类名** | kebab-case | `.sql-editor`, `.query-result` |

---

## 导入规范

### C# 导入顺序

```csharp
// 1. 系统命名空间
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// 2. 第三方库
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

// 3. 项目命名空间
using AIDataQuery.API.Models.Entities;
using AIDataQuery.API.Services.Interfaces;
```

### TypeScript 导入顺序

```typescript
// 1. Vue 核心
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'

// 2. 第三方库
import { ElMessage } from 'element-plus'
import axios from 'axios'

// 3. 项目内部模块
import { useUserStore } from '@/stores/user'
import { queryApi } from '@/services/query'

// 4. 组件
import SqlEditor from '@/components/query/SqlEditor.vue'

// 5. 类型
import type { QueryResult } from '@/types/models'

// 6. 样式
import '@/styles/query.scss'
```

---

## 代码结构规范

### C# 服务类结构

```csharp
// 1. 依赖注入字段
private readonly IUserRepository _userRepository;
private readonly ILogger<UserService> _logger;

// 2. 构造函数
public UserService(IUserRepository userRepository, ILogger<UserService> logger)
{
    _userRepository = userRepository;
    _logger = logger;
}

// 3. 公共方法
public async Task<UserDto> GetUserAsync(int id) { ... }

// 4. 私有辅助方法
private void ValidateUser(User user) { ... }
```

### Vue 组件结构

```vue
<template>
  <!-- 模板内容 -->
</template>

<script setup lang="ts">
// 1. 导入
import { ref, computed } from 'vue'

// 2. Props 定义
const props = defineProps<{
  title: string
}>()

// 3. Emits 定义
const emit = defineEmits<{
  (e: 'submit', data: FormData): void
}>()

// 4. 响应式状态
const loading = ref(false)

// 5. 计算属性
const isValid = computed(() => ...)

// 6. 方法
function handleSubmit() { ... }

// 7. 生命周期钩子
onMounted(() => { ... })
</script>

<style scoped lang="scss">
/* 组件样式 */
</style>
```

---

## 代码组织原则

### 1. 单一职责

每个文件应有一个清晰的用途：
- 一个控制器处理一类资源
- 一个服务处理一个业务领域
- 一个组件实现一个功能模块

### 2. 模块化

代码应组织为可复用的模块：
- 通用组件放在 `components/common/`
- 公共工具放在 `utils/`
- 共享类型放在 `types/`

### 3. 可测试性

代码结构应便于测试：
- 依赖注入而非硬编码
- 接口与实现分离
- 纯函数优于状态突变

### 4. 一致性

遵循已建立的模式：
- 使用相同的命名约定
- 使用相同的文件组织方式
- 使用相同的代码风格

---

## 模块边界

### 后端三层架构边界

```
┌─────────────────────────────────────────────────────────────┐
│                   Controllers (表现层)                       │
│           - 接收请求、返回响应、参数验证                       │
│                          ↓ 只调用                            │
├─────────────────────────────────────────────────────────────┤
│                    Services (业务逻辑层)                      │
│           - 业务规则、数据验证、事务管理                       │
│                          ↓ 直接使用                          │
├─────────────────────────────────────────────────────────────┤
│                 DbContext + EF Core (数据访问层)              │
│           - 实体映射、数据持久化、迁移管理                      │
│                          ↓ 访问                              │
├─────────────────────────────────────────────────────────────┤
│                       Database (数据层)                       │
│                   - SQLite / SQL Server                      │
└─────────────────────────────────────────────────────────────┘
```

**层间通信规则：**
- Controllers 只能调用 Services，不能直接访问 DbContext
- Services 直接注入并使用 DbContext 进行数据操作
- DbContext 负责所有数据库交互，包括查询、增删改和事务

### 前端模块边界

```
Views (页面)
    ↓ 使用
Components (组件) + Stores (状态)
    ↓ 调用
Services (API 服务)
    ↓ 请求
Backend API
```

---

## 代码大小指南

| 类型 | 建议行数 | 说明 |
|------|----------|------|
| **控制器方法** | < 30 行 | 仅处理请求/响应 |
| **服务方法** | < 50 行 | 复杂逻辑应拆分 |
| **Vue 组件** | < 300 行 | 超过应拆分子组件 |
| **工具函数** | < 30 行 | 保持单一职责 |
| **单个文件** | < 500 行 | 超过考虑拆分 |

---

## 文档标准

### C# 文档注释

```csharp
/// <summary>
/// 执行 SQL 查询并返回结果
/// </summary>
/// <param name="request">查询请求参数</param>
/// <returns>查询结果</returns>
/// <exception cref="InvalidOperationException">当 SQL 语法无效时抛出</exception>
public async Task<QueryResult> ExecuteQueryAsync(QueryRequest request)
```

### TypeScript 文档注释

```typescript
/**
 * 格式化日期时间
 * @param date - 日期对象或时间戳
 * @param format - 格式化模板，默认 'YYYY-MM-DD HH:mm:ss'
 * @returns 格式化后的日期字符串
 */
export function formatDateTime(date: Date | number, format?: string): string
```

---

## 配置文件说明

### appsettings.json 结构

```json
{
  "Jwt": {
    "Secret": "your-secret-key",
    "Issuer": "AIDataQuery",
    "Audience": "AIDataQuery",
    "ExpireHours": 8
  },
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=data/app_data.db"
  },
  "Query": {
    "TimeoutSeconds": 30,
    "MaxRows": 10000
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

### .env.development 结构

```bash
VITE_API_BASE_URL=http://localhost:5000/api
VITE_APP_TITLE=AIDataQuery
VITE_DEFAULT_THEME=auto
```
