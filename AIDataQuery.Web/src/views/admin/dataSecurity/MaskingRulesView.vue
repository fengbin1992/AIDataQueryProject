<template>
  <div class="masking-rules-view">
    <div class="page-header">
      <h3>脱敏规则管理</h3>
      <el-button type="primary" @click="showCreateDialog">
        <el-icon><Plus /></el-icon>
        新增规则
      </el-button>
    </div>

    <div class="page-content">
      <el-table :data="store.maskingRules" v-loading="store.loadingRules" stripe border>
        <el-table-column prop="name" label="规则名称" width="150" />
        <el-table-column prop="fieldPattern" label="字段匹配模式" min-width="200" show-overflow-tooltip />
        <el-table-column prop="maskTypeName" label="脱敏类型" width="100">
          <template #default="{ row }">
            <el-tag size="small">{{ getMaskTypeName(row.maskType) }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="priority" label="优先级" width="80" align="center" />
        <el-table-column prop="isActive" label="状态" width="80" align="center">
          <template #default="{ row }">
            <el-switch
              :model-value="row.isActive"
              @change="handleToggle(row)"
              size="small"
            />
          </template>
        </el-table-column>
        <el-table-column prop="description" label="说明" min-width="150" show-overflow-tooltip />
        <el-table-column label="操作" width="150" fixed="right">
          <template #default="{ row }">
            <el-button text type="primary" size="small" @click="showEditDialog(row)">编辑</el-button>
            <el-popconfirm title="确定删除该规则吗？" @confirm="handleDelete(row.id)">
              <template #reference>
                <el-button text type="danger" size="small">删除</el-button>
              </template>
            </el-popconfirm>
          </template>
        </el-table-column>
      </el-table>

      <div class="tips">
        <p><strong>说明：</strong></p>
        <ul>
          <li>字段匹配模式支持通配符，使用 * 匹配任意字符。例如：*phone* 匹配 user_phone, phone_number 等</li>
          <li>多个匹配模式用逗号分隔，例如：*phone*,*mobile*,*tel*</li>
          <li>优先级数字越大优先级越高，当一个字段匹配多个规则时，使用优先级最高的规则</li>
        </ul>
      </div>
    </div>

    <!-- 新增/编辑弹窗 -->
    <el-dialog
      v-model="dialogVisible"
      :title="isEditing ? '编辑规则' : '新增规则'"
      width="500px"
      @closed="resetForm"
    >
      <el-form :model="form" :rules="rules" ref="formRef" label-width="100px">
        <el-form-item label="规则名称" prop="name">
          <el-input v-model="form.name" placeholder="请输入规则名称" />
        </el-form-item>
        <el-form-item label="字段匹配" prop="fieldPattern">
          <el-input
            v-model="form.fieldPattern"
            placeholder="例如：*phone*,*mobile*"
            type="textarea"
            :rows="2"
          />
        </el-form-item>
        <el-form-item label="脱敏类型" prop="maskType">
          <el-select v-model="form.maskType" placeholder="请选择脱敏类型" style="width: 100%">
            <el-option
              v-for="option in MaskTypeOptions"
              :key="option.value"
              :label="`${option.label} (${option.example})`"
              :value="option.value"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="优先级" prop="priority">
          <el-input-number v-model="form.priority" :min="0" :max="1000" />
        </el-form-item>
        <el-form-item label="说明" prop="description">
          <el-input v-model="form.description" placeholder="规则说明（可选）" type="textarea" :rows="2" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSubmit" :loading="submitting">确定</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { Plus } from '@element-plus/icons-vue'
import type { FormInstance, FormRules } from 'element-plus'
import { useDataSecurityStore } from '@/stores/dataSecurity'
import { MaskType, MaskTypeOptions, type MaskingRule } from '@/types/dataSecurity'

const store = useDataSecurityStore()

const dialogVisible = ref(false)
const isEditing = ref(false)
const editingId = ref<number | null>(null)
const formRef = ref<FormInstance>()
const submitting = ref(false)

const form = reactive({
  name: '',
  fieldPattern: '',
  maskType: MaskType.Phone,
  priority: 0,
  description: ''
})

const rules: FormRules = {
  name: [{ required: true, message: '请输入规则名称', trigger: 'blur' }],
  fieldPattern: [{ required: true, message: '请输入字段匹配模式', trigger: 'blur' }],
  maskType: [{ required: true, message: '请选择脱敏类型', trigger: 'change' }]
}

function getMaskTypeName(type: MaskType) {
  return MaskTypeOptions.find(o => o.value === type)?.label || '未知'
}

function showCreateDialog() {
  isEditing.value = false
  editingId.value = null
  dialogVisible.value = true
}

function showEditDialog(row: MaskingRule) {
  isEditing.value = true
  editingId.value = row.id
  form.name = row.name
  form.fieldPattern = row.fieldPattern
  form.maskType = row.maskType
  form.priority = row.priority
  form.description = row.description || ''
  dialogVisible.value = true
}

function resetForm() {
  form.name = ''
  form.fieldPattern = ''
  form.maskType = MaskType.Phone
  form.priority = 0
  form.description = ''
  formRef.value?.resetFields()
}

async function handleSubmit() {
  const valid = await formRef.value?.validate()
  if (!valid) return

  submitting.value = true
  try {
    let success: boolean
    if (isEditing.value && editingId.value) {
      success = await store.updateMaskingRule(editingId.value, form)
    } else {
      success = await store.createMaskingRule(form)
    }
    if (success) {
      dialogVisible.value = false
    }
  } finally {
    submitting.value = false
  }
}

async function handleToggle(row: MaskingRule) {
  await store.toggleMaskingRule(row.id)
}

async function handleDelete(id: number) {
  await store.deleteMaskingRule(id)
}

onMounted(() => {
  store.loadMaskingRules()
})
</script>

<style scoped lang="scss">
.masking-rules-view {
  padding: 20px;
  height: 100%;
  display: flex;
  flex-direction: column;
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;

  h3 {
    margin: 0;
    font-weight: 500;
  }
}

.page-content {
  flex: 1;
  overflow: auto;
}

.tips {
  margin-top: 20px;
  padding: 12px 16px;
  background-color: var(--el-fill-color-light);
  border-radius: 4px;
  font-size: 13px;
  color: var(--el-text-color-secondary);

  p {
    margin: 0 0 8px 0;
  }

  ul {
    margin: 0;
    padding-left: 20px;

    li {
      margin-bottom: 4px;
    }
  }
}
</style>
