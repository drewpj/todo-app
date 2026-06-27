<template>
  <div class="task-item" :class="statusClass">
    <div class="task-main">
      <span class="task-title">{{ task.title }}</span>
      <span class="task-badges">
        <span class="badge status">{{ statusLabel }}</span>
        <span class="badge priority" :class="priorityClass">{{ task.priority }}</span>
      </span>
    </div>
    <div class="task-meta">
      <span v-if="task.description" class="task-desc">{{ task.description }}</span>
      <span v-if="task.dueDate" class="task-due" :class="{ overdue: isOverdue }">
        Due {{ formatDate(task.dueDate) }}
      </span>
    </div>
    <div class="task-actions">
      <button class="btn-secondary" @click="emit('edit')">Edit</button>
      <button class="btn-danger" @click="emit('delete')">Delete</button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import type { Task } from '../types/api'

const props = defineProps<{ task: Task }>()
const emit = defineEmits<{ edit: []; delete: [] }>()

const statusLabel = computed(() => {
  const map: Record<string, string> = { Todo: 'To Do', InProgress: 'In Progress', Done: 'Done' }
  return map[props.task.status] ?? props.task.status
})

const statusClass = computed(() => `status-${props.task.status.toLowerCase()}`)
const priorityClass = computed(() => `priority-${props.task.priority.toLowerCase()}`)

const isOverdue = computed(() => {
  if (!props.task.dueDate) return false
  return new Date(props.task.dueDate) < new Date() && props.task.status !== 'Done'
})

function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' })
}
</script>

<style scoped>
.task-item {
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius);
  padding: 0.875rem 1rem;
  display: flex;
  flex-direction: column;
  gap: 0.375rem;
}
.task-item.status-done { opacity: 0.65; }

.task-main { display: flex; justify-content: space-between; align-items: flex-start; gap: 0.5rem; }
.task-title { font-weight: 500; font-size: 0.9375rem; }
.task-badges { display: flex; gap: 0.375rem; flex-shrink: 0; }

.badge {
  font-size: 0.75rem;
  padding: 0.15rem 0.5rem;
  border-radius: 999px;
  font-weight: 500;
}
.status { background: #e0e7ff; color: #3730a3; }
.priority-low { background: #d1fae5; color: #065f46; }
.priority-medium { background: #fef3c7; color: #92400e; }
.priority-high { background: #fee2e2; color: #991b1b; }

.task-meta { display: flex; gap: 1rem; font-size: 0.8125rem; color: var(--color-text-muted); flex-wrap: wrap; }
.task-desc { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; max-width: 400px; }
.task-due.overdue { color: var(--color-danger); font-weight: 500; }

.task-actions { display: flex; gap: 0.5rem; justify-content: flex-end; margin-top: 0.25rem; }
</style>
