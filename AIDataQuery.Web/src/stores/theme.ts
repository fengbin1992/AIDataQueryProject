import { defineStore } from 'pinia'
import { ref, computed, watch } from 'vue'
import { useDark, useToggle } from '@vueuse/core'

export type ThemeMode = 'light' | 'dark' | 'auto'

export const useThemeStore = defineStore('theme', () => {
  // 使用 vueuse 的 useDark 来处理系统主题
  const isDark = useDark({
    storageKey: 'theme-appearance',
    valueDark: 'dark',
    valueLight: 'light'
  })
  const toggleDark = useToggle(isDark)

  // 主题模式：light, dark, auto
  const themeMode = ref<ThemeMode>(
    (localStorage.getItem('theme-mode') as ThemeMode) || 'auto'
  )

  // 计算当前实际主题
  const currentTheme = computed(() => {
    if (themeMode.value === 'auto') {
      return isDark.value ? 'dark' : 'light'
    }
    return themeMode.value
  })

  // 监听主题模式变化
  watch(themeMode, (newMode) => {
    localStorage.setItem('theme-mode', newMode)

    if (newMode === 'auto') {
      // 自动模式：根据系统设置
      const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches
      isDark.value = prefersDark
    } else {
      isDark.value = newMode === 'dark'
    }

    applyTheme()
  }, { immediate: true })

  // 应用主题到 document
  function applyTheme(): void {
    const theme = currentTheme.value
    document.documentElement.classList.remove('light', 'dark')
    document.documentElement.classList.add(theme)

    // 设置 Element Plus 的主题
    if (theme === 'dark') {
      document.documentElement.classList.add('dark')
    } else {
      document.documentElement.classList.remove('dark')
    }
  }

  /**
   * 设置主题模式
   */
  function setThemeMode(mode: ThemeMode): void {
    themeMode.value = mode
  }

  /**
   * 切换主题
   */
  function toggle(): void {
    if (themeMode.value === 'auto') {
      // 从 auto 切换到手动模式
      setThemeMode(isDark.value ? 'light' : 'dark')
    } else {
      toggleDark()
      setThemeMode(isDark.value ? 'dark' : 'light')
    }
  }

  // 初始化应用主题
  applyTheme()

  return {
    // 状态
    themeMode,
    isDark,
    // 计算属性
    currentTheme,
    // 操作
    setThemeMode,
    toggle,
    applyTheme
  }
})
