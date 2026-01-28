<template>
  <div class="query-view">
    <!-- 顶部选择区 -->
    <div class="query-selectors">
      <div class="selector-item">
        <span class="label">平台：</span>
        <el-select
          v-model="queryStore.selectedPlatformCode"
          placeholder="选择平台"
          @change="handlePlatformChange"
        >
          <el-option
            v-for="platform in queryStore.platforms"
            :key="platform.code"
            :label="platform.name"
            :value="platform.code"
          />
        </el-select>
      </div>
      <div class="selector-item database-selector">
        <span class="label">数据库：</span>
        <el-select
          v-model="selectedConnectionId"
          placeholder="选择数据库"
          @change="handleConnectionChange"
          :popper-options="{ strategy: 'fixed' }"
          :class="{ 'production-select': isSelectedProduction }"
        >
          <el-option
            v-for="conn in queryStore.connections"
            :key="conn.id"
            :label="conn.name"
            :value="conn.id"
            :class="{ 'production-option': conn.isProduction }"
          >
            <div class="connection-option">
              <span class="conn-name">{{ conn.name }}</span>
              <el-tag
                v-if="conn.isProduction"
                type="danger"
                size="small"
                class="env-tag"
              >
                正式
              </el-tag>
            </div>
          </el-option>
        </el-select>
        <el-tag v-if="isSelectedProduction" type="danger" size="small" class="selected-env-tag">
          正式环境
        </el-tag>
      </div>
    </div>

    <!-- 主内容区 -->
    <div class="query-content">
      <!-- 左侧模板树 -->
      <div class="left-panel" :class="{ collapsed: isTemplateCollapsed }">
        <el-card class="template-card" :body-style="{ padding: '12px', height: 'calc(100% - 20px)' }">
          <template #header>
            <div class="card-header">
              <span>模板列表</span>
            </div>
          </template>
          <TemplateTree @select="handleSelectTemplate" />
        </el-card>
      </div>

      <!-- 折叠/展开按钮 -->
      <div class="collapse-toggle" @click="toggleTemplatePanel">
        <el-icon :size="16">
          <DArrowLeft v-if="!isTemplateCollapsed" />
          <DArrowRight v-else />
        </el-icon>
      </div>

      <!-- 右侧编辑器和结果区 -->
      <div class="right-panel" ref="rightPanelRef">
        <!-- SQL 编辑器区 -->
        <div class="editor-section" :style="{ height: editorHeight + 'px' }">
          <el-card :body-style="{ padding: '12px', height: 'calc(100% - 20px)' }">
            <template #header>
              <div class="card-header">
                <span>SQL 编辑器</span>
                <div class="editor-actions">
                  <el-button-group>
                    <el-button
                      type="primary"
                      :icon="CaretRight"
                      :loading="queryStore.isQuerying"
                      @click="handleExecute"
                    >
                      执行 (F5)
                    </el-button>
                    <el-tooltip content="执行选中的 SQL (Ctrl+Enter)" placement="bottom">
                      <el-button
                        type="primary"
                        :loading="queryStore.isQuerying"
                        @click="handleExecuteSelected"
                      >
                        选中执行
                      </el-button>
                    </el-tooltip>
                  </el-button-group>
                  <el-button :icon="DocumentAdd" @click="handleSaveTemplate">
                    保存模板
                  </el-button>
                  <el-tooltip content="格式化 (Shift+Alt+F)" placement="bottom">
                    <el-button :icon="Brush" @click="handleFormat">
                      格式化
                    </el-button>
                  </el-tooltip>
                  <el-divider direction="vertical" />
                  <el-tooltip content="撤回 (Ctrl+Z)" placement="bottom">
                    <el-button :icon="RefreshLeft" @click="handleUndo" />
                  </el-tooltip>
                  <el-tooltip content="重做 (Ctrl+Y)" placement="bottom">
                    <el-button :icon="RefreshRight" @click="handleRedo" />
                  </el-tooltip>
                  <el-divider direction="vertical" />
                  <el-button :icon="Delete" @click="handleClear">
                    清空
                  </el-button>
                </div>
              </div>
            </template>
            <SqlEditor
              ref="sqlEditorRef"
              v-model="queryStore.sql"
              :tables="tableSuggestions"
              @execute="handleExecute"
              @execute-selected="handleExecuteSelected"
              @format="handleFormat"
            />
          </el-card>
        </div>

        <!-- 可拖拽分隔条 -->
        <div
          class="resize-handle"
          @mousedown="startResize"
        >
          <div class="resize-line"></div>
        </div>

        <!-- 查询结果区 -->
        <div class="result-section">
          <el-card :body-style="{ padding: '12px', height: 'calc(100% - 20px)' }">
            <template #header>
              <div class="card-header">
                <span>查询结果</span>
              </div>
            </template>
            <QueryResult
              :result="queryStore.queryResult"
              :height="resultHeight"
            />
          </el-card>
        </div>
      </div>
    </div>

    <!-- 保存模板对话框 -->
    <el-dialog
      v-model="saveDialogVisible"
      title="保存为模板"
      width="500px"
    >
      <el-form
        ref="templateFormRef"
        :model="templateForm"
        :rules="templateRules"
        label-width="80px"
      >
        <el-form-item label="模块" prop="moduleId">
          <el-tree-select
            v-model="templateForm.moduleId"
            :data="moduleTreeData"
            :props="moduleTreeProps"
            placeholder="选择所属模块"
            check-strictly
          />
        </el-form-item>
        <el-form-item label="名称" prop="name">
          <el-input v-model="templateForm.name" placeholder="模板名称" />
        </el-form-item>
        <el-form-item label="描述" prop="description">
          <el-input
            v-model="templateForm.description"
            type="textarea"
            :rows="3"
            placeholder="模板描述（可选）"
          />
        </el-form-item>
        <el-form-item label="公开">
          <el-switch v-model="templateForm.isPublic" />
          <span class="tip">公开后其他用户也可以使用此模板</span>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="saveDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="saving" @click="handleSaveConfirm">保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { ElMessage, type FormInstance, type FormRules } from 'element-plus'
