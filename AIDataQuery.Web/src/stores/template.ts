import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { templateApi } from '@/services'
import type { TemplateModuleDto, TemplateDto } from '@/types'

export const useTemplateStore = defineStore('template', () => {
  // 状态
  const modules = ref<TemplateModuleDto[]>([])
  const selectedModuleId = ref<number | null>(null)
  const templates = ref<TemplateDto[]>([])
  const isLoading = ref<boolean>(false)

  // 计算属性
  const selectedModule = computed(() => {
    if (!selectedModuleId.value) return null
    return findModuleById(modules.value, selectedModuleId.value)
  })

  // 辅助函数：递归查找模块
  function findModuleById(moduleList: TemplateModuleDto[], id: number): TemplateModuleDto | null {
    for (const module of moduleList) {
      if (module.id === id) return module
      if (module.children?.length) {
        const found = findModuleById(module.children, id)
        if (found) return found
      }
    }
    return null
  }

  // 操作
  /**
   * 加载模块树
   */
  async function loadModules(): Promise<void> {
    isLoading.value = true
    try {
      const { data } = await templateApi.getModules()
      if (data.success && data.data) {
        modules.value = data.data
      }
    } catch {
      modules.value = []
    } finally {
      isLoading.value = false
    }
  }

  /**
   * 选择模块并加载模板列表
   */
  async function selectModule(moduleId: number): Promise<void> {
    selectedModuleId.value = moduleId
    await loadTemplatesByModule(moduleId)
  }

  /**
   * 加载指定模块的模板列表
   */
  async function loadTemplatesByModule(moduleId: number): Promise<void> {
    isLoading.value = true
    try {
      const { data } = await templateApi.getTemplatesByModule(moduleId)
      if (data.success && data.data) {
        templates.value = data.data
      }
    } catch {
      templates.value = []
    } finally {
      isLoading.value = false
    }
  }

  /**
   * 搜索模板
   */
  async function searchTemplates(keyword: string): Promise<TemplateDto[]> {
    try {
      const { data } = await templateApi.searchTemplates(keyword)
      if (data.success && data.data) {
        return data.data
      }
    } catch {
      // 搜索失败返回空数组
    }
    return []
  }

  /**
   * 重置状态
   */
  function reset(): void {
    modules.value = []
    selectedModuleId.value = null
    templates.value = []
  }

  return {
    // 状态
    modules,
    selectedModuleId,
    templates,
    isLoading,
    // 计算属性
    selectedModule,
    // 操作
    loadModules,
    selectModule,
    loadTemplatesByModule,
    searchTemplates,
    reset
  }
})
