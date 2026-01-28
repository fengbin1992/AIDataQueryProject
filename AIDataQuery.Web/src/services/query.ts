import { request } from './api'
import type {
  QueryRequest,
  QueryResult,
  ExportRequest,
  TableInfo,
  ColumnInfo
} from '@/types'

/**
 * SQL 查询服务
 */
export const queryApi = {
  /**
   * 执行 SQL 查询
   */
  execute(data: QueryRequest) {
    return request.post<QueryResult>('/query/execute', data)
  },

  /**
   * 获取数据库表列表
   */
  getTables(connectionId: number) {
    return request.get<TableInfo[]>('/query/tables', { params: { connectionId } })
  },

  /**
   * 获取表字段列表
   */
  getColumns(connectionId: number, tableName: string) {
    return request.get<ColumnInfo[]>('/query/columns', {
      params: { connectionId, tableName }
    })
  },

  /**
   * 导出查询结果
   */
  export(data: ExportRequest) {
    return request.download('/query/export', data)
  },

  /**
   * 测试数据库连接
   */
  testConnection(connectionId: number) {
    return request.post<boolean>('/query/test-connection', null, {
      params: { connectionId }
    })
  }
}