import { CaretRight, DocumentAdd, Brush, Delete, DArrowLeft, DArrowRight, RefreshLeft, RefreshRight } from '@element-plus/icons-vue'
import { useQueryStore, useTemplateStore } from '@/stores'
import { templateApi } from '@/services'
import { format as formatSql } from 'sql-formatter'
import SqlEditor from '@/components/query/SqlEditor.vue'
import QueryResult from '@/components/query/QueryResult.vue'
import TemplateTree from '@/components/query/TemplateTree.vue'
import type { TemplateDto, CreateTemplateRequest } from '@/types'

const queryStore = useQueryStore()
const templateStore = useTemplateStore()

const sqlEditorRef = ref<InstanceType<typeof SqlEditor>>()
const selectedConnectionId = ref<number | null>(queryStore.selectedConnectionId)
const isTemplateCollapsed = ref(false)

// 判断当前选中的数据库是否为正式环境
const isSelectedProduction = computed(() => {
  if (!selectedConnectionId.value) return false
  const conn = queryStore.connections.find(c => c.id === selectedConnectionId.value)
  return conn?.isProduction ?? false
})

// 右侧面板引用和拖拽状态
const rightPanelRef = ref<HTMLElement>()
const editorHeight = ref(300)
const isResizing = ref(false)
const startY = ref(0)
const startHeight = ref(0)

// 切换模板面板
function toggleTemplatePanel() {
  isTemplateCollapsed.value = !isTemplateCollapsed.value
}

// 拖拽分隔条开始
function startResize(e: MouseEvent) {
  isResizing.value = true
  startY.value = e.clientY
  startHeight.value = editorHeight.value
  document.addEventListener('mousemove', onResize)
  document.addEventListener('mouseup', stopResize)
  document.body.style.cursor = 'row-resize'
  document.body.style.userSelect = 'none'
}

