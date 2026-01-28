// 平台与数据库连接相关类型

/** 平台DTO */
export interface PlatformDto {
  id: number
  code: string
  name: string
  description?: string
  isActive: boolean
  connectionCount: number
}

/** 创建平台请求 */
export interface CreatePlatformRequest {
  code: string
  name: string
  description?: string
  sortOrder?: number
}

/** 更新平台请求 */
export interface UpdatePlatformRequest {
  name: string
  description?: string
  sortOrder?: number
}

/** 数据库连接DTO */
export interface DatabaseConnectionDto {
  id: number
  name: string
  platformCode: string
  databaseType: string
  description?: string
  /** 是否为正式环境数据库 */
  isProduction: boolean
  isActive: boolean
  /** 连接字符串（仅管理员可见） */
  connectionString?: string
}

/** 创建数据库连接请求 */
export interface CreateConnectionRequest {
  name: string
  platformCode: string
  connectionString: string
  databaseType?: string
  description?: string
  /** 是否为正式环境数据库 */
  isProduction?: boolean
}

/** 更新数据库连接请求 */
export interface UpdateConnectionRequest extends CreateConnectionRequest {}
