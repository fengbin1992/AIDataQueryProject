<template>
  <div class="query-workspace">
    <!-- 顶部选择区 -->
    <div class="query-selectors" v-show="!selectorsHidden">
      <div class="selectors-content">
        <div class="selector-item">
          <span class="label">平台：</span>
          <el-select
            v-model="localPlatformCode"
            placeholder="选择平台"
            @change="handlePlatformChange"
          >
            <el-option
              v-for="platform in platforms"
              :key="platform.code"
              :label="platform.name"
              :value="platform.code"
            />
          </el-select>
        </div>
        <div class="selector-item database-selector">
          <span class="label">数据库：</span>
          <el-select
            v-model="localConnectionId"
            placeholder="选择数据库"
            @change="handleConnectionChange"
            :popper-options="{ strategy: 'fixed' }"
            :class="{ 'production-select': isSelectedProduction }"
          >
            <el-option
              v-for="conn in connections"
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
      <el-tooltip content="隐藏选择器" placement="bottom">
        <div class="panel-hide-btn" @click="emit('hideSelectors')">
          <el-icon :size="14"><Close /></el-icon>
        </div>
      </el-tooltip>
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
        <div class="editor-section" :class="{ 'editor-expanded': resultHidden }" :style="resultHidden ? {} : { height: editorHeight + 'px' }">
          <el-card :body-style="{ padding: '12px', height: 'calc(100% - 20px)' }">
            <template #header>
              <div class="card-header">
                <span class="header-title">SQL 编辑器</span>
                <div class="editor-actions">
                  <el-button-group>
                    <el-tooltip content="执行 (F5)" placement="bottom">
                      <el-button
                        type="primary"
                        :icon="CaretRight"
                        :loading="isQuerying"
                        @click="handleExecute"
                      >
                        <span class="btn-text">执行</span>
                      </el-button>
                    </el-tooltip>
                    <el-tooltip content="执行选中的 SQL (Ctrl+Enter)" placement="bottom">
                      <el-button
                        type="primary"
                        :loading="isQuerying"
                        @click="handleExecuteSelected"
                      >
                        <span class="btn-text">选中</span>
                      </el-button>
                    </el-tooltip>
                  </el-button-group>
                  <el-tooltip content="保存模板" placement="bottom">
                    <el-button :icon="DocumentAdd" @click="handleSaveTemplate" />
                  </el-tooltip>
                  <el-tooltip content="格式化 (Shift+Alt+F)" placement="bottom">
                    <el-button :icon="Brush" @click="handleFormat" />
                  </el-tooltip>
                  <el-tooltip content="撤回 (Ctrl+Z)" placement="bottom">
                    <el-button :icon="RefreshLeft" @click="handleUndo" />
                  </el-tooltip>
                  <el-tooltip content="重做 (Ctrl+Y)" placement="bottom">
                    <el-button :icon="RefreshRight" @click="handleRedo" />
                  </el-tooltip>
                  <el-tooltip content="清空" placement="bottom">
                    <el-button :icon="Delete" @click="handleClear" />
                  </el-tooltip>
                  <el-divider direction="vertical" />
                  <el-select
                    v-model="editorFontFamily"
                    class="font-family-select"
                    size="small"
                    @change="saveEditorSettings"
                  >
                    <el-option
                      v-for="f in fontFamilyOptions"
                      :key="f"
                      :label="f"
                      :value="f"
                    />
                  </el-select>
                  <el-input-number
                    v-model="editorFontSize"
                    class="font-size-input"
                    size="small"
                    :min="12"
                    :max="28"
                    :step="1"
                    controls-position="right"
                    @change="saveEditorSettings"
                  />
                </div>
              </div>
            </template>
            <SqlEditor
              ref="sqlEditorRef"
              v-model="localSql"
              :tables="tableSuggestions"
              :font-size="editorFontSize"
              :font-family="editorFontFamily"
              @execute="handleExecute"
              @execute-selected="handleExecuteSelected"
              @format="handleFormat"
            />
          </el-card>
        </div>

        <!-- 可拖拽分隔条 -->
        <div
          class="resize-handle"
          v-show="!resultHidden"
          @mousedown="startResize($event)"
        >
          <div class="resize-line"></div>
        </div>

        <!-- 查询结果区 -->
        <div class="result-section" v-show="!resultHidden">
          <el-card :body-style="{ padding: '12px', height: 'calc(100% - 20px)' }">
            <template #header>
              <div class="card-header">
                <span>查询结果</span>
                <el-tooltip content="隐藏结果" placement="bottom">
                  <div class="panel-hide-btn" @click="emit('hideResult')">
                    <el-icon :size="14"><Close /></el-icon>
                  </div>
                </el-tooltip>
              </div>
            </template>
            <QueryResult
              :result="queryResult"
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
import { ref, computed, watch, onMounted, onUnmounted, onActivated, onDeactivated } from 'vue'
import { ElMessage, type FormInstance, type FormRules } from 'element-plus'
import { CaretRight, DocumentAdd, Brush, Delete, DArrowLeft, DArrowRight, RefreshLeft, RefreshRight, Close } from '@element-plus/icons-vue'
import { useTemplateStore, useQueryTabsStore } from '@/stores'
import { platformApi, queryApi, templateApi } from '@/services'
import { format as formatSql } from 'sql-formatter'
import SqlEditor from './SqlEditor.vue'
import QueryResult from './QueryResult.vue'
import TemplateTree from './TemplateTree.vue'
import type { TemplateDto, CreateTemplateRequest, PlatformDto, DatabaseConnectionDto, TableInfo, QueryResult as QueryResultType } from '@/types'

