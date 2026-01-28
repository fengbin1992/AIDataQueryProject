<template>
  <div class="sql-editor-container">
    <div ref="editorContainer" class="editor-wrapper"></div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onBeforeUnmount, watch } from 'vue'
import * as monaco from 'monaco-editor'
import { useThemeStore } from '@/stores'

const props = withDefaults(defineProps<{
  modelValue?: string
  readonly?: boolean
  language?: string
  tables?: Array<{ name: string; comment?: string }>
  columns?: Array<{ name: string; type: string; comment?: string; tableName?: string }>
}>(), {
  modelValue: '',
  readonly: false,
  language: 'sql',
  tables: () => [],
  columns: () => []
})

const emit = defineEmits<{
  (e: 'update:modelValue', value: string): void
  (e: 'execute'): void
  (e: 'executeSelected'): void
  (e: 'format'): void
}>()

const themeStore = useThemeStore()
const editorContainer = ref<HTMLElement>()
let editor: monaco.editor.IStandaloneCodeEditor | null = null
let completionProvider: monaco.IDisposable | null = null

// SQL 关键字
const sqlKeywords = [
  'SELECT', 'FROM', 'WHERE', 'AND', 'OR', 'NOT', 'IN', 'BETWEEN', 'LIKE', 'IS', 'NULL',
  'ORDER BY', 'GROUP BY', 'HAVING', 'LIMIT', 'OFFSET', 'TOP',
  'JOIN', 'INNER JOIN', 'LEFT JOIN', 'RIGHT JOIN', 'FULL JOIN', 'CROSS JOIN', 'ON',
  'UNION', 'UNION ALL', 'EXCEPT', 'INTERSECT',
  'INSERT', 'INTO', 'VALUES', 'UPDATE', 'SET', 'DELETE',
  'CREATE', 'ALTER', 'DROP', 'TABLE', 'INDEX', 'VIEW', 'DATABASE',
  'AS', 'DISTINCT', 'ALL', 'EXISTS', 'CASE', 'WHEN', 'THEN', 'ELSE', 'END',
  'ASC', 'DESC', 'NULLS FIRST', 'NULLS LAST',
  'WITH', 'RECURSIVE', 'CTE'
]

// SQL 函数
const sqlFunctions = [
  { name: 'COUNT', detail: 'COUNT(expression) - 统计行数' },
  { name: 'SUM', detail: 'SUM(expression) - 求和' },
  { name: 'AVG', detail: 'AVG(expression) - 平均值' },
  { name: 'MAX', detail: 'MAX(expression) - 最大值' },
  { name: 'MIN', detail: 'MIN(expression) - 最小值' },
  { name: 'COALESCE', detail: 'COALESCE(val1, val2, ...) - 返回第一个非NULL值' },
  { name: 'ISNULL', detail: 'ISNULL(expression, replacement) - NULL替换' },
  { name: 'NULLIF', detail: 'NULLIF(expr1, expr2) - 相等返回NULL' },
  { name: 'CAST', detail: 'CAST(expression AS type) - 类型转换' },
  { name: 'CONVERT', detail: 'CONVERT(type, expression) - 类型转换' },
  { name: 'LEN', detail: 'LEN(string) - 字符串长度' },
  { name: 'LENGTH', detail: 'LENGTH(string) - 字符串长度' },
  { name: 'SUBSTRING', detail: 'SUBSTRING(string, start, length) - 截取字符串' },
  { name: 'LEFT', detail: 'LEFT(string, length) - 左截取' },
  { name: 'RIGHT', detail: 'RIGHT(string, length) - 右截取' },
  { name: 'TRIM', detail: 'TRIM(string) - 去除空格' },
  { name: 'LTRIM', detail: 'LTRIM(string) - 去除左侧空格' },
  { name: 'RTRIM', detail: 'RTRIM(string) - 去除右侧空格' },
  { name: 'UPPER', detail: 'UPPER(string) - 转大写' },
  { name: 'LOWER', detail: 'LOWER(string) - 转小写' },
  { name: 'REPLACE', detail: 'REPLACE(string, old, new) - 替换字符串' },
  { name: 'CHARINDEX', detail: 'CHARINDEX(substring, string) - 查找位置' },
  { name: 'CONCAT', detail: 'CONCAT(str1, str2, ...) - 连接字符串' },
  { name: 'GETDATE', detail: 'GETDATE() - 当前日期时间' },
  { name: 'GETUTCDATE', detail: 'GETUTCDATE() - UTC日期时间' },
  { name: 'DATEADD', detail: 'DATEADD(part, number, date) - 日期加减' },
  { name: 'DATEDIFF', detail: 'DATEDIFF(part, start, end) - 日期差值' },
  { name: 'DATEPART', detail: 'DATEPART(part, date) - 提取日期部分' },
  { name: 'YEAR', detail: 'YEAR(date) - 提取年份' },
  { name: 'MONTH', detail: 'MONTH(date) - 提取月份' },
  { name: 'DAY', detail: 'DAY(date) - 提取日期' },
  { name: 'FORMAT', detail: 'FORMAT(value, format) - 格式化' },
  { name: 'ROW_NUMBER', detail: 'ROW_NUMBER() OVER(...) - 行号' },
  { name: 'RANK', detail: 'RANK() OVER(...) - 排名(有并列)' },
  { name: 'DENSE_RANK', detail: 'DENSE_RANK() OVER(...) - 密集排名' },
  { name: 'OVER', detail: 'OVER(PARTITION BY ... ORDER BY ...) - 窗口函数' },
  { name: 'PARTITION BY', detail: '分区子句' },
  { name: 'IIF', detail: 'IIF(condition, true_val, false_val) - 条件判断' },
  { name: 'STUFF', detail: 'STUFF(string, start, length, replacement) - 替换' },
  { name: 'STRING_AGG', detail: 'STRING_AGG(expression, separator) - 字符串聚合' }
]