// 拖拽中
function onResize(e: MouseEvent) {
  if (!isResizing.value || !rightPanelRef.value) return

  const deltaY = e.clientY - startY.value
  const newHeight = startHeight.value + deltaY
  const panelHeight = rightPanelRef.value.clientHeight

  // 限制最小和最大高度
  const minHeight = 150
  const maxHeight = panelHeight - 200 // 保留结果区最小200px

  editorHeight.value = Math.max(minHeight, Math.min(maxHeight, newHeight))
}

// 拖拽结束
function stopResize() {
  isResizing.value = false
  document.removeEventListener('mousemove', onResize)
  document.removeEventListener('mouseup', stopResize)
  document.body.style.cursor = ''
  document.body.style.userSelect = ''
}

// 结果区高度（动态计算）
const resultHeight = computed(() => {
  if (!rightPanelRef.value) return 300
  // 减去编辑器高度、分隔条高度(12px)、gap(16px)和一些边距
  return rightPanelRef.value.clientHeight - editorHeight.value - 28
})

// 计算表名提示数据
const tableSuggestions = computed(() => {
  return queryStore.tables.map(t => ({
    name: t.name,
    comment: t.comment || ''
  }))
})

// 保存模板对话框
const saveDialogVisible = ref(false)
const saving = ref(false)
const templateFormRef = ref<FormInstance>()
const templateForm = ref<CreateTemplateRequest>({
  moduleId: 0,
  name: '',
  sqlContent: '',
  description: '',
  isPublic: false
})

const templateRules: FormRules = {
  moduleId: [{ required: true, message: '请选择模块', trigger: 'change' }],
  name: [{ required: true, message: '请输入模板名称', trigger: 'blur' }]
}

// 模块树数据
const moduleTreeData = computed(() => {
  return convertModulesToTreeSelect(templateStore.modules)
})

const moduleTreeProps = {
  value: 'id',
  label: 'label',
  children: 'children'
}

interface ModuleTreeNode {
  id: number
  label: string
  children?: ModuleTreeNode[]
}

function convertModulesToTreeSelect(modules: any[]): ModuleTreeNode[] {
  return modules.map(m => ({
    id: m.id,
    label: m.name,
    children: m.children?.length ? convertModulesToTreeSelect(m.children) : undefined
  }))
}

// 平台变更
async function handlePlatformChange(platformCode: string) {
  await queryStore.selectPlatform(platformCode)
  selectedConnectionId.value = queryStore.selectedConnectionId
}

// 数据库连接变更
async function handleConnectionChange(connectionId: number) {
  await queryStore.selectConnection(connectionId)
}

// 选择模板
function handleSelectTemplate(template: TemplateDto) {
  queryStore.setSql(template.sqlContent)
  ElMessage.success(`已加载模板: ${template.name}`)
}

// 执行查询
async function handleExecute() {
  if (!queryStore.selectedPlatformCode) {
    ElMessage.warning('请选择平台')
    return
  }
  if (!queryStore.selectedConnectionId) {
    ElMessage.warning('请选择数据库')
    return
  }
  if (!queryStore.sql.trim()) {
    ElMessage.warning('请输入 SQL 语句')
    return
  }

  // 检查是否是 SELECT 语句且没有 TOP/LIMIT 限制，自动添加 TOP 100
  const sqlTrimmed = queryStore.sql.trim()
  const isSelectWithoutLimit = /^\s*SELECT\s/i.test(sqlTrimmed) &&
    !/\bTOP\s+\d+/i.test(sqlTrimmed) &&
    !/\bLIMIT\s+\d+/i.test(sqlTrimmed)

  if (isSelectWithoutLimit) {
    // 在 SELECT 后添加 TOP 100，并更新编辑器显示
    const newSql = sqlTrimmed.replace(/^(\s*SELECT\s+)/i, '$1TOP 100 ')
    queryStore.setSql(newSql)
    sqlEditorRef.value?.setValue(newSql)
  }

  const success = await queryStore.executeQuery()
  if (success) {
    ElMessage.success('查询成功')
  }
}

