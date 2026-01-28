import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { platformApi, queryApi } from '@/services'
import type {
  PlatformDto,
  DatabaseConnectionDto,
  QueryRequest,
  QueryResult,
  TableInfo,
  ColumnInfo
} from '@/types'

export const useQueryStore = defineStore('query', () => {
  // 状态
  const platforms = ref<PlatformDto[]>([])
  const connections = ref<DatabaseConnectionDto[]>([])
  const selectedPlatformCode = ref<string>('')
  const selectedConnectionId = ref<number | null>(null)
  const sql = ref<string>('')
  const queryResult = ref<QueryResult | null>(null)
  const isQuerying = ref<boolean>(false)
  const tables = ref<TableInfo[]>([])
  const columns = ref<ColumnInfo[]>([])

  // 计算属性
  const selectedPlatform = computed(() =>
    platforms.value.find(p => p.code === selectedPlatformCode.value)
  )

  const selectedConnection = computed(() =>
    connections.value.find(c => c.id === selectedConnectionId.value)
  )

  const hasResult = computed(() => queryResult.value !== null)

  // 操作
  /**
   * 加载平台列表
   */
  async function loadPlatforms(): Promise<void> {
    try {
      const { data } = await platformApi.getPlatforms()
      if (data.success && data.data) {
        platforms.value = data.data
        // 如果有平台且当前没有选择，自动选择第一个
        if (data.data.length > 0 && !selectedPlatformCode.value) {
          await selectPlatform(data.data[0].code)
        }
      }
    } catch {
      platforms.value = []
    }
  }

  /**
   * 选择平台并加载连接列表
   */
  async function selectPlatform(platformCode: string): Promise<void> {
    selectedPlatformCode.value = platformCode
    selectedConnectionId.value = null
    connections.value = []
    tables.value = []

    try {
      const { data } = await platformApi.getConnections(platformCode)
      if (data.success && data.data) {
        connections.value = data.data
        // 如果有连接且当前没有选择，自动选择第一个
        if (data.data.length > 0) {
          await selectConnection(data.data[0].id)
        }
      }
    } catch {
      connections.value = []
    }
  }

  /**
   * 选择数据库连接并加载表列表
   */
  async function selectConnection(connectionId: number): Promise<void> {
    selectedConnectionId.value = connectionId
    tables.value = []
    columns.value = []

    try {
      const { data } = await queryApi.getTables(connectionId)
      if (data.success && data.data) {
        tables.value = data.data
      }
    } catch {
      tables.value = []
    }
  }

  /**
   * 获取表的列信息
   */
  async function loadColumns(tableName: string): Promise<void> {
    if (!selectedConnectionId.value) return

    try {
      const { data } = await queryApi.getColumns(selectedConnectionId.value, tableName)
      if (data.success && data.data) {
        columns.value = data.data
      }
    } catch {
      columns.value = []
    }
  }

  /**
   * 执行查询
   */
  async function executeQuery(): Promise<boolean> {
    if (!selectedPlatformCode.value || !selectedConnectionId.value || !sql.value.trim()) {
      return false
    }

    isQuerying.value = true
    queryResult.value = null

    try {
      const request: QueryRequest = {
        platformCode: selectedPlatformCode.value,
        connectionId: selectedConnectionId.value,
        sql: sql.value
      }

      const { data } = await queryApi.execute(request)
      if (data.success && data.data) {
        queryResult.value = data.data
        return data.data.success
      }
      return false
    } catch {
      return false
    } finally {
      isQuerying.value = false
    }
  }

  /**
   * 设置 SQL 内容
   */
  function setSql(content: string): void {
    sql.value = content
  }

  /**
   * 清空查询结果
   */
  function clearResult(): void {
    queryResult.value = null
  }

  /**
   * 重置所有状态
   */
  function reset(): void {
    platforms.value = []
    connections.value = []
    selectedPlatformCode.value = ''
    selectedConnectionId.value = null
    sql.value = ''
    queryResult.value = null
    tables.value = []
    columns.value = []
  }

  return {
    // 状态
    platforms,
    connections,
    selectedPlatformCode,
    selectedConnectionId,
    sql,
    queryResult,
    isQuerying,
    tables,
    columns,
    // 计算属性
    selectedPlatform,
    selectedConnection,
    hasResult,
    // 操作
    loadPlatforms,
    selectPlatform,
    selectConnection,
    loadColumns,
    executeQuery,
    setSql,
    clearResult,
    reset
  }
})
