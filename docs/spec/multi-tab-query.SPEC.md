# 数据查询多开页面功能 SPEC

## 概述

本文档定义数据查询多标签页功能的需求、设计和实施计划。该功能允许用户在同一界面中同时打开多个查询标签页，每个标签页可独立选择不同的平台和数据库进行查询操作。

**创建日期**: 2026-01-28
**功能编号**: FEAT-011
**关联需求**: REQ-005 (SQL 查询执行)

---

## 一、需求定义

### REQ-011 多标签页数据查询

**用户故事：** 作为数据分析人员，我希望同时打开多个查询标签页，以便在不同平台/数据库之间进行对比查询和交叉分析。

#### 验收标准

1. WHEN 用户点击"新建标签"按钮 THEN 系统 SHALL 创建一个新的查询标签页
2. WHEN 新标签创建时 THEN 系统 SHALL 显示空白的 SQL 编辑器和默认的平台/数据库选择
3. WHEN 用户切换标签 THEN 系统 SHALL 保留每个标签的独立状态（平台、数据库、SQL、结果）
4. WHEN 用户在某标签执行查询 THEN 查询结果 SHALL 仅显示在当前标签中
5. WHEN 用户点击标签的关闭按钮 THEN 系统 SHALL 关闭该标签（至少保留一个标签）
6. WHEN 用户关闭有未保存 SQL 的标签 THEN 系统 SHALL 弹出确认对话框
7. IF 只剩一个标签 THEN 关闭按钮 SHALL 被禁用或隐藏
8. WHEN 用户刷新页面 THEN 系统 SHALL 恢复之前打开的标签状态

#### 详细说明

- 最大同时打开标签数：10 个
- 标签命名规则：`查询 1`、`查询 2`... 或基于平台名称自动命名
- 支持双击标签重命名
- 支持拖拽调整标签顺序
- 标签状态持久化到 LocalStorage

---

## 二、设计方案

### 2.1 标签页状态模型

```typescript
// types/query-tab.ts
interface QueryTab {
  id: string                    // 唯一标识 (UUID)
  name: string                  // 标签名称
  platformCode: string | null   // 选中的平台编码
  connectionId: number | null   // 选中的数据库连接 ID
  sql: string                   // SQL 编辑器内容
  queryResult: QueryResult | null  // 查询结果
  isQuerying: boolean           // 是否正在查询
  isDirty: boolean              // 是否有未保存的更改
  createdAt: number             // 创建时间戳
}

interface QueryTabsState {
  tabs: QueryTab[]              // 所有标签
  activeTabId: string           // 当前激活的标签 ID
}
```

### 2.2 组件架构

```
QueryView.vue (重构)
├── QueryTabs.vue (新增)              # 标签栏组件
│   ├── TabItem.vue (新增)            # 单个标签项
│   └── AddTabButton.vue (新增)       # 新增标签按钮
├── QueryWorkspace.vue (新增)         # 单个查询工作区
│   ├── PlatformSelector.vue (提取)   # 平台/数据库选择器
│   ├── SqlEditor.vue (现有)          # SQL 编辑器
│   ├── QueryResult.vue (现有)        # 查询结果
│   └── TemplateTree.vue (现有)       # 模板树（共享）
```

### 2.3 页面布局

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  Logo   数据查询   模板管理   查询历史   [管理]     🌙  👤Admin  [退出]      │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ [查询1] [查询2-ERP_YYY] [查询3] [×]                           [+]   │   │  ← 标签栏
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  ┌──────────────────────────────────────────────────────────────────────┐  │
│  │ 平台: [ERP_YYY_GXXQ ▼]    数据库: [ERP ▼]                            │  │  ← 当前标签的选择器
│  └──────────────────────────────────────────────────────────────────────┘  │
│                                                                             │
│  ┌─────────────────────────┬────────────────────────────────────────────┐  │
│  │      模板列表           │               SQL 编辑器                    │  │
│  │  ┌───────────────────┐  │  ┌────────────────────────────────────────┐│  │
│  │  │ ▼ 客户模块        │  │  │ SELECT * FROM ...                      ││  │
│  │  │   ├─ KEY查询      │  │  │                                        ││  │
│  │  │   └─ ...          │  │  └────────────────────────────────────────┘│  │
│  │  │ ▶ 商品模块        │  │                                            │  │
│  │  └───────────────────┘  │  [执行] [保存模板] [格式化] [清空]         │  │
│  │                         ├────────────────────────────────────────────┤  │
│  │                         │               查询结果                      │  │
│  │                         │  ┌────────────────────────────────────────┐│  │
│  │                         │  │ 列1   │ 列2   │ 列3   │ ...            ││  │
│  │                         │  │ ...   │ ...   │ ...   │ ...            ││  │
│  │                         │  └────────────────────────────────────────┘│  │
│  └─────────────────────────┴────────────────────────────────────────────┘  │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 2.4 状态管理