// 代码片段
const sqlSnippets = [
  {
    label: 'sel',
    insertText: 'SELECT ${1:*} FROM ${2:table_name} WHERE ${3:1=1}',
    detail: 'SELECT 查询模板'
  },
  {
    label: 'selj',
    insertText: 'SELECT ${1:a.*}\nFROM ${2:table1} a\nINNER JOIN ${3:table2} b ON a.${4:id} = b.${5:id}\nWHERE ${6:1=1}',
    detail: 'JOIN 查询模板'
  },
  {
    label: 'selt',
    insertText: 'SELECT TOP ${1:20} ${2:*} FROM ${3:table_name} WHERE ${4:1=1}',
    detail: 'SELECT TOP 查询模板'
  },
  {
    label: 'cnt',
    insertText: 'SELECT COUNT(*) FROM ${1:table_name} WHERE ${2:1=1}',
    detail: 'COUNT 统计模板'
  },
  {
    label: 'grp',
    insertText: 'SELECT ${1:column}, COUNT(*) AS cnt\nFROM ${2:table_name}\nWHERE ${3:1=1}\nGROUP BY ${1:column}\nORDER BY cnt DESC',
    detail: 'GROUP BY 模板'
  },
  {
    label: 'cte',
    insertText: 'WITH ${1:cte_name} AS (\n    SELECT ${2:*}\n    FROM ${3:table_name}\n    WHERE ${4:1=1}\n)\nSELECT * FROM ${1:cte_name}',
    detail: 'CTE 公用表表达式模板'
  },
  {
    label: 'case',
    insertText: 'CASE\n    WHEN ${1:condition} THEN ${2:result}\n    ELSE ${3:default}\nEND',
    detail: 'CASE WHEN 模板'
  },
  {
    label: 'page',
    insertText: 'SELECT *\nFROM ${1:table_name}\nORDER BY ${2:id}\nOFFSET ${3:0} ROWS\nFETCH NEXT ${4:10} ROWS ONLY',
    detail: '分页查询模板 (SQL Server 2012+)'
  }
]

// 注册自动补全提供者
function registerCompletionProvider() {
  completionProvider = monaco.languages.registerCompletionItemProvider('sql', {
    triggerCharacters: ['.', ' ', '('],
    provideCompletionItems: (model, position) => {
      const word = model.getWordUntilPosition(position)
      const range = {
        startLineNumber: position.lineNumber,
        endLineNumber: position.lineNumber,
        startColumn: word.startColumn,
        endColumn: word.endColumn
      }

      const suggestions: monaco.languages.CompletionItem[] = []

      // 添加 SQL 关键字
      sqlKeywords.forEach(keyword => {
        suggestions.push({
          label: keyword,
          kind: monaco.languages.CompletionItemKind.Keyword,
          insertText: keyword,
          range,
          detail: 'SQL 关键字'
        })
      })

      // 添加 SQL 函数
      sqlFunctions.forEach(func => {
        suggestions.push({
          label: func.name,
          kind: monaco.languages.CompletionItemKind.Function,
          insertText: func.name + '($0)',
          insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
          range,
          detail: func.detail
        })
      })

      // 添加代码片段
      sqlSnippets.forEach(snippet => {
        suggestions.push({
          label: snippet.label,
          kind: monaco.languages.CompletionItemKind.Snippet,
          insertText: snippet.insertText,
          insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
          range,
          detail: snippet.detail,
          documentation: snippet.insertText
        })
      })

      // 添加表名提示
      props.tables.forEach(table => {
        suggestions.push({
          label: table.name,
          kind: monaco.languages.CompletionItemKind.Class,
          insertText: table.name,
          range,
          detail: table.comment || '表',
          documentation: table.comment
        })
      })

      // 添加列名提示
      props.columns.forEach(col => {
        suggestions.push({
          label: col.name,
          kind: monaco.languages.CompletionItemKind.Field,
          insertText: col.name,
          range,
          detail: `${col.type}${col.tableName ? ' - ' + col.tableName : ''}`,
          documentation: col.comment
        })
      })

      return { suggestions }
    }
  })
}

