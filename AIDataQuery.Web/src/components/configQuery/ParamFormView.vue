<template>
  <div class="param-form-view">
    <el-form :model="formValues" label-position="top" size="default">
      <el-row :gutter="16">
        <el-col
          v-for="param in parameters"
          :key="param.paramName"
          :span="getColSpan(param.paramType)"
        >
          <el-form-item
            :label="param.paramLabel"
            :required="param.isRequired"
          >
            <!-- 文本输入 -->
            <el-input
              v-if="param.paramType === 'text'"
              :model-value="formValues[param.paramName] as string"
              :placeholder="param.placeholder || '请输入'"
              clearable
              @update:model-value="(val: string) => { formValues[param.paramName] = val; emitChange() }"
            />

            <!-- 数字输入 -->
            <el-input-number
              v-else-if="param.paramType === 'number'"
              :model-value="formValues[param.paramName] as number"
              :placeholder="param.placeholder || '请输入'"
              :min="param.extraConfig?.min"
              :max="param.extraConfig?.max"
              :precision="param.extraConfig?.precision"
              :step="param.extraConfig?.step || 1"
              controls-position="right"
              style="width: 100%"
              @update:model-value="(val: number | undefined) => { formValues[param.paramName] = val ?? 0; emitChange() }"
            />

            <!-- 日期选择 -->
            <el-date-picker
              v-else-if="param.paramType === 'date'"
              :model-value="formValues[param.paramName] as string"
              type="date"
              :placeholder="param.placeholder || '请选择日期'"
              :format="param.extraConfig?.format || 'YYYY-MM-DD'"
              value-format="YYYY-MM-DD"
              style="width: 100%"
              @update:model-value="(val: string | null) => { formValues[param.paramName] = val ?? ''; emitChange() }"
            />

            <!-- 日期范围 -->
            <el-date-picker
              v-else-if="param.paramType === 'daterange'"
              :model-value="formValues[param.paramName] as [string, string]"
              type="daterange"
              range-separator="至"
              start-placeholder="开始日期"
              end-placeholder="结束日期"
              :format="param.extraConfig?.format || 'YYYY-MM-DD'"
              value-format="YYYY-MM-DD"
              style="width: 100%"
              @update:model-value="(val: [string, string] | null) => { formValues[param.paramName] = val ?? ['', '']; emitChange() }"
            />

            <!-- 下拉单选 -->
            <el-select
              v-else-if="param.paramType === 'select'"
              :model-value="formValues[param.paramName] as string | number"
              :placeholder="param.placeholder || '请选择'"
              clearable
              filterable
              style="width: 100%"
              @update:model-value="(val: string | number) => { formValues[param.paramName] = val; emitChange() }"
              @visible-change="(visible: boolean) => visible && loadDynamicOptions(param)"
            >
              <el-option
                v-for="opt in getOptions(param)"
                :key="opt.value"
                :label="opt.label"
                :value="opt.value"
              />
            </el-select>

            <!-- 下拉多选 -->
            <el-select
              v-else-if="param.paramType === 'multiselect'"
              :model-value="formValues[param.paramName] as (string | number)[]"
              :placeholder="param.placeholder || '请选择'"
              multiple
              clearable
              filterable
              collapse-tags
              collapse-tags-tooltip
              style="width: 100%"
              @update:model-value="(val: (string | number)[]) => { formValues[param.paramName] = val; emitChange() }"
              @visible-change="(visible: boolean) => visible && loadDynamicOptions(param)"
            >
              <el-option
                v-for="opt in getOptions(param)"
                :key="opt.value"
                :label="opt.label"
                :value="opt.value"
              />
            </el-select>
          </el-form-item>
        </el-col>
      </el-row>
    </el-form>
  </div>
</template>

<script setup lang="ts">
import { ref, watch, onMounted } from 'vue'
import { configQueryApi } from '@/services/configQuery'
import type { ConfigQueryParameter, OptionItem } from '@/types/configQuery'

const props = defineProps<{
  parameters: ConfigQueryParameter[]
  values: Record<string, unknown>
}>()

const emit = defineEmits<{
  'update:values': [values: Record<string, unknown>]
}>()

const formValues = ref<Record<string, unknown>>({})
const dynamicOptions = ref<Record<string, OptionItem[]>>({})

// 初始化表单值
watch(
  () => props.values,
  (newVal) => {
    formValues.value = { ...newVal }
  },
  { immediate: true, deep: true }
)

// 获取列宽
function getColSpan(paramType: string): number {
  if (paramType === 'daterange') return 12
  return 8
}

// 获取选项
function getOptions(param: ConfigQueryParameter): OptionItem[] {
  if (!param.optionsConfig) return []

  if (param.optionsConfig.mode === 'static') {
    return param.optionsConfig.options || []
  }

  // 动态选项
  return dynamicOptions.value[param.paramName] || []
}

// 加载动态选项
async function loadDynamicOptions(param: ConfigQueryParameter) {
  if (!param.optionsConfig || param.optionsConfig.mode !== 'dynamic') return
  if (!param.optionsConfig.sql || !param.optionsConfig.connectionId) return

  // 如果已加载且不需要每次刷新，则跳过
  if (dynamicOptions.value[param.paramName]?.length && !param.optionsConfig.refreshOnOpen) {
    return
  }

  try {
    const { data } = await configQueryApi.getOptions({
      connectionId: param.optionsConfig.connectionId,
      sql: param.optionsConfig.sql
    })
    if (data.success && data.data) {
      dynamicOptions.value[param.paramName] = data.data.options
    }
  } catch {
    dynamicOptions.value[param.paramName] = []
  }
}

// 发出变更事件
function emitChange() {
  emit('update:values', { ...formValues.value })
}

// 初始化时加载所有动态选项
onMounted(() => {
  for (const param of props.parameters) {
    if (param.optionsConfig?.mode === 'dynamic') {
      loadDynamicOptions(param)
    }
  }
})
</script>

<style scoped lang="scss">
.param-form-view {
  .el-form-item {
    margin-bottom: 12px;
  }

  :deep(.el-form-item__label) {
    font-size: 13px;
    padding-bottom: 4px;
  }
}
</style>
