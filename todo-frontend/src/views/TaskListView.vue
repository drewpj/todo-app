<template>
  <div class="layout">
    <AppHeader />
    <main class="main-content">
      <div class="task-header">
        <h2>My Tasks</h2>
        <button class="btn-primary" @click="openCreate">+ New Task</button>
      </div>
      <TaskFilters :filters="filters" @change="onFilterChange" />
      <div v-if="isLoading" class="status-msg">Loading…</div>
      <div v-else-if="error" class="error-msg">{{ error }}</div>
      <TaskList
        v-else
        :tasks="tasks"
        @edit="openEdit"
        @delete="handleDelete"
      />
    </main>
    <TaskFormModal
      v-if="modalOpen"
      :task="editingTask"
      @close="modalOpen = false"
      @saved="onSaved"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import AppHeader from '../components/AppHeader.vue'
import TaskFilters from '../components/TaskFilters.vue'
import TaskList from '../components/TaskList.vue'
import TaskFormModal from '../components/TaskFormModal.vue'
import { useTasks } from '../composables/useTasks'
import type { Task } from '../types/api'

const { tasks, isLoading, error, fetchTasks, deleteTask } = useTasks()

const filters = reactive({ status: '', sortBy: 'createdAt', sortOrder: 'desc' })
const modalOpen = ref(false)
const editingTask = ref<Task | null>(null)

onMounted(() => fetchTasks(filters))

function onFilterChange(updated: typeof filters) {
  Object.assign(filters, updated)
  fetchTasks(filters)
}

function openCreate() {
  editingTask.value = null
  modalOpen.value = true
}

function openEdit(task: Task) {
  editingTask.value = task
  modalOpen.value = true
}

async function handleDelete(task: Task) {
  if (!confirm(`Delete "${task.title}"?`)) return
  await deleteTask(task.id)
}

function onSaved() {
  modalOpen.value = false
  fetchTasks(filters)
}
</script>

<style scoped>
.layout { min-height: 100vh; display: flex; flex-direction: column; }
.main-content { max-width: 800px; width: 100%; margin: 0 auto; padding: 1.5rem 1rem; flex: 1; }
.task-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 1rem; }
.task-header h2 { font-size: 1.25rem; }
.status-msg { text-align: center; color: var(--color-text-muted); padding: 2rem 0; }
</style>
