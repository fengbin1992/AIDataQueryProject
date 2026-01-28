import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { authApi } from '@/services'
import type { UserInfo, LoginRequest } from '@/types'

export const useUserStore = defineStore('user', () => {
  // 状态
  const token = ref<string>(localStorage.getItem('token') || '')
  const user = ref<UserInfo | null>(null)

  // 初始化时尝试从 localStorage 恢复用户信息
  const storedUser = localStorage.getItem('user')
  if (storedUser) {
    try {
      user.value = JSON.parse(storedUser)
    } catch {
      user.value = null
    }
  }

  // 计算属性
  const isLoggedIn = computed(() => !!token.value && !!user.value)
  const isAdmin = computed(() => user.value?.role === 'Admin')
  const username = computed(() => user.value?.username || '')
  const nickname = computed(() => user.value?.nickname || '')
  const platforms = computed(() => user.value?.platforms || [])

  // 操作
  /**
   * 用户登录
   */
  async function login(loginData: LoginRequest): Promise<boolean> {
    try {
      const { data } = await authApi.login(loginData)
      if (data.success && data.data) {
        token.value = data.data.token
        user.value = data.data.user

        // 持久化存储
        localStorage.setItem('token', data.data.token)
        localStorage.setItem('user', JSON.stringify(data.data.user))

        // 如果选择了记住我，可以设置更长的过期时间
        if (loginData.rememberMe) {
          localStorage.setItem('rememberMe', 'true')
        }

        return true
      }
      return false
    } catch {
      return false
    }
  }

  /**
   * 用户登出
   */
  async function logout(): Promise<void> {
    try {
      await authApi.logout()
    } catch {
      // 即使请求失败也要清除本地状态
    } finally {
      clearUserData()
    }
  }

  /**
   * 清除用户数据
   */
  function clearUserData(): void {
    token.value = ''
    user.value = null
    localStorage.removeItem('token')
    localStorage.removeItem('user')
    localStorage.removeItem('rememberMe')
  }

  /**
   * 获取当前用户信息
   */
  async function fetchCurrentUser(): Promise<boolean> {
    try {
      const { data } = await authApi.getCurrentUser()
      if (data.success && data.data) {
        user.value = data.data
        localStorage.setItem('user', JSON.stringify(data.data))
        return true
      }
      return false
    } catch {
      clearUserData()
      return false
    }
  }

  /**
   * 检查是否有平台权限
   */
  function hasPlatformAccess(platformCode: string): boolean {
    if (isAdmin.value) return true
    return platforms.value.includes(platformCode)
  }

  return {
    // 状态
    token,
    user,
    // 计算属性
    isLoggedIn,
    isAdmin,
    username,
    nickname,
    platforms,
    // 操作
    login,
    logout,
    clearUserData,
    fetchCurrentUser,
    hasPlatformAccess
  }
})
