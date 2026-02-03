// 数据安全相关类型定义

// ==================== 脱敏类型枚举 ====================
export enum MaskType {
  Phone = 1,
  IdCard = 2,
  Email = 3,
  BankCard = 4,
  Name = 5,
  Address = 6,
  Amount = 7,
  Full = 8,
  Custom = 99
}

export const MaskTypeOptions = [
  { value: MaskType.Phone, label: '手机号', example: '138****5678' },
  { value: MaskType.IdCard, label: '身份证', example: '310***********1234' },
  { value: MaskType.Email, label: '邮箱', example: 'z***@example.com' },
  { value: MaskType.BankCard, label: '银行卡', example: '6222****0123' },
  { value: MaskType.Name, label: '姓名', example: '张**' },
  { value: MaskType.Address, label: '地址', example: '北京市朝阳区******' },
  { value: MaskType.Amount, label: '金额', example: '******' },
  { value: MaskType.Full, label: '完全隐藏', example: '******' },
  { value: MaskType.Custom, label: '自定义', example: '自定义规则' }
]

// ==================== 脱敏规则 ====================
export interface MaskingRule {
  id: number
  name: string
  fieldPattern: string
  maskType: MaskType
  maskTypeName: string
  maskConfig?: string
  priority: number
  isActive: boolean
  description?: string
  createdAt: string
}

export interface CreateMaskingRuleRequest {
  name: string
  fieldPattern: string
  maskType: MaskType
  maskConfig?: string
  priority?: number
  description?: string
}

export interface UpdateMaskingRuleRequest {
  name: string
  fieldPattern: string
  maskType: MaskType
  maskConfig?: string
  priority: number
  description?: string
}

// ==================== 敏感字段标记 ====================
export interface SensitiveFieldMark {
  id: number
  connectionId: number
  connectionName: string
  tableName: string
  fieldName: string
  maskType: MaskType
  maskTypeName: string
  maskConfig?: string
  description?: string
  markedBy: number
  markedByName: string
  createdAt: string
}

export interface CreateSensitiveFieldMarkRequest {
  connectionId: number
  tableName: string
  fieldName: string
  maskType: MaskType
  maskConfig?: string
  description?: string
}

export interface BatchCreateSensitiveFieldMarksRequest {
  connectionId: number
  fields: SensitiveFieldMarkItem[]
}

export interface SensitiveFieldMarkItem {
  tableName: string
  fieldName: string
  maskType: MaskType
  description?: string
}

// ==================== 表结构 ====================
export interface FieldSchema {
  name: string
  dataType: string
  isSensitive: boolean
  matchedRule?: string
  maskType?: MaskType
  isManuallyMarked: boolean
}

export interface TableSchema {
  tableName: string
  fields: FieldSchema[]
}
