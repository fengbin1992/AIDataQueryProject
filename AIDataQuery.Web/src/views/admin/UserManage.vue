<template>
  <div class="user-manage">
    <el-card class="page-header">
      <div class="header-content">
        <h2>用户管理</h2>
        <el-button type="primary" :icon="Plus" @click="handleCreate">新建用户</el-button>
      </div>
    </el-card>

    <el-card class="list-card">
      <!-- 搜索 -->
      <div class="search-bar">
        <el-input
          v-model="queryParams.keyword"
          placeholder="搜索用户名或昵称..."
          :prefix-icon="Search"
          clearable
          style="width: 300px"
          @keyup.enter="handleSearch"
        />
        <el-button type="primary" :icon="Search" @click="handleSearch">搜索</el-button>
      </div>

      <!-- 用户列表 -->
      <el-table :data="users" stripe v-loading="loading">
        <el-table-column prop="username" label="用户名" width="120" />
        <el-table-column prop="nickname" label="昵称" width="120" />
        <el-table-column prop="email" label="邮箱" width="180" />
        <el-table-column prop="role" label="角色" width="100" align="center">
          <template #default="{ row }">
            <el-tag :type="row.role === 1 ? 'danger' : 'info'" size="small">
              {{ row.role === 1 ? '管理员' : '普通用户' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="status" label="状态" width="80" align="center">
          <template #default="{ row }">
            <el-tag :type="row.status === 1 ? 'success' : 'danger'" size="small">
              {{ row.status === 1 ? '启用' : '禁用' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="platformCodes" label="数据权限" min-width="250">
          <template #default="{ row }">
            <div class="permission-display">
              <span v-if="row.platformCodes?.length" class="platform-count">
                {{ row.platformCodes.length }} 个平台
              </span>
              <span v-if="row.connectionIds?.length" class="connection-count-display">
                / {{ row.connectionIds.length }} 个连接
              </span>
              <span v-if="!row.platformCodes?.length && !row.connectionIds?.length" class="no-permission">无</span>
            </div>
          </template>
        </el-table-column>
        <el-table-column prop="lastLoginAt" label="最后登录" width="160">
          <template #default="{ row }">
            {{ row.lastLoginAt ? formatDateTime(row.lastLoginAt) : '-' }}
          </template>
        </el-table-column>
        <el-table-column label="操作" width="200" fixed="right">
          <template #default="{ row }">
            <el-button size="small" text type="primary" @click="handleEdit(row)">编辑</el-button>
            <el-button size="small" text type="primary" @click="handlePermission(row)">权限</el-button>
            <el-popconfirm
              :title="row.status === 1 ? '确定禁用此用户吗？' : '确定启用此用户吗？'"
              @confirm="handleToggleStatus(row)"
            >
              <template #reference>
                <el-button size="small" text :type="row.status === 1 ? 'danger' : 'success'">
                  {{ row.status === 1 ? '禁用' : '启用' }}
                </el-button>
              </template>
            </el-popconfirm>
          </template>
        </el-table-column>
      </el-table>

      <!-- 分页 -->
      <div class="pagination">
        <el-pagination
          v-model:current-page="queryParams.pageIndex"
          v-model:page-size="queryParams.pageSize"
          :page-sizes="[10, 20, 50]"
          :total="total"
          layout="total, sizes, prev, pager, next"
          @size-change="handleSearch"
          @current-change="handleSearch"
        />
      </div>
    </el-card>

    <!-- 用户编辑对话框 -->
    <el-dialog
      v-model="dialogVisible"
      :title="editingUser ? '编辑用户' : '新建用户'"
      width="500px"
      :close-on-click-modal="false"
    >
      <el-form ref="formRef" :model="form" :rules="rules" label-width="80px">
        <el-form-item label="用户名" prop="username">
          <el-input v-model="form.username" :disabled="!!editingUser" placeholder="请输入用户名" />
        </el-form-item>
        <el-form-item v-if="!editingUser" label="密码" prop="password">
          <el-input v-model="form.password" type="password" placeholder="请输入密码" show-password />
        </el-form-item>
        <el-form-item label="昵称" prop="nickname">
          <el-input v-model="form.nickname" placeholder="请输入昵称" />
        </el-form-item>
        <el-form-item label="邮箱" prop="email">
          <el-input v-model="form.email" placeholder="请输入邮箱（可选）" />
        </el-form-item>
        <el-form-item label="角色" prop="role">
          <el-radio-group v-model="form.role">
            <el-radio :label="0">普通用户</el-radio>
            <el-radio :label="1">管理员</el-radio>
          </el-radio-group>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="saving" @click="handleSave">保存</el-button>
      </template>
    </el-dialog>

    <!-- 权限设置对话框 -->
    <el-dialog
      v-model="permissionDialogVisible"
      title="设置数据权限"
      width="700px"
      :close-on-click-modal="false"
    >
      <div v-if="permissionUser" class="permission-dialog">
        <div class="permission-header">
          <span class="permission-tip">为用户 <strong>{{ permissionUser.nickname }}</strong> 设置可访问的平台和数据库连接：</span>
          <div class="permission-actions">
            <el-button size="small" @click="handleSelectAll">全选</el-button>
            <el-button size="small" @click="handleClearAll">清空</el-button>
          </div>
        </div>

        <el-collapse v-model="expandedPlatforms" class="permission-collapse">
          <el-collapse-item
            v-for="platform in platforms"
            :key="platform.code"
            :name="platform.code"
          >
            <template #title>
              <div class="platform-header" @click.stop>
                <el-checkbox
                  :model-value="isPlatformSelected(platform.code)"
                  :indeterminate="isPlatformIndeterminate(platform.code)"
                  @change="(val: boolean) => handlePlatformChange(platform.code, val)"
                >
                  <span class="platform-name">{{ platform.name }}</span>
                </el-checkbox>
                <el-tag size="small" type="info" class="connection-count">
                  {{ getSelectedConnectionCount(platform.code) }}/{{ getConnectionsByPlatform(platform.code).length }}
                </el-tag>
              </div>
            </template>

            <div class="connection-list">
              <el-checkbox-group v-model="selectedConnections">
                <div
                  v-for="conn in getConnectionsByPlatform(platform.code)"
                  :key="conn.id"
                  class="connection-item"
                >
                  <el-checkbox :label="conn.id" :value="conn.id">
                    <span class="connection-name">{{ conn.name }}</span>
                    <el-tag size="small" :type="conn.isActive ? 'success' : 'danger'" class="connection-status">
                      {{ conn.isActive ? '启用' : '禁用' }}
                    </el-tag>
                  </el-checkbox>
                </div>
                <el-empty
                  v-if="getConnectionsByPlatform(platform.code).length === 0"
                  description="该平台暂无数据库连接"
                  :image-size="60"
                />
              </el-checkbox-group>
            </div>
          </el-collapse-item>
        </el-collapse>

        <el-empty v-if="platforms.length === 0" description="暂无平台数据" />
      </div>
      <template #footer>
        <el-button @click="permissionDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="savingPermission" @click="handleSavePermission">保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { ElMessage, type FormInstance, type FormRules } from 'element-plus'
import { Plus, Search } from '@element-plus/icons-vue'
import { userApi, platformApi } from '@/services'
import { formatDateTime } from '@/utils'
import type { UserDto, CreateUserRequest, UpdateUserRequest, PlatformDto, DatabaseConnectionDto, QueryParams } from '@/types'

const loading = ref(false)
const saving = ref(false)
const savingPermission = ref(false)
const dialogVisible = ref(false)
const permissionDialogVisible = ref(false)
const users = ref<UserDto[]>([])
const platforms = ref<PlatformDto[]>([])
const connections = ref<DatabaseConnectionDto[]>([])
const total = ref(0)
const editingUser = ref<UserDto | null>(null)
const permissionUser = ref<UserDto | null>(null)
const selectedPlatforms = ref<string[]>([])
const selectedConnections = ref<number[]>([])
const expandedPlatforms = ref<string[]>([])
const formRef = ref<FormInstance>()

const queryParams = ref<QueryParams>({
  pageIndex: 1,
  pageSize: 20,
  keyword: ''
})

const form = ref<CreateUserRequest & { role: number }>({
  username: '',
  password: '',
  nickname: '',
  email: '',
  role: 0
})

const rules: FormRules = {
  username: [
    { required: true, message: '请输入用户名', trigger: 'blur' },
    { min: 3, max: 50, message: '用户名长度在 3 到 50 个字符', trigger: 'blur' }
  ],
  password: [
    { required: true, message: '请输入密码', trigger: 'blur' },
    { min: 6, message: '密码长度至少 6 个字符', trigger: 'blur' }
  ],
  nickname: [
    { required: true, message: '请输入昵称', trigger: 'blur' }
  ],
  email: [
    { type: 'email', message: '请输入有效的邮箱地址', trigger: 'blur' }
  ]
}

// 获取平台名称
function getPlatformName(code: string): string {
  const platform = platforms.value.find(p => p.code === code)
  return platform?.name || code
}

// 搜索用户
async function handleSearch() {
  loading.value = true
  try {
    const { data } = await userApi.getUsers(queryParams.value)
    if (data.success && data.data) {
      users.value = data.data.items
      total.value = data.data.totalCount
    }
  } finally {
    loading.value = false
  }
}

// 新建用户
function handleCreate() {
  editingUser.value = null
  form.value = {
    username: '',
    password: '',
    nickname: '',
    email: '',
    role: 0
  }
  dialogVisible.value = true
}

// 编辑用户
function handleEdit(user: UserDto) {
  editingUser.value = user
  form.value = {
    username: user.username,
    password: '',
    nickname: user.nickname,
    email: user.email || '',
    role: user.role
  }
  dialogVisible.value = true
}

// 保存用户
async function handleSave() {
  if (!formRef.value) return

  try {
    await formRef.value.validate()
    saving.value = true

    if (editingUser.value) {
      // 更新
      const updateData: UpdateUserRequest = {
        nickname: form.value.nickname,
        email: form.value.email || undefined,
        role: form.value.role
      }
      const { data } = await userApi.updateUser(editingUser.value.id, updateData)
      if (data.success) {
        ElMessage.success('更新成功')
      }
    } else {
      // 创建
      const { data } = await userApi.createUser(form.value)
      if (data.success) {
        ElMessage.success('创建成功')
      }
    }

    dialogVisible.value = false
    await handleSearch()
  } finally {
    saving.value = false
  }
}

// 权限设置
function handlePermission(user: UserDto) {
  permissionUser.value = user
  selectedPlatforms.value = [...(user.platformCodes || [])]
  selectedConnections.value = [...(user.connectionIds || [])]
  // 默认展开已选择的平台
  expandedPlatforms.value = [...(user.platformCodes || [])]
  permissionDialogVisible.value = true
}

// 根据平台获取连接列表
function getConnectionsByPlatform(platformCode: string): DatabaseConnectionDto[] {
  return connections.value.filter(c => c.platformCode === platformCode)
}

// 获取平台下已选中的连接数量
function getSelectedConnectionCount(platformCode: string): number {
  const platformConns = getConnectionsByPlatform(platformCode)
  return platformConns.filter(c => selectedConnections.value.includes(c.id)).length
}

// 判断平台是否全选
function isPlatformSelected(platformCode: string): boolean {
  const platformConns = getConnectionsByPlatform(platformCode)
  if (platformConns.length === 0) return selectedPlatforms.value.includes(platformCode)
  return platformConns.every(c => selectedConnections.value.includes(c.id))
}

// 判断平台是否部分选中
function isPlatformIndeterminate(platformCode: string): boolean {
  const platformConns = getConnectionsByPlatform(platformCode)
  if (platformConns.length === 0) return false
  const selectedCount = platformConns.filter(c => selectedConnections.value.includes(c.id)).length
  return selectedCount > 0 && selectedCount < platformConns.length
}

// 处理平台复选框变化
function handlePlatformChange(platformCode: string, checked: boolean) {
  const platformConns = getConnectionsByPlatform(platformCode)

  if (checked) {
    // 添加平台权限
    if (!selectedPlatforms.value.includes(platformCode)) {
      selectedPlatforms.value.push(platformCode)
    }
    // 选中该平台下所有连接
    platformConns.forEach(c => {
      if (!selectedConnections.value.includes(c.id)) {
        selectedConnections.value.push(c.id)
      }
    })
  } else {
    // 移除平台权限
    const idx = selectedPlatforms.value.indexOf(platformCode)
    if (idx > -1) {
      selectedPlatforms.value.splice(idx, 1)
    }
    // 取消选中该平台下所有连接
    platformConns.forEach(c => {
      const connIdx = selectedConnections.value.indexOf(c.id)
      if (connIdx > -1) {
        selectedConnections.value.splice(connIdx, 1)
      }
    })
  }
}

// 全选
function handleSelectAll() {
  selectedPlatforms.value = platforms.value.map(p => p.code)
  selectedConnections.value = connections.value.map(c => c.id)
}

// 清空
function handleClearAll() {
  selectedPlatforms.value = []
  selectedConnections.value = []
}

// 保存权限
async function handleSavePermission() {
  if (!permissionUser.value) return

  savingPermission.value = true
  try {
    // 根据选中的连接自动计算平台权限
    const platformsFromConnections = new Set<string>()
    selectedConnections.value.forEach(connId => {
      const conn = connections.value.find(c => c.id === connId)
      if (conn) {
        platformsFromConnections.add(conn.platformCode)
      }
    })
    // 合并手动选择的平台和通过连接推导的平台
    const finalPlatforms = [...new Set([...selectedPlatforms.value, ...platformsFromConnections])]

    const { data } = await userApi.setAllPermissions(permissionUser.value.id, {
      platformCodes: finalPlatforms,
      connectionIds: selectedConnections.value
    })
    if (data.success) {
      ElMessage.success('权限设置成功')
      permissionDialogVisible.value = false
      await handleSearch()
    }
  } finally {
    savingPermission.value = false
  }
}

// 切换用户状态
async function handleToggleStatus(user: UserDto) {
  try {
    if (user.status === 1) {
      // 禁用
      const { data } = await userApi.deleteUser(user.id)
      if (data.success) {
        ElMessage.success('用户已禁用')
      }
    } else {
      // 启用
      const { data } = await userApi.updateUser(user.id, { status: 1 })
      if (data.success) {
        ElMessage.success('用户已启用')
      }
    }
    await handleSearch()
  } catch {
    // 错误处理
  }
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

// 加载所有数据库连接
async function loadConnections() {
  try {
    const { data } = await platformApi.getAllConnections()
    if (data.success && data.data) {
      connections.value = data.data
    }
  } catch {
    // 错误处理
  }
}

onMounted(async () => {
  await Promise.all([loadPlatforms(), loadConnections()])
  await handleSearch()
})
</script>

<style scoped lang="scss">
.user-manage {
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

.list-card {
  .search-bar {
    display: flex;
    gap: 12px;
    margin-bottom: 16px;
  }
}

.pagination {
  display: flex;
  justify-content: flex-end;
  margin-top: 16px;
}

.no-permission {
  color: var(--el-text-color-placeholder);
  font-size: 12px;
}

.permission-display {
  display: flex;
  align-items: center;
  gap: 4px;
  font-size: 13px;

  .platform-count {
    color: var(--el-color-primary);
  }

  .connection-count-display {
    color: var(--el-text-color-secondary);
  }
}

// 权限设置对话框样式
.permission-dialog {
  max-height: 500px;
  overflow-y: auto;
}

.permission-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 16px;
  padding-bottom: 12px;
  border-bottom: 1px solid var(--el-border-color-lighter);

  .permission-tip {
    color: var(--el-text-color-regular);
  }

  .permission-actions {
    display: flex;
    gap: 8px;
  }
}

.permission-collapse {
  border: none;

  :deep(.el-collapse-item__header) {
    background-color: var(--el-fill-color-light);
    border-radius: 4px;
    padding: 0 12px;
    margin-bottom: 8px;
  }

  :deep(.el-collapse-item__wrap) {
    border: none;
  }

  :deep(.el-collapse-item__content) {
    padding: 0 0 12px 24px;
  }
}

.platform-header {
  display: flex;
  align-items: center;
  width: 100%;
  gap: 12px;

  .platform-name {
    font-weight: 500;
  }

  .connection-count {
    margin-left: auto;
    margin-right: 20px;
  }
}

.connection-list {
  .connection-item {
    padding: 8px 0;
    border-bottom: 1px dashed var(--el-border-color-lighter);

    &:last-child {
      border-bottom: none;
    }

    .el-checkbox {
      display: flex;
      align-items: center;
    }

    .connection-name {
      margin-right: 8px;
    }

    .connection-status {
      font-size: 11px;
    }
  }
}

.platform-checkbox {
  display: block;
  margin: 8px 0;
}
</style>
