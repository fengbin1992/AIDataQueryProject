import { defineStore } from 'pinia'
import { ref, computed, watch } from 'vue'
import { ElMessage } from 'element-plus'
import type { QueryTab, StoredTabsData, CreateTabOptions, QueryTabDto } from '@/types'
import type { QueryResult } from '@/types'
import { queryTabApi } from '@/services'

const STORAGE_KEY = 'ai-data-query-tabs'
const STORAGE_VERSION = 2
const MAX_TABS = 10
const DEFAULT_TAB_NAME = '查询'

/** 生成 UUID */
function generateId(): string {
  return crypto.randomUUID ? crypto.randomUUID() :
    'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, c => {
      const r = Math.random() * 16 | 0
      const v = c === 'x' ? r : (r & 0x3 | 0x8)
      return v.toString(16)
    })
}

/** 将服务端 DTO 转换为本地 QueryTab */
function dtoToQueryTab(dto: QueryTabDto): QueryTab {
  return {
    id: `server-${dto.id}`,
    serverId: dto.id,
    name: dto.name,
    platformCode: dto.platformCode,
    connectionId: dto.connectionId,
    sql: dto.sqlContent || '',
    queryResult: null,
    isQuerying: false,
    isDirty: false,
    isSaved: true,
    createdAt: new Date(dto.createdAt).getTime()
  }
}

