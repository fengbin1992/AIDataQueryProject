import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { ElMessage } from 'element-plus'
import * as dataSecurityApi from '@/services/dataSecurity'
import type {
  MaskingRule,
  CreateMaskingRuleRequest,
  UpdateMaskingRuleRequest,
  SensitiveFieldMark,
  CreateSensitiveFieldMarkRequest,
  BatchCreateSensitiveFieldMarksRequest,
  TableSchema
} from '@/types/dataSecurity'

export const useDataSecurityStore = defineStore('dataSecurity', () => {
  // ==================== 脱敏规则 State ====================
  const maskingRules = ref<MaskingRule[]>([])
  const loadingRules = ref(false)

  // ==================== 敏感字段标记 State ====================
  const fieldMarks = ref<SensitiveFieldMark[]>([])
  const loadingMarks = ref(false)
  const tableSchema = ref<TableSchema | null>(null)
  const loadingSchema = ref(false)

  // ==================== 计算属性 ====================
  const activeRules = computed(() => maskingRules.value.filter(r => r.isActive))

  // ==================== 脱敏规则 Actions ====================

  async function loadMaskingRules() {
    loadingRules.value = true
    try {
      const { data } = await dataSecurityApi.getMaskingRules()
      if (data.success) {
        maskingRules.value = data.data || []
      }
    } catch (error) {
      console.error('Failed to load masking rules:', error)
    } finally {
      loadingRules.value = false
    }
  }

  async function createMaskingRule(request: CreateMaskingRuleRequest) {
    const { data } = await dataSecurityApi.createMaskingRule(request)
    if (data.success && data.data) {
      maskingRules.value.unshift(data.data)
      ElMessage.success(data.message || '创建成功')
      return true
    }
    ElMessage.error(data.message || '创建失败')
    return false
  }

  async function updateMaskingRule(id: number, request: UpdateMaskingRuleRequest) {
    const { data } = await dataSecurityApi.updateMaskingRule(id, request)
    if (data.success && data.data) {
      const index = maskingRules.value.findIndex(r => r.id === id)
      if (index !== -1) {
        maskingRules.value[index] = data.data
      }
      ElMessage.success(data.message || '更新成功')
      return true
    }
    ElMessage.error(data.message || '更新失败')
    return false
  }

  async function deleteMaskingRule(id: number) {
    const { data } = await dataSecurityApi.deleteMaskingRule(id)
    if (data.success) {
      maskingRules.value = maskingRules.value.filter(r => r.id !== id)
      ElMessage.success(data.message || '删除成功')
      return true
    }
    ElMessage.error(data.message || '删除失败')
    return false
  }

  async function toggleMaskingRule(id: number) {
    const { data } = await dataSecurityApi.toggleMaskingRule(id)
    if (data.success) {
      const rule = maskingRules.value.find(r => r.id === id)
      if (rule) {
        rule.isActive = !rule.isActive
      }
      return true
    }
    ElMessage.error(data.message || '操作失败')
    return false
  }

  // ==================== 敏感字段标记 Actions ====================

  async function loadFieldMarks(connectionId?: number) {
    loadingMarks.value = true
    try {
      const { data } = await dataSecurityApi.getSensitiveFieldMarks(connectionId)
      if (data.success) {
        fieldMarks.value = data.data || []
      }
    } catch (error) {
      console.error('Failed to load field marks:', error)
    } finally {
      loadingMarks.value = false
    }
  }

  async function createFieldMark(request: CreateSensitiveFieldMarkRequest) {
    const { data } = await dataSecurityApi.createSensitiveFieldMark(request)
    if (data.success && data.data) {
      fieldMarks.value.push(data.data)
      ElMessage.success(data.message || '标记成功')
      return true
    }
    ElMessage.error(data.message || '标记失败')
    return false
  }

  async function batchCreateFieldMarks(request: BatchCreateSensitiveFieldMarksRequest) {
    const { data } = await dataSecurityApi.batchCreateSensitiveFieldMarks(request)
    if (data.success) {
      await loadFieldMarks(request.connectionId)
      ElMessage.success(data.message || `成功标记 ${data.data} 个字段`)
      return true
    }
    ElMessage.error(data.message || '批量标记失败')
    return false
  }

  async function deleteFieldMark(id: number) {
    const { data } = await dataSecurityApi.deleteSensitiveFieldMark(id)
    if (data.success) {
      fieldMarks.value = fieldMarks.value.filter(m => m.id !== id)
      ElMessage.success(data.message || '删除成功')
      return true
    }
    ElMessage.error(data.message || '删除失败')
    return false
  }

  async function loadTableSchema(connectionId: number, tableName: string) {
    loadingSchema.value = true
    try {
      const { data } = await dataSecurityApi.getTableSchemaWithSensitivity(connectionId, tableName)
      if (data.success) {
        tableSchema.value = data.data || null
      }
    } catch (error) {
      console.error('Failed to load table schema:', error)
    } finally {
      loadingSchema.value = false
    }
  }

  return {
    // State
    maskingRules,
    loadingRules,
    fieldMarks,
    loadingMarks,
    tableSchema,
    loadingSchema,

    // Computed
    activeRules,

    // Actions - 脱敏规则
    loadMaskingRules,
    createMaskingRule,
    updateMaskingRule,
    deleteMaskingRule,
    toggleMaskingRule,

    // Actions - 敏感字段
    loadFieldMarks,
    createFieldMark,
    batchCreateFieldMarks,
    deleteFieldMark,
    loadTableSchema
  }
})