const props = defineProps<{
  tabId: string
  selectorsHidden?: boolean
  resultHidden?: boolean
}>()

const emit = defineEmits<{
  (e: 'hideSelectors'): void
  (e: 'showSelectors'): void
  (e: 'hideResult'): void
  (e: 'showResult'): void
}>()

const templateStore = useTemplateStore()
const tabsStore = useQueryTabsStore()

// 当前标签数据
const currentTab = computed(() => tabsStore.tabs.find(t => t.id === props.tabId))

// 本地状态（与标签同步）
const localPlatformCode = ref<string | null>(null)
const localConnectionId = ref<number | null>(null)
const localSql = ref('')

// 平台和连接数据
const platforms = ref<PlatformDto[]>([])
const connections = ref<DatabaseConnectionDto[]>([])
const tables = ref<TableInfo[]>([])

// 查询状态
const isQuerying = ref(false)
const queryResult = ref<QueryResultType | null>(null)

// 编辑器引用
const sqlEditorRef = ref<InstanceType<typeof SqlEditor>>()

// 编辑器字体设置（从 localStorage 加载）
const EDITOR_SETTINGS_KEY = 'sql-editor-settings'
const fontFamilyOptions = ['Consolas', 'Monaco', 'Courier New', 'Fira Code', 'Source Code Pro', 'JetBrains Mono', 'Cascadia Code']

function loadEditorSettings() {
  try {
    const stored = localStorage.getItem(EDITOR_SETTINGS_KEY)
    if (stored) {
      return JSON.parse(stored) as { fontSize: number; fontFamily: string }
    }
  } catch { /* ignore */ }
  return { fontSize: 14, fontFamily: 'Consolas' }
}

const editorSettings = loadEditorSettings()
const editorFontSize = ref(editorSettings.fontSize)
const editorFontFamily = ref(editorSettings.fontFamily)

function saveEditorSettings() {
  localStorage.setItem(EDITOR_SETTINGS_KEY, JSON.stringify({
    fontSize: editorFontSize.value,
    fontFamily: editorFontFamily.value
  }))
}
// 模板面板折叠状态
const isTemplateCollapsed = ref(false)

// 判断当前选中的数据库是否为正式环境
const isSelectedProduction = computed(() => {
  if (!localConnectionId.value) return false
  const conn = connections.value.find(c => c.id === localConnectionId.value)
  return conn?.isProduction ?? false
})

// 右侧面板引用和拖拽状态
const rightPanelRef = ref<HTMLElement>()
const editorHeight = ref(400)
const isResizing = ref(false)
const startY = ref(0)
const startHeight = ref(0)

// 计算表名提示数据
const tableSuggestions = computed(() => {
  return tables.value.map(t => ({
    name: t.name,
    comment: ''
  }))
})

// 结果区高度
const resultHeight = computed(() => {
  if (!rightPanelRef.value) return 300
  return rightPanelRef.value.clientHeight - editorHeight.value - 28
})

