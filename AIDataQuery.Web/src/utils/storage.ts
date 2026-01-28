/**
 * 本地存储工具
 */

const TOKEN_KEY = 'token'
const USER_KEY = 'user'
const THEME_KEY = 'theme-mode'

export const storage = {
  // Token
  getToken(): string | null {
    return localStorage.getItem(TOKEN_KEY)
  },

  setToken(token: string): void {
    localStorage.setItem(TOKEN_KEY, token)
  },

  removeToken(): void {
    localStorage.removeItem(TOKEN_KEY)
  },

  // User
  getUser<T>(): T | null {
    const user = localStorage.getItem(USER_KEY)
    if (user) {
      try {
        return JSON.parse(user) as T
      } catch {
        return null
      }
    }
    return null
  },

  setUser<T>(user: T): void {
    localStorage.setItem(USER_KEY, JSON.stringify(user))
  },

  removeUser(): void {
    localStorage.removeItem(USER_KEY)
  },

  // Theme
  getTheme(): string | null {
    return localStorage.getItem(THEME_KEY)
  },

  setTheme(theme: string): void {
    localStorage.setItem(THEME_KEY, theme)
  },

  // 通用方法
  get<T>(key: string): T | null {
    const value = localStorage.getItem(key)
    if (value) {
      try {
        return JSON.parse(value) as T
      } catch {
        return value as unknown as T
      }
    }
    return null
  },

  set(key: string, value: unknown): void {
    if (typeof value === 'string') {
      localStorage.setItem(key, value)
    } else {
      localStorage.setItem(key, JSON.stringify(value))
    }
  },

  remove(key: string): void {
    localStorage.removeItem(key)
  },

  clear(): void {
    localStorage.clear()
  }
}

export default storage
