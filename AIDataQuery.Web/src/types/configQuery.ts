// 配置查询相关类型

/** 参数类型枚举 */
export type ParamType = 'text' | 'number' | 'date' | 'daterange' | 'select' | 'multiselect'

/** 选项项 */
export interface OptionItem {
  label: string
  value: string
}

/** 选项配置 */
export interface OptionsConfig {
  mode: 'static' | 'dynamic'
  options?: OptionItem[]
  sql?: string
  connectionId?: number
  refreshOnOpen?: boolean
}

/** 扩展配置 */
export interface ExtraConfig {
  // 数字类型配置
  min?: number
  max?: number
  precision?: number
  step?: number
  // 日期类型配置
  format?: string
  defaultType?: string
  customDefault?: string
  // 日期范围配置
  startParamName?: string
  endParamName?: string
  // 多选配置
  separator?: string
  wrapper?: string
}

/** 配置查询列表项 */
export interface ConfigQueryListItem {
  id: number
  name: string
  description?: string
  isPublic: boolean
  createdBy: number
  createdByName: string
  isOwner: boolean
  canEdit: boolean
  createdAt: string
}

/** 配置查询参数 */
export interface ConfigQueryParameter {
  id: number
  paramName: string
  paramLabel: string
  paramType: ParamType
  isRequired: boolean
  defaultValue?: string
  placeholder?: string
  optionsConfig?: OptionsConfig
  validationRule?: string
  extraConfig?: ExtraConfig
  sortOrder: number
}

/** 配置查询详情 */
export interface ConfigQueryDetail {
  id: number
  name: string
  description?: string
  sqlContent: string
  connectionId?: number
  connectionName?: string
  isPublic: boolean
  createdBy: number
  createdByName: string
  isOwner: boolean
  canEdit: boolean
  sortOrder: number
  createdAt: string
  updatedAt?: string
  parameters: ConfigQueryParameter[]
}

/** 创建参数请求 */
export interface CreateConfigQueryParameterRequest {
  paramName: string
  paramLabel: string
  paramType: ParamType
  isRequired: boolean
  defaultValue?: string
  placeholder?: string
  optionsConfig?: OptionsConfig
  validationRule?: string
  extraConfig?: ExtraConfig
  sortOrder: number
}

/** 创建配置查询请求 */
export interface CreateConfigQueryRequest {
  name: string
  description?: string
  sqlContent: string
  connectionId?: number
  isPublic: boolean
  sortOrder?: number
  parameters: CreateConfigQueryParameterRequest[]
}

/** 更新配置查询请求 */
export interface UpdateConfigQueryRequest {
  name?: string
  description?: string
  sqlContent?: string
  connectionId?: number
  isPublic?: boolean
  sortOrder?: number
  parameters?: CreateConfigQueryParameterRequest[]
}

/** 执行配置查询请求 */
export interface ExecuteConfigQueryRequest {
  connectionId?: number
  parameters: Record<string, unknown>
}

/** 参数预设 */
export interface ConfigQueryParamPreset {
  id: number
  name: string
  paramValues: Record<string, unknown>
  isDefault: boolean
  createdAt: string
}

/** 创建参数预设请求 */
export interface CreateParamPresetRequest {
  name: string
  paramValues: Record<string, unknown>
  isDefault: boolean
}

/** 更新参数预设请求 */
export interface UpdateParamPresetRequest {
  name?: string
  paramValues?: Record<string, unknown>
  isDefault?: boolean
}

/** 解析参数响应 */
export interface ParseParamsResponse {
  parameters: string[]
}

/** 获取选项请求 */
export interface GetOptionsRequest {
  connectionId: number
  sql: string
}

/** 获取选项响应 */
export interface GetOptionsResponse {
  options: OptionItem[]
}

/** 配置查询导出格式 */
export interface ConfigQueryExport {
  name: string
  description?: string
  sql: string
  connectionId?: number
  parameters: ConfigQueryParameterExport[]
}

/** 参数导出格式 */
export interface ConfigQueryParameterExport {
  name: string
  label: string
  type: ParamType
  required: boolean
  defaultValue?: string
  placeholder?: string
  options?: OptionsConfig
  validationRule?: string
  extraConfig?: ExtraConfig
}

/** 分页列表响应 */
export interface PagedListResponse<T> {
  items: T[]
  total: number
  pageIndex: number
  pageSize: number
}