// 同步标签状态到本地
watch(
  () => currentTab.value,
  (tab) => {
    if (tab) {
      localPlatformCode.value = tab.platformCode
      localConnectionId.value = tab.connectionId
      localSql.value = tab.sql
      queryResult.value = tab.queryResult
      isQuerying.value = tab.isQuerying
    }
  },
  { immediate: true, deep: true }
)

// 同步本地 SQL 到标签
watch(localSql, (newSql) => {
  if (currentTab.value && currentTab.value.sql !== newSql) {
    tabsStore.updateTab(props.tabId, { sql: newSql, isDirty: true })
  }
})

// 切换模板面板
function toggleTemplatePanel() {
  isTemplateCollapsed.value = !isTemplateCollapsed.value
}

// 拖拽分隔条
function startResize(e: MouseEvent) {
  isResizing.value = true
  startY.value = e.clientY
  startHeight.value = editorHeight.value
  document.addEventListener('mousemove', onResize)
  document.addEventListener('mouseup', stopResize)
  document.body.style.cursor = 'row-resize'
  document.body.style.userSelect = 'none'
}

function onResize(e: MouseEvent) {
  if (!isResizing.value || !rightPanelRef.value) return

  const deltaY = e.clientY - startY.value
  const newHeight = startHeight.value + deltaY
  const panelHeight = rightPanelRef.value.clientHeight

  const minHeight = 150
  const maxHeight = panelHeight - 200

  editorHeight.value = Math.max(minHeight, Math.min(maxHeight, newHeight))
}

function stopResize() {
  isResizing.value = false
  document.removeEventListener('mousemove', onResize)
  document.removeEventListener('mouseup', stopResize)
  document.body.style.cursor = ''
  document.body.style.userSelect = ''
}

// 加载平台列表
async function loadPlatforms(): Promise<void> {
  try {
    const { data } = await platformApi.getPlatforms()
    if (data.success && data.data) {
      platforms.value = data.data
      // 如果标签没有选中平台，自动选择第一个
      if (data.data.length > 0 && !localPlatformCode.value) {
        await handlePlatformChange(data.data[0].code)
      } else if (localPlatformCode.value) {
        // 恢复之前的选择，加载连接
        await loadConnections(localPlatformCode.value)
      }
    }
  } catch {
    platforms.value = []
  }
}

// 加载连接列表
async function loadConnections(platformCode: string): Promise<void> {
  try {
    const { data } = await platformApi.getConnections(platformCode)
    if (data.success && data.data) {
      connections.value = data.data
      // 如果标签没有选中连接，自动选择第一个
      if (data.data.length > 0 && !localConnectionId.value) {
        await handleConnectionChange(data.data[0].id)
      } else if (localConnectionId.value) {
        // 恢复之前的选择，加载表
        await loadTables(localConnectionId.value)
      }
    }
  } catch {
    connections.value = []
  }
}

// 加载表列表
async function loadTables(connectionId: number): Promise<void> {
  try {
    const { data } = await queryApi.getTables(connectionId)
    if (data.success && data.data) {
      tables.value = data.data
    }
  } catch {
    tables.value = []
  }
}

// 平台变更
async function handlePlatformChange(platformCode: string) {
  localPlatformCode.value = platformCode
  localConnectionId.value = null
  connections.value = []
  tables.value = []

  tabsStore.updateTab(props.tabId, {
    platformCode,
    connectionId: null
  })

  await loadConnections(platformCode)
}

// 数据库连接变更
async function handleConnectionChange(connectionId: number) {
  localConnectionId.value = connectionId
  tables.value = []

  tabsStore.updateTab(props.tabId, { connectionId })

  await loadTables(connectionId)
}

// 选择模板 - 新开标签页展示
function handleSelectTemplate(template: TemplateDto) {
  // 检查是否可以创建新标签
  if (!tabsStore.canCreateTab) {
    // 达到最大标签数，在当前标签加载
    localSql.value = template.sqlContent
    sqlEditorRef.value?.setValue(template.sqlContent)
    ElMessage.warning(`已达到最大标签数，在当前标签加载模板: ${template.name}`)
    return
  }

  // 创建新标签页，继承当前的平台和数据库选择
  tabsStore.createTab({
    name: template.name,
    platformCode: localPlatformCode.value,
    connectionId: localConnectionId.value,
    sql: template.sqlContent
  })

  ElMessage.success(`已在新标签打开模板: ${template.name}`)
}

