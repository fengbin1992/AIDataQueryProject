<template>
  <div class="query-result-container">
    <!-- 工具栏 -->
    <div class="result-toolbar">
      <div class="result-info">
        <span v-if="result">
          共 <strong>{{ formatNumber(result.totalRows) }}</strong> 条记录
          | 耗时 <strong>{{ formatDuration(result.executionTimeMs) }}</strong>
        </span>
        <span v-else class="no-data">暂无数据</span>
      </div>
      <div class="result-actions" v-if="result && result.rows.length > 0">
        <el-dropdown trigger="click" @command="handleExport">
          <el-button size="small" :icon="Download">
            导出 <el-icon class="el-icon--right"><ArrowDown /></el-icon>
          </el-button>
          <template #dropdown>
            <el-dropdown-menu>
              <el-dropdown-item command="csv">CSV (.csv)</el-dropdown-item>
              <el-dropdown-item command="excel">Excel (.xlsx)</el-dropdown-item>
              <el-dropdown-item command="json">JSON (.json)</el-dropdown-item>
              <el-dropdown-item command="text">文本 (.txt)</el-dropdown-item>
              <el-dropdown-item command="sql">SQL INSERT (.sql)</el-dropdown-item>
            </el-dropdown-menu>
          </template>
        </el-dropdown>
      </div>
    </div>

    <!-- 数据表格 -->
    <el-table
      v-if="result && result.columns.length > 0"
      :data="result.rows"
      stripe
      border
      highlight-current-row
      :height="tableHeight"
      style="width: 100%"
    >
      <el-table-column
        v-for="column in result.columns"
        :key="column"
        :prop="column"
        :label="column"
        :min-width="getColumnWidth(column)"
        show-overflow-tooltip
      >
        <template #default="{ row }">
          <span>{{ formatCellValue(row[column]) }}</span>
        </template>
      </el-table-column>
    </el-table>

    <!-- 无数据提示 -->
    <el-empty
      v-else-if="result && result.rows.length === 0"
      description="查询结果为空"
    />

    <!-- 错误提示 -->
    <div v-if="result && !result.success" class="error-message">
      <el-alert
        :title="result.errorMessage || '查询失败'"
        type="error"
        show-icon
        :closable="false"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { Download, ArrowDown } from '@element-plus/icons-vue'
import type { QueryResult } from '@/types'
import { formatNumber, formatDuration } from '@/utils'
import { queryApi } from '@/services'
import { useQueryStore } from '@/stores'
import { saveAs } from 'file-saver'
import { ElMessage } from 'element-plus'

const props = withDefaults(defineProps<{
  result?: QueryResult | null
  height?: number | string
}>(), {
  result: null,
  height: 400
})

const queryStore = useQueryStore()

const tableHeight = computed(() => {
  if (typeof props.height === 'number') {
    return props.height
  }
  return parseInt(props.height) || 400
})

// 格式化单元格值
function formatCellValue(value: unknown): string {
  if (value === null || value === undefined) {
    return '(NULL)'
  }
  if (typeof value === 'boolean') {
    return value ? 'true' : 'false'
  }
  if (value instanceof Date) {
    return value.toLocaleString()
  }
  return String(value)
}

// 获取列宽度
function getColumnWidth(column: string): number {
  const len = column.length
  if (len <= 5) return 100
  if (len <= 10) return 120
  if (len <= 20) return 150
  return 180
}

