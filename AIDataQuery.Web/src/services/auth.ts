import { request } from './api'
import type { LoginRequest, LoginResponse, UserInfo, ChangePasswordRequest, ApiResponse } from '@/types'

/**
 * 认证服务
 */
export const authApi = {
  /**
   * 用户登录
   */
  login(data: LoginRequest) {
    return request.post<LoginResponse>('/auth/login', data)
  },

  /**
   * 用户登出
   */
  logout() {
    return request.post('/auth/logout')
  },

  /**
   * 获取当前用户信息
   */
  getCurrentUser() {
    return request.get<UserInfo>('/auth/me')
  },

  /**
   * 修改密码
   */
  changePassword(data: ChangePasswordRequest) {
    return request.post('/auth/change-password', data)
  }
}
