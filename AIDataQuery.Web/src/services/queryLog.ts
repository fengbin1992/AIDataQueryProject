import { request } from './api'
import type { QueryLogDto, QueryLogParams, PagedResult } from '@/types'

/**
 * 查询日志服务
 */
export const queryLogApi = {
  /**
   * 获取查询历史（普通用户只能看自己的记录）
   */
  getLogs(params?: QueryLogParams) {
    return request.get<PagedResult<QueryLogDto>>('/query-logs', { params })
  },

  /**
   * 获取所有用户的查询历史（管理员专用）
   */
  getAllLogs(params?: QueryLogParams) {
    return request.get<PagedResult<QueryLogDto>>('/query-logs/all', { params })
  },

  /**
   * 获取查询历史详情
   */
  getLog(id: number) {
    return request.get<QueryLogDto>(`/query-logs/${id}`)
  }
}
