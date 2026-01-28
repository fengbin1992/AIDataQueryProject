<template>
  <div class="template-view">
    <el-card class="page-header">
      <div class="header-content">
        <h2>模板管理</h2>
        <el-button type="primary" :icon="Plus" @click="handleCreate">
          新建模板
        </el-button>
      </div>
    </el-card>

    <div class="template-content">
      <!-- 左侧模块树 -->
      <div class="left-panel">
        <el-card :body-style="{ padding: '12px' }">
          <template #header>
            <div class="module-header">
              <span>模块列表</span>
              <el-button type="primary" size="small" :icon="Plus" @click="handleCreateModule">
                新建
              </el-button>
            </div>
          </template>
          <el-tree
            ref="treeRef"
            :data="treeData"
            :props="defaultProps"
            node-key="id"
            default-expand-all
            highlight-current
            :expand-on-click-node="false"
            @node-click="handleTreeNodeClick"
          >
            <template #default="{ node, data }">
              <span class="tree-node" :class="{ 'is-template': data.type === 'template' }">
                <el-icon v-if="data.type === 'module'" class="icon-module" :class="{ 'is-expanded': node.expanded }">
                  <FolderOpened v-if="node.expanded" />
                  <Folder v-else />
                </el-icon>
                <el-icon v-else class="icon-template"><Document /></el-icon>
                <span class="node-name">{{ node.label }}</span>
                <span v-if="data.type === 'module'" class="template-count">({{ data.templateCount || 0 }})</span>
                <span v-if="data.type === 'module'" class="module-actions">
                  <el-icon v-if="isAdmin" class="action-icon" @click.stop="handleEditModule(data)"><Edit /></el-icon>
                  <el-popconfirm
                    v-if="isAdmin"
                    title="确定删除此模块吗？"
                    @confirm="handleDeleteModule(data)"
                  >
                    <template #reference>
                      <el-icon class="action-icon delete" @click.stop><Delete /></el-icon>
                    </template>
                  </el-popconfirm>
                </span>
                <span v-else class="template-actions">
                  <el-button size="small" link type="primary" @click.stop="handleUseTemplate(data.data)">
                    使用
                  </el-button>
                </span>
              </span>
            </template>
          </el-tree>
        </el-card>
      </div>

      <!-- 右侧模板列表 -->
      <div class="right-panel">
        <el-card :body-style="{ padding: '16px' }">
          <template #header>
            <div class="list-header">
              <span>{{ selectedModuleName || '全部模板' }}</span>
              <el-input
                v-model="searchKeyword"
                placeholder="搜索模板..."
                :prefix-icon="Search"
                style="width: 200px"
                clearable
              />
            </div>
          </template>

          <el-table
            :data="filteredTemplates"
            stripe
            v-loading="loading"
          >
            <el-table-column type="expand">
              <template #default="{ row }">
                <div class="sql-preview" @click="handleUseTemplate(row)">
                  <div class="sql-preview-header">
                    <span class="sql-label">SQL 内容（点击使用）:</span>
                  </div>
                  <pre class="sql-code">{{ row.sqlContent }}</pre>
                </div>
              </template>
            </el-table-column>
            <el-table-column prop="name" label="模板名称" min-width="150">
              <template #default="{ row }">
                <el-link type="primary" @click="handleEdit(row)">
                  {{ row.name }}
                </el-link>
              </template>
            </el-table-column>
            <el-table-column prop="moduleName" label="所属模块" width="120" />
            <el-table-column prop="createdByName" label="创建人" width="100" />
            <el-table-column prop="isPublic" label="公开" width="80" align="center">
              <template #default="{ row }">
                <el-tag :type="row.isPublic ? 'success' : 'info'" size="small">
                  {{ row.isPublic ? '是' : '否' }}
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="createdAt" label="创建时间" width="160">
              <template #default="{ row }">
                {{ formatDateTime(row.createdAt) }}
              </template>
            </el-table-column>
            <el-table-column label="操作" width="150" fixed="right">
              <template #default="{ row }">
                <el-button size="small" text type="primary" @click="handleUseTemplate(row)">
                  使用
                </el-button>
                <el-popconfirm
                  v-if="isAdmin || row.createdBy === currentUserId"
                  title="确定删除此模板吗？"
                  @confirm="handleDelete(row)"
                >
                  <template #reference>
                    <el-button size="small" text type="danger">删除</el-button>
                  </template>
                </el-popconfirm>
              </template>
            </el-table-column>
          </el-table>
        </el-card>
      </div>
    </div>

    <!-- 模板编辑对话框 -->
    <el-dialog
      v-model="dialogVisible"
      :title="editingTemplate ? '编辑模板' : '新建模板'"
      width="700px"
      :close-on-click-modal="false"
    >
      <el-form
        ref="formRef"
        :model="form"
        :rules="rules"
        label-width="80px"
      >
        <el-form-item label="模块" prop="moduleId">
          <el-tree-select
            v-model="form.moduleId"
            :data="moduleOptions"
            :props="{ label: 'name', children: 'children' }"
            placeholder="选择所属模块"
            check-strictly
            style="width: 100%"
          />
        </el-form-item>
        <el-form-item label="名称" prop="name">
          <el-input v-model="form.name" placeholder="请输入模板名称" />
        </el-form-item>
        <el-form-item label="SQL" prop="sqlContent">
          <div style="width: 100%; height: 200px;">
            <SqlEditor v-model="form.sqlContent" />
          </div>
        </el-form-item>
        <el-form-item label="描述">
          <el-input
            v-model="form.description"
            type="textarea"
            :rows="3"
            placeholder="模板描述（可选）"
          />
        </el-form-item>
        <el-form-item v-if="isAdmin" label="公开">
          <el-switch v-model="form.isPublic" />
          <span class="form-tip">公开后所有用户可见</span>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="saving" @click="handleSave">保存</el-button>
      </template>
    </el-dialog>

    <!-- 模板查看对话框 -->
    <el-dialog
      v-model="viewDialogVisible"
      title="模板详情"
      width="600px"
    >
      <el-descriptions :column="2" border v-if="viewingTemplate">
        <el-descriptions-item label="模板名称">{{ viewingTemplate.name }}</el-descriptions-item>
        <el-descriptions-item label="所属模块">{{ viewingTemplate.moduleName }}</el-descriptions-item>
        <el-descriptions-item label="创建人">{{ viewingTemplate.createdByName }}</el-descriptions-item>
        <el-descriptions-item label="是否公开">
          <el-tag :type="viewingTemplate.isPublic ? 'success' : 'info'" size="small">
            {{ viewingTemplate.isPublic ? '是' : '否' }}
          </el-tag>
        </el-descriptions-item>
        <el-descriptions-item label="创建时间" :span="2">
          {{ formatDateTime(viewingTemplate.createdAt) }}
        </el-descriptions-item>
        <el-descriptions-item label="描述" :span="2">
          {{ viewingTemplate.description || '无' }}
        </el-descriptions-item>
        <el-descriptions-item label="SQL内容" :span="2">
          <pre class="sql-content">{{ viewingTemplate.sqlContent }}</pre>
        </el-descriptions-item>
      </el-descriptions>
      <template #footer>
        <el-button @click="viewDialogVisible = false">关闭</el-button>
        <el-button type="primary" @click="handleUseTemplate(viewingTemplate!)">使用此模板</el-button>
      </template>
    </el-dialog>

    <!-- 模块编辑对话框 -->
    <el-dialog
      v-model="moduleDialogVisible"
      :title="editingModule ? '编辑模块' : '新建模块'"
      width="450px"
      :close-on-click-modal="false"
    >
      <el-form
        ref="moduleFormRef"
        :model="moduleForm"
        :rules="moduleRules"
        label-width="80px"
      >
        <el-form-item label="模块名称" prop="name">
          <el-input v-model="moduleForm.name" placeholder="请输入模块名称" />
        </el-form-item>
        <el-form-item label="父模块">
          <el-tree-select
            v-model="moduleForm.parentId"
            :data="moduleOptionsForSelect"
            :props="{ label: 'name', children: 'children' }"
            placeholder="选择父模块（可选）"
            check-strictly
            clearable
            style="width: 100%"
          />
        </el-form-item>
        <el-form-item label="排序">
          <el-input-number v-model="moduleForm.sortOrder" :min="0" :max="999" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="moduleDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="moduleSaving" @click="handleSaveModule">保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, type FormInstance, type FormRules } from 'element-plus'
