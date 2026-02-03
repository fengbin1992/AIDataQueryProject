<template>
  <div class="config-query-tab">
    <!-- 左侧配置查询列表 -->
    <div class="list-panel" :class="{ collapsed: listCollapsed }">
      <template v-if="!listCollapsed">
        <div class="list-header">
          <el-button class="collapse-btn" text @click="toggleList">
            <el-icon><ArrowLeft /></el-icon>
          </el-button>
          <span class="title">配置列表</span>
        </div>

        <div class="search-box">
          <el-input
            v-model="searchKeyword"
            placeholder="搜索..."
            :prefix-icon="Search"
            clearable
            @input="handleSearch"
          />
        </div>

        <div class="list-content">
          <!-- 我创建的 -->
          <div class="list-group">
            <div class="group-header" @click="toggleMyList">
              <el-icon><CaretRight v-if="!myListExpanded" /><CaretBottom v-else /></el-icon>
              <span>我创建的</span>
              <span class="count">({{ myQueries.length }})</span>
            </div>
            <div class="group-items" v-show="myListExpanded">
              <!-- 未分组的查询 -->
              <div
                v-for="item in myUnfolderedQueries"
                :key="item.id"
                class="list-item"
                :class="{ active: store.currentId === item.id }"
                @click="selectQuery(item.id)"
                @contextmenu.prevent="showMoveDialog(item.id, item.folderId)"
              >
                <span class="item-name">{{ item.name }}</span>
                <el-dropdown trigger="click" @click.stop @command="(cmd: string) => handleQueryCommand(cmd, item)">
                  <el-icon class="item-more"><MoreFilled /></el-icon>
                  <template #dropdown>
                    <el-dropdown-menu>
                      <el-dropdown-item command="move">移动到文件夹</el-dropdown-item>
                    </el-dropdown-menu>
                  </template>
                </el-dropdown>
              </div>

              <!-- 文件夹 -->
              <div v-for="folder in store.folders" :key="'folder-' + folder.id" class="folder-group">
                <div class="folder-header" @click="store.toggleFolder(folder.id)">
                  <el-icon>
                    <FolderOpened v-if="store.isFolderExpanded(folder.id)" />
                    <Folder v-else />
                  </el-icon>
                  <span class="folder-name">{{ folder.name }}</span>
                  <span class="count">({{ getQueriesInFolder(folder.id).length }})</span>
                  <el-dropdown trigger="click" @click.stop @command="(cmd: string) => handleFolderCommand(cmd, folder)">
                    <el-icon class="folder-more"><MoreFilled /></el-icon>
                    <template #dropdown>
                      <el-dropdown-menu>
                        <el-dropdown-item command="rename">重命名</el-dropdown-item>
                        <el-dropdown-item command="delete">删除</el-dropdown-item>
                      </el-dropdown-menu>
                    </template>
                  </el-dropdown>
                </div>
                <div class="folder-items" v-show="store.isFolderExpanded(folder.id)">
                  <div
                    v-for="item in getQueriesInFolder(folder.id)"
                    :key="item.id"
                    class="list-item"
                    :class="{ active: store.currentId === item.id }"
                    @click="selectQuery(item.id)"
                  >
                    <span class="item-name">{{ item.name }}</span>
                    <el-dropdown trigger="click" @click.stop @command="(cmd: string) => handleQueryCommand(cmd, item)">
                      <el-icon class="item-more"><MoreFilled /></el-icon>
                      <template #dropdown>
                        <el-dropdown-menu>
                          <el-dropdown-item command="move">移动到文件夹</el-dropdown-item>
                          <el-dropdown-item command="remove">移出文件夹</el-dropdown-item>
                        </el-dropdown-menu>
                      </template>
                    </el-dropdown>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <!-- 公开的 -->
          <div class="list-group">
            <div class="group-header" @click="togglePublicList">
              <el-icon><CaretRight v-if="!publicListExpanded" /><CaretBottom v-else /></el-icon>
              <span>公开的</span>
              <span class="count">({{ publicQueries.length }})</span>
            </div>
            <div class="group-items" v-show="publicListExpanded">
              <div
                v-for="item in publicQueries"
                :key="item.id"
                class="list-item"
                :class="{ active: store.currentId === item.id }"
                @click="selectQuery(item.id)"
              >
                <span class="item-name">{{ item.name }}</span>
              </div>
            </div>
          </div>
        </div>

        <div class="list-footer">
          <el-button type="primary" text @click="showEditor(null)">
            <el-icon><Plus /></el-icon>
            <span>新建</span>
          </el-button>
          <el-button text @click="showNewFolderDialog">
            <el-icon><Folder /></el-icon>
            <span>新建文件夹</span>
          </el-button>
        </div>
      </template>

      <!-- 收缩状态 -->
      <template v-else>
        <div class="collapsed-icons">
          <el-tooltip content="展开列表" placement="right">
            <div class="icon-btn" @click="toggleList">
              <el-icon><ArrowRight /></el-icon>
            </div>
          </el-tooltip>
          <el-tooltip content="配置列表" placement="right">
            <div class="icon-btn" @click="toggleList">
              <el-icon><Document /></el-icon>
            </div>
          </el-tooltip>
          <el-tooltip content="新建" placement="right">
            <div class="icon-btn" @click="showEditor(null)">
              <el-icon><Plus /></el-icon>
            </div>
          </el-tooltip>
        </div>
      </template>
    </div>

    <!-- 右侧内容区 -->
    <div class="content-panel">
      <template v-if="store.currentQuery">
        <!-- 顶部数据库选择区 -->
        <div class="database-selectors">
          <div class="selector-item">
            <span class="label">平台：</span>
            <el-select
              v-model="selectedPlatformCode"
              placeholder="选择平台"
              @change="handlePlatformChange"
              :disabled="!platforms.length"
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
              v-model="selectedConnectionId"
              placeholder="选择数据库"
              @change="handleConnectionChange"
              :disabled="!connections.length"
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
          <div class="default-connection-hint" v-if="store.currentQuery.connectionName">
            <el-tag type="info" size="small">
              默认：{{ store.currentQuery.connectionName }}
            </el-tag>
          </div>
        </div>

        <!-- SQL 预览面板 -->
        <div class="sql-panel" :class="{ expanded: store.sqlPanelExpanded }">
          <div class="panel-header" @click="store.toggleSqlPanel">
            <el-icon><CaretBottom v-if="store.sqlPanelExpanded" /><CaretRight v-else /></el-icon>
            <span>SQL 预览</span>
            <template v-if="!store.sqlPanelExpanded">
              <span class="summary">{{ store.sqlPreviewSummary }}</span>
            </template>
            <div class="panel-actions">
              <el-button v-if="store.sqlPanelExpanded" text size="small" @click.stop="copySql">
                <el-icon><CopyDocument /></el-icon>
                复制
              </el-button>
              <el-button text size="small" @click.stop="showEditor(store.currentId)" v-if="store.canEdit">
                <el-icon><Edit /></el-icon>
                编辑
              </el-button>
            </div>
          </div>
          <div class="panel-content" v-show="store.sqlPanelExpanded">
            <div class="sql-preview">
              <pre>{{ store.currentQuery.sqlContent }}</pre>
            </div>
          </div>
        </div>

        <!-- 参数配置面板 -->
        <div class="param-panel">
          <div class="panel-header" @click="store.toggleParamPanel">
            <el-icon><CaretBottom v-if="store.paramPanelExpanded" /><CaretRight v-else /></el-icon>
            <span>参数配置</span>
            <template v-if="!store.paramPanelExpanded">
              <span class="summary">{{ store.paramValuesSummary }}</span>
            </template>
            <div class="panel-actions" v-if="store.paramPanelExpanded">
              <el-radio-group v-model="store.inputMode" size="small" @click.stop>
                <el-radio-button value="form">表单</el-radio-button>
                <el-radio-button value="json">JSON</el-radio-button>
              </el-radio-group>
            </div>
          </div>
          <div class="panel-content" v-show="store.paramPanelExpanded">
            <!-- 表单视图 -->
            <ParamFormView
              v-if="store.inputMode === 'form'"
              :parameters="store.currentQuery.parameters"
              v-model:values="store.paramValues"
            />

            <!-- JSON 视图 -->
            <ParamJsonView
              v-else
              v-model:content="store.jsonEditorContent"
              :error="store.jsonParseError"
              @sync="store.syncJsonToForm"
            />

            <!-- 预设和执行按钮 -->
            <div class="action-bar">
              <div class="preset-section">
                <span>预设:</span>
                <el-select
                  v-model="store.currentPresetId"
                  placeholder="选择预设"
                  size="small"
                  clearable
                  @change="handlePresetChange"
                >
                  <el-option
                    v-for="preset in store.presets"
                    :key="preset.id"
                    :label="preset.name"
                    :value="preset.id"
                  />
                </el-select>
                <el-button size="small" @click="showSavePreset">
                  <el-icon><FolderAdd /></el-icon>
                  保存
                </el-button>
              </div>
              <el-button
                type="primary"
                :loading="store.executing"
                @click="executeQuery"
              >
                <el-icon><VideoPlay /></el-icon>
                执行查询
              </el-button>
            </div>
          </div>
        </div>

        <!-- 非展开状态的执行按钮 -->
        <div class="quick-execute" v-if="!store.paramPanelExpanded">
          <el-button
            type="primary"
            :loading="store.executing"
            @click="executeQuery"
          >
            <el-icon><VideoPlay /></el-icon>
            执行
          </el-button>
        </div>

        <!-- 查询结果区 -->
        <div class="result-panel" :class="{ expanded: store.resultPanelExpanded }" v-if="store.result">
          <div class="panel-header" @click="store.toggleResultPanel">
            <el-icon><CaretBottom v-if="store.resultPanelExpanded" /><CaretRight v-else /></el-icon>
            <span>查询结果 ({{ store.result.totalRows }}条)</span>
            <span class="execution-time">执行耗时: {{ store.result.executionTimeMs }}ms</span>
          </div>
          <div class="result-content" v-show="store.resultPanelExpanded">
            <el-table
              :data="store.result.rows"
              stripe
              border
              size="small"
              height="100%"
            >
              <el-table-column
                v-for="col in store.result.columns"
                :key="col"
                :prop="col"
                :label="col"
                min-width="120"
                show-overflow-tooltip
              />
            </el-table>
          </div>
        </div>
      </template>

      <!-- 未选择配置查询 -->
      <div class="empty-state" v-else>
        <el-empty description="请选择或创建配置查询">
          <el-button type="primary" @click="showEditor(null)">新建配置查询</el-button>
        </el-empty>
      </div>
    </div>

    <!-- 新建/编辑弹窗 -->
    <ConfigQueryEditor
      v-model:visible="editorVisible"
      :config-query-id="editingId"
      @success="handleEditorSuccess"
    />

    <!-- 保存预设弹窗 -->
    <el-dialog v-model="savePresetVisible" title="保存参数预设" width="400px">
      <el-form :model="presetForm" label-width="80px">
        <el-form-item label="预设名称" required>
          <el-input v-model="presetForm.name" placeholder="请输入预设名称" />
        </el-form-item>
        <el-form-item label="设为默认">
          <el-switch v-model="presetForm.isDefault" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="savePresetVisible = false">取消</el-button>
        <el-button type="primary" @click="savePreset">保存</el-button>
      </template>
    </el-dialog>

    <!-- 新建文件夹弹窗 -->
    <el-dialog v-model="newFolderVisible" title="新建文件夹" width="400px">
      <el-form label-width="80px">
        <el-form-item label="名称" required>
          <el-input v-model="newFolderName" placeholder="请输入文件夹名称" @keyup.enter="createNewFolder" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="newFolderVisible = false">取消</el-button>
        <el-button type="primary" @click="createNewFolder">创建</el-button>
      </template>
    </el-dialog>

    <!-- 重命名文件夹弹窗 -->
    <el-dialog v-model="editFolderVisible" title="重命名文件夹" width="400px">
      <el-form label-width="80px">
        <el-form-item label="名称" required>
          <el-input v-model="editFolderName" placeholder="请输入文件夹名称" @keyup.enter="updateFolderName" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="editFolderVisible = false">取消</el-button>
        <el-button type="primary" @click="updateFolderName">保存</el-button>
      </template>
    </el-dialog>

    <!-- 移动到文件夹弹窗 -->
    <el-dialog v-model="moveQueryVisible" title="移动到文件夹" width="400px">
      <el-form label-width="80px">
        <el-form-item label="目标文件夹">
          <el-select v-model="moveTargetFolderId" placeholder="选择文件夹" clearable style="width: 100%">
            <el-option label="未分组" :value="-1" />
            <el-option v-for="folder in store.folders" :key="folder.id" :label="folder.name" :value="folder.id" />
          </el-select>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="moveQueryVisible = false">取消</el-button>
        <el-button type="primary" @click="moveQueryToFolder">移动</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onBeforeUnmount, watch } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import {
  Search,
  Plus,
  ArrowLeft,
  ArrowRight,
  CaretRight,
  CaretBottom,
  Document,
  CopyDocument,
  Edit,
  FolderAdd,
  VideoPlay,
  Folder,
  FolderOpened,
  MoreFilled
} from '@element-plus/icons-vue'
import { useConfigQueryStore } from '@/stores/configQuery'
import { platformApi } from '@/services'
import ParamFormView from './ParamFormView.vue'
import ParamJsonView from './ParamJsonView.vue'
import ConfigQueryEditor from './ConfigQueryEditor.vue'
import type { PlatformDto, DatabaseConnectionDto } from '@/types'
import type { ConfigQueryListItem, ConfigQueryFolder } from '@/types/configQuery'

