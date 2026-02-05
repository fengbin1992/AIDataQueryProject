import { defineStore } from 'pinia'
import { ref, computed, watch } from 'vue'
import { configQueryApi } from '@/services/configQuery'
import type {
  ConfigQueryListItem,
  ConfigQueryDetail,
  ConfigQueryParamPreset,
  CreateConfigQueryRequest,
  UpdateConfigQueryRequest,
  ExecuteConfigQueryRequest,
  CreateParamPresetRequest,
  ConfigQueryFolder,
  CreateFolderRequest,
  UpdateFolderRequest,
  ConditionSwitchItem
} from '@/types/configQuery'
import type { QueryResult } from '@/types'

export type InputMode = 'form' | 'json' | 'dual'

export const useConfigQueryStore = defineStore('configQuery', () => {
  // ==================== 状态 ====================

  // 列表
  const list = ref<ConfigQueryListItem[]>([])
  const total = ref(0)
  const pageIndex = ref(1)
  const pageSize = ref(20)
  const keyword = ref('')
  const loading = ref(false)

  // 文件夹
  const folders = ref<ConfigQueryFolder[]>([])
  const expandedFolders = ref<Set<number>>(new Set())

  // 当前选中
  const currentId = ref<number | null>(null)
  const currentQuery = ref<ConfigQueryDetail | null>(null)

  // 输入模式
  const inputMode = ref<InputMode>('form')

  // 参数值（表单和 JSON 共享此状态）
  const paramValues = ref<Record<string, unknown>>({})

  // JSON 编辑器内容（用于语法错误时保留用户输入）
  const jsonEditorContent = ref('')
  const jsonParseError = ref<string | null>(null)

  // 参数预设
  const presets = ref<ConfigQueryParamPreset[]>([])
  const currentPresetId = ref<number | null>(null)

  // 条件开关状态（key: 条件组名或参数名, value: 是否启用）
  const conditionSwitches = ref<Record<string, boolean>>({})

  // 执行结果
  const result = ref<QueryResult | null>(null)
  const executing = ref(false)

  // 面板状态
  const sqlPanelExpanded = ref(false)
  const paramPanelExpanded = ref(true)
  const listPanelCollapsed = ref(false)
  const resultPanelExpanded = ref(false)

  // ==================== 计算属性 ====================

  const currentQueryName = computed(() => currentQuery.value?.name ?? '')

  const isOwner = computed(() => currentQuery.value?.isOwner ?? false)

  const canEdit = computed(() => currentQuery.value?.canEdit ?? false)

  // 参数值摘要（用于收缩状态显示）
  const paramValuesSummary = computed(() => {
    if (!currentQuery.value) return ''
    const parts: string[] = []
    for (const param of currentQuery.value.parameters) {
      const value = paramValues.value[param.paramName]
      if (value !== undefined && value !== null && value !== '') {
        if (param.paramType === 'daterange' && Array.isArray(value)) {
          parts.push(`${value[0]} ~ ${value[1]}`)
        } else if (param.paramType === 'multiselect' && Array.isArray(value)) {
          parts.push(`${param.paramLabel}:${value.length}项`)
        } else {
          parts.push(`${value}`)
        }
      }
    }
    return parts.join(' | ')
  })

  // SQL 预览摘要
  const sqlPreviewSummary = computed(() => {
    if (!currentQuery.value) return ''
    const sql = currentQuery.value.sqlContent
    return sql.length > 50 ? sql.substring(0, 50) + '...' : sql
  })

  // 条件开关列表（每个参数或条件组一个开关）
  const conditionSwitchList = computed<ConditionSwitchItem[]>(() => {
    if (!currentQuery.value) return []
    if (currentQuery.value.parameters.length === 0) return []

    const groups = new Map<string, { label: string; paramNames: string[] }>()

    for (const param of currentQuery.value.parameters) {
      // 有条件组用条件组名，没有则用参数名
      const key = param.conditionGroup || param.paramName
      if (!groups.has(key)) {
        groups.set(key, {
          // 有条件组显示条件组名，没有显示参数标签
          label: param.conditionGroup || param.paramLabel,
          paramNames: []
        })
      }
      groups.get(key)!.paramNames.push(param.paramName)
    }

    return Array.from(groups.entries()).map(([key, info]) => ({
      key,
      label: info.label,
      enabled: conditionSwitches.value[key] !== false, // 默认启用
      paramNames: info.paramNames
    }))
  })

  // ==================== 监听器 ====================

  // 表单值变化时同步到 JSON
  watch(
    paramValues,
    (newVal) => {
      if (inputMode.value !== 'json') {
        jsonEditorContent.value = JSON.stringify(newVal, null, 2)
        jsonParseError.value = null
      }
    },
    { deep: true }
  )

  // ==================== 操作 ====================

  /**
   * 加载配置查询列表
   */
  async function loadList(): Promise<void> {
    loading.value = true
    try {
      const [listRes, foldersRes] = await Promise.all([
        configQueryApi.getList({
          keyword: keyword.value || undefined,
          pageIndex: pageIndex.value,
          pageSize: pageSize.value
        }),
        configQueryApi.getFolders()
      ])
      if (listRes.data.success && listRes.data.data) {
        list.value = listRes.data.data.items
        total.value = listRes.data.data.total
      }
      if (foldersRes.data.success && foldersRes.data.data) {
        folders.value = foldersRes.data.data
      }
    } catch {
      list.value = []
      total.value = 0
      folders.value = []
    } finally {
      loading.value = false
    }
  }

  /**
   * 搜索配置查询
   */
  async function search(kw: string): Promise<void> {
    keyword.value = kw
    pageIndex.value = 1
    await loadList()
  }

  /**
   * 选择配置查询
   */
  async function selectQuery(id: number): Promise<void> {
    if (currentId.value === id) return

    currentId.value = id
    loading.value = true
    try {
      const { data } = await configQueryApi.getById(id)
      if (data.success && data.data) {
        currentQuery.value = data.data
        initParamValues()
        await loadPresets()
      }
    } catch {
      currentQuery.value = null
    } finally {
      loading.value = false
    }
  }

  /**
   * 初始化参数值
   */
  function initParamValues(): void {
    if (!currentQuery.value) return

    const values: Record<string, unknown> = {}
    for (const param of currentQuery.value.parameters) {
      values[param.paramName] = getDefaultValue(param)
    }
    paramValues.value = values
    jsonEditorContent.value = JSON.stringify(values, null, 2)
    jsonParseError.value = null
    currentPresetId.value = null

    // 初始化条件开关
    initConditionSwitches()
  }

  /**
   * 初始化条件开关（每个参数或条件组一个开关，默认全部启用）
   */
  function initConditionSwitches(): void {
    if (!currentQuery.value) return

    const switches: Record<string, boolean> = {}
    for (const param of currentQuery.value.parameters) {
      const key = param.conditionGroup || param.paramName
      if (!(key in switches)) {
        switches[key] = true // 默认启用
      }
    }
    conditionSwitches.value = switches
  }

  /**
   * 切换条件启用状态
   */
  function toggleCondition(key: string): void {
    conditionSwitches.value[key] = !conditionSwitches.value[key]
  }

  /**
   * 设置条件启用状态
   */
  function setConditionEnabled(key: string, enabled: boolean): void {
    conditionSwitches.value[key] = enabled
  }

  /**
   * 获取参数默认值
   */
  function getDefaultValue(param: { paramType: string; defaultValue?: string; extraConfig?: { defaultType?: string } }): unknown {
    const { paramType, defaultValue, extraConfig } = param

    if (defaultValue) {
      if (paramType === 'number') {
        return parseFloat(defaultValue) || 0
      }
      if (paramType === 'multiselect') {
        try {
          return JSON.parse(defaultValue)
        } catch {
          return []
        }
      }
      return defaultValue
    }

    // 根据类型返回空值
    switch (paramType) {
      case 'number':
        return extraConfig?.defaultType === 'today' ? 0 : 0
      case 'date':
        if (extraConfig?.defaultType === 'today') {
          return new Date().toISOString().split('T')[0]
        }
        return ''
      case 'daterange':
        if (extraConfig?.defaultType === 'last7days') {
          const end = new Date()
          const start = new Date()
          start.setDate(start.getDate() - 7)
          return [start.toISOString().split('T')[0], end.toISOString().split('T')[0]]
        }
        return ['', '']
      case 'multiselect':
        return []
      default:
        return ''
    }
  }

  /**
   * 加载预设列表
   */
  async function loadPresets(): Promise<void> {
    if (!currentId.value) return

    try {
      const { data } = await configQueryApi.getPresets(currentId.value)
      if (data.success && data.data) {
        presets.value = data.data
        // 自动应用默认预设
        const defaultPreset = data.data.find(p => p.isDefault)
        if (defaultPreset) {
          applyPreset(defaultPreset.id)
        }
      }
    } catch {
      presets.value = []
    }
  }

  /**
   * 应用预设
   */
  function applyPreset(presetId: number): void {
    const preset = presets.value.find(p => p.id === presetId)
    if (!preset) return

    currentPresetId.value = presetId
    paramValues.value = { ...preset.paramValues }
    jsonEditorContent.value = JSON.stringify(preset.paramValues, null, 2)
    jsonParseError.value = null
  }

  /**
   * 保存预设
   */
  async function savePreset(name: string, isDefault: boolean = false): Promise<void> {
    if (!currentId.value) return

    const request: CreateParamPresetRequest = {
      name,
      paramValues: paramValues.value,
      isDefault
    }

    try {
      await configQueryApi.createPreset(currentId.value, request)
      await loadPresets()
    } catch (error) {
      throw error
    }
  }

  /**
   * 删除预设
   */
  async function deletePreset(presetId: number): Promise<void> {
    if (!currentId.value) return

    try {
      await configQueryApi.deletePreset(currentId.value, presetId)
      await loadPresets()
      if (currentPresetId.value === presetId) {
        currentPresetId.value = null
      }
    } catch (error) {
      throw error
    }
  }

  /**
   * 从 JSON 同步到表单
   */
  function syncJsonToForm(): boolean {
    try {
      const parsed = JSON.parse(jsonEditorContent.value)
      // 只保留在参数列表中定义的参数，过滤掉无效的键
      if (currentQuery.value) {
        const validKeys = new Set(currentQuery.value.parameters.map(p => p.paramName))
        const filtered: Record<string, unknown> = {}
        for (const key of Object.keys(parsed)) {
          if (validKeys.has(key)) {
            filtered[key] = parsed[key]
          }
        }
        paramValues.value = filtered
        // 同时更新 JSON 内容，移除无效的键
        jsonEditorContent.value = JSON.stringify(filtered, null, 2)
      } else {
        paramValues.value = parsed
      }
      jsonParseError.value = null
      return true
    } catch (e) {
      jsonParseError.value = 'JSON 格式错误: ' + (e as Error).message
      return false
    }
  }

  /**
   * 从表单同步到 JSON
   */
  function syncFormToJson(): void {
    jsonEditorContent.value = JSON.stringify(paramValues.value, null, 2)
    jsonParseError.value = null
  }

  /**
   * 执行查询
   */
  async function execute(connectionId?: number): Promise<void> {
    if (!currentId.value) return

    // 如果是 JSON 模式，先同步到表单
    if (inputMode.value === 'json') {
      if (!syncJsonToForm()) {
        return
      }
    }

    executing.value = true
    result.value = null

    // 构建启用的条件列表
    const enabledConditions = Object.entries(conditionSwitches.value)
      .filter(([, isEnabled]) => isEnabled)
      .map(([key]) => key)

    // 如果全部启用或没有条件开关，传 null（后端优化）
    const allEnabled = conditionSwitchList.value.length === 0 ||
      enabledConditions.length === conditionSwitchList.value.length

    const request: ExecuteConfigQueryRequest = {
      connectionId,
      parameters: paramValues.value,
      enabledConditions: allEnabled ? null : enabledConditions
    }

    try {
      const { data } = await configQueryApi.execute(currentId.value, request)
      if (data.success && data.data) {
        result.value = data.data
        resultPanelExpanded.value = true
      } else {
        result.value = {
          success: false,
          errorMessage: data.message || '执行失败',
          columns: [],
          rows: [],
          totalRows: 0,
          executionTimeMs: 0
        }
      }
    } catch (error) {
      result.value = {
        success: false,
        errorMessage: (error as Error).message || '执行失败',
        columns: [],
        rows: [],
        totalRows: 0,
        executionTimeMs: 0
      }
    } finally {
      executing.value = false
    }
  }

  /**
   * 创建配置查询
   */
  async function create(data: CreateConfigQueryRequest): Promise<number> {
    const { data: response } = await configQueryApi.create(data)
    if (response.success && response.data) {
      await loadList()
      return response.data.id
    }
    throw new Error(response.message || '创建失败')
  }

  /**
   * 更新配置查询
   */
  async function update(id: number, data: UpdateConfigQueryRequest): Promise<void> {
    await configQueryApi.update(id, data)
    await loadList()
    if (currentId.value === id) {
      await selectQuery(id)
    }
  }

  /**
   * 删除配置查询
   */
  async function remove(id: number): Promise<void> {
    await configQueryApi.delete(id)
    await loadList()
    if (currentId.value === id) {
      currentId.value = null
      currentQuery.value = null
      result.value = null
    }
  }

  /**
   * 复制配置查询
   */
  async function copy(id: number): Promise<number> {
    const { data: response } = await configQueryApi.copy(id)
    if (response.success && response.data) {
      await loadList()
      return response.data.id
    }
    throw new Error(response.message || '复制失败')
  }

  /**
   * 导出配置
   */
  async function exportConfig(id: number): Promise<string> {
    const { data: response } = await configQueryApi.export(id)
    if (response.success && response.data) {
      return JSON.stringify(response.data, null, 2)
    }
    throw new Error(response.message || '导出失败')
  }

  /**
   * 导入配置
   */
  async function importConfig(json: string): Promise<number> {
    const { data: response } = await configQueryApi.import(json)
    if (response.success && response.data) {
      await loadList()
      return response.data.id
    }
    throw new Error(response.message || '导入失败')
  }

  /**
   * 解析 SQL 参数
   */
  async function parseParams(sql: string): Promise<string[]> {
    const { data: response } = await configQueryApi.parseParams(sql)
    if (response.success && response.data) {
      return response.data.parameters
    }
    return []
  }

  // ==================== 文件夹操作 ====================

  /**
   * 加载文件夹列表
   */
  async function loadFolders(): Promise<void> {
    try {
      const { data } = await configQueryApi.getFolders()
      if (data.success && data.data) {
        folders.value = data.data
      }
    } catch {
      folders.value = []
    }
  }

  /**
   * 创建文件夹
   */
  async function createFolder(data: CreateFolderRequest): Promise<number> {
    const { data: response } = await configQueryApi.createFolder(data)
    if (response.success && response.data) {
      await loadFolders()
      return response.data.id
    }
    throw new Error(response.message || '创建失败')
  }

  /**
   * 更新文件夹
   */
  async function updateFolder(id: number, data: UpdateFolderRequest): Promise<void> {
    await configQueryApi.updateFolder(id, data)
    await loadFolders()
  }

  /**
   * 删除文件夹
   */
  async function deleteFolder(id: number): Promise<void> {
    await configQueryApi.deleteFolder(id)
    await loadList()
    expandedFolders.value.delete(id)
  }

  /**
   * 移动配置查询到文件夹
   */
  async function moveToFolder(queryId: number, folderId: number | null): Promise<void> {
    await configQueryApi.moveToFolder(queryId, { folderId: folderId ?? undefined })
    await loadList()
  }

  /**
   * 切换文件夹展开状态
   */
  function toggleFolder(folderId: number): void {
    if (expandedFolders.value.has(folderId)) {
      expandedFolders.value.delete(folderId)
    } else {
      expandedFolders.value.add(folderId)
    }
  }

  /**
   * 检查文件夹是否展开
   */
  function isFolderExpanded(folderId: number): boolean {
    return expandedFolders.value.has(folderId)
  }

  /**
   * 切换输入模式
   */
  function setInputMode(mode: InputMode): void {
    inputMode.value = mode
    if (mode === 'form' || mode === 'dual') {
      syncJsonToForm()
    }
  }

  /**
   * 切换面板展开状态
   */
  function toggleSqlPanel(): void {
    sqlPanelExpanded.value = !sqlPanelExpanded.value
    if (sqlPanelExpanded.value) {
      paramPanelExpanded.value = false
    }
  }

  function toggleParamPanel(): void {
    paramPanelExpanded.value = !paramPanelExpanded.value
    if (paramPanelExpanded.value) {
      sqlPanelExpanded.value = false
    }
  }

  function toggleListPanel(): void {
    listPanelCollapsed.value = !listPanelCollapsed.value
  }

  function toggleResultPanel(): void {
    resultPanelExpanded.value = !resultPanelExpanded.value
  }

  /**
   * 重置状态
   */
  function reset(): void {
    list.value = []
    total.value = 0
    pageIndex.value = 1
    keyword.value = ''
    folders.value = []
    expandedFolders.value = new Set()
    currentId.value = null
    currentQuery.value = null
    paramValues.value = {}
    jsonEditorContent.value = ''
    jsonParseError.value = null
    presets.value = []
    currentPresetId.value = null
    conditionSwitches.value = {}
    result.value = null
    resultPanelExpanded.value = false
    inputMode.value = 'form'
  }

  return {
    // 状态
    list,
    total,
    pageIndex,
    pageSize,
    keyword,
    loading,
    folders,
    expandedFolders,
    currentId,
    currentQuery,
    inputMode,
    paramValues,
    jsonEditorContent,
    jsonParseError,
    presets,
    currentPresetId,
    conditionSwitches,
    result,
    executing,
    sqlPanelExpanded,
    paramPanelExpanded,
    listPanelCollapsed,
    resultPanelExpanded,

    // 计算属性
    currentQueryName,
    isOwner,
    canEdit,
    paramValuesSummary,
    sqlPreviewSummary,
    conditionSwitchList,

    // 操作
    loadList,
    search,
    selectQuery,
    initParamValues,
    initConditionSwitches,
    toggleCondition,
    setConditionEnabled,
    loadPresets,
    applyPreset,
    savePreset,
    deletePreset,
    syncJsonToForm,
    syncFormToJson,
    execute,
    create,
    update,
    remove,
    copy,
    exportConfig,
    importConfig,
    parseParams,
    loadFolders,
    createFolder,
    updateFolder,
    deleteFolder,
    moveToFolder,
    toggleFolder,
    isFolderExpanded,
    setInputMode,
    toggleSqlPanel,
    toggleParamPanel,
    toggleListPanel,
    toggleResultPanel,
    reset
  }
})
