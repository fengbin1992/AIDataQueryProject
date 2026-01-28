// 认证相关类型

/** 登录请求 */
export interface LoginRequest {
  username: string
  password: string
  rememberMe?: boolean
}

/** 登录响应 */
export interface LoginResponse {
  token: string
  expiresAt: string
  user: UserInfo
}

/** 用户信息（登录后） */
export interface UserInfo {
  id: number
  username: string
  nickname: string
  email?: string
  role: string
  themePreference: string
  platforms: string[]
}

/** 修改密码请求 */
export interface ChangePasswordRequest {
  currentPassword: string
  newPassword: string
  confirmPassword: string
}
