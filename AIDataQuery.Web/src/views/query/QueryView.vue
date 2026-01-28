<template>
  <div class="query-view">
    <!-- 标签栏 -->
    <QueryTabs @tab-change="handleTabChange" />

    <!-- 工作区容器 - 使用 KeepAlive 缓存已访问的标签 -->
    <div class="workspace-container">
      <KeepAlive :max="10">
        <QueryWorkspace
          v-if="tabsStore.activeTabId"
          :key="tabsStore.activeTabId"
          :tab-id="tabsStore.activeTabId"
          ref="workspaceRef"
        />
      </KeepAlive>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onBeforeUnmount } from 'vue'
import { useQueryTabsStore } from '@/stores'
import QueryTabs from '@/components/query/QueryTabs.vue'
import QueryWorkspace from '@/components/query/QueryWorkspace.vue'

const tabsStore = useQueryTabsStore()
const workspaceRef = ref<InstanceType<typeof QueryWorkspace>>()

// 标签切换 - 聚焦到编辑器
function handleTabChange(_tabId: string) {
  // 延迟聚焦，等待组件渲染完成
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

// 初始化
onMounted(async () => {
  // 从服务端加载已保存的标签 + 本地临时标签
  await tabsStore.loadFromServer()

  // 注册离开页面确认
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
  flex-direction: column;
  overflow: hidden;
}

.workspace-container {
  flex: 1;
  padding: 12px;
  overflow: hidden;
}
</style>