const store = useConfigQueryStore()

// 平台和数据库连接
const platforms = ref<PlatformDto[]>([])
const connections = ref<DatabaseConnectionDto[]>([])
const selectedPlatformCode = ref<string>('')
const selectedConnectionId = ref<number | undefined>(undefined)

// 列表状态
const listCollapsed = ref(false)
const myListExpanded = ref(true)
const publicListExpanded = ref(true)
const searchKeyword = ref('')

// 编辑器状态
const editorVisible = ref(false)
const editingId = ref<number | null>(null)

// 预设表单
const savePresetVisible = ref(false)
const presetForm = ref({
  name: '',
  isDefault: false
})

// 文件夹相关
const newFolderVisible = ref(false)
const newFolderName = ref('')
const editFolderVisible = ref(false)
const editFolderId = ref<number | null>(null)
const editFolderName = ref('')
const moveQueryVisible = ref(false)
const moveQueryId = ref<number | null>(null)
const moveTargetFolderId = ref<number>(-1)

// 计算属性
const myQueries = computed(() => {
  return store.list.filter(q => q.isOwner)
})

// 我创建的查询按文件夹分组
const myUnfolderedQueries = computed(() => {
  return myQueries.value.filter(q => !q.folderId)
})

// 获取文件夹内的查询
function getQueriesInFolder(folderId: number): ConfigQueryListItem[] {
  return myQueries.value.filter(q => q.folderId === folderId)
}