// 导出
async function handleExport(format: 'csv' | 'excel' | 'json' | 'text' | 'sql') {
  if (!props.result || props.result.rows.length === 0) {
    ElMessage.warning('没有可导出的数据')
    return
  }

  const timestamp = new Date().toISOString().slice(0, 19).replace(/[-:T]/g, '')

  try {
    // JSON、文本、SQL 在前端直接生成
    if (format === 'json') {
      const jsonData = JSON.stringify(props.result.rows, null, 2)
      const blob = new Blob([jsonData], { type: 'application/json;charset=utf-8' })
      saveAs(blob, `export_${timestamp}.json`)
      ElMessage.success('导出成功')
      return
    }

    if (format === 'text') {
      const textData = generateTextFormat(props.result.columns, props.result.rows)
      const blob = new Blob([textData], { type: 'text/plain;charset=utf-8' })
      saveAs(blob, `export_${timestamp}.txt`)
      ElMessage.success('导出成功')
      return
    }

    if (format === 'sql') {
      const sqlData = generateSqlInsert(props.result.columns, props.result.rows)
      const blob = new Blob([sqlData], { type: 'text/plain;charset=utf-8' })
      saveAs(blob, `export_${timestamp}.sql`)
      ElMessage.success('导出成功')
      return
    }

    // CSV 和 Excel 通过后端 API 导出
    if (!queryStore.selectedPlatformCode || !queryStore.selectedConnectionId || !queryStore.sql) {
      // 如果没有查询信息，在前端生成 CSV
      if (format === 'csv') {
        const csvData = generateCsv(props.result.columns, props.result.rows)
        const blob = new Blob(['\ufeff' + csvData], { type: 'text/csv;charset=utf-8' })
        saveAs(blob, `export_${timestamp}.csv`)
        ElMessage.success('导出成功')
        return
      }
      ElMessage.warning('Excel 导出需要先执行查询')
      return
    }

    const response = await queryApi.export({
      platformCode: queryStore.selectedPlatformCode,
      connectionId: queryStore.selectedConnectionId,
      sql: queryStore.sql,
      format
    })

    const blob = new Blob([response.data], {
      type: format === 'excel'
        ? 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'
        : 'text/csv;charset=utf-8'
    })

    const filename = `export_${timestamp}.${format === 'excel' ? 'xlsx' : 'csv'}`
    saveAs(blob, filename)
    ElMessage.success('导出成功')
  } catch {
    ElMessage.error('导出失败')
  }
}

// 生成 CSV 格式
function generateCsv(columns: string[], rows: Record<string, unknown>[]): string {
  const header = columns.map(c => escapeCsvField(c)).join(',')
  const dataRows = rows.map(row =>
    columns.map(col => escapeCsvField(formatExportValue(row[col]))).join(',')
  )
  return [header, ...dataRows].join('\n')
}

// CSV 字段转义
function escapeCsvField(value: string): string {
  if (value.includes(',') || value.includes('"') || value.includes('\n')) {
    return `"${value.replace(/"/g, '""')}"`
  }
  return value
}

// 生成文本格式（制表符分隔）
function generateTextFormat(columns: string[], rows: Record<string, unknown>[]): string {
  const header = columns.join('\t')
  const dataRows = rows.map(row =>
    columns.map(col => formatExportValue(row[col])).join('\t')
  )
  return [header, ...dataRows].join('\n')
}

// 生成 SQL INSERT 语句
function generateSqlInsert(columns: string[], rows: Record<string, unknown>[]): string {
  const tableName = 'table_name' // 占位符表名
  const columnList = columns.map(c => `[${c}]`).join(', ')

  const insertStatements = rows.map(row => {
    const values = columns.map(col => formatSqlValue(row[col])).join(', ')
    return `INSERT INTO ${tableName} (${columnList}) VALUES (${values});`
  })

  return `-- 请将 table_name 替换为实际的表名\n-- Generated at ${new Date().toLocaleString()}\n\n${insertStatements.join('\n')}`
}

// 格式化导出值
function formatExportValue(value: unknown): string {
  if (value === null || value === undefined) {
    return ''
  }
  if (typeof value === 'boolean') {
    return value ? 'true' : 'false'
  }
  return String(value)
}

// 格式化 SQL 值
function formatSqlValue(value: unknown): string {
  if (value === null || value === undefined) {
    return 'NULL'
  }
  if (typeof value === 'number') {
    return String(value)
  }
  if (typeof value === 'boolean') {
    return value ? '1' : '0'
  }
  // 字符串转义单引号
  const strValue = String(value).replace(/'/g, "''")
  return `'${strValue}'`
}
</script>

<style scoped lang="scss">
.query-result-container {
  display: flex;
  flex-direction: column;
  height: 100%;
}

.result-toolbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 8px 0;
  margin-bottom: 8px;
  border-bottom: 1px solid var(--el-border-color-light);

  .result-info {
    font-size: 14px;
    color: var(--el-text-color-secondary);

    strong {
      color: var(--el-color-primary);
    }

    .no-data {
      color: var(--el-text-color-placeholder);
    }
  }

  .result-actions {
    display: flex;
    gap: 8px;
  }
}

.error-message {
  margin-top: 16px;
}

:deep(.el-table) {
  .cell {
    font-family: 'Consolas', 'Monaco', monospace;
    font-size: 13px;
  }
}
</style>
