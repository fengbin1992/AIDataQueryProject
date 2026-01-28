<template>
  <div class="query-tabs">
    <div class="tabs-container">
      <div class="tabs-list">
        <div
          v-for="(tab, index) in tabsStore.tabs"
          :key="tab.id"
          class="tab-item"
          :class="{
            active: tab.id === tabsStore.activeTabId,
            dirty: tab.isDirty,
            saved: tab.isSaved,
            dragging: dragIndex === index
          }"
          draggable="true"
          @click="handleTabClick(tab.id)"
          @dblclick="startRename(tab)"
          @contextmenu.prevent="showContextMenu($event, tab)"
          @dragstart="onDragStart($event, index)"
          @dragover.prevent="onDragOver($event, index)"
          @dragend="onDragEnd"
          @drop="onDrop($event, index)"
        >
          <el-icon v-if="tab.isSaved" class="saved-icon" :size="12">
            <Folder />
          </el-icon>
          <span v-if="renamingTabId !== tab.id" class="tab-name">
            {{ tab.name }}
            <span v-if="tab.isDirty" class="dirty-indicator">•</span>
          </span>
          <input
            v-else
            ref="renameInputRef"
            v-model="renameValue"
            class="rename-input"
            @blur="finishRename"
            @keydown.enter="finishRename"
            @keydown.escape="cancelRename"
            @click.stop
          />
          <el-icon
            v-if="tabsStore.canCloseTab"
            class="close-btn"
            @click.stop="handleCloseTab(tab)"
          >
            <Close />
          </el-icon>
        </div>
      </div>

      <el-tooltip content="新建标签 (Ctrl+T)" placement="bottom">
        <div
          class="add-tab-btn"
          :class="{ disabled: !tabsStore.canCreateTab }"
          @click="handleAddTab"
        >
          <el-icon><Plus /></el-icon>
        </div>
      </el-tooltip>
    </div>

    <!-- 右键菜单 -->
    <div
      v-if="contextMenuVisible"
      class="context-menu"
      :style="{ left: contextMenuPosition.x + 'px', top: contextMenuPosition.y + 'px' }"
    >
      <div
        v-if="!contextMenuTab?.isSaved"
        class="menu-item save-item"
        @click="handleContextSave"
      >
        <el-icon><FolderAdd /></el-icon>
        <span>保存标签</span>
      </div>
      <div class="menu-item" @click="handleContextRename">
        <el-icon><Edit /></el-icon>
        <span>重命名</span>
      </div>
      <div class="menu-divider"></div>
      <div
        class="menu-item"
        :class="{ disabled: !tabsStore.canCloseTab }"
        @click="handleContextClose"
      >
        <el-icon><Close /></el-icon>
        <span>关闭</span>
      </div>
      <div
        class="menu-item"
        :class="{ disabled: tabsStore.tabs.length <= 1 }"
        @click="handleContextCloseOthers"
      >
        <el-icon><FolderRemove /></el-icon>
        <span>关闭其他</span>
      </div>
      <div
        class="menu-item"
        :class="{ disabled: isLastTab }"
        @click="handleContextCloseRight"
      >
        <el-icon><Right /></el-icon>
        <span>关闭右侧</span>
      </div>
    </div>

    <!-- 关闭确认对话框 -->
    <el-dialog
      v-model="closeConfirmVisible"
      title="关闭确认"
      width="400px"
      :close-on-click-modal="false"
    >
      <p>标签 "{{ tabToClose?.name }}" 有未保存的内容，确定要关闭吗？</p>
      <template #footer>
        <el-button @click="closeConfirmVisible = false">取消</el-button>
        <el-button type="danger" @click="confirmClose">确定关闭</el-button>
      </template>
    </el-dialog>

    <!-- 删除已保存标签确认对话框 -->
    <el-dialog
      v-model="deleteConfirmVisible"
      title="删除确认"
      width="400px"
      :close-on-click-modal="false"
    >
      <p>标签 "{{ tabToDelete?.name }}" 已保存到服务器，确定要永久删除吗？</p>
      <template #footer>
        <el-button @click="deleteConfirmVisible = false">取消</el-button>
        <el-button type="danger" :loading="isDeleting" @click="confirmDelete">确定删除</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, nextTick, onMounted, onUnmounted } from 'vue'