const publicQueries = computed(() => {
  return store.list.filter(q => !q.isOwner && q.isPublic)
})

// 是否选中正式环境
const isSelectedProduction = computed(() => {
  if (!selectedConnectionId.value) return false
  const conn = connections.value.find(c => c.id === selectedConnectionId.value)
  return conn?.isProduction ?? false
})

// 加载平台列表
async function loadPlatforms(): Promise<void> {
  try {
    const { data } = await platformApi.getPlatforms()
    if (data.success && data.data) {
      platforms.value = data.data
      // 如果有平台，自动选择第一个
      if (data.data.length > 0 && !selectedPlatformCode.value) {
        await handlePlatformChange(data.data[0].code)
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
      // 自动选择第一个连接
      if (data.data.length > 0 && !selectedConnectionId.value) {
        selectedConnectionId.value = data.data[0].id
      }
    }
  } catch {
    connections.value = []
  }
}

// 平台变更
async function handlePlatformChange(platformCode: string): Promise<void> {
  selectedPlatformCode.value = platformCode
  selectedConnectionId.value = undefined
  await loadConnections(platformCode)
}

// 连接变更
function handleConnectionChange(connectionId: number): void {
  selectedConnectionId.value = connectionId
}

// 方法
function toggleList() {
  listCollapsed.value = !listCollapsed.value
}

function toggleMyList() {
  myListExpanded.value = !myListExpanded.value
}

function togglePublicList() {
  publicListExpanded.value = !publicListExpanded.value
}

async function handleSearch() {
  await store.search(searchKeyword.value)
}

async function selectQuery(id: number) {
  await store.selectQuery(id)
  // 如果配置查询有默认连接，自动选择对应的平台和数据库
  await setDefaultConnection()
}

// 根据配置查询的默认连接设置平台和数据库
async function setDefaultConnection() {
  if (!store.currentQuery?.connectionId) return

  const targetConnectionId = store.currentQuery.connectionId

  // 遍历平台查找包含该连接的平台
  for (const platform of platforms.value) {
    const { data } = await platformApi.getConnections(platform.code)
    if (data.success && data.data) {
      const conn = data.data.find(c => c.id === targetConnectionId)
      if (conn) {
        // 找到了，设置平台和连接
        selectedPlatformCode.value = platform.code
        connections.value = data.data
        selectedConnectionId.value = targetConnectionId
        return
      }
    }
  }
}

function showEditor(id: number | null) {
  editingId.value = id
  editorVisible.value = true
}

async function handleEditorSuccess() {
  editorVisible.value = false
  const currentId = store.currentId
  await store.loadList()
  // 强制重新加载当前选中的配置查询详情
  if (currentId) {
    store.currentId = null
    await store.selectQuery(currentId)
  }
}

function copySql() {
  if (store.currentQuery) {
    navigator.clipboard.writeText(store.currentQuery.sqlContent)
    ElMessage.success('SQL 已复制到剪贴板')
  }
}

function handlePresetChange(presetId: number | null) {
  if (presetId) {
    store.applyPreset(presetId)
  }
}

function showSavePreset() {
  presetForm.value = { name: '', isDefault: false }
  savePresetVisible.value = true
}

async function savePreset() {
  if (!presetForm.value.name) {
    ElMessage.warning('请输入预设名称')
    return
  }
  try {
    await store.savePreset(presetForm.value.name, presetForm.value.isDefault)
    ElMessage.success('预设保存成功')
    savePresetVisible.value = false
  } catch (error) {
    ElMessage.error('保存失败')
  }
}

async function executeQuery() {
  await store.execute(selectedConnectionId.value)
  if (store.result && !store.result.success) {
    ElMessage.error(store.result.errorMessage || '执行失败')
  }
}

// 文件夹操作
function showNewFolderDialog() {
  newFolderName.value = ''
  newFolderVisible.value = true
}

async function createNewFolder() {
  if (!newFolderName.value.trim()) {
    ElMessage.warning('请输入文件夹名称')
    return
  }
  try {
    await store.createFolder({ name: newFolderName.value.trim() })
    ElMessage.success('文件夹创建成功')
    newFolderVisible.value = false
  } catch (error) {
    ElMessage.error('创建失败')
  }
}

function showEditFolderDialog(folder: ConfigQueryFolder) {
  editFolderId.value = folder.id
  editFolderName.value = folder.name
  editFolderVisible.value = true
}

async function updateFolderName() {
  if (!editFolderId.value || !editFolderName.value.trim()) {
    ElMessage.warning('请输入文件夹名称')
    return
  }
  try {
    await store.updateFolder(editFolderId.value, { name: editFolderName.value.trim() })
    ElMessage.success('文件夹更新成功')
    editFolderVisible.value = false
  } catch (error) {
    ElMessage.error('更新失败')
  }
}

async function confirmDeleteFolder(folder: ConfigQueryFolder) {
  try {
    await ElMessageBox.confirm(
      `确定删除文件夹"${folder.name}"吗？内部的查询将移到未分组。`,
      '删除文件夹',
      { type: 'warning' }
    )
    await store.deleteFolder(folder.id)
    ElMessage.success('文件夹删除成功')
  } catch {
    // 用户取消
  }
}

function showMoveDialog(queryId: number, currentFolderId: number | undefined) {
  moveQueryId.value = queryId
  moveTargetFolderId.value = currentFolderId ?? -1
  moveQueryVisible.value = true
}

async function moveQueryToFolder() {
  if (moveQueryId.value === null) return
  try {
    // -1 表示未分组，转换为 null
    const targetFolderId = moveTargetFolderId.value === -1 ? null : moveTargetFolderId.value
    await store.moveToFolder(moveQueryId.value, targetFolderId)
    ElMessage.success('移动成功')
    moveQueryVisible.value = false
  } catch (error) {
    ElMessage.error('移动失败')
  }
}

function handleFolderCommand(command: string, folder: ConfigQueryFolder) {
  if (command === 'rename') {
    showEditFolderDialog(folder)
  } else if (command === 'delete') {
    confirmDeleteFolder(folder)
  }
}

function handleQueryCommand(command: string, item: ConfigQueryListItem) {
  if (command === 'move') {
    showMoveDialog(item.id, item.folderId)
  } else if (command === 'remove') {
    // 移出文件夹
    store.moveToFolder(item.id, null)
      .then(() => ElMessage.success('已移出文件夹'))
      .catch(() => ElMessage.error('移动失败'))
  }
}

// 快捷键
function handleKeydown(e: KeyboardEvent) {
  if (e.ctrlKey && e.key === 'Enter') {
    e.preventDefault()
    executeQuery()
  } else if (e.ctrlKey && e.key === '1') {
    e.preventDefault()
    store.toggleSqlPanel()
  } else if (e.ctrlKey && e.key === '2') {
    e.preventDefault()
    store.toggleParamPanel()
  }
}

onMounted(async () => {
  await Promise.all([
    store.loadList(),
    loadPlatforms()
  ])
  window.addEventListener('keydown', handleKeydown)
})

onBeforeUnmount(() => {
  window.removeEventListener('keydown', handleKeydown)
})

watch(
  () => store.currentId,
  () => {
    // 选中新配置查询时，默认展开参数配置面板
    store.paramPanelExpanded = true
    store.sqlPanelExpanded = false
  }
)
</script>

<style scoped lang="scss">
.config-query-tab {
  height: 100%;
  display: flex;
  flex-direction: row;
  overflow: hidden;
}

.list-panel {
  width: 220px;
  min-width: 220px;
  border-right: 1px solid var(--el-border-color-light);
  display: flex;
  flex-direction: column;
  background-color: var(--el-bg-color);

  &.collapsed {
    width: 48px;
    min-width: 48px;
  }

  .list-header {
    display: flex;
    align-items: center;
    padding: 8px 12px;
    border-bottom: 1px solid var(--el-border-color-light);

    .collapse-btn {
      padding: 4px;
    }

    .title {
      flex: 1;
      font-weight: 500;
      margin-left: 8px;
    }
  }

  .search-box {
    padding: 8px 12px;
  }

  .list-content {
    flex: 1;
    overflow-y: auto;
    padding: 8px 0;
  }

  .list-group {
    .group-header {
      display: flex;
      align-items: center;
      padding: 6px 12px;
      cursor: pointer;
      font-size: 13px;
      color: var(--el-text-color-secondary);

      &:hover {
        background-color: var(--el-fill-color-light);
      }

      .count {
        margin-left: auto;
        font-size: 12px;
      }
    }

    .group-items {
      .list-item {
        padding: 8px 12px 8px 28px;
        cursor: pointer;
        font-size: 13px;
        white-space: nowrap;
        overflow: hidden;
        text-overflow: ellipsis;
        display: flex;
        align-items: center;
        justify-content: space-between;

        &:hover {
          background-color: var(--el-fill-color-light);

          .item-more {
            opacity: 1;
          }
        }

        &.active {
          background-color: var(--el-color-primary-light-9);
          color: var(--el-color-primary);
        }

        .item-name {
          flex: 1;
          overflow: hidden;
          text-overflow: ellipsis;
        }

        .item-more {
          opacity: 0;
          cursor: pointer;
          padding: 2px;
          margin-left: 4px;
          flex-shrink: 0;

          &:hover {
            color: var(--el-color-primary);
          }
        }
      }

      .folder-group {
        .folder-header {
          display: flex;
          align-items: center;
          padding: 6px 12px 6px 20px;
          cursor: pointer;
          font-size: 13px;
          color: var(--el-text-color-regular);

          &:hover {
            background-color: var(--el-fill-color-light);

            .folder-more {
              opacity: 1;
            }
          }

          .folder-name {
            flex: 1;
            margin-left: 6px;
            overflow: hidden;
            text-overflow: ellipsis;
          }

          .count {
            font-size: 12px;
            color: var(--el-text-color-placeholder);
            margin-left: 4px;
          }

          .folder-more {
            opacity: 0;
            cursor: pointer;
            padding: 2px;
            margin-left: 4px;
            flex-shrink: 0;

            &:hover {
              color: var(--el-color-primary);
            }
          }
        }

        .folder-items {
          .list-item {
            padding-left: 44px;
          }
        }
      }
    }
  }

  .list-footer {
    padding: 8px 12px;
    border-top: 1px solid var(--el-border-color-light);
  }

  .collapsed-icons {
    display: flex;
    flex-direction: column;
    align-items: center;
    padding: 8px 0;
    gap: 8px;

    .icon-btn {
      width: 32px;
      height: 32px;
      display: flex;
      align-items: center;
      justify-content: center;
      cursor: pointer;
      border-radius: 4px;
      color: var(--el-text-color-secondary);

      &:hover {
        background-color: var(--el-fill-color-light);
        color: var(--el-color-primary);
      }
    }
  }
}

.content-panel {
  flex: 1;
  display: flex;
  flex-direction: column;
  overflow: hidden;
  padding: 12px;
  gap: 12px;
  min-width: 0;

  .database-selectors {
    display: flex;
    align-items: center;
    gap: 16px;
    padding: 8px 12px;
    background-color: var(--el-fill-color-light);
    border-radius: 4px;
    flex-shrink: 0;
    flex-wrap: wrap;

    .selector-item {
      display: flex;
      align-items: center;
      gap: 8px;

      .label {
        font-size: 13px;
        color: var(--el-text-color-secondary);
        white-space: nowrap;
      }

      .el-select {
        min-width: 300px;
      }
    }

    .database-selector {
      .el-select {
        min-width: 350px;
      }

      .connection-option {
        display: flex;
        align-items: center;
        justify-content: space-between;

        .conn-name {
          flex: 1;
        }

        .env-tag {
          margin-left: 8px;
        }
      }

      .selected-env-tag {
        margin-left: 8px;
      }
    }

    .default-connection-hint {
      margin-left: auto;
    }

    .production-select {
      :deep(.el-input__wrapper) {
        border-color: var(--el-color-danger);
      }
    }
  }

  .sql-panel,
  .param-panel {
    border: 1px solid var(--el-border-color-light);
    border-radius: 4px;
    background-color: var(--el-bg-color);
    flex-shrink: 0;

    .panel-header {
      display: flex;
      align-items: center;
      padding: 8px 12px;
      cursor: pointer;
      border-bottom: 1px solid var(--el-border-color-light);
      user-select: none;

      &:hover {
        background-color: var(--el-fill-color-light);
      }

      .summary {
        margin-left: 8px;
        color: var(--el-text-color-secondary);
        font-size: 12px;
        flex: 1;
        white-space: nowrap;
        overflow: hidden;
        text-overflow: ellipsis;
      }

      .panel-actions {
        margin-left: auto;
        display: flex;
        align-items: center;
        gap: 8px;
      }
    }

    .panel-content {
      padding: 12px;
    }
  }

  .sql-panel {
    &.expanded {
      flex: 1;
      overflow: hidden;
      display: flex;
      flex-direction: column;

      .panel-content {
        flex: 1;
        overflow: auto;
      }
    }
  }

  .param-panel {
    max-height: 50%;
    overflow-y: auto;
  }

  .sql-preview {
    background-color: var(--el-fill-color-light);
    border-radius: 4px;
    padding: 12px;
    font-family: monospace;
    font-size: 13px;
    white-space: pre-wrap;
    word-break: break-all;
    overflow: auto;
    max-height: 100%;

    pre {
      margin: 0;
    }
  }

  .action-bar {
    display: flex;
    align-items: center;
    justify-content: space-between;
    margin-top: 12px;
    padding-top: 12px;
    border-top: 1px solid var(--el-border-color-light);
    flex-wrap: wrap;
    gap: 8px;

    .preset-section {
      display: flex;
      align-items: center;
      gap: 8px;
      flex-shrink: 0;

      > span {
        white-space: nowrap;
      }

      .el-select {
        min-width: 180px;
      }
    }
  }

  .quick-execute {
    display: flex;
    justify-content: flex-end;
  }

  .empty-state {
    flex: 1;
    display: flex;
    align-items: center;
    justify-content: center;
  }

  .result-panel {
    border: 1px solid var(--el-border-color-light);
    border-radius: 4px;
    background-color: var(--el-bg-color);
    flex-shrink: 0;

    .panel-header {
      display: flex;
      align-items: center;
      padding: 8px 12px;
      cursor: pointer;
      border-bottom: 1px solid var(--el-border-color-light);
      user-select: none;
      font-size: 13px;

      &:hover {
        background-color: var(--el-fill-color-light);
      }

      .execution-time {
        margin-left: auto;
        color: var(--el-text-color-secondary);
        font-size: 12px;
      }
    }

    .result-content {
      height: 0;
      overflow: hidden;
    }

    &.expanded {
      flex: 1;
      min-height: 0;
      display: flex;
      flex-direction: column;
      overflow: hidden;

      .result-content {
        flex: 1;
        height: auto;
        min-height: 0;
        overflow: hidden;
      }
    }
  }
}
</style>