新增 `useQueryTabsStore` Pinia Store：

```typescript
// stores/queryTabs.ts
export const useQueryTabsStore = defineStore('queryTabs', () => {
  const tabs = ref<QueryTab[]>([])
  const activeTabId = ref<string>('')

  // 计算属性
  const activeTab = computed(() => tabs.value.find(t => t.id === activeTabId.value))
  const canCloseTab = computed(() => tabs.value.length > 1)

  // Actions
  function createTab(options?: Partial<QueryTab>): QueryTab
  function closeTab(tabId: string): void
  function switchTab(tabId: string): void
  function updateTab(tabId: string, updates: Partial<QueryTab>): void
  function renameTab(tabId: string, name: string): void
  function reorderTabs(fromIndex: number, toIndex: number): void
  function saveToStorage(): void
  function loadFromStorage(): void

  return { tabs, activeTabId, activeTab, canCloseTab, ... }
})
```

### 2.5 数据持久化

使用 LocalStorage 存储标签状态：

```typescript
// 存储键名
const STORAGE_KEY = 'ai-data-query-tabs'

// 存储结构
interface StoredTabsData {
  version: number           // 数据版本号
  tabs: QueryTab[]          // 标签数据（不含查询结果）
  activeTabId: string       // 当前激活标签
  savedAt: number           // 保存时间戳
}
```

**持久化策略**：
- 标签创建/关闭/切换时自动保存
- SQL 内容变更时防抖保存（500ms）
- 查询结果不持久化（数据量大）
- 页面加载时自动恢复

### 2.6 标签栏交互

| 操作 | 触发方式 | 行为 |
|------|----------|------|
| 切换标签 | 单击标签 | 切换到目标标签，保存当前标签状态 |
| 关闭标签 | 点击 × 按钮 | 关闭标签，如有未保存内容则确认 |
| 新建标签 | 点击 + 按钮 | 创建新标签并切换到新标签 |
| 重命名 | 双击标签名 | 进入编辑模式，回车或失焦保存 |
| 调整顺序 | 拖拽标签 | 重新排列标签顺序 |
| 关闭其他 | 右键菜单 | 关闭除当前标签外的所有标签 |
| 关闭右侧 | 右键菜单 | 关闭当前标签右侧的所有标签 |

### 2.7 键盘快捷键

| 快捷键 | 功能 |
|--------|------|
| `Ctrl+T` | 新建标签 |
| `Ctrl+W` | 关闭当前标签 |
| `Ctrl+Tab` | 切换到下一个标签 |
| `Ctrl+Shift+Tab` | 切换到上一个标签 |
| `Ctrl+1~9` | 切换到第 N 个标签 |

---

## 三、实施任务清单

### 3.1 准备阶段

#### TASK-019: 创建标签状态类型定义
**优先级**: P0
**依赖**: 无

- [ ] 创建 `types/query-tab.ts` 文件
- [ ] 定义 `QueryTab` 接口
- [ ] 定义 `QueryTabsState` 接口
- [ ] 导出类型到 `types/index.ts`

**验收标准**:
- 类型定义完整，符合 TypeScript 规范

---

#### TASK-020: 创建 queryTabs Store
**优先级**: P0
**依赖**: TASK-019

- [ ] 创建 `stores/queryTabs.ts`
- [ ] 实现基础 CRUD 操作 (createTab, closeTab, switchTab, updateTab)
- [ ] 实现重命名和排序功能
- [ ] 实现 LocalStorage 持久化
- [ ] 编写单元测试

**验收标准**:
- Store 功能完整可用
- 持久化正常工作

---

### 3.2 组件开发阶段

#### TASK-021: 开发 QueryTabs 标签栏组件
**优先级**: P0
**依赖**: TASK-020

- [ ] 创建 `components/query/QueryTabs.vue`
- [ ] 实现标签渲染和切换
- [ ] 实现新增标签按钮
- [ ] 实现关闭标签功能
- [ ] 实现双击重命名
- [ ] 实现拖拽排序 (使用 Vue.Draggable)
- [ ] 实现右键菜单