import { ElMessage } from 'element-plus'
import { Close, Plus, Edit, FolderRemove, Right, Folder, FolderAdd } from '@element-plus/icons-vue'
import { useQueryTabsStore } from '@/stores'
import type { QueryTab } from '@/types'

const emit = defineEmits<{
  (e: 'tabChange', tabId: string): void
}>()

const tabsStore = useQueryTabsStore()

// 重命名状态
const renamingTabId = ref<string | null>(null)
const renameValue = ref('')
const renameInputRef = ref<HTMLInputElement[]>()

// 右键菜单状态
const contextMenuVisible = ref(false)
const contextMenuPosition = ref({ x: 0, y: 0 })
const contextMenuTab = ref<QueryTab | null>(null)

// 关闭确认
const closeConfirmVisible = ref(false)
const tabToClose = ref<QueryTab | null>(null)

// 删除确认
const deleteConfirmVisible = ref(false)
const tabToDelete = ref<QueryTab | null>(null)
const isDeleting = ref(false)

// 拖拽状态
const dragIndex = ref<number | null>(null)
const dropIndex = ref<number | null>(null)

// 计算当前右键菜单的标签是否是最后一个
const isLastTab = computed(() => {
  if (!contextMenuTab.value) return true
  const index = tabsStore.tabs.findIndex(t => t.id === contextMenuTab.value!.id)
  return index === tabsStore.tabs.length - 1
})

// 点击标签
function handleTabClick(tabId: string) {
  tabsStore.switchTab(tabId)
  emit('tabChange', tabId)
}

// 添加新标签
function handleAddTab() {
  if (!tabsStore.canCreateTab) {
    ElMessage.warning('最多只能打开 10 个标签页')
    return
  }
  const newTab = tabsStore.createTab()
  emit('tabChange', newTab.id)
}

// 关闭标签
function handleCloseTab(tab: QueryTab) {
  if (tab.isSaved && tab.serverId) {
    // 已保存的标签需要确认删除
    tabToDelete.value = tab
    deleteConfirmVisible.value = true
  } else if (tab.isDirty && tab.sql.trim()) {
    // 临时标签有未保存内容需要确认
    tabToClose.value = tab
    closeConfirmVisible.value = true
  } else {
    tabsStore.closeTab(tab.id)
  }
}

// 确认关闭（临时标签）
function confirmClose() {
  if (tabToClose.value) {
    tabsStore.closeTab(tabToClose.value.id)
  }
  closeConfirmVisible.value = false
  tabToClose.value = null
}

// 确认删除（已保存标签）
async function confirmDelete() {
  if (tabToDelete.value) {
    isDeleting.value = true
    await tabsStore.deleteTabFromServer(tabToDelete.value.id)
    isDeleting.value = false
  }
  deleteConfirmVisible.value = false
  tabToDelete.value = null
}

// 开始重命名
function startRename(tab: QueryTab) {
  renamingTabId.value = tab.id
  renameValue.value = tab.name
  nextTick(() => {
    if (renameInputRef.value?.[0]) {
      renameInputRef.value[0].focus()
      renameInputRef.value[0].select()
    }
  })
}

// 完成重命名
function finishRename() {
  if (renamingTabId.value && renameValue.value.trim()) {
    tabsStore.renameTab(renamingTabId.value, renameValue.value)
  }
  renamingTabId.value = null
}

// 取消重命名
function cancelRename() {
  renamingTabId.value = null
}

// 拖拽开始
function onDragStart(event: DragEvent, index: number) {
  dragIndex.value = index
  if (event.dataTransfer) {
    event.dataTransfer.effectAllowed = 'move'
    event.dataTransfer.setData('text/plain', index.toString())
  }
}

