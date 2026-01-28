import { request } from './api'
import type {
  TemplateModuleDto,
  TemplateDto,
  CreateTemplateRequest,
  UpdateTemplateRequest,
  CreateModuleRequest,
  UpdateModuleRequest
} from '@/types'

/**
 * 模板管理服务
 */
export const templateApi = {
  // ==================== 模块管理 ====================

  /**
   * 获取模板模块树
   */
  getModules() {
    return request.get<TemplateModuleDto[]>('/templates/modules')
  },

  /**
   * 创建模块
   */
  createModule(data: CreateModuleRequest) {
    return request.post<TemplateModuleDto>('/templates/modules', data)
  },

  /**
   * 更新模块
   */
  updateModule(id: number, data: UpdateModuleRequest) {
    return request.put<TemplateModuleDto>(`/templates/modules/${id}`, data)
  },

  /**
   * 删除模块
   */
  deleteModule(id: number) {
    return request.delete(`/templates/modules/${id}`)
  },

  // ==================== 模板管理 ====================

  /**
   * 获取指定模块下的模板列表
   */
  getTemplatesByModule(moduleId: number) {
    return request.get<TemplateDto[]>(`/templates/module/${moduleId}`)
  },

  /**
   * 获取模板详情
   */
  getTemplate(id: number) {
    return request.get<TemplateDto>(`/templates/${id}`)
  },

  /**
   * 创建模板
   */
  createTemplate(data: CreateTemplateRequest) {
    return request.post<TemplateDto>('/templates', data)
  },

  /**
   * 更新模板
   */
  updateTemplate(id: number, data: UpdateTemplateRequest) {
    return request.put<TemplateDto>(`/templates/${id}`, data)
  },

  /**
   * 删除模板
   */
  deleteTemplate(id: number) {
    return request.delete(`/templates/${id}`)
  },

  /**
   * 搜索模板
   */
  searchTemplates(keyword: string) {
    return request.get<TemplateDto[]>('/templates/search', { params: { keyword } })
  }
}