import { Plus, Search, Folder, FolderOpened, Edit, Delete, Document } from '@element-plus/icons-vue'
import { useTemplateStore, useQueryStore, useUserStore } from '@/stores'
import { templateApi } from '@/services'
import { formatDateTime } from '@/utils'
import SqlEditor from '@/components/query/SqlEditor.vue'
import type { TemplateDto, TemplateModuleDto, CreateTemplateRequest, CreateModuleRequest } from '@/types'

const router = useRouter()
const templateStore = useTemplateStore()
const queryStore = useQueryStore()
const userStore = useUserStore()

// 判断是否是管理员
const isAdmin = computed(() => userStore.isAdmin)
// 当前用户ID
const currentUserId = computed(() => userStore.user?.id)

const treeRef = ref()
const formRef = ref<FormInstance>()
const loading = ref(false)
const saving = ref(false)
const dialogVisible = ref(false)
const viewDialogVisible = ref(false)
const searchKeyword = ref('')
const selectedModuleId = ref<number | null>(null)
const selectedModuleName = ref('')
const templates = ref<TemplateDto[]>([])
const editingTemplate = ref<TemplateDto | null>(null)
const viewingTemplate = ref<TemplateDto | null>(null)

const form = ref<CreateTemplateRequest>({
  moduleId: 0,
  name: '',
  sqlContent: '',
  description: '',
  isPublic: false
})