// 拖拽经过
function onDragOver(event: DragEvent, index: number) {
  event.preventDefault()
  dropIndex.value = index
}

// 拖拽放下
function onDrop(event: DragEvent, toIndex: number) {
  event.preventDefault()
  const fromIndex = dragIndex.value
  if (fromIndex !== null && fromIndex !== toIndex) {
    tabsStore.reorderTabs(fromIndex, toIndex)
  }
}

// 拖拽结束
function onDragEnd() {
  dragIndex.value = null
  dropIndex.value = null
  tabsStore.saveToStorage()
}

// 显示右键菜单
function showContextMenu(event: MouseEvent, tab: QueryTab) {
  contextMenuTab.value = tab
  contextMenuPosition.value = {
    x: event.clientX,
    y: event.clientY
  }
  contextMenuVisible.value = true
}

// 隐藏右键菜单
function hideContextMenu() {
  contextMenuVisible.value = false
  contextMenuTab.value = null
}

// 右键菜单 - 保存标签
async function handleContextSave() {
  if (contextMenuTab.value && !contextMenuTab.value.isSaved) {
    await tabsStore.saveTabToServer(contextMenuTab.value.id)
  }
  hideContextMenu()
}

// 右键菜单操作
function handleContextRename() {
  if (contextMenuTab.value) {
    startRename(contextMenuTab.value)
  }
  hideContextMenu()
}

function handleContextClose() {
  if (contextMenuTab.value && tabsStore.canCloseTab) {
    handleCloseTab(contextMenuTab.value)
  }
  hideContextMenu()
}

function handleContextCloseOthers() {
  if (contextMenuTab.value && tabsStore.tabs.length > 1) {
    tabsStore.closeOtherTabs(contextMenuTab.value.id)
  }
  hideContextMenu()
}

function handleContextCloseRight() {
  if (contextMenuTab.value && !isLastTab.value) {
    tabsStore.closeRightTabs(contextMenuTab.value.id)
  }
  hideContextMenu()
}

// 键盘快捷键
function handleKeydown(event: KeyboardEvent) {
  // Ctrl+T: 新建标签
  if (event.ctrlKey && event.key === 't') {
    event.preventDefault()
    handleAddTab()
    return
  }

  // Ctrl+W: 关闭当前标签
  if (event.ctrlKey && event.key === 'w') {
    event.preventDefault()
    if (tabsStore.activeTab && tabsStore.canCloseTab) {
      handleCloseTab(tabsStore.activeTab)
    }
    return
  }

  // Ctrl+S: 保存当前标签
  if (event.ctrlKey && event.key === 's') {
    event.preventDefault()
    if (tabsStore.activeTab) {
      tabsStore.saveTabToServer(tabsStore.activeTab.id)
    }
    return
  }

  // Ctrl+Tab: 下一个标签
  if (event.ctrlKey && event.key === 'Tab' && !event.shiftKey) {
    event.preventDefault()
    tabsStore.switchToNextTab()
    emit('tabChange', tabsStore.activeTabId)
    return
  }

  // Ctrl+Shift+Tab: 上一个标签
  if (event.ctrlKey && event.shiftKey && event.key === 'Tab') {
    event.preventDefault()
    tabsStore.switchToPrevTab()
    emit('tabChange', tabsStore.activeTabId)
    return
  }

  // Ctrl+1~9: 切换到第N个标签
  if (event.ctrlKey && event.key >= '1' && event.key <= '9') {
    event.preventDefault()
    const index = parseInt(event.key) - 1
    tabsStore.switchToTabByIndex(index)
    emit('tabChange', tabsStore.activeTabId)
    return
  }
}

// 点击外部关闭右键菜单
function handleClickOutside(event: MouseEvent) {
  const target = event.target as HTMLElement
  if (!target.closest('.context-menu')) {
    hideContextMenu()
  }
}

onMounted(() => {
  document.addEventListener('keydown', handleKeydown)
  document.addEventListener('click', handleClickOutside)
})

