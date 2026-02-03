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
            <div class="connection-select-wrapper">
              <el-select
                v-model="form.connectionId"
                placeholder="选择数据库连接"
                clearable
                style="width: 100%"
                :class="{ 'production-select': isSelectedProduction }"
              >
                <el-option
                  v-for="conn in connections"
                  :key="conn.id"
                  :label="conn.name"
                  :value="conn.id"
                  :class="{ 'production-option': conn.isProduction }"
                >
                  <div class="connection-option">
                    <span class="conn-name">{{ conn.name }}</span>
                    <el-tag
                      v-if="conn.isProduction"
                      type="danger"
                      size="small"
                      class="env-tag"
                    >
                      正式
                    </el-tag>
                  </div>
                </el-option>
              </el-select>
              <el-tag v-if="isSelectedProduction" type="danger" size="small" class="selected-env-tag">
                正式环境
              </el-tag>
            </div>
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

      <el-form-item label="所属文件夹" prop="folderId">
        <el-select v-model="form.folderId" placeholder="选择文件夹（可选）" clearable style="width: 300px">
          <el-option v-for="folder in folders" :key="folder.id" :label="folder.name" :value="folder.id" />
        </el-select>
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
        <el-empty v-if="form.parameters.length === 0" description="暂无参数，请点击下方按钮添加或使用解析参数自动识别" :image-size="60" />

        <div v-for="(param, index) in form.parameters" :key="index" class="param-card">
          <div class="param-header">
            <div class="param-title">
              <el-tag type="primary" effect="dark" size="small">参数 {{ index + 1 }}</el-tag>
              <code class="param-code">@{{ param.paramName || 'unnamed' }}</code>
            </div>
            <el-button type="danger" text size="small" @click="removeParameter(index)">
              <el-icon><Delete /></el-icon>
              删除
            </el-button>
          </div>

          <div class="param-body">
            <!-- 基础信息行 -->
            <div class="form-row">
              <div class="form-item" style="flex: 1;">
                <label class="form-label required">参数名</label>
                <el-input v-model="param.paramName" placeholder="与SQL中@后的名称一致">
                  <template #prepend>@</template>
                </el-input>
                <div class="form-tip">SQL中使用 @{{ param.paramName || 'paramName' }} 引用</div>
              </div>
              <div class="form-item" style="flex: 1;">
                <label class="form-label required">显示名称</label>
                <el-input v-model="param.paramLabel" placeholder="用户看到的标签名" />
              </div>
              <div class="form-item" style="width: 140px;">
                <label class="form-label required">类型</label>
                <el-select v-model="param.paramType" style="width: 100%" @change="handleTypeChange(param)">
                  <el-option label="文本" value="text" />
                  <el-option label="数字" value="number" />
                  <el-option label="日期" value="date" />
                  <el-option label="日期范围" value="daterange" />
                  <el-option label="下拉单选" value="select" />
                  <el-option label="下拉多选" value="multiselect" />
                </el-select>
              </div>
              <div class="form-item" style="width: 80px;">
                <label class="form-label">必填</label>
                <el-switch v-model="param.isRequired" />
              </div>
            </div>

            <!-- 默认值行 -->
            <div class="form-row">
              <div class="form-item" style="flex: 1;">
                <label class="form-label">默认值</label>
                <el-input v-model="param.defaultValue" placeholder="参数的默认值（可选）" />
              </div>
            </div>

            <!-- 下拉选项配置 -->
            <template v-if="param.paramType === 'select' || param.paramType === 'multiselect'">
              <div class="options-section">
                <div class="section-title">
                  <span>选项配置</span>
                  <el-radio-group v-model="param.optionsConfig!.mode" size="small">
                    <el-radio-button value="static">固定选项</el-radio-button>
                    <el-radio-button value="dynamic">SQL动态</el-radio-button>
                  </el-radio-group>
                </div>

                <!-- 固定选项 -->
                <div v-if="param.optionsConfig?.mode === 'static'" class="static-options">
                  <div v-for="(opt, optIndex) in param.optionsConfig.options" :key="optIndex" class="option-row">
                    <el-input v-model="opt.value" placeholder="值">
                      <template #prepend>值</template>
                    </el-input>
                    <el-input v-model="opt.label" placeholder="显示文本">
                      <template #prepend>文本</template>
                    </el-input>
                    <el-button type="danger" text @click="removeOption(param, optIndex)">
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
                  <div class="form-row">
                    <div class="form-item" style="width: 300px;">
                      <label class="form-label">数据库连接</label>
                      <el-select v-model="param.optionsConfig!.connectionId" placeholder="选择连接" style="width: 100%">
                        <el-option v-for="conn in connections" :key="conn.id" :label="conn.name" :value="conn.id" />
                      </el-select>
                    </div>
                    <div class="form-item" style="flex: 1;">
                      <label class="form-label">SQL 查询</label>
                      <el-input v-model="param.optionsConfig!.sql" placeholder="SELECT id as value, name as label FROM ..." />
                    </div>
                  </div>
                </div>
              </div>
            </template>
          </div>
        </div>

        <el-button type="primary" plain @click="addParameter" class="add-param-btn">
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
  CreateConfigQueryParameterRequest,
  ConfigQueryFolder
} from '@/types/configQuery'