export const useQueryTabsStore = defineStore('queryTabs', () => {
  // 状态
  const tabs = ref<QueryTab[]>([])
  const activeTabId = ref<string>('')
  const isLoading = ref(false)

  // 计算属性
  const activeTab = computed(() =>
    tabs.value.find(t => t.id === activeTabId.value)
  )

  const canCloseTab = computed(() => tabs.value.length > 1)

  const canCreateTab = computed(() => tabs.value.length < MAX_TABS)

  const activeTabIndex = computed(() =>
    tabs.value.findIndex(t => t.id === activeTabId.value)
  )

  /** 获取已保存的标签 */
  const savedTabs = computed(() => tabs.value.filter(t => t.isSaved))

  /** 获取临时标签 */
  const unsavedTabs = computed(() => tabs.value.filter(t => !t.isSaved))

  // 获取下一个标签名称
  function getNextTabName(): string {
    const existingNumbers = tabs.value
      .map(t => {
        const match = t.name.match(new RegExp(`^${DEFAULT_TAB_NAME}\\s*(\\d+)$`))
        return match ? parseInt(match[1], 10) : 0
      })
      .filter(n => n > 0)

    let nextNumber = 1
    while (existingNumbers.includes(nextNumber)) {
      nextNumber++
    }
    return `${DEFAULT_TAB_NAME} ${nextNumber}`
  }

  /**
   * 创建新标签（临时标签）
   */
  function createTab(options?: CreateTabOptions): QueryTab {
    if (!canCreateTab.value) {
      console.warn(`最多只能打开 ${MAX_TABS} 个标签`)
      return tabs.value[tabs.value.length - 1]
    }

    const newTab: QueryTab = {
      id: generateId(),
      name: options?.name || getNextTabName(),
      platformCode: options?.platformCode ?? null,
      connectionId: options?.connectionId ?? null,
      sql: options?.sql ?? '',
      queryResult: null,
      isQuerying: false,
      isDirty: false,
      isSaved: false,
      createdAt: Date.now()
    }

    tabs.value.push(newTab)
    activeTabId.value = newTab.id
    saveToStorage()
    return newTab
  }

  /**
   * 关闭标签
   */
  function closeTab(tabId: string): boolean {
    if (!canCloseTab.value) {
      return false
    }

    const index = tabs.value.findIndex(t => t.id === tabId)
    if (index === -1) return false

    // 如果关闭的是当前激活的标签，需要切换到相邻标签
    if (tabId === activeTabId.value) {
      // 优先切换到右侧，否则切换到左侧
      const newActiveIndex = index < tabs.value.length - 1 ? index + 1 : index - 1
      activeTabId.value = tabs.value[newActiveIndex].id
    }

    tabs.value.splice(index, 1)
    saveToStorage()
    return true
  }

  /**
   * 关闭其他标签
   */
  function closeOtherTabs(tabId: string): void {
    const targetTab = tabs.value.find(t => t.id === tabId)
    if (!targetTab) return

    tabs.value = [targetTab]
    activeTabId.value = tabId
    saveToStorage()
  }

  /**
   * 关闭右侧标签
   */
  function closeRightTabs(tabId: string): void {
    const index = tabs.value.findIndex(t => t.id === tabId)
    if (index === -1) return

    tabs.value = tabs.value.slice(0, index + 1)

    // 如果当前激活的标签被关闭了，切换到目标标签
    if (!tabs.value.find(t => t.id === activeTabId.value)) {
      activeTabId.value = tabId
    }
    saveToStorage()
  }

  /**
   * 切换标签
   */
  function switchTab(tabId: string): void {
    if (tabs.value.find(t => t.id === tabId)) {
      activeTabId.value = tabId
      saveToStorage()
    }
  }

  /**
   * 切换到下一个标签
   */
  function switchToNextTab(): void {
    const currentIndex = activeTabIndex.value
    if (currentIndex < tabs.value.length - 1) {
      activeTabId.value = tabs.value[currentIndex + 1].id
    } else {
      activeTabId.value = tabs.value[0].id
    }
  }

  /**
   * 切换到上一个标签
   */
  function switchToPrevTab(): void {
    const currentIndex = activeTabIndex.value
    if (currentIndex > 0) {
      activeTabId.value = tabs.value[currentIndex - 1].id
    } else {
      activeTabId.value = tabs.value[tabs.value.length - 1].id
    }
  }

  /**
   * 切换到指定索引的标签
   */
  function switchToTabByIndex(index: number): void {
    if (index >= 0 && index < tabs.value.length) {
      activeTabId.value = tabs.value[index].id
    }
  }

  /**
   * 更新标签
   */
  function updateTab(tabId: string, updates: Partial<QueryTab>): void {
    const tab = tabs.value.find(t => t.id === tabId)
    if (tab) {
      Object.assign(tab, updates)
      // 如果更新了 SQL 内容，标记为脏
      if ('sql' in updates && updates.sql !== '') {
        tab.isDirty = true
      }
    }
  }

  /**
   * 更新当前激活标签
   */
  function updateActiveTab(updates: Partial<QueryTab>): void {
    if (activeTabId.value) {
      updateTab(activeTabId.value, updates)
    }
  }

  /**
   * 设置当前标签的查询结果
   */
  function setQueryResult(tabId: string, result: QueryResult | null): void {
    const tab = tabs.value.find(t => t.id === tabId)
    if (tab) {
      tab.queryResult = result
    }
  }

  /**
   * 设置当前标签的查询状态
   */
  function setQuerying(tabId: string, isQuerying: boolean): void {
    const tab = tabs.value.find(t => t.id === tabId)
    if (tab) {
      tab.isQuerying = isQuerying
    }
  }

  /**
   * 重命名标签
   */
  function renameTab(tabId: string, name: string): void {
    const tab = tabs.value.find(t => t.id === tabId)
    if (tab && name.trim()) {
      tab.name = name.trim()
      saveToStorage()

      // 如果是已保存的标签，同步到服务端
      if (tab.isSaved && tab.serverId) {
        queryTabApi.updateTab(tab.serverId, { name: tab.name }).catch(err => {
          console.error('更新标签名称失败:', err)
        })
      }
    }
  }

  /**
   * 重新排序标签
   */
  function reorderTabs(fromIndex: number, toIndex: number): void {
    if (fromIndex < 0 || fromIndex >= tabs.value.length) return
    if (toIndex < 0 || toIndex >= tabs.value.length) return
    if (fromIndex === toIndex) return

    const [movedTab] = tabs.value.splice(fromIndex, 1)
    tabs.value.splice(toIndex, 0, movedTab)
    saveToStorage()

    // 同步已保存标签的排序到服务端
    syncOrderToServer()
  }

  /**
   * 同步排序到服务端
   */
  async function syncOrderToServer(): Promise<void> {
    const savedTabIds = tabs.value
      .filter(t => t.isSaved && t.serverId)
      .map(t => t.serverId as number)

    if (savedTabIds.length > 0) {
      try {
        await queryTabApi.reorderTabs({ tabIds: savedTabIds })
      } catch (err) {
        console.error('同步排序失败:', err)
      }
    }
  }

  /**
   * 标记标签为干净（已保存）
   */
  function markClean(tabId: string): void {
    const tab = tabs.value.find(t => t.id === tabId)
    if (tab) {
      tab.isDirty = false
    }
  }

  /**
   * 检查是否有未保存的标签
   */
  function hasUnsavedTabs(): boolean {
    return tabs.value.some(t => t.isDirty && t.sql.trim())
  }

  /**
   * 保存标签到服务端
   */
  async function saveTabToServer(tabId: string): Promise<boolean> {
    const tab = tabs.value.find(t => t.id === tabId)
    if (!tab) return false

    try {
      if (tab.isSaved && tab.serverId) {
        // 更新已存在的标签
        const { data } = await queryTabApi.updateTab(tab.serverId, {
          name: tab.name,
          platformCode: tab.platformCode,
          connectionId: tab.connectionId,
          sqlContent: tab.sql
        })
        if (data.success) {
          tab.isDirty = false
          ElMessage.success('标签保存成功')
          return true
        }
      } else {
        // 创建新标签
        const { data } = await queryTabApi.createTab({
          name: tab.name,
          platformCode: tab.platformCode,
          connectionId: tab.connectionId,
          sqlContent: tab.sql
        })
        if (data.success && data.data) {
          // 更新本地标签为已保存状态
          const newServerId = data.data.id
          tab.serverId = newServerId
          tab.id = `server-${newServerId}`
          tab.isSaved = true
          tab.isDirty = false
          activeTabId.value = tab.id
          saveToStorage()
          ElMessage.success('标签保存成功')
          return true
        }
      }
    } catch (err) {
      console.error('保存标签失败:', err)
      ElMessage.error('保存标签失败')
    }
    return false
  }

  /**
   * 从服务端删除标签
   */
  async function deleteTabFromServer(tabId: string): Promise<boolean> {
    const tab = tabs.value.find(t => t.id === tabId)
    if (!tab || !tab.isSaved || !tab.serverId) {
      // 不是已保存的标签，直接关闭
      return closeTab(tabId)
    }

    try {
      const { data } = await queryTabApi.deleteTab(tab.serverId)
      if (data.success) {
        closeTab(tabId)
        ElMessage.success('标签删除成功')
        return true
      }
    } catch (err) {
      console.error('删除标签失败:', err)
      ElMessage.error('删除标签失败')
    }
    return false
  }

  /**
   * 从服务端加载标签
   */
  async function loadFromServer(): Promise<void> {
    isLoading.value = true
    try {
      const { data } = await queryTabApi.getTabs()
      if (data.success && data.data) {
        const serverTabs = data.data.map(dtoToQueryTab)

        // 加载本地临时标签
        const localTabs = loadLocalTabs()

        // 合并：服务端标签 + 本地临时标签
        tabs.value = [...serverTabs, ...localTabs]

        // 设置活跃标签
        if (tabs.value.length > 0) {
          const storedActiveId = getStoredActiveTabId()
          if (storedActiveId && tabs.value.find(t => t.id === storedActiveId)) {
            activeTabId.value = storedActiveId
          } else {
            activeTabId.value = tabs.value[0].id
          }
        } else {
          // 没有标签，创建默认的临时标签
          createTab({ name: `${DEFAULT_TAB_NAME} 1` })
        }
      }
    } catch (err) {
      console.error('加载标签失败:', err)
      // 加载失败时，尝试从本地恢复
      loadFromStorage()
    } finally {
      isLoading.value = false
    }
  }

  /**
   * 获取存储的活跃标签 ID
   */
  function getStoredActiveTabId(): string | null {
    try {
      const stored = localStorage.getItem(STORAGE_KEY)
      if (stored) {
        const data = JSON.parse(stored)
        return data.activeTabId || null
      }
    } catch {
      // ignore
    }
    return null
  }

  /**
   * 加载本地临时标签
   */
  function loadLocalTabs(): QueryTab[] {
    try {
      const stored = localStorage.getItem(STORAGE_KEY)
      if (!stored) return []

      const data: StoredTabsData = JSON.parse(stored)

      // 只返回未保存的标签
      return data.tabs
        .filter(t => !t.isSaved)
        .map(t => ({
          ...t,
          queryResult: null,
          isQuerying: false,
          isSaved: false
        }))
    } catch {
      return []
    }
  }

  /**
   * 保存到 LocalStorage（只保存临时标签）
   */
  function saveToStorage(): void {
    try {
      // 限制每个标签的 SQL 内容长度（最大 100KB）
      const MAX_SQL_LENGTH = 100 * 1024

      // 只保存临时标签到 LocalStorage
      const tabsToSave = tabs.value.filter(t => !t.isSaved)

      const data: StoredTabsData = {
        version: STORAGE_VERSION,
        tabs: tabsToSave.map(t => ({
          id: t.id,
          serverId: t.serverId,
          name: t.name,
          platformCode: t.platformCode,
          connectionId: t.connectionId,
          sql: t.sql.length > MAX_SQL_LENGTH ? t.sql.slice(0, MAX_SQL_LENGTH) : t.sql,
          isDirty: t.isDirty,
          isSaved: t.isSaved,
          createdAt: t.createdAt
        })),
        activeTabId: activeTabId.value,
        savedAt: Date.now()
      }

      const jsonData = JSON.stringify(data)

      // 检查数据大小
      const MAX_STORAGE_SIZE = 4 * 1024 * 1024 // 4MB
      if (jsonData.length > MAX_STORAGE_SIZE) {
        console.warn('标签数据过大，清理旧数据')
        // 清理最旧的标签的 SQL 内容
        const sortedTabs = [...tabsToSave].sort((a, b) => a.createdAt - b.createdAt)
        for (const tab of sortedTabs) {
          if (tab.id !== activeTabId.value && tab.sql.length > 1000) {
            tab.sql = tab.sql.slice(0, 1000) + '\n-- [内容已截断以节省存储空间]'
            tab.isDirty = true
          }
        }
        // 重新生成数据
        const trimmedData: StoredTabsData = {
          version: STORAGE_VERSION,
          tabs: tabsToSave.map(t => ({
            id: t.id,
            serverId: t.serverId,
            name: t.name,
            platformCode: t.platformCode,
            connectionId: t.connectionId,
            sql: t.sql,
            isDirty: t.isDirty,
            isSaved: t.isSaved,
            createdAt: t.createdAt
          })),
          activeTabId: activeTabId.value,
          savedAt: Date.now()
        }
        localStorage.setItem(STORAGE_KEY, JSON.stringify(trimmedData))
      } else {
        localStorage.setItem(STORAGE_KEY, jsonData)
      }
    } catch (e) {
      if (e instanceof DOMException && e.name === 'QuotaExceededError') {
        console.error('LocalStorage 配额已满')
      } else {
        console.error('保存标签状态失败:', e)
      }
    }
  }

  /**
   * 从 LocalStorage 加载（回退方案）
   */
  function loadFromStorage(): void {
    try {
      const stored = localStorage.getItem(STORAGE_KEY)
      if (!stored) {
        initializeDefault()
        return
      }

      const data: StoredTabsData = JSON.parse(stored)

      // 恢复标签
      tabs.value = data.tabs.map(t => ({
        ...t,
        isSaved: t.isSaved ?? false,
        queryResult: null,
        isQuerying: false
      }))

      // 恢复激活状态
      if (data.activeTabId && tabs.value.find(t => t.id === data.activeTabId)) {
        activeTabId.value = data.activeTabId
      } else if (tabs.value.length > 0) {
        activeTabId.value = tabs.value[0].id
      }

      // 如果没有标签，创建默认标签
      if (tabs.value.length === 0) {
        initializeDefault()
      }
    } catch (e) {
      console.error('加载标签状态失败:', e)
      initializeDefault()
    }
  }

  /**
   * 初始化默认状态
   */
  function initializeDefault(): void {
    const defaultTab: QueryTab = {
      id: generateId(),
      name: `${DEFAULT_TAB_NAME} 1`,
      platformCode: null,
      connectionId: null,
      sql: '',
      queryResult: null,
      isQuerying: false,
      isDirty: false,
      isSaved: false,
      createdAt: Date.now()
    }
    tabs.value = [defaultTab]
    activeTabId.value = defaultTab.id
    saveToStorage()
  }

  /**
   * 重置所有状态
   */
  function reset(): void {
    localStorage.removeItem(STORAGE_KEY)
    initializeDefault()
  }

  // 防抖保存 SQL 变更
  let saveTimeout: ReturnType<typeof setTimeout> | null = null

  function debouncedSave(): void {
    if (saveTimeout) {
      clearTimeout(saveTimeout)
    }
    saveTimeout = setTimeout(() => {
      saveToStorage()
    }, 500)
  }

  // 监听标签变化，自动保存
  watch(
    () => tabs.value.map(t => t.sql),
    () => {
      debouncedSave()
    },
    { deep: true }
  )

  return {
    // 状态
    tabs,
    activeTabId,
    isLoading,
    // 计算属性
    activeTab,
    canCloseTab,
    canCreateTab,
    activeTabIndex,
    savedTabs,
    unsavedTabs,
    // 标签操作
    createTab,
    closeTab,
    closeOtherTabs,
    closeRightTabs,
    switchTab,
    switchToNextTab,
    switchToPrevTab,
    switchToTabByIndex,
    updateTab,
    updateActiveTab,
    setQueryResult,
    setQuerying,
    renameTab,
    reorderTabs,
    markClean,
    hasUnsavedTabs,
    // 服务端同步
    loadFromServer,
    saveTabToServer,
    deleteTabFromServer,
    // 持久化
    saveToStorage,
    loadFromStorage,
    initializeDefault,
    reset
  }
})
