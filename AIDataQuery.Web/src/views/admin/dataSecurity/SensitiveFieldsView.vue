<template>
  <div class="sensitive-fields-view">
    <div class="page-header">
      <h3>敏感字段标记</h3>
    </div>

    <div class="page-content">
      <div class="filter-bar">
        <el-select
          v-model="selectedConnectionId"
          placeholder="选择数据库连接"
          clearable
          style="width: 250px"
          @change="handleConnectionChange"
        >
          <el-option
            v-for="conn in connections"
            :key="conn.id"
            :label="conn.name"
            :value="conn.id"
          />
        </el-select>
        <el-select
          v-model="selectedTable"
          placeholder="选择表"
          clearable
          filterable
          style="width: 200px"
          @change="handleTableChange"
          :disabled="!selectedConnectionId"
        >
          <el-option v-for="table in tables" :key="table.name" :label="table.name" :value="table.name" />
        </el-select>
        <el-button @click="refreshSchema" :loading="store.loadingSchema" :disabled="!selectedTable">
          <el-icon><Refresh /></el-icon>
          刷新表结构
        </el-button>
      </div>

      <!-- 表结构视图 -->
      <div class="schema-section" v-if="store.tableSchema">
        <div class="schema-header">
          <h4>表 {{ store.tableSchema.tableName }} 的字段</h4>
          <el-input
            v-model="fieldSearchKeyword"
            placeholder="搜索字段名"
            clearable
            style="width: 200px; margin-left: 16px;"
            :prefix-icon="Search"
          />
        </div>
        <el-table :data="filteredFields" border stripe>
          <el-table-column type="selection" width="50" />
          <el-table-column prop="name" label="字段名" width="200" />
          <el-table-column prop="dataType" label="数据类型" width="120" />
          <el-table-column label="敏感识别" width="150">
            <template #default="{ row }">
              <template v-if="row.isSensitive">
                <el-tag type="warning" size="small">
                  {{ row.isManuallyMarked ? '手动标记' : row.matchedRule }}
                </el-tag>
              </template>
              <span v-else class="text-muted">-</span>
            </template>
          </el-table-column>
          <el-table-column label="脱敏类型" width="150">
            <template #default="{ row }">
              <el-select
                v-model="fieldMaskTypes[row.name]"
                placeholder="选择脱敏类型"
                size="small"
                clearable
                style="width: 100%"
              >
                <el-option
                  v-for="option in MaskTypeOptions"
                  :key="option.value"
                  :label="option.label"
                  :value="option.value"
                />
              </el-select>
            </template>
          </el-table-column>
          <el-table-column label="操作" width="100">
            <template #default="{ row }">
              <el-button
                v-if="!row.isManuallyMarked && fieldMaskTypes[row.name]"
                text
                type="primary"
                size="small"
                @click="markField(row.name)"
              >
                标记
              </el-button>
              <el-button
                v-else-if="row.isManuallyMarked"
                text
                type="danger"
                size="small"
                @click="unmarkField(row.name)"
              >
                取消
              </el-button>
            </template>
          </el-table-column>
        </el-table>

        <div class="batch-actions">
          <el-button type="primary" @click="batchMark" :disabled="!hasFieldsToMark">
            批量标记选中字段
          </el-button>
        </div>
      </div>

      <!-- 已标记的敏感字段列表 -->
      <div class="marks-section">
        <h4>已标记的敏感字段</h4>
        <el-table :data="store.fieldMarks" v-loading="store.loadingMarks" border stripe>
          <el-table-column prop="connectionName" label="数据库连接" width="150" />
          <el-table-column prop="tableName" label="表名" width="150" />
          <el-table-column prop="fieldName" label="字段名" width="150" />
          <el-table-column label="脱敏类型" width="100">
            <template #default="{ row }">
              <el-tag size="small">{{ getMaskTypeName(row.maskType) }}</el-tag>
            </template>
          </el-table-column>
          <el-table-column prop="description" label="说明" min-width="150" show-overflow-tooltip />
          <el-table-column prop="markedByName" label="标记人" width="100" />
          <el-table-column prop="createdAt" label="标记时间" width="160">
            <template #default="{ row }">
              {{ formatDate(row.createdAt) }}
            </template>
          </el-table-column>
          <el-table-column label="操作" width="80" fixed="right">
            <template #default="{ row }">
              <el-popconfirm title="确定取消该字段的敏感标记吗？" @confirm="handleDeleteMark(row.id)">
                <template #reference>
                  <el-button text type="danger" size="small">删除</el-button>
                </template>
              </el-popconfirm>
            </template>
          </el-table-column>
        </el-table>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { Refresh, Search } from '@element-plus/icons-vue'
import { useDataSecurityStore } from '@/stores/dataSecurity'
import { MaskType, MaskTypeOptions } from '@/types/dataSecurity'
import { platformApi } from '@/services/platform'
import { queryApi } from '@/services/query'

const store = useDataSecurityStore()

