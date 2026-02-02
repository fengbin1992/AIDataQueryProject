import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router'
import { useUserStore } from '@/stores'

// 定义路由
const routes: RouteRecordRaw[] = [
  {
    path: '/login',
    name: 'Login',
    component: () => import('@/views/login/LoginView.vue'),
    meta: {
      title: '登录',
      requiresAuth: false
    }
  },
  {
    path: '/',
    component: () => import('@/layouts/MainLayout.vue'),
    redirect: '/query',
    meta: {
      requiresAuth: true
    },
    children: [
      {
        path: 'query',
        name: 'Query',
        component: () => import('@/views/query/DataQueryView.vue'),
        meta: {
          title: '数据查询',
          icon: 'Search'
        }
      },
      {
        path: 'templates',
        name: 'Templates',
        component: () => import('@/views/template/TemplateView.vue'),
        meta: {
          title: '模板管理',
          icon: 'Document'
        }
      },
      {
        path: 'history',
        name: 'History',
        component: () => import('@/views/history/HistoryView.vue'),
        meta: {
          title: '查询历史',
          icon: 'Clock'
        }
      },
      {
        path: 'admin',
        name: 'Admin',
        redirect: '/admin/users',
        meta: {
          title: '系统管理',
          icon: 'Setting',
          requiresAdmin: true
        },
        children: [
          {
            path: 'users',
            name: 'UserManage',
            component: () => import('@/views/admin/UserManage.vue'),
            meta: {
              title: '用户管理'
            }
          },
          {
            path: 'platforms',
            name: 'PlatformManage',
            component: () => import('@/views/admin/PlatformManage.vue'),
            meta: {
              title: '平台管理'
            }
          },
          {
            path: 'connections',
            name: 'ConnectionManage',
            component: () => import('@/views/admin/ConnectionManage.vue'),
            meta: {
              title: '连接管理'
            }
          }
        ]
      }
    ]
  },
  {
    path: '/404',
    name: 'NotFound',
    component: () => import('@/views/error/NotFound.vue'),
    meta: {
      title: '页面不存在',
      requiresAuth: false
    }
  },
  {
    path: '/:pathMatch(.*)*',
    redirect: '/404'
  }
]

// 创建路由实例
const router = createRouter({
  history: createWebHistory(),
  routes,
  scrollBehavior: () => ({ top: 0 })
})

// 路由守卫
router.beforeEach((to, _from, next) => {
  // 设置页面标题
  const appTitle = import.meta.env.VITE_APP_TITLE || 'AIDataQuery'
  document.title = to.meta.title ? `${to.meta.title} - ${appTitle}` : appTitle

  const userStore = useUserStore()

  // 检查是否需要认证
  const requiresAuth = to.matched.some(record => record.meta.requiresAuth !== false)
  const requiresAdmin = to.matched.some(record => record.meta.requiresAdmin)

  if (requiresAuth && !userStore.isLoggedIn) {
    // 需要登录但未登录，跳转到登录页
    next({
      path: '/login',
      query: { redirect: to.fullPath }
    })
  } else if (requiresAdmin && !userStore.isAdmin) {
    // 需要管理员权限但当前用户不是管理员
    next({ path: '/query' })
  } else if (to.path === '/login' && userStore.isLoggedIn) {
    // 已登录用户访问登录页，跳转到首页
    next({ path: '/' })
  } else {
    next()
  }
})

export default router

// 导出路由元数据类型
declare module 'vue-router' {
  interface RouteMeta {
    title?: string
    icon?: string
    requiresAuth?: boolean
    requiresAdmin?: boolean
  }
}