// 初始化编辑器
function initEditor() {
  if (!editorContainer.value) return

  // 注册补全提供者
  registerCompletionProvider()

  editor = monaco.editor.create(editorContainer.value, {
    value: props.modelValue,
    language: props.language,
    theme: themeStore.isDark ? 'vs-dark' : 'vs',
    automaticLayout: true,
    minimap: { enabled: false },
    fontSize: 14,
    lineNumbers: 'on',
    scrollBeyondLastLine: false,
    wordWrap: 'on',
    tabSize: 4,
    insertSpaces: true,
    readOnly: props.readonly,
    renderWhitespace: 'selection',
    folding: true,
    foldingStrategy: 'indentation',
    suggestOnTriggerCharacters: true,
    quickSuggestions: {
      other: true,
      comments: false,
      strings: true
    },
    acceptSuggestionOnEnter: 'on',
    tabCompletion: 'on',
    wordBasedSuggestions: 'currentDocument',
    suggest: {
      showKeywords: true,
      showSnippets: true,
      showFunctions: true,
      showClasses: true,
      showFields: true,
      insertMode: 'replace',
      filterGraceful: true,
      snippetsPreventQuickSuggestions: false
    },
    scrollbar: {
      verticalScrollbarSize: 8,
      horizontalScrollbarSize: 8
    }
  })

  // 监听内容变化
  editor.onDidChangeModelContent(() => {
    const value = editor?.getValue() || ''
    emit('update:modelValue', value)
  })

  // 注册快捷键 F5 执行查询
  editor.addAction({
    id: 'execute-query',
    label: 'Execute Query',
    keybindings: [monaco.KeyCode.F5],
    run: () => {
      emit('execute')
    }
  })

  // Ctrl+Enter 执行选中的 SQL
  editor.addAction({
    id: 'execute-selected-query',
    label: 'Execute Selected Query',
    keybindings: [monaco.KeyMod.CtrlCmd | monaco.KeyCode.Enter],
    run: () => {
      emit('executeSelected')
    }
  })

  // Shift+Alt+F 格式化
  editor.addAction({
    id: 'format-sql',
    label: 'Format SQL',
    keybindings: [monaco.KeyMod.Shift | monaco.KeyMod.Alt | monaco.KeyCode.KeyF],
    run: () => {
      emit('format')
    }
  })
}

// 监听主题变化
watch(() => themeStore.isDark, (isDark) => {
  if (editor) {
    monaco.editor.setTheme(isDark ? 'vs-dark' : 'vs')
  }
})

// 监听外部值变化
watch(() => props.modelValue, (newValue) => {
  if (editor && editor.getValue() !== newValue) {
    editor.setValue(newValue)
  }
})

// 暴露方法
function getValue(): string {
  return editor?.getValue() || ''
}

function setValue(value: string): void {
  editor?.setValue(value)
}

function focus(): void {
  editor?.focus()
}

function getSelectedText(): string {
  const selection = editor?.getSelection()
  if (selection) {
    return editor?.getModel()?.getValueInRange(selection) || ''
  }
  return ''
}

function undo(): void {
  if (editor) {
    editor.focus()
    editor.trigger('keyboard', 'undo', null)
  }
}

function redo(): void {
  if (editor) {
    editor.focus()
    editor.trigger('keyboard', 'redo', null)
  }
}

function format(): void {
  editor?.getAction('editor.action.formatDocument')?.run()
}

// 获取编辑器实例（供高级操作）
function getEditor(): monaco.editor.IStandaloneCodeEditor | null {
  return editor
}

defineExpose({
  getValue,
  setValue,
  focus,
  getSelectedText,
  undo,
  redo,
  format,
  getEditor
})

onMounted(() => {
  initEditor()
})

onBeforeUnmount(() => {
  completionProvider?.dispose()
  editor?.dispose()
})
</script>

<style scoped lang="scss">
.sql-editor-container {
  width: 100%;
  height: 100%;
  border: 1px solid var(--el-border-color);
  border-radius: 4px;
  overflow: hidden;

  .editor-wrapper {
    width: 100%;
    height: 100%;
    min-height: 200px;
  }
}
</style>