const selectedConnectionId = ref<number | null>(null)
const selectedTable = ref('')
const connections = ref<any[]>([])
const tables = ref<any[]>([])
const fieldMaskTypes = reactive<Record<string, MaskType | undefined>>({})
const fieldSearchKeyword = ref('')

// 过滤后的字段列表
const filteredFields = computed(() => {
  if (!store.tableSchema) return []
  if (!fieldSearchKeyword.value) return store.tableSchema.fields
  const keyword = fieldSearchKeyword.value.toLowerCase()
  return store.tableSchema.fields.filter(f =>
    f.name.toLowerCase().includes(keyword)
  )
})

const hasFieldsToMark = computed(() => {
  if (!store.tableSchema) return false
  return store.tableSchema.fields.some(f =>
    !f.isManuallyMarked && fieldMaskTypes[f.name]
  )
})

function getMaskTypeName(type: MaskType) {
  return MaskTypeOptions.find(o => o.value === type)?.label || '未知'
}

function formatDate(dateStr: string) {
  return new Date(dateStr).toLocaleString('zh-CN')
}

async function loadConnections() {
  try {
    const { data } = await platformApi.getAllConnections()
    if (data.success && data.data) {
      // 只显示正式环境的数据库连接
      connections.value = data.data.filter((c: any) => c.isProduction)
    }
  } catch (error) {
    console.error('Failed to load connections:', error)
  }
}

async function handleConnectionChange() {
  selectedTable.value = ''
  tables.value = []
  store.tableSchema = null
  fieldSearchKeyword.value = ''
  Object.keys(fieldMaskTypes).forEach(key => delete fieldMaskTypes[key])

  if (selectedConnectionId.value) {
    try {
      const { data } = await queryApi.getTables(selectedConnectionId.value)
      if (data.success && data.data) {
        tables.value = data.data
      }
    } catch (error) {
      console.error('Failed to load tables:', error)
    }

    await store.loadFieldMarks(selectedConnectionId.value)
  }
}

async function handleTableChange() {
  fieldSearchKeyword.value = ''
  if (selectedConnectionId.value && selectedTable.value) {
    await refreshSchema()
  }
}

async function refreshSchema() {
  if (!selectedConnectionId.value || !selectedTable.value) return
  Object.keys(fieldMaskTypes).forEach(key => delete fieldMaskTypes[key])
  await store.loadTableSchema(selectedConnectionId.value, selectedTable.value)

  // 预填充已识别的脱敏类型
  if (store.tableSchema) {
    store.tableSchema.fields.forEach(field => {
      if (field.isSensitive && field.maskType) {
        fieldMaskTypes[field.name] = field.maskType
      }
    })
  }
}

async function markField(fieldName: string) {
  if (!selectedConnectionId.value || !selectedTable.value) return
  const maskType = fieldMaskTypes[fieldName]
  if (!maskType) return

  const success = await store.createFieldMark({
    connectionId: selectedConnectionId.value,
    tableName: selectedTable.value,
    fieldName,
    maskType
  })

  if (success) {
    await refreshSchema()
  }
}

async function unmarkField(fieldName: string) {
  const mark = store.fieldMarks.find(m =>
    m.connectionId === selectedConnectionId.value &&
    m.tableName === selectedTable.value &&
    m.fieldName === fieldName
  )
  if (mark) {
    const success = await store.deleteFieldMark(mark.id)
    if (success) {
      await refreshSchema()
    }
  }
}

async function batchMark() {
  if (!selectedConnectionId.value || !selectedTable.value || !store.tableSchema) return

  const fields = store.tableSchema.fields
    .filter(f => !f.isManuallyMarked && fieldMaskTypes[f.name])
    .map(f => ({
      tableName: selectedTable.value,
      fieldName: f.name,
      maskType: fieldMaskTypes[f.name]!
    }))

  if (fields.length === 0) return

  const success = await store.batchCreateFieldMarks({
    connectionId: selectedConnectionId.value,
    fields
  })

  if (success) {
    await refreshSchema()
  }
}

async function handleDeleteMark(id: number) {
  const success = await store.deleteFieldMark(id)
  if (success && selectedConnectionId.value && selectedTable.value) {
    await refreshSchema()
  }
}

onMounted(async () => {
  await loadConnections()
  await store.loadFieldMarks()
})
</script>

<style scoped lang="scss">
.sensitive-fields-view {
  padding: 20px;
  height: 100%;
  display: flex;
  flex-direction: column;
}

.page-header {
  margin-bottom: 20px;

  h3 {
    margin: 0;
    font-weight: 500;
  }
}

.page-content {
  flex: 1;
  overflow: auto;
}

.filter-bar {
  display: flex;
  gap: 12px;
  margin-bottom: 20px;
}

.schema-section,
.marks-section {
  margin-bottom: 24px;

  h4 {
    margin: 0;
    font-weight: 500;
    font-size: 14px;
  }
}

.schema-header {
  display: flex;
  justify-content: flex-start;
  align-items: center;
  margin-bottom: 12px;
}

.batch-actions {
  margin-top: 12px;
  display: flex;
  justify-content: flex-end;
}

.text-muted {
  color: var(--el-text-color-secondary);
}
</style>