**验收标准**:
- 标签切换流畅
- 拖拽排序正常
- 样式与现有 UI 风格一致

---

#### TASK-022: 开发 QueryWorkspace 工作区组件
**优先级**: P0
**依赖**: TASK-021

- [ ] 创建 `components/query/QueryWorkspace.vue`
- [ ] 从 QueryView.vue 提取平台/数据库选择器
- [ ] 集成 SqlEditor 和 QueryResult 组件
- [ ] 实现与标签状态的双向绑定
- [ ] 处理模板选择事件

**验收标准**:
- 单个工作区功能与原 QueryView 一致
- 状态与 Store 正确同步

---

#### TASK-023: 重构 QueryView 页面
**优先级**: P0
**依赖**: TASK-021, TASK-022

- [ ] 集成 QueryTabs 组件
- [ ] 使用 QueryWorkspace 替换原有内容区
- [ ] 实现标签切换时的工作区切换
- [ ] 处理未保存内容的关闭确认
- [ ] 实现键盘快捷键

**验收标准**:
- 多标签功能完整可用
- 原有功能无回归

---

### 3.3 优化阶段

#### TASK-024: 性能优化
**优先级**: P1
**依赖**: TASK-023

- [ ] 使用 `v-show` 或 `KeepAlive` 缓存标签内容
- [ ] 优化 Monaco Editor 实例管理
- [ ] 添加 SQL 内容变更的防抖保存
- [ ] 测试多标签性能（10个标签场景）

**验收标准**:
- 切换标签响应时间 < 100ms
- 10 个标签时内存占用合理

---

#### TASK-025: 边界情况处理
**优先级**: P1
**依赖**: TASK-023

- [ ] 处理最大标签数限制（10个）
- [ ] 处理最后一个标签的关闭限制
- [ ] 处理存储空间不足的情况
- [ ] 添加错误边界处理

**验收标准**:
- 边界情况有合理提示
- 不会出现异常崩溃

---

### 3.4 任务依赖关系

```
TASK-019 ─────────────────────────────────────┐
    │                                         │
    ▼                                         │
TASK-020 ─────────────────────────────────────┤
    │                                         │
    ├─────────────┬───────────────┐           │
    ▼             ▼               │           │
TASK-021    TASK-022              │           │
    │             │               │           │
    └──────┬──────┘               │           │
           ▼                      │           │
       TASK-023 ──────────────────┘           │
           │                                  │
           ├─────────────┬────────────────────┘
           ▼             ▼
       TASK-024     TASK-025
```

---

## 四、技术考量

### 4.1 Monaco Editor 实例管理

每个标签对应一个独立的 Monaco Editor 实例，需要注意：

1. **延迟初始化**：仅在标签首次激活时初始化编辑器
2. **实例复用**：使用 `KeepAlive` 保持编辑器实例
3. **内存释放**：关闭标签时销毁编辑器实例
4. **状态同步**：编辑器内容与 Store 保持同步

### 4.2 拖拽排序实现

推荐使用 `vuedraggable` 库：

```vue
<draggable
  v-model="tabs"
  item-key="id"
  @end="onDragEnd"
>
  <template #item="{ element }">
    <TabItem :tab="element" />
  </template>
</draggable>
```

### 4.3 LocalStorage 容量限制

- 单个标签 SQL 内容限制：100KB
- 总存储大小限制：5MB
- 超出限制时清理最旧的查询结果

---

## 五、风险评估

| 风险 | 可能性 | 影响 | 缓解措施 |
|------|--------|------|----------|
| Monaco Editor 多实例内存占用 | 中 | 中 | 使用懒加载和 KeepAlive 优化 |
| LocalStorage 容量不足 | 低 | 低 | 实现自动清理策略 |
| 标签状态同步问题 | 中 | 高 | 使用响应式 Store 确保数据一致 |
| 页面刷新丢失未保存内容 | 高 | 中 | 实现自动保存和离开确认 |

---

## 六、验收清单

- [ ] 可以创建、关闭、切换多个查询标签
- [ ] 每个标签的平台/数据库/SQL/结果相互独立
- [ ] 标签可以重命名和拖拽排序
- [ ] 页面刷新后标签状态正确恢复
- [ ] 关闭未保存标签时有确认提示
- [ ] 键盘快捷键正常工作
- [ ] 性能满足要求（10标签场景）
- [ ] 无功能回归

---

## 变更历史

| 版本 | 日期 | 作者 | 变更内容 |
|------|------|------|----------|
| 1.0 | 2026-01-28 | Claude | 初始版本 |
