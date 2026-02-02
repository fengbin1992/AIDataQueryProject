import { request } from './api'
import type {
  ConfigQueryListItem,
  ConfigQueryDetail,
  CreateConfigQueryRequest,
  UpdateConfigQueryRequest,
  ExecuteConfigQueryRequest,
  ConfigQueryParamPreset,
  CreateParamPresetRequest,
  UpdateParamPresetRequest,
  ParseParamsResponse,
  GetOptionsRequest,
  GetOptionsResponse,
  ConfigQueryExport,
  PagedListResponse
} from '@/types/configQuery'
import type { QueryResult } from '@/types'

/**
 * 配置查询服务
 */
export const configQueryApi = {
  // ==================== 配置查询 CRUD ====================

  /**
   * 获取配置查询列表
   */
  getList(params?: { keyword?: string; pageIndex?: number; pageSize?: number }) {
    return request.get<PagedListResponse<ConfigQueryListItem>>('/config-queries', { params })
  },

  /**
   * 获取配置查询详情
   */
  getById(id: number) {
    return request.get<ConfigQueryDetail>(`/config-queries/${id}`)
  },

  /**
   * 创建配置查询
   */
  create(data: CreateConfigQueryRequest) {
    return request.post<{ id: number }>('/config-queries', data)
  },

  /**
   * 更新配置查询
   */
  update(id: number, data: UpdateConfigQueryRequest) {
    return request.put(`/config-queries/${id}`, data)
  },

  /**
   * 删除配置查询
   */
  delete(id: number) {
    return request.delete(`/config-queries/${id}`)
  },

  /**
   * 复制配置查询
   */
  copy(id: number) {
    return request.post<{ id: number }>(`/config-queries/${id}/copy`)
  },

  // ==================== 执行相关 ====================

  /**
   * 执行配置查询
   */
  execute(id: number, data: ExecuteConfigQueryRequest) {
    return request.post<QueryResult>(`/config-queries/${id}/execute`, data)
  },

  /**
   * 解析SQL中的参数
   */
  parseParams(sql: string) {
    return request.post<ParseParamsResponse>('/config-queries/parse-params', { sql })
  },

  /**
   * 获取动态选项
   */
  getOptions(data: GetOptionsRequest) {
    return request.post<GetOptionsResponse>('/config-queries/options', data)
  },

  // ==================== 导入导出 ====================

  /**
   * 导入配置
   */
  import(json: string) {
    return request.post<{ id: number }>('/config-queries/import', { json })
  },

  /**
   * 导出配置
   */
  export(id: number) {
    return request.get<ConfigQueryExport>(`/config-queries/${id}/export`)
  },

  // ==================== 参数预设 ====================

  /**
   * 获取参数预设列表
   */
  getPresets(configQueryId: number) {
    return request.get<ConfigQueryParamPreset[]>(`/config-queries/${configQueryId}/presets`)
  },

  /**
   * 创建参数预设
   */
  createPreset(configQueryId: number, data: CreateParamPresetRequest) {
    return request.post<{ id: number }>(`/config-queries/${configQueryId}/presets`, data)
  },

  /**
   * 更新参数预设
   */
  updatePreset(configQueryId: number, presetId: number, data: UpdateParamPresetRequest) {
    return request.put(`/config-queries/${configQueryId}/presets/${presetId}`, data)
  },

  /**
   * 删除参数预设
   */
  deletePreset(configQueryId: number, presetId: number) {
    return request.delete(`/config-queries/${configQueryId}/presets/${presetId}`)
  }
}
