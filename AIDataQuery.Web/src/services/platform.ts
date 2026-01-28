import { request } from './api'
import type {
  PlatformDto,
  CreatePlatformRequest,
  UpdatePlatformRequest,
  DatabaseConnectionDto,
  CreateConnectionRequest
} from '@/types'

/**
 * 平台与数据库连接服务
 */
export const platformApi = {
  // ================== 平台管理 ==================

  /**
   * 获取当前用户有权限的平台列表
   */
  getPlatforms() {
    return request.get<PlatformDto[]>('/platforms')
  },

  /**
   * 获取所有平台列表（管理员专用）
   */
  getAllPlatforms() {
    return request.get<PlatformDto[]>('/platforms/all')
  },

  /**
   * 获取单个平台详情（管理员专用）
   */
  getPlatformById(id: number) {
    return request.get<PlatformDto>(`/platforms/${id}`)
  },

  /**
   * 创建平台（管理员专用）
   */
  createPlatform(data: CreatePlatformRequest) {
    return request.post<PlatformDto>('/platforms', data)
  },

  /**
   * 更新平台（管理员专用）
   */
  updatePlatform(id: number, data: UpdatePlatformRequest) {
    return request.put(`/platforms/${id}`, data)
  },

  /**
   * 切换平台状态（管理员专用）
   */
  togglePlatformStatus(id: number) {
    return request.put(`/platforms/${id}/toggle-status`)
  },

  /**
   * 删除平台（管理员专用）
   */
  deletePlatform(id: number) {
    return request.delete(`/platforms/${id}`)
  },

  // ================== 数据库连接管理 ==================

  /**
   * 获取指定平台下的数据库连接列表
   */
  getConnections(platformCode: string) {
    return request.get<DatabaseConnectionDto[]>(`/platforms/${platformCode}/connections`)
  },

  /**
   * 获取所有数据库连接列表（管理员专用，包含连接字符串）
   */
  getAllConnections() {
    return request.get<DatabaseConnectionDto[]>('/platforms/admin/connections')
  },

  /**
   * 获取单个数据库连接详情（管理员专用，包含连接字符串）
   */
  getConnectionById(id: number) {
    return request.get<DatabaseConnectionDto>(`/platforms/connection/${id}`)
  },

  /**
   * 创建数据库连接（管理员专用）
   */
  createConnection(data: CreateConnectionRequest) {
    return request.post<DatabaseConnectionDto>('/platforms/connections', data)
  },

  /**
   * 更新数据库连接（管理员专用）
   */
  updateConnection(id: number, data: CreateConnectionRequest) {
    return request.put(`/platforms/connections/${id}`, data)
  },

  /**
   * 删除数据库连接（管理员专用）
   */
  deleteConnection(id: number) {
    return request.delete(`/platforms/connections/${id}`)
  },

  /**
   * 测试数据库连接（管理员专用）
   */
  testConnection(id: number) {
    return request.post<boolean>(`/platforms/connections/${id}/test`)
  }
}
