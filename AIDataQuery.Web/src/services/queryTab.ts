import { request } from './api'
import type {
  QueryTabDto,
  CreateQueryTabRequest,
  UpdateQueryTabRequest,
  ReorderQueryTabsRequest
} from '@/types'

/**
 * 查询标签页 API 服务
 */
export const queryTabApi = {
  /**
   * 获取当前用户的所有标签页
   */
  getTabs() {
    return request.get<QueryTabDto[]>('/query-tabs')
  },

  /**
   * 获取单个标签页
   */
  getTab(id: number) {
    return request.get<QueryTabDto>(`/query-tabs/${id}`)
  },

  /**
   * 创建标签页
   */
  createTab(data: CreateQueryTabRequest) {
    return request.post<QueryTabDto>('/query-tabs', data)
  },

  /**
   * 更新标签页
   */
  updateTab(id: number, data: UpdateQueryTabRequest) {
    return request.put<QueryTabDto>(`/query-tabs/${id}`, data)
  },

  /**
   * 删除标签页
   */
  deleteTab(id: number) {
    return request.delete(`/query-tabs/${id}`)
  },

  /**
   * 调整标签页排序
   */
  reorderTabs(data: ReorderQueryTabsRequest) {
    return request.put('/query-tabs/reorder', data)
  }
}
