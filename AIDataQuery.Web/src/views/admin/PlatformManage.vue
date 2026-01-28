<template>
  <div class="platform-manage">
    <el-card class="page-header">
      <div class="header-content">
        <h2>平台管理</h2>
        <el-button type="primary" @click="handleAdd">
          <el-icon><Plus /></el-icon>
          新增平台
        </el-button>
      </div>
    </el-card>

    <el-card class="list-card">
      <el-table :data="platforms" stripe v-loading="loading">
        <el-table-column prop="code" label="平台编码" min-width="180" show-overflow-tooltip />
        <el-table-column prop="name" label="平台名称" min-width="250" show-overflow-tooltip />
        <el-table-column prop="description" label="描述" min-width="200" show-overflow-tooltip />
        <el-table-column prop="connectionCount" label="连接数" width="100" align="center">
          <template #default="{ row }">
            <el-tag type="info" size="small">{{ row.connectionCount }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="isActive" label="状态" width="100" align="center">
          <template #default="{ row }">
            <el-switch
              v-model="row.isActive"
              :loading="row.statusLoading"
              @change="handleToggleStatus(row)"
            />
          </template>
        </el-table-column>
        <el-table-column label="操作" width="180" fixed="right">
          <template #default="{ row }">
            <el-button type="primary" link size="small" @click="handleEdit(row)">
              编辑
            </el-button>
            <el-button
              type="danger"
              link
              size="small"
              :disabled="row.connectionCount > 0"
              @click="handleDelete(row)"
            >
              删除
            </el-button>
          </template>
        </el-table-column>
      </el-table>

      <el-empty v-if="!loading && platforms.length === 0" description="暂无平台数据" />
    </el-card>

    <!-- 新增/编辑对话框 -->
    <el-dialog
      v-model="dialogVisible"
      :title="isEdit ? '编辑平台' : '新增平台'"
      width="500px"
      :close-on-click-modal="false"
    >
      <el-form
        ref="formRef"
        :model="formData"
        :rules="formRules"
        label-width="100px"
      >
        <el-form-item label="平台编码" prop="code">
          <el-input
            v-model="formData.code"
            placeholder="请输入平台编码（如：platform1）"
            :disabled="isEdit"
          />
          <div class="form-tip" v-if="!isEdit">
            编码创建后不可修改，只能包含字母、数字、下划线和连字符
          </div>
        </el-form-item>
        <el-form-item label="平台名称" prop="name">
          <el-input v-model="formData.name" placeholder="请输入平台名称" />
        </el-form-item>
        <el-form-item label="描述" prop="description">
          <el-input
            v-model="formData.description"
            type="textarea"
            :rows="3"
            placeholder="请输入平台描述"
          />
        </el-form-item>
        <el-form-item label="排序" prop="sortOrder">
          <el-input-number v-model="formData.sortOrder" :min="0" :max="999" />
          <div class="form-tip">数值越小越靠前</div>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitLoading" @click="handleSubmit">
          确定
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { Plus } from '@element-plus/icons-vue'
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
import { platformApi } from '@/services'
import type { PlatformDto, CreatePlatformRequest, UpdatePlatformRequest } from '@/types'

interface PlatformRow extends PlatformDto {
  statusLoading?: boolean
}

const loading = ref(false)
const platforms = ref<PlatformRow[]>([])
const dialogVisible = ref(false)
const isEdit = ref(false)
const editingId = ref<number | null>(null)
const submitLoading = ref(false)
const formRef = ref<FormInstance>()

const formData = reactive<CreatePlatformRequest>({
  code: '',
  name: '',
  description: '',
  sortOrder: 0
})

const formRules: FormRules = {
  code: [
    { required: true, message: '请输入平台编码', trigger: 'blur' },
    { min: 2, max: 50, message: '长度在 2 到 50 个字符', trigger: 'blur' },
    {
      pattern: /^[a-zA-Z][a-zA-Z0-9_-]*$/,
      message: '必须以字母开头，只能包含字母、数字、下划线和连字符',
      trigger: 'blur'
    }
  ],
  name: [
    { required: true, message: '请输入平台名称', trigger: 'blur' },
    { min: 2, max: 100, message: '长度在 2 到 100 个字符', trigger: 'blur' }
  ],
  description: [
    { max: 500, message: '描述长度不能超过500', trigger: 'blur' }
  ]
}

async function loadPlatforms() {
  loading.value = true
  try {
    const { data } = await platformApi.getAllPlatforms()
    if (data.success && data.data) {
      platforms.value = data.data.map(p => ({ ...p, statusLoading: false }))
    }
  } finally {
    loading.value = false
  }
}

function handleAdd() {
  isEdit.value = false
  editingId.value = null
  formData.code = ''
  formData.name = ''
  formData.description = ''
  formData.sortOrder = 0
  dialogVisible.value = true
}

function handleEdit(row: PlatformDto) {
  isEdit.value = true
  editingId.value = row.id
  formData.code = row.code
  formData.name = row.name
  formData.description = row.description || ''
  formData.sortOrder = 0
  dialogVisible.value = true
}

async function handleSubmit() {
  if (!formRef.value) return

  try {
    await formRef.value.validate()
  } catch {
    return
  }

  submitLoading.value = true
  try {
    if (isEdit.value && editingId.value) {
      const updateData: UpdatePlatformRequest = {
        name: formData.name,
        description: formData.description,
        sortOrder: formData.sortOrder
      }
      const { data } = await platformApi.updatePlatform(editingId.value, updateData)
      if (data.success) {
        ElMessage.success('平台更新成功')
        dialogVisible.value = false
        loadPlatforms()
      } else {
        ElMessage.error(data.message || '更新失败')
      }
    } else {
      const { data } = await platformApi.createPlatform(formData)
      if (data.success) {
        ElMessage.success('平台创建成功')
        dialogVisible.value = false
        loadPlatforms()
      } else {
        ElMessage.error(data.message || '创建失败')
      }
    }
  } finally {
    submitLoading.value = false
  }
}

async function handleToggleStatus(row: PlatformRow) {
  row.statusLoading = true
  try {
    const { data } = await platformApi.togglePlatformStatus(row.id)
    if (data.success) {
      ElMessage.success('状态更新成功')
    } else {
      // 恢复原状态
      row.isActive = !row.isActive
      ElMessage.error(data.message || '状态更新失败')
    }
  } catch {
    row.isActive = !row.isActive
    ElMessage.error('状态更新失败')
  } finally {
    row.statusLoading = false
  }
}

async function handleDelete(row: PlatformDto) {
  try {
    await ElMessageBox.confirm(
      `确定要删除平台"${row.name}"吗？此操作不可恢复。`,
      '删除确认',
      {
        confirmButtonText: '确定',
        cancelButtonText: '取消',
        type: 'warning'
      }
    )

    const { data } = await platformApi.deletePlatform(row.id)
    if (data.success) {
      ElMessage.success('平台删除成功')
      loadPlatforms()
    } else {
      ElMessage.error(data.message || '删除失败')
    }
  } catch {
    // 用户取消操作
  }
}

onMounted(() => {
  loadPlatforms()
})
</script>

<style scoped lang="scss">
.platform-manage {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.page-header {
  .header-content {
    display: flex;
    justify-content: space-between;
    align-items: center;

    h2 {
      margin: 0;
      font-size: 18px;
    }
  }
}

.form-tip {
  font-size: 12px;
  color: #909399;
  margin-top: 4px;
}
</style>
