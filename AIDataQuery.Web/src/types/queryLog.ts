import { QueryStatus, QueryParams } from './api'

// 查询日志相关类型

/** 查询日志DTO */
export interface QueryLogDto {
  id: number
  username: string
  platformCode?: string
  databaseName?: string
  sqlContent: string
  executionTimeMs: number
  rowCount: number
  status: QueryStatus
  errorMessage?: string
  clientIp?: string
  createdAt: string
}

/** 查询日志筛选参数 */
export interface QueryLogParams extends QueryParams {
  platformCode?: string
  status?: QueryStatus
  startDate?: string
  endDate?: string
}
