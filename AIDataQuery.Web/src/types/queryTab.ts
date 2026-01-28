// 查询标签页相关类型
import type { QueryResult } from './query'

/** 单个查询标签 */
export interface QueryTab {
  /** 唯一标识 (UUID) */
  id: string
  /** 服务端 ID（已保存的标签才有） */
  serverId?: number
  /** 标签名称 */
  name: string
  /** 选中的平台编码 */
  platformCode: string | null
  /** 选中的数据库连接 ID */
  connectionId: number | null
  /** SQL 编辑器内容 */
  sql: string
  /** 查询结果 */
  queryResult: QueryResult | null
  /** 是否正在查询 */
  isQuerying: boolean
  /** 是否有未保存的更改 */
  isDirty: boolean
  /** 是否已保存到服务端 */
  isSaved: boolean
  /** 创建时间戳 */
  createdAt: number
}

/** 标签页状态 */
export interface QueryTabsState {
  /** 所有标签 */
  tabs: QueryTab[]
  /** 当前激活的标签 ID */
  activeTabId: string
}

/** 持久化到 LocalStorage 的数据结构 */
export interface StoredTabsData {
  /** 数据版本号 */
  version: number
  /** 标签数据（不含查询结果） */
  tabs: Omit<QueryTab, 'queryResult' | 'isQuerying'>[]
  /** 当前激活标签 ID */
  activeTabId: string
  /** 保存时间戳 */
  savedAt: number
}

/** 服务端返回的标签 DTO */
export interface QueryTabDto {
  id: number
  name: string
  platformCode: string | null
  connectionId: number | null
  sqlContent: string | null
  sortOrder: number
  createdAt: string
  updatedAt: string
}

/** 创建标签请求 */
export interface CreateQueryTabRequest {
  name: string
  platformCode?: string | null
  connectionId?: number | null
  sqlContent?: string | null
}

/** 更新标签请求 */
export interface UpdateQueryTabRequest {
  name?: string
  platformCode?: string | null
  connectionId?: number | null
  sqlContent?: string | null
}

/** 调整排序请求 */
export interface ReorderQueryTabsRequest {
  tabIds: number[]
}

/** 创建标签的选项 */
export interface CreateTabOptions {
  /** 标签名称 */
  name?: string
  /** 平台编码 */
  platformCode?: string | null
  /** 数据库连接 ID */
  connectionId?: number | null
  /** SQL 内容 */
  sql?: string
}
