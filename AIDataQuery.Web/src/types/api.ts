// API 通用响应类型

/** API 响应基础结构（带数据） */
export interface ApiResponse<T = unknown> {
  success: boolean
  message?: string
  data?: T
}

/** 分页查询参数 */
export interface QueryParams {
  pageIndex?: number
  pageSize?: number
  keyword?: string
}

/** 分页结果 */
export interface PagedResult<T> {
  items: T[]
  totalCount: number
  pageIndex: number
  pageSize: number
  totalPages: number
  hasPrevious: boolean
  hasNext: boolean
}

/** 用户角色枚举 */
export enum UserRole {
  User = 0,
  Admin = 1
}

/** 用户状态枚举 */
export enum UserStatus {
  Disabled = 0,
  Active = 1
}

/** 查询状态枚举 */
export enum QueryStatus {
  Failed = 0,
  Success = 1
}
