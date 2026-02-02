<template>
  <div class="param-json-view">
    <div class="json-editor-wrapper">
      <el-input
        type="textarea"
        v-model="localContent"
        :rows="10"
        placeholder="请输入 JSON 格式的参数值"
        @input="handleInput"
        @blur="handleBlur"
      />
    </div>

    <div class="json-status" :class="{ error: !!error }">
      <template v-if="error">
        <el-icon><WarningFilled /></el-icon>
        <span>{{ error }}</span>
      </template>
      <template v-else>
        <el-icon><SuccessFilled /></el-icon>
        <span>JSON 格式正确</span>
      </template>
    </div>

    <div class="json-actions">
      <el-button size="small" @click="formatJson">
        <el-icon><MagicStick /></el-icon>
        格式化
      </el-button>
      <el-button size="small" @click="copyJson">
        <el-icon><CopyDocument /></el-icon>
        复制
      </el-button>
      <el-button size="small" @click="pasteJson">
        <el-icon><DocumentCopy /></el-icon>
        粘贴
      </el-button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue'
import { ElMessage } from 'element-plus'
import {
  WarningFilled,
  SuccessFilled,
  MagicStick,
  CopyDocument,
  DocumentCopy
} from '@element-plus/icons-vue'

const props = defineProps<{
  content: string
  error: string | null
}>()

const emit = defineEmits<{
  'update:content': [content: string]
  sync: []
}>()

const localContent = ref('')

watch(
  () => props.content,
  (newVal) => {
    localContent.value = newVal
  },
  { immediate: true }
)

function handleInput() {
  emit('update:content', localContent.value)
}

function handleBlur() {
  emit('sync')
}

function formatJson() {
  try {
    const parsed = JSON.parse(localContent.value)
    localContent.value = JSON.stringify(parsed, null, 2)
    emit('update:content', localContent.value)
    emit('sync')
  } catch {
    ElMessage.warning('JSON 格式错误，无法格式化')
  }
}

function copyJson() {
  navigator.clipboard.writeText(localContent.value)
  ElMessage.success('已复制到剪贴板')
}

async function pasteJson() {
  try {
    const text = await navigator.clipboard.readText()
    localContent.value = text
    emit('update:content', text)
    emit('sync')
  } catch {
    ElMessage.warning('无法访问剪贴板')
  }
}
</script>

<style scoped lang="scss">
.param-json-view {
  display: flex;
  flex-direction: column;
  gap: 8px;

  .json-editor-wrapper {
    :deep(.el-textarea__inner) {
      font-family: monospace;
      font-size: 13px;
      line-height: 1.5;
    }
  }

  .json-status {
    display: flex;
    align-items: center;
    gap: 4px;
    font-size: 12px;
    color: var(--el-color-success);

    &.error {
      color: var(--el-color-danger);
    }
  }

  .json-actions {
    display: flex;
    gap: 8px;
  }
}
</style>
