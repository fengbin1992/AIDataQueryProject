# AIDataQuery 项目规格文档

## 文档索引

本目录包含 AIDataQuery 数据查询中心项目的完整规格文档。

| 文档 | 描述 | 主要内容 |
|------|------|----------|
| [product.md](./product.md) | 产品愿景 | 产品目标、价值主张、功能模块概览 |
| [requirements.md](./requirements.md) | 需求文档 | 功能需求、非功能需求、用户故事 |
| [tech.md](./tech.md) | 技术栈 | 后端/前端技术选型、架构设计 |
| [design.md](./design.md) | 设计文档 | 组件设计、数据模型、API 接口、页面布局 |
| [structure.md](./structure.md) | 项目结构 | 目录组织、命名规范、代码规范 |
| [tasks.md](./tasks.md) | 任务清单 | 开发任务、里程碑、进度跟踪 |

## 项目概述

**AIDataQuery** 是一套企业级数据查询中心 Web 系统，主要功能包括：

- **多平台数据库管理**：统一管理多个 ERP 系统的数据库连接
- **SQL 查询执行**：安全的只读查询，带智能提示
- **查询模板管理**：保存和复用常用 SQL
- **用户权限控制**：基于平台的访问权限管理
- **查询历史记录**：完整的操作审计日志

## 技术栈

| 层级 | 技术 |
|------|------|
| 后端 | .NET 8 + ASP.NET Core Web API + EF Core |
| 前端 | Vue 3 + TypeScript + Element Plus + Pinia |
| 配置数据库 | SQLite |
| 业务数据库 | SQL Server (动态连接) |
| 代码编辑器 | Monaco Editor |

## 支持的平台

| 平台编码 | 平台名称 |
|----------|----------|
| ERP_YYY_GXXQ | 【正式】ERP系统-药约约-高新西区 |
| ERP_YYY_CZ | 【正式】ERP系统-药约约-崇州 |
| ERP_HYYX_GXXQ | 【正式】ERP系统-好药优选-高新西区 |
| ERP_YYY_TJ | 【正式】ERP系统-药约約-天津 |

## 快速开始

详见各文档的具体说明。

---

**版本**: v1.0
**更新日期**: 2024-01
