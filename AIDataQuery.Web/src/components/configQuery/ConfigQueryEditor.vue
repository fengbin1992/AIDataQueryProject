<template>
  <el-dialog
    v-model="dialogVisible"
    :title="isEdit ? '编辑配置查询' : '新建配置查询'"
    width="900px"
    destroy-on-close
    @open="handleOpen"
  >
    <el-form :model="form" :rules="rules" ref="formRef" label-width="100px">
      <!-- 基本信息 -->
      <el-divider content-position="left">基本信息</el-divider>

      <el-row :gutter="16">
        <el-col :span="12">
          <el-form-item label="名称" prop="name">
            <el-input v-model="form.name" placeholder="请输入配置查询名称" />
          </el-form-item>
        </el-col>
        <el-col :span="12">
          <el-form-item label="默认连接" prop="connectionId">
            <el-select v-model="form.connectionId" placeholder="选择数据库连接" clearable style="width: 100%">
              <el-option
                v-for="conn in connections"
                :key="conn.id"
                :label="conn.name"
                :value="conn.id"
              />
            </el-select>
          </el-form-item>
        </el-col>
      </el-row>

      <el-form-item label="描述" prop="description">
        <el-input v-model="form.description" type="textarea" :rows="2" placeholder="请输入描述说明" />
      </el-form-item>

      <el-form-item label="公开" prop="isPublic" v-if="isAdmin">
        <el-switch v-model="form.isPublic" />
        <span class="form-tip">公开后其他用户可以使用此配置查询</span>
      </el-form-item>

      <!-- SQL 模板 -->
      <el-divider content-position="left">SQL 模板</el-divider>

      <el-form-item label="SQL" prop="sqlContent">
        <el-input
          v-model="form.sqlContent"
          type="textarea"
          :rows="8"
          placeholder="使用 @参数名 作为参数占位符，例如: SELECT * FROM orders WHERE status = @status"
        />
      </el-form-item>

      <div class="sql-actions">
        <el-button size="small" @click="parseParameters">
          <el-icon><Search /></el-icon>
          解析参数
        </el-button>
        <el-button size="small" @click="formatSql">
          <el-icon><MagicStick /></el-icon>
          格式化
        </el-button>
      </div>

      <!-- 参数配置 -->
      <el-divider content-position="left">参数配置</el-divider>

      <div class="config-mode">
        <el-radio-group v-model="configMode" size="small">
          <el-radio-button value="visual">可视化配置</el-radio-button>
          <el-radio-button value="json">JSON 配置</el-radio-button>
        </el-radio-group>
      </div>

      <!-- 可视化配置 -->
      <div class="params-config" v-if="configMode === 'visual'">
        <div v-for="(param, index) in form.parameters" :key="index" class="param-card">
          <div class="param-header">
            <span class="param-name">@{{ param.paramName }}</span>
            <el-button text type="danger" size="small" @click="removeParameter(index)">
              <el-icon><Delete /></el-icon>
            </el-button>
          </div>

          <el-row :gutter="12">
            <el-col :span="8">
              <el-form-item label="显示名称" :prop="`parameters.${index}.paramLabel`" required>
                <el-input v-model="param.paramLabel" placeholder="参数显示名称" />
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item label="类型" :prop="`parameters.${index}.paramType`" required>
                <el-select v-model="param.paramType" style="width: 100%" @change="handleTypeChange(param)">
                  <el-option label="文本输入" value="text" />
                  <el-option label="数字输入" value="number" />
                  <el-option label="日期选择" value="date" />
                  <el-option label="日期范围" value="daterange" />
                  <el-option label="下拉单选" value="select" />
                  <el-option label="下拉多选" value="multiselect" />
                </el-select>
              </el-form-item>
            </el-col>
            <el-col :span="8">
              <el-form-item label="必填">
                <el-switch v-model="param.isRequired" />
              </el-form-item>
            </el-col>
          </el-row>

          <el-row :gutter="12">
            <el-col :span="12">
              <el-form-item label="默认值">
                <el-input v-model="param.defaultValue" placeholder="默认值" />
              </el-form-item>
            </el-col>
            <el-col :span="12">
              <el-form-item label="占位提示">
                <el-input v-model="param.placeholder" placeholder="输入框占位提示" />
              </el-form-item>
            </el-col>
          </el-row>

          <!-- 下拉选项配置 -->
          <template v-if="param.paramType === 'select' || param.paramType === 'multiselect'">
            <el-form-item label="选项来源">
              <el-radio-group v-model="param.optionsConfig!.mode" size="small">
                <el-radio-button value="static">固定选项</el-radio-button>
                <el-radio-button value="dynamic">SQL 查询</el-radio-button>
              </el-radio-group>
            </el-form-item>

            <!-- 固定选项 -->
            <div v-if="param.optionsConfig?.mode === 'static'" class="static-options">
              <div
                v-for="(opt, optIndex) in param.optionsConfig.options"
                :key="optIndex"
                class="option-row"
              >
                <el-input v-model="opt.value" placeholder="值" style="width: 120px" />
                <el-input v-model="opt.label" placeholder="显示文本" style="flex: 1" />
                <el-button text type="danger" @click="removeOption(param, optIndex)">
                  <el-icon><Delete /></el-icon>
                </el-button>
              </div>
              <el-button size="small" @click="addOption(param)">
                <el-icon><Plus /></el-icon>
                添加选项
              </el-button>
            </div>

            <!-- 动态 SQL -->
            <div v-else class="dynamic-options">
              <el-form-item label="数据库连接">
                <el-select v-model="param.optionsConfig!.connectionId" placeholder="选择连接" style="width: 100%">
                  <el-option
                    v-for="conn in connections"
                    :key="conn.id"
                    :label="conn.name"
                    :value="conn.id"
                  />
                </el-select>
              </el-form-item>
              <el-form-item label="SQL 查询">
                <el-input
                  v-model="param.optionsConfig!.sql"
                  type="textarea"
                  :rows="2"
                  placeholder="SELECT id as value, name as label FROM ..."
                />
              </el-form-item>
            </div>
          </template>
        </div>

        <el-button @click="addParameter">
          <el-icon><Plus /></el-icon>
          添加参数
        </el-button>
      </div>

      <!-- JSON 配置 -->
      <div class="json-config" v-else>
        <el-input
          v-model="parametersJson"
          type="textarea"
          :rows="12"
          placeholder="请输入参数配置 JSON"
        />
        <div class="json-actions">
          <el-button size="small" @click="formatParamsJson">格式化</el-button>
          <el-button size="small" @click="convertToVisual">转为可视化配置</el-button>
        </div>
      </div>
    </el-form>

    <template #footer>
      <el-button @click="dialogVisible = false">取消</el-button>
      <el-button type="primary" @click="handleSubmit" :loading="submitting">保存</el-button>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { ElMessage } from 'element-plus'