// 选中执行
async function handleExecuteSelected() {
  if (!queryStore.selectedPlatformCode) {
    ElMessage.warning('请选择平台')
    return
  }
  if (!queryStore.selectedConnectionId) {
    ElMessage.warning('请选择数据库')
    return
  }

  // 获取选中的文本
  const selectedSql = sqlEditorRef.value?.getSelectedText()?.trim()
  if (!selectedSql) {
    ElMessage.warning('请先选中要执行的 SQL 语句')
    return
  }

  // 检查是否是 SELECT 语句且没有 TOP/LIMIT 限制，自动添加 TOP 100
  const isSelectWithoutLimit = /^\s*SELECT\s/i.test(selectedSql) &&
    !/\bTOP\s+\d+/i.test(selectedSql) &&
    !/\bLIMIT\s+\d+/i.test(selectedSql)

  let sqlToExecute = selectedSql
  if (isSelectWithoutLimit) {
    sqlToExecute = selectedSql.replace(/^(\s*SELECT\s+)/i, '$1TOP 100 ')
  }

  // 临时设置 SQL 并执行
  const originalSql = queryStore.sql
  queryStore.setSql(sqlToExecute)

  const success = await queryStore.executeQuery()
  if (success) {
    ElMessage.success('选中 SQL 执行成功')
  }

  // 恢复原来的 SQL
  queryStore.setSql(originalSql)
}

// 保存模板
function handleSaveTemplate() {
  if (!queryStore.sql.trim()) {
    ElMessage.warning('请先输入 SQL 语句')
    return
  }

  templateForm.value.sqlContent = queryStore.sql
  saveDialogVisible.value = true
}

// 确认保存模板
async function handleSaveConfirm() {
  if (!templateFormRef.value) return

  try {
    await templateFormRef.value.validate()
    saving.value = true

    const { data } = await templateApi.createTemplate(templateForm.value)
    if (data.success) {
      ElMessage.success('模板保存成功')
      saveDialogVisible.value = false
      // 重新加载模板树
      templateStore.loadModules()
      // 重置表单
      templateForm.value = {
        moduleId: 0,
        name: '',
        sqlContent: '',
        description: '',
        isPublic: false
      }
    }
  } catch {
    // 验证失败
  } finally {
    saving.value = false
  }
}

// 格式化 SQL
function handleFormat() {
  const currentSql = sqlEditorRef.value?.getValue() || queryStore.sql
  if (!currentSql.trim()) {
    ElMessage.warning('请先输入 SQL 语句')
    return
  }

  try {
    // 使用 sql-formatter 进行高级格式化
    const formattedSql = formatSql(currentSql, {
      language: 'tsql', // 使用 T-SQL 方言 (SQL Server)
      tabWidth: 4,
      useTabs: false,
      keywordCase: 'upper', // 关键字大写
      indentStyle: 'standard',
      logicalOperatorNewline: 'before',
      expressionWidth: 80,
      linesBetweenQueries: 2
    })

    // 直接更新编辑器内容
    sqlEditorRef.value?.setValue(formattedSql)
    ElMessage.success('格式化完成')
  } catch (error) {
    console.error('SQL 格式化失败:', error)
    ElMessage.error('格式化失败，请检查 SQL 语法')
  }
}

// 撤回
function handleUndo() {
  sqlEditorRef.value?.undo()
}

// 重做
function handleRedo() {
  sqlEditorRef.value?.redo()
}

// 清空编辑器
function handleClear() {
  queryStore.setSql('')
  queryStore.clearResult()
}

// 初始化
onMounted(async () => {
  await queryStore.loadPlatforms()
  selectedConnectionId.value = queryStore.selectedConnectionId

  // 初始化编辑器高度为面板的45%
  if (rightPanelRef.value) {
    editorHeight.value = Math.floor(rightPanelRef.value.clientHeight * 0.45)
  }
})

