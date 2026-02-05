<template>
  <div class="param-form-view">
    <!-- 条件开关区域 -->
    <div v-if="store.conditionSwitchList.length > 0" class="condition-switches">
      <div class="switches-header">
        <span class="label">条件开关</span>
        <el-button link type="primary" size="small" @click="enableAll">全部启用</el-button>
        <el-button link type="info" size="small" @click="disableAll">全部禁用</el-button>
      </div>
      <div class="switches-body">
        <el-tag
          v-for="item in store.conditionSwitchList"
          :key="item.key"
          :type="item.enabled ? 'primary' : 'info'"
          :effect="item.enabled ? 'dark' : 'plain'"
          class="condition-tag"
          @click="store.toggleCondition(item.key)"
        >
          <el-icon v-if="item.enabled"><Check /></el-icon>
          <el-icon v-else><Close /></el-icon>
          {{ item.label }}
        </el-tag>
      </div>
    </div>

    <!-- 参数表单 -->
    <el-form :model="formValues" label-position="top" size="default">
      <el-row :gutter="16">
        <el-col
          v-for="param in parameters"
          :key="param.paramName"
          :span="getColSpan(param.paramType)"
        >
          <el-form-item
            :label="param.paramLabel"
            :required="param.isRequired && isParamEnabled(param)"
            :class="{ 'param-disabled': !isParamEnabled(param) }"
          >
            <!-- 文本输入 -->
            <el-input
              v-if="param.paramType === 'text'"
              :model-value="formValues[param.paramName] as string"
              :placeholder="param.placeholder || '请输入'"
              :disabled="!isParamEnabled(param)"
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
              :disabled="!isParamEnabled(param)"
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
              :disabled="!isParamEnabled(param)"
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
              :disabled="!isParamEnabled(param)"
              style="width: 100%"
              @update:model-value="(val: [string, string] | null) => { formValues[param.paramName] = val ?? ['', '']; emitChange() }"
            />

            <!-- 下拉单选 -->
            <el-select
              v-else-if="param.paramType === 'select'"
              :model-value="formValues[param.paramName] as string | number"
              :placeholder="param.placeholder || '请选择'"
              :disabled="!isParamEnabled(param)"
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
              :disabled="!isParamEnabled(param)"
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
import { Check, Close } from '@element-plus/icons-vue'
import { configQueryApi } from '@/services/configQuery'
import { useConfigQueryStore } from '@/stores/configQuery'
import type { ConfigQueryParameter, OptionItem } from '@/types/configQuery'

const props = defineProps<{
  parameters: ConfigQueryParameter[]
  values: Record<string, unknown>
}>()

const emit = defineEmits<{
  'update:values': [values: Record<string, unknown>]
}>()

const store = useConfigQueryStore()
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

// 判断参数是否启用
function isParamEnabled(param: ConfigQueryParameter): boolean {
  const key = param.conditionGroup || param.paramName
  return store.conditionSwitches[key] !== false
}

// 全部启用
function enableAll(): void {
  for (const item of store.conditionSwitchList) {
    store.setConditionEnabled(item.key, true)
  }
}

// 全部禁用
function disableAll(): void {
  for (const item of store.conditionSwitchList) {
    store.setConditionEnabled(item.key, false)
  }
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
  .condition-switches {
    margin-bottom: 16px;
    padding: 12px;
    background: var(--el-fill-color-lighter);
    border-radius: 8px;

    .switches-header {
      display: flex;
      align-items: center;
      gap: 12px;
      margin-bottom: 8px;

      .label {
        font-size: 13px;
        font-weight: 500;
        color: var(--el-text-color-secondary);
      }
    }

    .switches-body {
      display: flex;
      flex-wrap: wrap;
      gap: 8px;
    }

    .condition-tag {
      cursor: pointer;
      transition: all 0.2s;
      user-select: none;
      max-width: none !important;

      &:hover {
        opacity: 0.8;
      }

      :deep(.el-tag__content) {
        display: inline-flex;
        align-items: center;
        white-space: nowrap;
        overflow: visible;
      }

      :deep(.el-icon) {
        margin-right: 6px;
        font-size: 14px;
        flex-shrink: 0;
      }
    }
  }

  .el-form-item {
    margin-bottom: 12px;

    &.param-disabled {
      opacity: 0.5;

      :deep(.el-form-item__label) {
        text-decoration: line-through;
      }
    }
  }

  :deep(.el-form-item__label) {
    font-size: 13px;
    padding-bottom: 4px;
  }
}
</style>