import type { FormInstance, FormRules } from 'element-plus'
import { Search, MagicStick, Delete, Plus } from '@element-plus/icons-vue'
import { useUserStore } from '@/stores'
import { useConfigQueryStore } from '@/stores/configQuery'
import { configQueryApi } from '@/services/configQuery'
import { platformApi } from '@/services'
import type {
  CreateConfigQueryRequest,
  CreateConfigQueryParameterRequest
} from '@/types/configQuery'

interface ConnectionItem {
  id: number
  name: string
}

const props = defineProps<{
  visible: boolean
  configQueryId: number | null
}>()

const emit = defineEmits<{
  'update:visible': [value: boolean]
  success: []
}>()

const userStore = useUserStore()
const configQueryStore = useConfigQueryStore()

const dialogVisible = computed({
  get: () => props.visible,
  set: (val) => emit('update:visible', val)
})

const isEdit = computed(() => props.configQueryId !== null)
const isAdmin = computed(() => userStore.isAdmin)

const formRef = ref<FormInstance>()
const submitting = ref(false)
const configMode = ref<'visual' | 'json'>('visual')
const connections = ref<ConnectionItem[]>([])
const parametersJson = ref('')

const form = ref<CreateConfigQueryRequest>({
  name: '',
  description: '',
  sqlContent: '',
  connectionId: undefined,
  isPublic: false,
  parameters: []
})

const rules: FormRules = {
  name: [{ required: true, message: '请输入名称', trigger: 'blur' }],
  sqlContent: [{ required: true, message: '请输入 SQL 模板', trigger: 'blur' }]
}

watch(
  () => form.value.parameters,
  (params) => {
    if (configMode.value === 'visual') {
      parametersJson.value = JSON.stringify(params, null, 2)
    }
  },
  { deep: true }
)

async function handleOpen() {
  // 加载连接列表
  await loadConnections()

  if (props.configQueryId) {
    // 编辑模式：加载现有数据
    const { data } = await configQueryApi.getById(props.configQueryId)
    if (data.success && data.data) {
      const query = data.data
      form.value = {
        name: query.name,
        description: query.description || '',
        sqlContent: query.sqlContent,
        connectionId: query.connectionId || undefined,
        isPublic: query.isPublic,
        parameters: query.parameters.map(p => ({
          paramName: p.paramName,
          paramLabel: p.paramLabel,
          paramType: p.paramType,
          isRequired: p.isRequired,
          defaultValue: p.defaultValue || '',
          placeholder: p.placeholder || '',
          optionsConfig: p.optionsConfig || { mode: 'static', options: [] },
          validationRule: p.validationRule || '',
          extraConfig: p.extraConfig || {},
          sortOrder: p.sortOrder
        }))
      }
      parametersJson.value = JSON.stringify(form.value.parameters, null, 2)
    }
  } else {
    // 新建模式：重置表单
    form.value = {
      name: '',
      description: '',
      sqlContent: '',
      connectionId: undefined,
      isPublic: false,
      parameters: []
    }
    parametersJson.value = '[]'
  }
}