// 执行查询
async function handleExecute() {
  if (!localPlatformCode.value) {
    ElMessage.warning('请选择平台')
    return
  }
  if (!localConnectionId.value) {
    ElMessage.warning('请选择数据库')
    return
  }
  if (!localSql.value.trim()) {
    ElMessage.warning('请输入 SQL 语句')
    return
  }

  // 检查是否需要添加 TOP 20
  const sqlTrimmed = localSql.value.trim()
  const isSelectWithoutLimit = /^\s*SELECT\s/i.test(sqlTrimmed) &&
    !/\bTOP\s+\d+/i.test(sqlTrimmed) &&
    !/\bLIMIT\s+\d+/i.test(sqlTrimmed)

  let sqlToExecute = sqlTrimmed
  if (isSelectWithoutLimit) {
    sqlToExecute = sqlTrimmed.replace(/^(\s*SELECT\s+)/i, '$1TOP 20 ')
    localSql.value = sqlToExecute
    sqlEditorRef.value?.setValue(sqlToExecute)
  }

  isQuerying.value = true
  tabsStore.setQuerying(props.tabId, true)
  queryResult.value = null

  try {
    const { data } = await queryApi.execute({
      platformCode: localPlatformCode.value,
      connectionId: localConnectionId.value,
      sql: sqlToExecute
    })

    if (data.success && data.data) {
      queryResult.value = data.data
      tabsStore.setQueryResult(props.tabId, data.data)
      if (data.data.success) {
        ElMessage.success('查询成功')
      }
    }
  } catch {
    ElMessage.error('查询失败')
  } finally {
    isQuerying.value = false
    tabsStore.setQuerying(props.tabId, false)
  }
}

// 选中执行
async function handleExecuteSelected() {
  if (!localPlatformCode.value) {
    ElMessage.warning('请选择平台')
    return
  }
  if (!localConnectionId.value) {
    ElMessage.warning('请选择数据库')
    return
  }

  const selectedSql = sqlEditorRef.value?.getSelectedText()?.trim()
  if (!selectedSql) {
    ElMessage.warning('请先选中要执行的 SQL 语句')
    return
  }

  // 检查是否需要添加 TOP 20
  const isSelectWithoutLimit = /^\s*SELECT\s/i.test(selectedSql) &&
    !/\bTOP\s+\d+/i.test(selectedSql) &&
    !/\bLIMIT\s+\d+/i.test(selectedSql)

  let sqlToExecute = selectedSql
  if (isSelectWithoutLimit) {
    sqlToExecute = selectedSql.replace(/^(\s*SELECT\s+)/i, '$1TOP 20 ')
  }

  isQuerying.value = true
  tabsStore.setQuerying(props.tabId, true)
  queryResult.value = null

  try {
    const { data } = await queryApi.execute({
      platformCode: localPlatformCode.value,
      connectionId: localConnectionId.value,
      sql: sqlToExecute
    })

    if (data.success && data.data) {
      queryResult.value = data.data
      tabsStore.setQueryResult(props.tabId, data.data)
      if (data.data.success) {
        ElMessage.success('选中 SQL 执行成功')
      }
    }
  } catch {
    ElMessage.error('查询失败')
  } finally {
    isQuerying.value = false
    tabsStore.setQuerying(props.tabId, false)
  }
}

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

function handleSaveTemplate() {
  if (!localSql.value.trim()) {
    ElMessage.warning('请先输入 SQL 语句')
    return
  }

  templateForm.value.sqlContent = localSql.value
  saveDialogVisible.value = true
}