// 清理事件监听
onUnmounted(() => {
  document.removeEventListener('mousemove', onResize)
  document.removeEventListener('mouseup', stopResize)
})
</script>

<style scoped lang="scss">
.query-view {
  height: 100%;
  display: flex;
  flex-direction: column;
}

.query-selectors {
  display: flex;
  gap: 24px;
  margin-bottom: 16px;
  padding: 16px;
  background-color: var(--el-bg-color);
  border-radius: 8px;
  box-shadow: 0 1px 4px rgba(0, 0, 0, 0.08);

  .selector-item {
    display: flex;
    align-items: center;
    gap: 8px;

    .label {
      font-size: 14px;
      color: var(--el-text-color-regular);
      white-space: nowrap;
    }

    .el-select {
      width: 280px;
    }

    &.database-selector .el-select {
      width: 400px;
    }
  }
}

.query-content {
  flex: 1;
  display: flex;
  gap: 8px;
  min-height: 0;
}

.left-panel {
  width: 280px;
  flex-shrink: 0;
  transition: width 0.3s ease, margin-left 0.3s ease, opacity 0.3s ease;
  overflow: hidden;

  &.collapsed {
    width: 0;
    margin-left: -8px;
    opacity: 0;
    pointer-events: none;
  }

  .template-card {
    height: 100%;
  }
}

.collapse-toggle {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 16px;
  flex-shrink: 0;
  cursor: pointer;
  color: var(--el-text-color-secondary);
  transition: color 0.2s;
  margin-left: -4px;
  margin-right: -4px;

  &:hover {
    color: var(--el-color-primary);
  }
}

.right-panel {
  flex: 1;
  display: flex;
  flex-direction: column;
  min-width: 0;
}

.editor-section {
  flex-shrink: 0;
  min-height: 150px;

  .el-card {
    height: 100%;
  }
}

.resize-handle {
  height: 12px;
  cursor: row-resize;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  position: relative;
  z-index: 10;

  &:hover .resize-line,
  &:active .resize-line {
    background-color: var(--el-color-primary);
    height: 4px;
  }

  .resize-line {
    width: 60px;
    height: 3px;
    background-color: var(--el-border-color);
    border-radius: 2px;
    transition: all 0.2s ease;
  }
}

.result-section {
  flex: 1;
  min-height: 150px;
  overflow: hidden;

  .el-card {
    height: 100%;
  }
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;

  span {
    font-size: 14px;
    font-weight: 500;
  }

  .editor-actions {
    display: flex;
    gap: 8px;
  }
}

.tip {
  margin-left: 8px;
  font-size: 12px;
  color: var(--el-text-color-secondary);
}

// 数据库选项样式
.connection-option {
  display: flex;
  align-items: center;
  justify-content: space-between;
  width: 100%;

  .conn-name {
    flex: 1;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  .env-tag {
    margin-left: 8px;
    flex-shrink: 0;
  }
}

// 正式环境选项高亮
:deep(.production-option) {
  background-color: rgba(var(--el-color-danger-rgb), 0.08) !important;
  font-weight: 500;

  &:hover {
    background-color: rgba(var(--el-color-danger-rgb), 0.15) !important;
  }

  &.is-selected {
    background-color: rgba(var(--el-color-danger-rgb), 0.2) !important;
  }
}

// 正式环境选中时输入框高亮
.production-select {
  :deep(.el-input__wrapper) {
    background-color: rgba(var(--el-color-danger-rgb), 0.1) !important;
    border-color: var(--el-color-danger) !important;
    box-shadow: 0 0 0 1px var(--el-color-danger) inset !important;
  }

  :deep(.el-input__inner) {
    color: var(--el-color-danger) !important;
    font-weight: 600;
  }
}

// 选中环境标签
.selected-env-tag {
  margin-left: 8px;
  animation: pulse 2s infinite;
}

@keyframes pulse {
  0%, 100% {
    opacity: 1;
  }
  50% {
    opacity: 0.6;
  }
}
</style>