onUnmounted(() => {
  document.removeEventListener('keydown', handleKeydown)
  document.removeEventListener('click', handleClickOutside)
})
</script>

<style scoped lang="scss">
.query-tabs {
  position: relative;
}

.tabs-container {
  display: flex;
  align-items: center;
  background-color: var(--el-bg-color);
  border-bottom: 1px solid var(--el-border-color-light);
  padding: 0 8px;
  height: 36px;
}

.tabs-list {
  display: flex;
  flex: 1;
  overflow-x: auto;
  gap: 2px;

  &::-webkit-scrollbar {
    height: 3px;
  }

  &::-webkit-scrollbar-thumb {
    background-color: var(--el-border-color);
    border-radius: 3px;
  }
}

.tab-item {
  display: flex;
  align-items: center;
  gap: 4px;
  padding: 4px 10px;
  background-color: var(--el-fill-color-light);
  border-radius: 4px 4px 0 0;
  cursor: pointer;
  user-select: none;
  min-width: 80px;
  max-width: 180px;
  transition: all 0.2s;
  position: relative;

  &:hover {
    background-color: var(--el-fill-color);

    .close-btn {
      opacity: 1;
    }
  }

  &.active {
    background-color: var(--el-color-primary-light-9);
    box-shadow: 0 -2px 0 0 var(--el-color-primary) inset;

    .tab-name {
      color: var(--el-color-primary);
      font-weight: 600;
    }

    .close-btn {
      opacity: 1;
    }
  }

  &.saved {
    .saved-icon {
      color: var(--el-color-success);
    }
  }

  &.dirty .tab-name {
    font-style: italic;
  }

  &.dragging {
    opacity: 0.5;
    background-color: var(--el-color-primary-light-9);
  }

  .saved-icon {
    flex-shrink: 0;
    color: var(--el-text-color-secondary);
  }

  .tab-name {
    flex: 1;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
    font-size: 13px;
    color: var(--el-text-color-regular);
  }

  .dirty-indicator {
    color: var(--el-color-warning);
    margin-left: 2px;
  }

  .close-btn {
    opacity: 0;
    font-size: 12px;
    color: var(--el-text-color-secondary);
    transition: all 0.2s;
    padding: 2px;
    border-radius: 4px;
    flex-shrink: 0;

    &:hover {
      background-color: var(--el-fill-color-darker);
      color: var(--el-color-danger);
    }
  }

  .rename-input {
    flex: 1;
    min-width: 60px;
    border: 1px solid var(--el-color-primary);
    border-radius: 2px;
    padding: 2px 4px;
    font-size: 13px;
    outline: none;
    background-color: var(--el-bg-color);
    color: var(--el-text-color-primary);
  }
}

.add-tab-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 26px;
  height: 26px;
  margin-left: 4px;
  border-radius: 4px;
  cursor: pointer;
  color: var(--el-text-color-secondary);
  transition: all 0.2s;

  &:hover:not(.disabled) {
    background-color: var(--el-fill-color);
    color: var(--el-color-primary);
  }

  &.disabled {
    opacity: 0.5;
    cursor: not-allowed;
  }
}

// 右键菜单
.context-menu {
  position: fixed;
  z-index: 3000;
  min-width: 140px;
  background-color: var(--el-bg-color-overlay);
  border: 1px solid var(--el-border-color-light);
  border-radius: 4px;
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.1);
  padding: 4px 0;

  .menu-item {
    display: flex;
    align-items: center;
    gap: 8px;
    padding: 8px 16px;
    cursor: pointer;
    font-size: 13px;
    color: var(--el-text-color-regular);
    transition: background-color 0.2s;

    &:hover:not(.disabled) {
      background-color: var(--el-fill-color-light);
    }

    &.disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

    &.save-item {
      color: var(--el-color-primary);
    }

    .el-icon {
      font-size: 14px;
    }
  }

  .menu-divider {
    height: 1px;
    background-color: var(--el-border-color-lighter);
    margin: 4px 0;
  }
}
</style>
