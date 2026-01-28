<template>
  <div class="template-tree-container">
    <!-- 搜索框 -->
    <div class="tree-search">
      <el-input
        v-model="searchKeyword"
        placeholder="搜索模板..."
        :prefix-icon="Search"
        clearable
        @input="handleSearch"
      />
    </div>

    <!-- 搜索结果 -->
    <div v-if="searchResults.length > 0" class="search-results">
      <div class="search-title">搜索结果</div>
      <div
        v-for="template in searchResults"
        :key="template.id"
        class="search-item"
        @click="handleSelectTemplate(template)"
      >
        <el-icon><Document /></el-icon>
        <span class="template-name">{{ template.name }}</span>
        <span class="module-name">{{ template.moduleName }}</span>
      </div>
    </div>

    <!-- 模板树 -->
    <el-tree
      v-else
      ref="treeRef"
      :data="treeData"
      :props="defaultProps"
      node-key="id"
      default-expand-all
      highlight-current
      :expand-on-click-node="false"
      @node-click="handleNodeClick"
    >
      <template #default="{ node, data }">
        <span class="tree-node">
          <el-icon v-if="data.type === 'module'" class="icon-module" :class="{ 'is-expanded': node.expanded }">
            <FolderOpened v-if="node.expanded" />
            <Folder v-else />
          </el-icon>
          <el-icon v-else class="icon-template"><Document /></el-icon>
          <span class="node-label">{{ node.label }}</span>
        </span>
      </template>
    </el-tree>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { Search, Document, Folder, FolderOpened } from '@element-plus/icons-vue'
import { useTemplateStore } from '@/stores'
import type { TemplateDto, TemplateModuleDto } from '@/types'

interface TreeNode {
  id: string
  label: string
  type: 'module' | 'template'
  data?: TemplateDto | TemplateModuleDto
  children?: TreeNode[]
}

const emit = defineEmits<{
  (e: 'select', template: TemplateDto): void
}>()

const templateStore = useTemplateStore()
const treeRef = ref()
const searchKeyword = ref('')
const searchResults = ref<TemplateDto[]>([])
const hasLoadAttempted = ref(false)
let searchTimer: ReturnType<typeof setTimeout> | null = null

const defaultProps = {
  children: 'children',
  label: 'label'
}

// 将模块数据转换为树形结构
const treeData = computed<TreeNode[]>(() => {
  return convertModulesToTree(templateStore.modules)
})

function convertModulesToTree(modules: TemplateModuleDto[]): TreeNode[] {
  return modules.map(module => {
    const children: TreeNode[] = []

    // 先添加模板（模板排在子模块前面）
    if (module.templates?.length) {
      children.push(...module.templates.map(template => ({
        id: `template-${template.id}`,
        label: template.name,
        type: 'template' as const,
        data: template
      })))
    }

    // 再添加子模块
    if (module.children?.length) {
      children.push(...convertModulesToTree(module.children))
    }

    return {
      id: `module-${module.id}`,
      label: module.name,
      type: 'module' as const,
      data: module,
      children
    }
  })
}

// 处理节点点击
function handleNodeClick(data: TreeNode) {
  if (data.type === 'template' && data.data) {
    emit('select', data.data as TemplateDto)
  }
}

// 搜索
async function handleSearch() {
  if (searchTimer) {
    clearTimeout(searchTimer)
  }

  if (!searchKeyword.value.trim()) {
    searchResults.value = []
    return
  }

  searchTimer = setTimeout(async () => {
    const results = await templateStore.searchTemplates(searchKeyword.value.trim())
    searchResults.value = results
  }, 300)
}

// 选择搜索结果
function handleSelectTemplate(template: TemplateDto) {
  emit('select', template)
  searchKeyword.value = ''
  searchResults.value = []
}

// 监听模块数据变化，自动加载
watch(() => templateStore.modules, (modules) => {
  if (modules.length === 0 && !hasLoadAttempted.value) {
    hasLoadAttempted.value = true
    templateStore.loadModules()
  }
}, { immediate: true })

onMounted(() => {
  if (templateStore.modules.length === 0 && !hasLoadAttempted.value) {
    hasLoadAttempted.value = true
    templateStore.loadModules()
  }
})
</script>

<style scoped lang="scss">
.template-tree-container {
  height: 100%;
  display: flex;
  flex-direction: column;
}

.tree-search {
  margin-bottom: 12px;
}

.search-results {
  margin-bottom: 12px;

  .search-title {
    font-size: 12px;
    color: var(--el-text-color-secondary);
    margin-bottom: 8px;
  }

  .search-item {
    display: flex;
    align-items: center;
    gap: 8px;
    padding: 8px;
    cursor: pointer;
    border-radius: 4px;
    transition: background-color 0.2s;

    &:hover {
      background-color: var(--el-fill-color-light);
    }

    .template-name {
      flex: 1;
      font-size: 14px;
    }

    .module-name {
      font-size: 12px;
      color: var(--el-text-color-secondary);
    }
  }
}

:deep(.el-tree) {
  flex: 1;
  overflow: auto;
  background-color: transparent;

  .el-tree-node__content {
    height: 32px;
  }
}

.tree-node {
  display: flex;
  align-items: center;
  gap: 6px;

  .icon-module {
    color: #e6a23c;
    font-size: 16px;

    &.is-expanded {
      color: #f5a623;
    }
  }

  .icon-template {
    color: #409eff;
    font-size: 15px;
  }

  .node-label {
    font-size: 14px;
  }
}
</style>