async function loadConnections() {
  try {
    // 获取用户有权限的平台列表
    const { data: platformsRes } = await platformApi.getPlatforms()
    if (platformsRes.success && platformsRes.data) {
      // 从每个平台获取连接
      const allConnections: ConnectionItem[] = []
      for (const platform of platformsRes.data) {
        const { data: connRes } = await platformApi.getConnections(platform.code)
        if (connRes.success && connRes.data) {
          for (const conn of connRes.data) {
            allConnections.push({
              id: conn.id,
              name: `${platform.name} - ${conn.name}`
            })
          }
        }
      }
      connections.value = allConnections
    }
  } catch {
    connections.value = []
  }
}

async function parseParameters() {
  const params = await configQueryStore.parseParams(form.value.sqlContent)
  if (params.length === 0) {
    ElMessage.info('未找到参数占位符')
    return
  }

  // 保留已有参数配置，添加新参数
  const existingNames = form.value.parameters.map(p => p.paramName)
  for (const paramName of params) {
    if (!existingNames.includes(paramName)) {
      form.value.parameters.push(createDefaultParameter(paramName))
    }
  }

  ElMessage.success(`解析到 ${params.length} 个参数`)
}

function createDefaultParameter(paramName: string): CreateConfigQueryParameterRequest {
  return {
    paramName,
    paramLabel: paramName,
    paramType: 'text',
    isRequired: true,
    defaultValue: '',
    placeholder: '',
    optionsConfig: { mode: 'static', options: [] },
    validationRule: '',
    extraConfig: {},
    sortOrder: form.value.parameters.length
  }
}

function formatSql() {
  // 简单格式化
  let sql = form.value.sqlContent
  sql = sql.replace(/\s+/g, ' ')
  sql = sql.replace(/\s*(SELECT|FROM|WHERE|AND|OR|ORDER BY|GROUP BY|HAVING|LIMIT|JOIN|LEFT JOIN|RIGHT JOIN|INNER JOIN|ON)\s*/gi,
    (match) => '\n' + match.trim() + ' ')
  form.value.sqlContent = sql.trim()
}

function handleTypeChange(param: CreateConfigQueryParameterRequest) {
  if (param.paramType === 'select' || param.paramType === 'multiselect') {
    if (!param.optionsConfig) {
      param.optionsConfig = { mode: 'static', options: [] }
    }
  }
}

function addParameter() {
  const name = `param${form.value.parameters.length + 1}`
  form.value.parameters.push(createDefaultParameter(name))
}

function removeParameter(index: number) {
  form.value.parameters.splice(index, 1)
}

function addOption(param: CreateConfigQueryParameterRequest) {
  if (!param.optionsConfig) {
    param.optionsConfig = { mode: 'static', options: [] }
  }
  if (!param.optionsConfig.options) {
    param.optionsConfig.options = []
  }
  param.optionsConfig.options.push({ label: '', value: '' })
}

function removeOption(param: CreateConfigQueryParameterRequest, index: number) {
  param.optionsConfig?.options?.splice(index, 1)
}

function formatParamsJson() {
  try {
    const parsed = JSON.parse(parametersJson.value)
    parametersJson.value = JSON.stringify(parsed, null, 2)
  } catch {
    ElMessage.warning('JSON 格式错误')
  }
}

function convertToVisual() {
  try {
    form.value.parameters = JSON.parse(parametersJson.value)
    configMode.value = 'visual'
    ElMessage.success('转换成功')
  } catch {
    ElMessage.error('JSON 格式错误，无法转换')
  }
}

async function handleSubmit() {
  if (!formRef.value) return

  await formRef.value.validate(async (valid) => {
    if (!valid) return

    // 如果是 JSON 模式，先转换
    if (configMode.value === 'json') {
      try {
        form.value.parameters = JSON.parse(parametersJson.value)
      } catch {
        ElMessage.error('参数配置 JSON 格式错误')
        return
      }
    }

    submitting.value = true
    try {
      if (isEdit.value && props.configQueryId) {
        await configQueryStore.update(props.configQueryId, form.value)
        ElMessage.success('更新成功')
      } else {
        await configQueryStore.create(form.value)
        ElMessage.success('创建成功')
      }
      emit('success')
    } catch (error) {
      ElMessage.error((error as Error).message || '操作失败')
    } finally {
      submitting.value = false
    }
  })
}
</script>

<style scoped lang="scss">
.form-tip {
  margin-left: 12px;
  color: var(--el-text-color-secondary);
  font-size: 12px;
}

.sql-actions {
  display: flex;
  gap: 8px;
  margin-bottom: 16px;
}

.config-mode {
  margin-bottom: 16px;
}

.params-config {
  .param-card {
    border: 1px solid var(--el-border-color-light);
    border-radius: 4px;
    padding: 12px;
    margin-bottom: 12px;

    .param-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      margin-bottom: 12px;

      .param-name {
        font-weight: 500;
        color: var(--el-color-primary);
      }
    }

    .static-options {
      .option-row {
        display: flex;
        align-items: center;
        gap: 8px;
        margin-bottom: 8px;
      }
    }
  }
}

.json-config {
  .json-actions {
    display: flex;
    gap: 8px;
    margin-top: 8px;
  }
}
</style>
