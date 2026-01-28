// 查询相关类型

/** 查询请求 */
export interface QueryRequest {
  platformCode: string
  connectionId: number
  sql: string
}

/** 查询结果 */
export interface QueryResult {
  success: boolean
  errorMessage?: string
  columns: string[]
  rows: Record<string, unknown>[]
  totalRows: number
  executionTimeMs: number
}

/** 导出请求 */
export interface ExportRequest {
  platformCode: string
  connectionId: number
  sql: string
  format: 'csv' | 'excel'
}

/** 表信息 */
export interface TableInfo {
  name: string
  schema?: string
  type?: string
}

/** 列信息 */
export interface ColumnInfo {
  name: string
  dataType: string
  isNullable: boolean
  maxLength?: number
  precision?: number
  scale?: number
}
