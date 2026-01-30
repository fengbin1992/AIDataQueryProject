<template>
  <div class="query-view">
    <!-- 左侧边栏（显示隐藏的面板标签） -->
    <div class="sidebar" v-show="hasHiddenPanels">
      <el-tooltip content="显示标签栏" placement="right" v-if="isTabsHidden">
        <div class="sidebar-item" @click="showTabs">
          <el-icon :size="18"><Document /></el-icon>
          <span class="sidebar-label">标签</span>
        </div>
      </el-tooltip>
      <el-tooltip content="显示选择器" placement="right" v-if="isSelectorsHidden">
        <div class="sidebar-item" @click="showSelectors">
          <el-icon :size="18"><Connection /></el-icon>
          <span class="sidebar-label">选择器</span>
        </div>
      </el-tooltip>
      <el-tooltip content="显示结果" placement="right" v-if="isResultHidden">
        <div class="sidebar-item" @click="showResult">
          <el-icon :size="18"><List /></el-icon>
          <span class="sidebar-label">结果</span>
        </div>
      </el-tooltip>
    </div>

    <!-- 主内容区 -->
    <div class="main-content">
      <!-- 标签栏 -->
      <div class="tabs-wrapper" v-show="!isTabsHidden">
        <QueryTabs @tab-change="handleTabChange" />
        <el-tooltip content="隐藏标签栏" placement="bottom">
          <div class="panel-hide-btn" @click="hideTabs">
            <el-icon :size="14"><Close /></el-icon>
          </div>
        </el-tooltip>
      </div>

      <!-- 工作区容器 -->
      <div class="workspace-container">
        <KeepAlive :max="10">
          <QueryWorkspace
            v-if="tabsStore.activeTabId"
            :key="tabsStore.activeTabId"
            :tab-id="tabsStore.activeTabId"
            ref="workspaceRef"
            :selectors-hidden="isSelectorsHidden"
            :result-hidden="isResultHidden"
            @hide-selectors="hideSelectors"
            @show-selectors="showSelectors"
            @hide-result="hideResult"
            @show-result="showResult"
          />
        </KeepAlive>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onBeforeUnmount } from 'vue'
import { Document, Connection, List, Close } from '@element-plus/icons-vue'
import { useQueryTabsStore } from '@/stores'
import QueryTabs from '@/components/query/QueryTabs.vue'
import QueryWorkspace from '@/components/query/QueryWorkspace.vue'

const tabsStore = useQueryTabsStore()
const workspaceRef = ref<InstanceType<typeof QueryWorkspace>>()

// 面板隐藏状态
const isTabsHidden = ref(false)
const isSelectorsHidden = ref(false)
const isResultHidden = ref(false)

// 是否有隐藏的面板
const hasHiddenPanels = computed(() => {
  return isTabsHidden.value || isSelectorsHidden.value || isResultHidden.value
})

// 隐藏/显示标签栏
function hideTabs() {
  isTabsHidden.value = true
}
function showTabs() {
  isTabsHidden.value = false
}

// 隐藏/显示选择器
function hideSelectors() {
  isSelectorsHidden.value = true
}
function showSelectors() {
  isSelectorsHidden.value = false
}

// 隐藏/显示结果区
function hideResult() {
  isResultHidden.value = true
}
function showResult() {
  isResultHidden.value = false
}

// 标签切换 - 聚焦到编辑器
function handleTabChange(_tabId: string) {
  setTimeout(() => {
    workspaceRef.value?.focus()
  }, 100)
}

// 页面离开前确认
function handleBeforeUnload(event: BeforeUnloadEvent) {
  if (tabsStore.hasUnsavedTabs()) {
    event.preventDefault()
    event.returnValue = '有未保存的查询内容，确定要离开吗？'
    return event.returnValue
  }
}

onMounted(async () => {
  await tabsStore.loadFromServer()
  window.addEventListener('beforeunload', handleBeforeUnload)
})

onBeforeUnmount(() => {
  window.removeEventListener('beforeunload', handleBeforeUnload)
})
</script>

<style scoped lang="scss">
.query-view {
  height: 100%;
  display: flex;
  overflow: hidden;
}

.sidebar {
  width: 42px;
  background-color: var(--el-bg-color-page);
  border-right: 1px solid var(--el-border-color-light);
  display: flex;
  flex-direction: column;
  padding: 8px 0;
  gap: 4px;

  .sidebar-item {
    display: flex;
    flex-direction: column;
    align-items: center;
    padding: 8px 4px;
    cursor: pointer;
    color: var(--el-text-color-secondary);
    transition: all 0.2s;
    border-left: 2px solid transparent;

    &:hover {
      color: var(--el-color-primary);
      background-color: var(--el-fill-color-light);
      border-left-color: var(--el-color-primary);
    }

    .sidebar-label {
      font-size: 10px;
      margin-top: 2px;
      writing-mode: horizontal-tb;
    }
  }
}

.main-content {
  flex: 1;
  display: flex;
  flex-direction: column;
  overflow: hidden;
  min-width: 0;
}

.tabs-wrapper {
  display: flex;
  align-items: center;
  background-color: var(--el-bg-color);
  border-bottom: 1px solid var(--el-border-color-light);

  :deep(.query-tabs) {
    flex: 1;
  }

  .panel-hide-btn {
    display: flex;
    align-items: center;
    justify-content: center;
    width: 28px;
    height: 28px;
    margin-right: 8px;
    cursor: pointer;
    color: var(--el-text-color-secondary);
    border-radius: 4px;
    transition: all 0.2s;

    &:hover {
      color: var(--el-color-primary);
      background-color: var(--el-fill-color-light);
    }
  }
}

.workspace-container {
  flex: 1;
  padding: 12px;
  overflow: hidden;
}
</style>
