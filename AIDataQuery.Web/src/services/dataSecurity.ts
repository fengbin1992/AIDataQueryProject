import { request } from './api'
import type {
  MaskingRule,
  CreateMaskingRuleRequest,
  UpdateMaskingRuleRequest,
  SensitiveFieldMark,
  CreateSensitiveFieldMarkRequest,
  BatchCreateSensitiveFieldMarksRequest,
  TableSchema
} from '@/types/dataSecurity'

// ==================== 脱敏规则管理 ====================

export function getMaskingRules() {
  return request.get<MaskingRule[]>('/data-security/masking-rules')
}

export function getMaskingRule(id: number) {
  return request.get<MaskingRule>(`/data-security/masking-rules/${id}`)
}

export function createMaskingRule(data: CreateMaskingRuleRequest) {
  return request.post<MaskingRule>('/data-security/masking-rules', data)
}

export function updateMaskingRule(id: number, data: UpdateMaskingRuleRequest) {
  return request.put<MaskingRule>(`/data-security/masking-rules/${id}`, data)
}

export function deleteMaskingRule(id: number) {
  return request.delete<void>(`/data-security/masking-rules/${id}`)
}

export function toggleMaskingRule(id: number) {
  return request.put<void>(`/data-security/masking-rules/${id}/toggle`)
}

// ==================== 敏感字段标记 ====================

export function getSensitiveFieldMarks(connectionId?: number) {
  return request.get<SensitiveFieldMark[]>('/data-security/sensitive-fields', {
    params: { connectionId }
  })
}

export function createSensitiveFieldMark(data: CreateSensitiveFieldMarkRequest) {
  return request.post<SensitiveFieldMark>('/data-security/sensitive-fields', data)
}

export function batchCreateSensitiveFieldMarks(data: BatchCreateSensitiveFieldMarksRequest) {
  return request.post<number>('/data-security/sensitive-fields/batch', data)
}

export function deleteSensitiveFieldMark(id: number) {
  return request.delete<void>(`/data-security/sensitive-fields/${id}`)
}

export function getTableSchemaWithSensitivity(connectionId: number, tableName: string) {
  return request.get<TableSchema>('/data-security/sensitive-fields/schema', {
    params: { connectionId, tableName }
  })
}
