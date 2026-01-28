import { UserRole, UserStatus } from './api'

// 用户管理相关类型

/** 用户DTO */
export interface UserDto {
  id: number
  username: string
  nickname: string
  email?: string
  role: UserRole
  status: UserStatus
  themePreference: string
  createdAt: string
  lastLoginAt?: string
  platformCodes: string[]
  connectionIds: number[]
}

/** 创建用户请求 */
export interface CreateUserRequest {
  username: string
  password: string
  nickname: string
  email?: string
  role?: UserRole
  platformCodes?: string[]
}

/** 更新用户请求 */
export interface UpdateUserRequest {
  nickname?: string
  email?: string
  role?: UserRole
  status?: UserStatus
}

/** 设置用户权限请求 */
export interface SetPermissionsRequest {
  platformCodes: string[]
  connectionIds: number[]
}