const rules: FormRules = {
  moduleId: [{ required: true, message: '请选择模块', trigger: 'change' }],
  name: [{ required: true, message: '请输入模板名称', trigger: 'blur' }],
  sqlContent: [{ required: true, message: '请输入SQL内容', trigger: 'blur' }]
}

// 模块编辑相关
const moduleDialogVisible = ref(false)
const moduleSaving = ref(false)
const moduleFormRef = ref<FormInstance>()
const editingModule = ref<TreeNode | null>(null)
const moduleForm = ref<CreateModuleRequest>({
  name: '',
  parentId: undefined,
  sortOrder: 0
})

const moduleRules: FormRules = {
  name: [{ required: true, message: '请输入模块名称', trigger: 'blur' }]
}

const defaultProps = {
  children: 'children',
  label: 'name'
}

// 树形数据
interface TreeNode {
  id: string
  numericId: number
  name: string
  type: 'module' | 'template'
  templateCount?: number
  sqlContent?: string
  data?: TemplateDto
  children?: TreeNode[]
}

const treeData = computed<TreeNode[]>(() => {
  return convertToTreeData(templateStore.modules)
})

function convertToTreeData(modules: TemplateModuleDto[]): TreeNode[] {
  return modules.map(m => {
    const children: TreeNode[] = []

    // 先添加模板（模板排在子模块前面）
    if (m.templates?.length) {
      children.push(...m.templates.map(t => ({
        id: `template-${t.id}`,
        numericId: t.id,
        name: t.name,
        type: 'template' as const,
        sqlContent: t.sqlContent,
        data: t
      })))
    }

    // 再添加子模块
    if (m.children?.length) {
      children.push(...convertToTreeData(m.children))
    }

    return {
      id: `module-${m.id}`,
      numericId: m.id,
      name: m.name,
      type: 'module' as const,
      templateCount: m.templates?.length || 0,
      children: children.length > 0 ? children : undefined
    }
  })
}

// 模块选项
const moduleOptions = computed(() => templateStore.modules)

// 模块选项（用于模块编辑对话框的父模块选择，排除当前编辑的模块）
const moduleOptionsForSelect = computed(() => {
  if (!editingModule.value) return templateStore.modules
  return filterModuleTree(templateStore.modules, editingModule.value.numericId)
})

