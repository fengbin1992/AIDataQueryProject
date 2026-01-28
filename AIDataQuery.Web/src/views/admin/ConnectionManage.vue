<template>
  <div class="connection-manage">
    <el-card class="page-header">
      <div class="header-content">
        <h2>数据库连接管理</h2>
        <el-button type="primary" :icon="Plus" @click="handleCreate">新建连接</el-button>
      </div>
    </el-card>

    <!-- 平台选择 -->
    <el-card class="filter-card">
      <el-form :inline="true">
        <el-form-item label="选择平台">
          <el-select
            v-model="selectedPlatformCode"
            placeholder="全部平台"
            clearable
            style="width: 320px"
            @change="handlePlatformChange"
          >
            <el-option
              v-for="platform in platforms"
              :key="platform.code"
              :label="platform.name"
              :value="platform.code"
            />
          </el-select>
        </el-form-item>
      </el-form>
    </el-card>

    <!-- 连接列表 -->
    <el-card class="list-card">
      <el-table
        :data="connections"
        stripe
        v-loading="loading"
        :row-class-name="tableRowClassName"
      >
        <el-table-column prop="name" label="连接名称" min-width="200" show-overflow-tooltip />
        <el-table-column prop="platformCode" label="所属平台" min-width="180" show-overflow-tooltip>
          <template #default="{ row }">
            {{ getPlatformName(row.platformCode) }}
          </template>
        </el-table-column>
        <el-table-column prop="isProduction" label="环境" width="100" align="center">
          <template #default="{ row }">
            <el-tag :type="row.isProduction ? 'danger' : 'info'" size="small">
              {{ row.isProduction ? '正式' : '测试' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="databaseType" label="数据库类型" width="120" />
        <el-table-column prop="connectionString" label="连接字符串" min-width="300" show-overflow-tooltip>
          <template #default="{ row }">
            <template v-if="row.connectionString">
              <el-tooltip :content="row.connectionString" placement="top" :show-after="500">
                <span class="connection-string">{{ row.connectionString }}</span>
              </el-tooltip>
            </template>
            <template v-else>
              <el-button size="small" text type="primary" @click="handleViewConnectionString(row)">查看</el-button>
            </template>
          </template>
        </el-table-column>
        <el-table-column prop="description" label="描述" min-width="150" show-overflow-tooltip />
        <el-table-column prop="isActive" label="状态" width="80" align="center">
          <template #default="{ row }">
            <el-tag :type="row.isActive ? 'success' : 'danger'" size="small">
              {{ row.isActive ? '启用' : '禁用' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="200" fixed="right">
          <template #default="{ row }">
            <el-button size="small" text type="primary" @click="handleTest(row)">测试</el-button>
            <el-button size="small" text type="primary" @click="handleEdit(row)">编辑</el-button>
            <el-popconfirm title="确定删除此连接吗？" @confirm="handleDelete(row)">
              <template #reference>
                <el-button size="small" text type="danger">删除</el-button>
              </template>
            </el-popconfirm>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <!-- 连接编辑对话框 -->
    <el-dialog
      v-model="dialogVisible"
      :title="editingConnection ? '编辑连接' : '新建连接'"
      width="600px"
      :close-on-click-modal="false"
    >
      <el-form ref="formRef" :model="form" :rules="rules" label-width="100px">
        <el-form-item label="连接名称" prop="name">
          <el-input v-model="form.name" placeholder="请输入连接名称" />
        </el-form-item>
        <el-form-item label="所属平台" prop="platformCode">
          <el-select v-model="form.platformCode" placeholder="选择平台" style="width: 100%">
            <el-option
              v-for="platform in platforms"
              :key="platform.code"
              :label="platform.name"
              :value="platform.code"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="数据库类型" prop="databaseType">
          <el-select v-model="form.databaseType" style="width: 100%">
            <el-option label="SQL Server" value="SqlServer" />
            <el-option label="MySQL" value="MySql" />
            <el-option label="PostgreSQL" value="PostgreSql" />
          </el-select>
        </el-form-item>
        <el-form-item label="连接字符串" prop="connectionString">
          <el-input
            v-model="form.connectionString"
            type="textarea"
            :rows="4"
            placeholder="请输入数据库连接字符串"
          />
        </el-form-item>
        <el-form-item label="描述">
          <el-input v-model="form.description" type="textarea" :rows="2" placeholder="描述（可选）" />
        </el-form-item>
        <el-form-item label="正式环境">
          <el-switch v-model="form.isProduction" />
          <span class="form-tip warning">正式环境数据库将高亮显示，请谨慎操作</span>
        </el-form-item>
      </el-form>

      <el-alert
        type="warning"
        title="安全提示"
        description="连接字符串将使用 AES-256 加密后存储，请确保连接信息正确。"
        show-icon
        :closable="false"
        style="margin-top: 16px"
      />

      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="saving" @click="handleSave">保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { ElMessage, type FormInstance, type FormRules } from 'element-plus'
import { Plus } from '@element-plus/icons-vue'
import { platformApi } from '@/services'
import type { PlatformDto, DatabaseConnectionDto, CreateConnectionRequest } from '@/types'

const loading = ref(false)
const saving = ref(false)
const dialogVisible = ref(false)
const platforms = ref<PlatformDto[]>([])
const connections = ref<DatabaseConnectionDto[]>([])
const selectedPlatformCode = ref('')
const editingConnection = ref<DatabaseConnectionDto | null>(null)
const formRef = ref<FormInstance>()

const form = ref<CreateConnectionRequest>({
  name: '',
  platformCode: '',
  connectionString: '',
  databaseType: 'SqlServer',
  description: '',
  isProduction: false
})

const rules: FormRules = {
  name: [{ required: true, message: '请输入连接名称', trigger: 'blur' }],
  platformCode: [{ required: true, message: '请选择平台', trigger: 'change' }],
  connectionString: [{ required: true, message: '请输入连接字符串', trigger: 'blur' }],
  databaseType: [{ required: true, message: '请选择数据库类型', trigger: 'change' }]
}

// 获取平台名称
function getPlatformName(code: string): string {
  const platform = platforms.value.find(p => p.code === code)
  return platform?.name || code
}

// 表格行样式
function tableRowClassName({ row }: { row: DatabaseConnectionDto }) {
  if (row.isProduction) {
    return 'production-row'
  }
  return ''
}

// 加载平台列表
async function loadPlatforms() {
  try {
    const { data } = await platformApi.getAllPlatforms()
    if (data.success && data.data) {
      platforms.value = data.data
    }
  } catch {
    // 错误处理
  }
}

// 平台变更
async function handlePlatformChange() {
  await loadConnections()
}

// 加载连接列表
async function loadConnections() {
  loading.value = true
  try {
    // 先尝试管理员 API
    const { data } = await platformApi.getAllConnections()
    if (data.success && data.data && data.data.length > 0) {
      let allConnections = data.data
      if (selectedPlatformCode.value) {
        allConnections = allConnections.filter(c => c.platformCode === selectedPlatformCode.value)
      }
      connections.value = allConnections
      return
    }

    // 如果管理员 API 无数据，回退到按平台加载
    connections.value = []
    for (const platform of platforms.value) {
      if (selectedPlatformCode.value && platform.code !== selectedPlatformCode.value) {
        continue
      }
      try {
        const { data: connData } = await platformApi.getConnections(platform.code)
        if (connData.success && connData.data) {
          connections.value.push(...connData.data)
        }
      } catch {
        // 继续加载下一个
      }
    }
  } catch (error: any) {
    // 管理员 API 失败，回退到按平台加载
    console.warn('管理员 API 失败，使用普通 API:', error)
    connections.value = []
    for (const platform of platforms.value) {
      if (selectedPlatformCode.value && platform.code !== selectedPlatformCode.value) {
        continue
      }
      try {
        const { data: connData } = await platformApi.getConnections(platform.code)
        if (connData.success && connData.data) {
          connections.value.push(...connData.data)
        }
      } catch {
        // 继续加载下一个
      }
    }
  } finally {
    loading.value = false
  }
}

// 新建连接
function handleCreate() {
  editingConnection.value = null
  form.value = {
    name: '',
    platformCode: selectedPlatformCode.value || '',
    connectionString: '',
    databaseType: 'SqlServer',
    description: '',
    isProduction: false
  }
  dialogVisible.value = true
}

// 查看连接字符串
async function handleViewConnectionString(conn: DatabaseConnectionDto) {
  try {
    const { data } = await platformApi.getConnectionById(conn.id)
    if (data.success && data.data?.connectionString) {
      // 更新列表中的连接字符串
      const index = connections.value.findIndex(c => c.id === conn.id)
      if (index !== -1) {
        connections.value[index].connectionString = data.data.connectionString
      }
    } else {
      ElMessage.warning('无法获取连接字符串')
    }
  } catch {
    ElMessage.error('获取连接字符串失败')
  }
}

// 编辑连接
async function handleEdit(conn: DatabaseConnectionDto) {
  editingConnection.value = conn
  // 获取连接详情（包含连接字符串）
  try {
    const { data } = await platformApi.getConnectionById(conn.id)
    if (data.success && data.data) {
      form.value = {
        name: data.data.name,
        platformCode: data.data.platformCode,
        connectionString: data.data.connectionString || '',
        databaseType: data.data.databaseType,
        description: data.data.description || '',
        isProduction: data.data.isProduction
      }
    }
  } catch {
    // 如果获取失败，使用空连接字符串
    form.value = {
      name: conn.name,
      platformCode: conn.platformCode,
      connectionString: '',
      databaseType: conn.databaseType,
      description: conn.description || '',
      isProduction: conn.isProduction
    }
    ElMessage.warning('无法获取连接字符串，请重新输入')
  }
  dialogVisible.value = true
}

// 保存连接
async function handleSave() {
  if (!formRef.value) return

  try {
    await formRef.value.validate()
    saving.value = true

    if (editingConnection.value) {
      // 更新
      const { data } = await platformApi.updateConnection(editingConnection.value.id, form.value)
      if (data.success) {
        ElMessage.success('更新成功')
      }
    } else {
      // 创建
      const { data } = await platformApi.createConnection(form.value)
      if (data.success) {
        ElMessage.success('创建成功')
      }
    }

    dialogVisible.value = false
    await loadConnections()
  } finally {
    saving.value = false
  }
}

// 测试连接
async function handleTest(conn: DatabaseConnectionDto) {
  try {
    const { data } = await platformApi.testConnection(conn.id)
    if (data.success && data.data) {
      ElMessage.success('连接测试成功')
    } else {
      ElMessage.error('连接测试失败')
    }
  } catch {
    ElMessage.error('连接测试失败')
  }
}

// 删除连接
async function handleDelete(conn: DatabaseConnectionDto) {
  try {
    const { data } = await platformApi.deleteConnection(conn.id)
    if (data.success) {
      ElMessage.success('删除成功')
      await loadConnections()
    }
  } catch {
    // 错误处理
  }
}

onMounted(async () => {
  await loadPlatforms()
  await loadConnections()
})
</script>

<style scoped lang="scss">
.connection-manage {
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

.connection-string {
  font-family: 'Consolas', 'Monaco', monospace;
  font-size: 12px;
  color: #606266;
  max-width: 280px;
  display: inline-block;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.form-tip {
  margin-left: 12px;
  font-size: 12px;
  color: var(--el-text-color-secondary);

  &.warning {
    color: var(--el-color-warning);
  }
}

// 正式环境行高亮
:deep(.production-row) {
  background-color: rgba(var(--el-color-danger-rgb), 0.08) !important;

  &:hover > td {
    background-color: rgba(var(--el-color-danger-rgb), 0.12) !important;
  }
}
</style>
