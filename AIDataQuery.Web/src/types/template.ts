// 模板相关类型

/** 模板模块DTO */
export interface TemplateModuleDto {
  id: number
  name: string
  parentId?: number
  icon?: string
  sortOrder: number
  children: TemplateModuleDto[]
  templates: TemplateDto[]
}

/** 模板DTO */
export interface TemplateDto {
  id: number
  moduleId: number
  moduleName: string
  name: string
  sqlContent: string
  description?: string
  isPublic: boolean
  createdBy: number
  createdByName: string
  createdAt: string
}

/** 创建模板请求 */
export interface CreateTemplateRequest {
  moduleId: number
  name: string
  sqlContent: string
  description?: string
  isPublic?: boolean
}

/** 更新模板请求 */
export interface UpdateTemplateRequest {
  name?: string
  sqlContent?: string
  description?: string
  isPublic?: boolean
}

/** 创建模块请求 */
export interface CreateModuleRequest {
  name: string
  parentId?: number
  icon?: string
  sortOrder?: number
}

/** 更新模块请求 */
export interface UpdateModuleRequest {
  name?: string
  parentId?: number
  icon?: string
  sortOrder?: number
}