function filterModuleTree(modules: TemplateModuleDto[], excludeId: number): TemplateModuleDto[] {
  return modules
    .filter(m => m.id !== excludeId)
    .map(m => ({
      ...m,
      children: m.children?.length ? filterModuleTree(m.children, excludeId) : []
    }))
}

// 过滤后的模板列表
const filteredTemplates = computed(() => {
  if (!searchKeyword.value) return templates.value
  const keyword = searchKeyword.value.toLowerCase()
  return templates.value.filter(t =>
    t.name.toLowerCase().includes(keyword) ||
    t.description?.toLowerCase().includes(keyword)
  )
})

// 点击树节点
async function handleTreeNodeClick(data: TreeNode) {
  if (data.type === 'module') {
    selectedModuleId.value = data.numericId
    selectedModuleName.value = data.name
    await loadTemplates(data.numericId)
  } else if (data.type === 'template' && data.data) {
    // 点击模板，打开编辑对话框
    handleEdit(data.data)
  }
}

// 加载模板
async function loadTemplates(moduleId: number) {
  loading.value = true
  try {
    const { data } = await templateApi.getTemplatesByModule(moduleId)
    if (data.success && data.data) {
      templates.value = data.data
    }
  } finally {
    loading.value = false
  }
}

// 新建模板
function handleCreate() {
  editingTemplate.value = null
  form.value = {
    moduleId: selectedModuleId.value || 0,
    name: '',
    sqlContent: '',
    description: '',
    isPublic: false
  }
  dialogVisible.value = true
}

// 编辑模板
function handleEdit(template: TemplateDto) {
  editingTemplate.value = template
  form.value = {
    moduleId: template.moduleId,
    name: template.name,
    sqlContent: template.sqlContent,
    description: template.description || '',
    isPublic: template.isPublic
  }
  dialogVisible.value = true
}

// 保存模板
async function handleSave() {
  if (!formRef.value) return

  try {
    await formRef.value.validate()
    saving.value = true

    if (editingTemplate.value) {
      // 更新
      const { data } = await templateApi.updateTemplate(editingTemplate.value.id, form.value)
      if (data.success) {
        ElMessage.success('更新成功')
      }
    } else {
      // 创建
      const { data } = await templateApi.createTemplate(form.value)
      if (data.success) {
        ElMessage.success('创建成功')
      }
    }

    dialogVisible.value = false
    // 刷新列表
    if (selectedModuleId.value) {
      await loadTemplates(selectedModuleId.value)
    }
    templateStore.loadModules()
  } finally {
    saving.value = false
  }
}

// 删除模板
async function handleDelete(template: TemplateDto) {
  try {
    const { data } = await templateApi.deleteTemplate(template.id)
    if (data.success) {
      ElMessage.success('删除成功')
      if (selectedModuleId.value) {
        await loadTemplates(selectedModuleId.value)
      }
      templateStore.loadModules()
    }
  } catch {
    // 错误已由拦截器处理
  }
}

// 使用模板
function handleUseTemplate(template: TemplateDto) {
  queryStore.setSql(template.sqlContent)
  viewDialogVisible.value = false
  router.push('/query')
  ElMessage.success(`已加载模板: ${template.name}`)
}

// ==================== 模块管理 ====================

// 新建模块
function handleCreateModule() {
  editingModule.value = null
  moduleForm.value = {
    name: '',
    parentId: undefined,
    sortOrder: 0
  }
  moduleDialogVisible.value = true
}

// 编辑模块
function handleEditModule(module: TreeNode) {
  editingModule.value = module
  moduleForm.value = {
    name: module.name,
    parentId: undefined, // 暂时不支持修改父模块，避免循环引用问题
    sortOrder: 0
  }
  moduleDialogVisible.value = true
}

