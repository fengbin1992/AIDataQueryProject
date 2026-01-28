<template>
  <div class="login-container">
    <div class="login-box">
      <div class="login-header">
        <h1 class="title">AIDataQuery</h1>
        <p class="subtitle">数据查询中心</p>
      </div>

      <el-form
        ref="loginFormRef"
        :model="loginForm"
        :rules="loginRules"
        class="login-form"
        @keyup.enter="handleLogin"
      >
        <el-form-item prop="username">
          <el-input
            v-model="loginForm.username"
            placeholder="请输入用户名"
            size="large"
            :prefix-icon="User"
          />
        </el-form-item>

        <el-form-item prop="password">
          <el-input
            v-model="loginForm.password"
            type="password"
            placeholder="请输入密码"
            size="large"
            :prefix-icon="Lock"
            show-password
          />
        </el-form-item>

        <el-form-item>
          <el-checkbox v-model="loginForm.rememberMe">记住我</el-checkbox>
        </el-form-item>

        <el-form-item>
          <el-button
            type="primary"
            size="large"
            class="login-btn"
            :loading="loading"
            @click="handleLogin"
          >
            {{ loading ? '登录中...' : '登 录' }}
          </el-button>
        </el-form-item>
      </el-form>

      <div class="login-footer">
        <el-divider />
        <p class="copyright">{{ new Date().getFullYear() }} AIDataQuery</p>
      </div>
    </div>

    <!-- 主题切换按钮 -->
    <div class="theme-toggle">
      <el-button circle @click="themeStore.toggle">
        <el-icon>
          <Moon v-if="!themeStore.isDark" />
          <Sunny v-else />
        </el-icon>
      </el-button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { ElMessage, type FormInstance, type FormRules } from 'element-plus'
import { User, Lock, Moon, Sunny } from '@element-plus/icons-vue'
import { useUserStore, useThemeStore } from '@/stores'
import type { LoginRequest } from '@/types'

const router = useRouter()
const route = useRoute()
const userStore = useUserStore()
const themeStore = useThemeStore()

const loginFormRef = ref<FormInstance>()
const loading = ref(false)

const loginForm = ref<LoginRequest>({
  username: '',
  password: '',
  rememberMe: false
})

const loginRules: FormRules = {
  username: [
    { required: true, message: '请输入用户名', trigger: 'blur' },
    { min: 3, max: 50, message: '用户名长度在 3 到 50 个字符', trigger: 'blur' }
  ],
  password: [
    { required: true, message: '请输入密码', trigger: 'blur' },
    { min: 6, message: '密码长度至少 6 个字符', trigger: 'blur' }
  ]
}

async function handleLogin() {
  if (!loginFormRef.value) return

  try {
    await loginFormRef.value.validate()
    loading.value = true

    const success = await userStore.login(loginForm.value)

    if (success) {
      ElMessage.success('登录成功')
      // 跳转到之前访问的页面或首页
      const redirect = route.query.redirect as string
      router.push(redirect || '/')
    }
  } catch {
    // 验证失败
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  // 检查是否有记住的用户名
  const rememberedUser = localStorage.getItem('rememberedUsername')
  if (rememberedUser) {
    loginForm.value.username = rememberedUser
    loginForm.value.rememberMe = true
  }
})
</script>

<style scoped lang="scss">
.login-container {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, var(--el-color-primary-light-5) 0%, var(--el-color-primary) 100%);
  position: relative;
}

.login-box {
  width: 400px;
  padding: 40px;
  background-color: var(--el-bg-color);
  border-radius: 8px;
  box-shadow: 0 4px 20px rgba(0, 0, 0, 0.15);
}

.login-header {
  text-align: center;
  margin-bottom: 30px;

  .title {
    font-size: 28px;
    font-weight: bold;
    color: var(--el-color-primary);
    margin-bottom: 8px;
  }

  .subtitle {
    font-size: 14px;
    color: var(--el-text-color-secondary);
  }
}

.login-form {
  .el-form-item {
    margin-bottom: 24px;
  }

  .login-btn {
    width: 100%;
  }
}

.login-footer {
  text-align: center;

  .copyright {
    font-size: 12px;
    color: var(--el-text-color-secondary);
  }
}

.theme-toggle {
  position: absolute;
  top: 20px;
  right: 20px;
}
</style>