async function handleSaveConfirm() {
  if (!templateFormRef.value) return

  try {
    await templateFormRef.value.validate()
    saving.value = true

    const { data } = await templateApi.createTemplate(templateForm.value)
    if (data.success) {
      ElMessage.success('模板保存成功')
      saveDialogVisible.value = false
      templateStore.loadModules()
      tabsStore.markClean(props.tabId)
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
  const currentSql = sqlEditorRef.value?.getValue() || localSql.value
  if (!currentSql.trim()) {
    ElMessage.warning('请先输入 SQL 语句')
    return
  }

  try {
    const formattedSql = formatSql(currentSql, {
      language: 'tsql',
      tabWidth: 4,
      useTabs: false,
      keywordCase: 'upper',
      indentStyle: 'standard',
      logicalOperatorNewline: 'before',
      expressionWidth: 80,
      linesBetweenQueries: 2
    })

    localSql.value = formattedSql
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
  localSql.value = ''
  queryResult.value = null
  tabsStore.updateTab(props.tabId, { sql: '', queryResult: null, isDirty: false })
}

// 初始化
onMounted(async () => {
  await loadPlatforms()

  // 初始化编辑器高度（编辑器占70%，结果区占30%）
  if (rightPanelRef.value) {
    editorHeight.value = Math.floor(rightPanelRef.value.clientHeight * 0.7)
  }
})

// KeepAlive 激活时
onActivated(() => {
  // 重新计算编辑器高度（编辑器占60%，结果区占40%）
  if (rightPanelRef.value) {
    editorHeight.value = Math.floor(rightPanelRef.value.clientHeight * 0.6)
  }
  // 聚焦编辑器
  setTimeout(() => {
    sqlEditorRef.value?.focus()
  }, 50)
})

// KeepAlive 停用时
onDeactivated(() => {
  // 保存当前状态到 store
  tabsStore.saveToStorage()
})

// 清理
onUnmounted(() => {
  document.removeEventListener('mousemove', onResize)
  document.removeEventListener('mouseup', stopResize)
})

// 暴露方法供父组件调用
defineExpose({
  focus: () => sqlEditorRef.value?.focus()
})
</script>

<style scoped lang="scss">
.query-workspace {
  height: 100%;
  display: flex;
  flex-direction: column;
}

.query-selectors {
  display: flex;
  flex-wrap: wrap;
  gap: 16px;
  margin-bottom: 12px;
  padding: 12px 16px;
  background-color: var(--el-bg-color);
  border-radius: 8px;
  box-shadow: 0 1px 4px rgba(0, 0, 0, 0.08);
  transition: padding 0.2s ease;
  align-items: center;

  .selectors-content {
    display: flex;
    flex-wrap: wrap;
    gap: 16px;
    flex: 1;
  }

  .selector-item {
    display: flex;
    align-items: center;
    gap: 8px;
    flex: 1;
    min-width: 280px;

    .label {
      font-size: 14px;
      color: var(--el-text-color-regular);
      white-space: nowrap;
    }

    .el-select {
      flex: 1;
      min-width: 200px;
    }
  }

  .database-selector {
    .el-select {
      min-width: 220px;
    }
  }
}

.panel-hide-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 24px;
  height: 24px;
  cursor: pointer;
  color: var(--el-text-color-secondary);
  border-radius: 4px;
  transition: all 0.2s;
  flex-shrink: 0;

  &:hover {
    background-color: var(--el-fill-color-light);
    color: var(--el-color-primary);
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

  &.editor-expanded {
    flex: 1;
    height: auto !important;
  }

  .el-card {
    height: 100%;
  }
}

.resize-handle {
  height: 20px;
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
  gap: 12px;

  .header-title {
    font-size: 14px;
    font-weight: 500;
    white-space: nowrap;
    flex-shrink: 0;
  }

  // 非编辑器区域的标题（查询结果等）
  > span:not(.header-title) {
    font-size: 14px;
    font-weight: 500;
  }

  .editor-actions {
    display: flex;
    gap: 4px;
    align-items: center;
    flex-wrap: nowrap;
    flex-shrink: 1;
    min-width: 0;

    .btn-text {
      margin-left: 4px;
    }

    .font-family-select {
      width: 130px;
      flex-shrink: 0;
    }

    .font-size-input {
      width: 72px;
      flex-shrink: 0;
    }
  }
}

// 小屏幕: 隐藏按钮文字，缩小字体选择器
@media (max-width: 1440px) {
  .card-header .editor-actions {
    .btn-text {
      display: none;
    }

    .font-family-select {
      width: 110px;
    }
  }
}

// 超小屏幕: 进一步缩小
@media (max-width: 1280px) {
  .card-header .editor-actions {
    gap: 2px;

    .font-family-select {
      width: 95px;
    }

    .font-size-input {
      width: 66px;
    }

    .el-divider {
      display: none;
    }
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