interface ConnectionItem {
  id: number
  name: string
  isProduction: boolean
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

// 是否选中正式环境
const isSelectedProduction = computed(() => {
  if (!form.value.connectionId) return false
  const conn = connections.value.find(c => c.id === form.value.connectionId)
  return conn?.isProduction ?? false
})

const formRef = ref<FormInstance>()
const submitting = ref(false)
const configMode = ref<'visual' | 'json'>('visual')
const connections = ref<ConnectionItem[]>([])
const folders = ref<ConfigQueryFolder[]>([])
const parametersJson = ref('')

const form = ref<CreateConfigQueryRequest>({
  name: '',
  description: '',
  sqlContent: '',
  connectionId: undefined,
  isPublic: false,
  folderId: undefined,
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
  // 加载连接列表和文件夹列表
  await Promise.all([loadConnections(), loadFolders()])

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
        folderId: (query as any).folderId || undefined,
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
      folderId: undefined,
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
              name: `${platform.name} - ${conn.name}`,
              isProduction: conn.isProduction
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

async function loadFolders() {
  try {
    const { data } = await configQueryApi.getFolders()
    if (data.success && data.data) {
      folders.value = data.data
    }
  } catch {
    folders.value = []
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

.connection-select-wrapper {
  display: flex;
  align-items: center;
  gap: 8px;
  width: 100%;

  .el-select {
    flex: 1;
  }

  .selected-env-tag {
    flex-shrink: 0;
  }
}

.connection-option {
  display: flex;
  align-items: center;
  justify-content: space-between;
  width: 100%;

  .conn-name {
    flex: 1;
    overflow: hidden;
    text-overflow: ellipsis;
  }

  .env-tag {
    margin-left: 8px;
    flex-shrink: 0;
  }
}

.production-select {
  :deep(.el-input__wrapper) {
    border-color: var(--el-color-danger);
    box-shadow: 0 0 0 1px var(--el-color-danger) inset;
  }
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
    border: 1px solid var(--el-border-color);
    border-radius: 8px;
    margin-bottom: 16px;
    background-color: var(--el-bg-color);
    box-shadow: 0 1px 3px rgba(0, 0, 0, 0.05);

    .param-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 12px 16px;
      background: linear-gradient(to right, var(--el-fill-color-light), var(--el-bg-color));
      border-bottom: 1px solid var(--el-border-color-lighter);
      border-radius: 8px 8px 0 0;

      .param-title {
        display: flex;
        align-items: center;
        gap: 12px;

        .param-code {
          font-family: 'Monaco', 'Menlo', 'Consolas', monospace;
          font-size: 14px;
          color: var(--el-color-primary);
          background-color: var(--el-color-primary-light-9);
          padding: 2px 8px;
          border-radius: 4px;
        }
      }
    }

    .param-body {
      padding: 20px;

      .form-row {
        display: flex;
        gap: 16px;
        margin-bottom: 16px;

        &:last-child {
          margin-bottom: 0;
        }
      }

      .form-item {
        display: flex;
        flex-direction: column;
        gap: 6px;

        .form-label {
          font-size: 13px;
          color: var(--el-text-color-regular);
          font-weight: 500;

          &.required::before {
            content: '*';
            color: var(--el-color-danger);
            margin-right: 4px;
          }
        }

        .form-tip {
          font-size: 12px;
          color: var(--el-text-color-placeholder);
        }
      }

      .options-section {
        margin-top: 16px;
        padding: 16px;
        background-color: var(--el-fill-color-lighter);
        border-radius: 6px;

        .section-title {
          display: flex;
          align-items: center;
          justify-content: space-between;
          margin-bottom: 16px;
          font-size: 13px;
          font-weight: 500;
          color: var(--el-text-color-secondary);
        }
      }

      .static-options {
        .option-row {
          display: flex;
          align-items: center;
          gap: 12px;
          margin-bottom: 12px;

          .el-input {
            flex: 1;
          }
        }
      }

      .dynamic-options {
        .form-row {
          margin-bottom: 0;
        }
      }
    }
  }

  .add-param-btn {
    width: 100%;
    height: 44px;
    border-style: dashed;
    font-size: 14px;
  }

  .el-empty {
    padding: 30px 0;
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