// 保存模块
async function handleSaveModule() {
  if (!moduleFormRef.value) return

  try {
    await moduleFormRef.value.validate()
    moduleSaving.value = true

    if (editingModule.value) {
      // 更新
      const { data } = await templateApi.updateModule(editingModule.value.numericId, {
        name: moduleForm.value.name,
        sortOrder: moduleForm.value.sortOrder
      })
      if (data.success) {
        ElMessage.success('模块更新成功')
      }
    } else {
      // 创建
      const { data } = await templateApi.createModule(moduleForm.value)
      if (data.success) {
        ElMessage.success('模块创建成功')
      }
    }

    moduleDialogVisible.value = false
    await templateStore.loadModules()
  } finally {
    moduleSaving.value = false
  }
}

// 删除模块
async function handleDeleteModule(module: TreeNode) {
  try {
    const { data } = await templateApi.deleteModule(module.numericId)
    if (data.success) {
      ElMessage.success('模块删除成功')
      await templateStore.loadModules()
      // 如果删除的是当前选中的模块，清空选中状态
      if (selectedModuleId.value === module.numericId) {
        selectedModuleId.value = null
        selectedModuleName.value = ''
        templates.value = []
      }
    }
  } catch {
    // 错误已由拦截器处理
  }
}

onMounted(async () => {
  await templateStore.loadModules()
  // 默认选中第一个模块
  if (treeData.value.length > 0) {
    await handleTreeNodeClick(treeData.value[0])
  }
})
</script>

<style scoped lang="scss">
.template-view {
  height: 100%;
  display: flex;
  flex-direction: column;
  gap: 16px;
  padding: 0 24px;
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

.template-content {
  flex: 1;
  display: flex;
  gap: 16px;
  min-height: 0;
}

.left-panel {
  width: 300px;
  flex-shrink: 0;

  .el-card {
    height: 100%;
  }

  .module-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
  }

  .tree-node {
    display: flex;
    align-items: center;
    gap: 6px;
    flex: 1;
    min-width: 0;

    &.is-template {
      color: var(--el-text-color-regular);
    }

    .icon-module {
      color: #e6a23c;
      font-size: 16px;
      flex-shrink: 0;

      &.is-expanded {
        color: #f5a623;
      }
    }

    .icon-template {
      color: #409eff;
      font-size: 15px;
      flex-shrink: 0;
    }

    .node-name {
      flex: 1;
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    .template-count {
      color: var(--el-text-color-secondary);
      font-size: 12px;
      flex-shrink: 0;
    }

    .module-actions,
    .template-actions {
      display: none;
      gap: 4px;
      margin-left: 8px;
      flex-shrink: 0;

      .action-icon {
        cursor: pointer;
        color: var(--el-text-color-secondary);
        transition: color 0.2s;

        &:hover {
          color: var(--el-color-primary);
        }

        &.delete:hover {
          color: var(--el-color-danger);
        }
      }
    }

    &:hover .module-actions,
    &:hover .template-actions {
      display: flex;
    }
  }
}

.right-panel {
  flex: 1;
  min-width: 0;

  .el-card {
    height: 100%;
  }

  .list-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
  }
}

.sql-content {
  background-color: var(--el-fill-color-light);
  padding: 12px;
  border-radius: 4px;
  white-space: pre-wrap;
  word-break: break-all;
  font-family: 'Consolas', 'Monaco', monospace;
  font-size: 13px;
  max-height: 200px;
  overflow: auto;
}

.sql-preview {
  padding: 12px 20px;
  cursor: pointer;
  transition: background-color 0.2s;

  &:hover {
    background-color: var(--el-fill-color-light);
  }

  .sql-preview-header {
    margin-bottom: 8px;

    .sql-label {
      font-size: 13px;
      color: var(--el-text-color-secondary);
    }
  }

  .sql-code {
    background-color: var(--el-fill-color);
    padding: 12px;
    border-radius: 4px;
    white-space: pre-wrap;
    word-break: break-all;
    font-family: 'Consolas', 'Monaco', monospace;
    font-size: 13px;
    max-height: 150px;
    overflow: auto;
    margin: 0;
    border: 1px solid var(--el-border-color-light);
  }
}

.form-tip {
  margin-left: 12px;
  font-size: 12px;
  color: var(--el-text-color-secondary);
}
</style>
