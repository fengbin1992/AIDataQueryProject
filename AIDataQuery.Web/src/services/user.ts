import { request } from './api'
import type {
  UserDto,
  CreateUserRequest,
  UpdateUserRequest,
  SetPermissionsRequest,
  PagedResult,
  QueryParams
} from '@/types'

/**
 * 用户管理服务（管理员专用）
 */
export const userApi = {
  /**
   * 获取用户列表（分页）
   */
  getUsers(params?: QueryParams) {
    return request.get<PagedResult<UserDto>>('/users', { params })
  },

  /**
   * 获取用户详情
   */
  getUser(id: number) {
    return request.get<UserDto>(`/users/${id}`)
  },

  /**
   * 创建用户
   */
  createUser(data: CreateUserRequest) {
    return request.post<UserDto>('/users', data)
  },

  /**
   * 更新用户
   */
  updateUser(id: number, data: UpdateUserRequest) {
    return request.put<UserDto>(`/users/${id}`, data)
  },

  /**
   * 设置用户平台权限
   */
  setPermissions(id: number, platformCodes: string[]) {
    return request.put(`/users/${id}/permissions`, platformCodes)
  },

  /**
   * 设置用户所有权限（平台权限和数据库连接权限）
   */
  setAllPermissions(id: number, data: SetPermissionsRequest) {
    return request.put(`/users/${id}/all-permissions`, data)
  },

  /**
   * 禁用用户
   */
  deleteUser(id: number) {
    return request.delete(`/users/${id}`)
  }
}
