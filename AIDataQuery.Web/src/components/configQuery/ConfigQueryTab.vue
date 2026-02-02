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
              <div
                v-for="item in myQueries"
                :key="item.id"
                class="list-item"
                :class="{ active: store.currentId === item.id }"
                @click="selectQuery(item.id)"
              >
                <span class="item-name">{{ item.name }}</span>
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
        <!-- SQL 预览面板 -->
        <div class="sql-panel" :class="{ expanded: store.sqlPanelExpanded }">
          <div class="panel-header" @click="store.toggleSqlPanel">
            <el-icon><CaretBottom v-if="store.sqlPanelExpanded" /><CaretRight v-else /></el-icon>
            <span>SQL 预览</span>
            <template v-if="!store.sqlPanelExpanded">
              <span class="summary">{{ store.sqlPreviewSummary }}</span>
            </template>
            <div class="panel-actions" v-if="store.sqlPanelExpanded">
              <el-button text size="small" @click.stop="copySql">
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
        <div class="param-panel" :class="{ expanded: store.paramPanelExpanded }">
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

        <!-- 查询结果区（移到 content-panel 内部） -->
        <div class="result-panel" v-if="store.result">
          <div class="result-header">
            <span>查询结果 ({{ store.result.totalRows }}条)</span>
            <span class="execution-time">执行耗时: {{ store.result.executionTimeMs }}ms</span>
          </div>
          <div class="result-content">
            <el-table
              :data="store.result.rows"
              stripe
              border
              size="small"
              max-height="300"
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
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onBeforeUnmount, watch } from 'vue'
import { ElMessage } from 'element-plus'
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
  VideoPlay
} from '@element-plus/icons-vue'
import { useConfigQueryStore } from '@/stores/configQuery'
import ParamFormView from './ParamFormView.vue'
import ParamJsonView from './ParamJsonView.vue'
import ConfigQueryEditor from './ConfigQueryEditor.vue'

const store = useConfigQueryStore()

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

// 计算属性
const myQueries = computed(() => {
  return store.list.filter(q => q.isOwner)
})

const publicQueries = computed(() => {
  return store.list.filter(q => !q.isOwner && q.isPublic)
})

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
}

function showEditor(id: number | null) {
  editingId.value = id
  editorVisible.value = true
}

async function handleEditorSuccess() {
  editorVisible.value = false
  await store.loadList()
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
  await store.execute()
  if (store.result && !store.result.success) {
    ElMessage.error(store.result.errorMessage || '执行失败')
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
  await store.loadList()
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

        &:hover {
          background-color: var(--el-fill-color-light);
        }

        &.active {
          background-color: var(--el-color-primary-light-9);
          color: var(--el-color-primary);
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

  .sql-panel,
  .param-panel {
    border: 1px solid var(--el-border-color-light);
    border-radius: 4px;
    background-color: var(--el-bg-color);

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

    .result-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 8px 12px;
      border-bottom: 1px solid var(--el-border-color-light);
      font-size: 13px;

      .execution-time {
        color: var(--el-text-color-secondary);
        font-size: 12px;
      }
    }

    .result-content {
      padding: 12px;
    }
  }
}
</style>
