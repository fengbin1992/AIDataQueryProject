<template>
  <div class="history-view">
    <el-card class="page-header">
      <div class="header-content">
        <h2>查询历史</h2>
      </div>
    </el-card>

    <!-- 筛选条件 -->
    <el-card class="filter-card">
      <el-form :inline="true" :model="queryParams">
        <el-form-item label="平台">
          <el-select v-model="queryParams.platformCode" placeholder="全部" clearable style="width: 280px">
            <el-option
              v-for="platform in platforms"
              :key="platform.code"
              :label="platform.name"
              :value="platform.code"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="状态">
          <el-select v-model="queryParams.status" placeholder="全部" clearable style="width: 120px">
            <el-option label="成功" :value="1" />
            <el-option label="失败" :value="0" />
          </el-select>
        </el-form-item>
        <el-form-item label="时间范围">
          <el-date-picker
            v-model="dateRange"
            type="daterange"
            range-separator="至"
            start-placeholder="开始日期"
            end-placeholder="结束日期"
            value-format="YYYY-MM-DD"
            style="width: 260px"
          />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" :icon="Search" @click="handleSearch">查询</el-button>
          <el-button :icon="RefreshLeft" @click="handleReset">重置</el-button>
        </el-form-item>
      </el-form>
    </el-card>

    <!-- 查询历史列表 -->
    <el-card class="list-card">
      <el-table
        :data="logs"
        stripe
        v-loading="loading"
        @row-click="handleRowClick"
      >
        <el-table-column type="expand">
          <template #default="{ row }">
            <div class="expand-content">
              <div class="sql-label">SQL 语句:</div>
              <pre class="sql-content">{{ row.sqlContent }}</pre>
              <div class="expand-actions">
                <el-button size="small" :icon="CopyDocument" @click.stop="handleCopySql(row)">
                  复制 SQL
                </el-button>
                <el-button size="small" type="primary" :icon="CaretRight" @click.stop="handleReExecute(row)">
                  重新执行
                </el-button>
              </div>
            </div>
          </template>
        </el-table-column>
        <el-table-column prop="createdAt" label="执行时间" width="170">
          <template #default="{ row }">
            {{ formatDateTime(row.createdAt) }}
          </template>
        </el-table-column>
        <el-table-column v-if="userStore.isAdmin" prop="username" label="用户" width="100" />
        <el-table-column prop="platformCode" label="平台" min-width="200" show-overflow-tooltip>
          <template #default="{ row }">
            {{ getPlatformName(row.platformCode) }}
          </template>
        </el-table-column>
        <el-table-column prop="databaseName" label="数据库" width="180" show-overflow-tooltip />
        <el-table-column prop="executionTimeMs" label="耗时" width="100" align="right">
          <template #default="{ row }">
            {{ formatDuration(row.executionTimeMs) }}
          </template>
        </el-table-column>
        <el-table-column prop="rowCount" label="行数" width="80" align="right">
          <template #default="{ row }">
            {{ formatNumber(row.rowCount) }}
          </template>
        </el-table-column>
        <el-table-column prop="status" label="状态" width="80" align="center">
          <template #default="{ row }">
            <el-tag :type="row.status === 1 ? 'success' : 'danger'" size="small">
              {{ row.status === 1 ? '成功' : '失败' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="sqlContent" label="SQL 摘要" min-width="200" show-overflow-tooltip>
          <template #default="{ row }">
            <span class="sql-summary">{{ truncateSql(row.sqlContent) }}</span>
          </template>
        </el-table-column>
      </el-table>

      <!-- 分页 -->
      <div class="pagination">
        <el-pagination
          v-model:current-page="queryParams.pageIndex"
          v-model:page-size="queryParams.pageSize"
          :page-sizes="[10, 20, 50, 100]"
          :total="total"
          layout="total, sizes, prev, pager, next, jumper"
          @size-change="handleSearch"
          @current-change="handleSearch"
        />
      </div>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, watch } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { Search, RefreshLeft, CopyDocument, CaretRight } from '@element-plus/icons-vue'
import { useUserStore, useQueryStore } from '@/stores'
import { queryLogApi, platformApi } from '@/services'
import { formatDateTime, formatDuration, formatNumber } from '@/utils'
import type { QueryLogDto, QueryLogParams, PlatformDto } from '@/types'

const router = useRouter()
const userStore = useUserStore()
const queryStore = useQueryStore()

const loading = ref(false)
const logs = ref<QueryLogDto[]>([])
const total = ref(0)
const platforms = ref<PlatformDto[]>([])
const dateRange = ref<[string, string] | null>(null)

const queryParams = ref<QueryLogParams>({
  pageIndex: 1,
  pageSize: 20,
  platformCode: undefined,
  status: undefined,
  startDate: undefined,
  endDate: undefined
})

// 监听日期范围变化
watch(dateRange, (val) => {
  if (val) {
    queryParams.value.startDate = val[0]
    queryParams.value.endDate = val[1]
  } else {
    queryParams.value.startDate = undefined
    queryParams.value.endDate = undefined
  }
})

// 获取平台名称
function getPlatformName(code: string | undefined): string {
  if (!code) return '-'
  const platform = platforms.value.find(p => p.code === code)
  return platform?.name || code
}

// 截断 SQL
function truncateSql(sql: string): string {
  const maxLen = 100
  const trimmed = sql.replace(/\s+/g, ' ').trim()
  return trimmed.length > maxLen ? trimmed.slice(0, maxLen) + '...' : trimmed
}

// 搜索
async function handleSearch() {
  loading.value = true
  try {
    const { data } = userStore.isAdmin
      ? await queryLogApi.getAllLogs(queryParams.value)
      : await queryLogApi.getLogs(queryParams.value)

    if (data.success && data.data) {
      logs.value = data.data.items
      total.value = data.data.totalCount
    }
  } finally {
    loading.value = false
  }
}

// 重置
function handleReset() {
  queryParams.value = {
    pageIndex: 1,
    pageSize: 20,
    platformCode: undefined,
    status: undefined,
    startDate: undefined,
    endDate: undefined
  }
  dateRange.value = null
  handleSearch()
}

// 行点击
function handleRowClick(_row: QueryLogDto) {
  // 展开/收起行详情
}

// 复制 SQL
function handleCopySql(row: QueryLogDto) {
  navigator.clipboard.writeText(row.sqlContent)
  ElMessage.success('SQL 已复制到剪贴板')
}

// 重新执行
function handleReExecute(row: QueryLogDto) {
  queryStore.setSql(row.sqlContent)
  if (row.platformCode) {
    queryStore.selectedPlatformCode = row.platformCode
  }
  router.push('/query')
  ElMessage.success('已加载 SQL，请选择数据库后执行')
}

// 加载平台列表
async function loadPlatforms() {
  try {
    const { data } = await platformApi.getPlatforms()
    if (data.success && data.data) {
      platforms.value = data.data
    }
  } catch {
    // 错误处理
  }
}

onMounted(async () => {
  await loadPlatforms()
  await handleSearch()
})
</script>

<style scoped lang="scss">
.history-view {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.page-header {
  .header-content {
    h2 {
      margin: 0;
      font-size: 18px;
    }
  }
}

.filter-card {
  :deep(.el-form-item) {
    margin-bottom: 0;
  }
}

.list-card {
  flex: 1;
}

.expand-content {
  padding: 16px;
  background-color: var(--el-fill-color-light);
  border-radius: 4px;

  .sql-label {
    font-size: 12px;
    color: var(--el-text-color-secondary);
    margin-bottom: 8px;
  }

  .sql-content {
    background-color: var(--el-bg-color);
    padding: 12px;
    border-radius: 4px;
    white-space: pre-wrap;
    word-break: break-all;
    font-family: 'Consolas', 'Monaco', monospace;
    font-size: 13px;
    max-height: 200px;
    overflow: auto;
    margin-bottom: 12px;
  }

  .expand-actions {
    display: flex;
    gap: 8px;
  }
}

.sql-summary {
  font-family: 'Consolas', 'Monaco', monospace;
  font-size: 12px;
  color: var(--el-text-color-secondary);
}

.pagination {
  display: flex;
  justify-content: flex-end;
  margin-top: 16px;
}
</style>
