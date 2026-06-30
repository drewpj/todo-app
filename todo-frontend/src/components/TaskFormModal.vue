<template>
  <div class="modal-overlay" @click.self="emit('close')">
    <div class="modal" role="dialog" aria-modal="true" :aria-label="task ? 'Edit task' : 'New task'">
      <h2>{{ task ? 'Edit Task' : 'New Task' }}</h2>
      <form @submit.prevent="handleSubmit">
        <div class="form-group">
          <label for="title">Title *</label>
          <input id="title" v-model="form.title" maxlength="200" required />
          <span v-if="fieldErrors.title" class="error-msg">{{ fieldErrors.title }}</span>
        </div>
        <div class="form-group">
          <label for="description">Description</label>
          <textarea id="description" v-model="form.description" rows="3" maxlength="2000" />
        </div>
        <div class="row">
          <div class="form-group">
            <label for="status">Status</label>
            <select id="status" v-model="form.status">
              <option value="Todo">To Do</option>
              <option value="InProgress">In Progress</option>
              <option value="Done">Done</option>
            </select>
          </div>
          <div class="form-group">
            <label for="priority">Priority</label>
            <select id="priority" v-model="form.priority">
              <option value="Low">Low</option>
              <option value="Medium">Medium</option>
              <option value="High">High</option>
            </select>
          </div>
        </div>
        <div class="form-group">
          <label for="dueDate">Due Date</label>
          <input id="dueDate" v-model="form.dueDate" type="date" />
        </div>
        <p v-if="errorMsg" class="error-msg">{{ errorMsg }}</p>
        <div class="modal-actions">
          <button type="button" class="btn-secondary" @click="emit('close')">Cancel</button>
          <button type="submit" class="btn-primary" :disabled="loading">
            {{ loading ? 'Saving…' : 'Save' }}
          </button>
        </div>
      </form>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue'
import { useTasks } from '../composables/useTasks'
import { ApiError } from '../lib/apiClient'
import type { Task, TaskStatus, TaskPriority } from '../types/api'

const props = defineProps<{ task: Task | null }>()
const emit = defineEmits<{ close: []; saved: [] }>()

const { createTask, updateTask } = useTasks()

const form = reactive({
  title: props.task?.title ?? '',
  description: props.task?.description ?? '',
  status: (props.task?.status ?? 'Todo') as TaskStatus,
  priority: (props.task?.priority ?? 'Medium') as TaskPriority,
  dueDate: props.task?.dueDate ? props.task.dueDate.substring(0, 10) : '',
})

const loading = ref(false)
const errorMsg = ref('')
const fieldErrors = reactive<Record<string, string>>({})

async function handleSubmit() {
  errorMsg.value = ''
  Object.keys(fieldErrors).forEach(k => delete fieldErrors[k])
  loading.value = true
  try {
    const request = {
      title: form.title,
      description: form.description || null,
      status: form.status,
      priority: form.priority,
      dueDate: form.dueDate ? new Date(form.dueDate).toISOString() : null,
    }
    if (props.task) {
      await updateTask(props.task.id, request)
    } else {
      await createTask(request)
    }
    emit('saved')
  } catch (e: unknown) {
    if (e instanceof ApiError && e.details?.length) {
      e.details.forEach(d => { fieldErrors[d.field.toLowerCase()] = d.message })
    } else {
      errorMsg.value = e instanceof Error ? e.message : 'Failed to save task'
    }
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.modal-overlay {
  position: fixed; inset: 0;
  background: rgba(0,0,0,0.4);
  display: flex; align-items: center; justify-content: center;
  z-index: 100;
}
.modal {
  background: var(--color-surface);
  border-radius: var(--radius);
  box-shadow: 0 4px 24px rgba(0,0,0,0.15);
  padding: 1.75rem;
  width: 100%; max-width: 480px;
  max-height: 90vh; overflow-y: auto;
}
h2 { font-size: 1.125rem; margin-bottom: 1.25rem; }
.row { display: flex; gap: 1rem; }
.row .form-group { flex: 1; }
.modal-actions { display: flex; justify-content: flex-end; gap: 0.75rem; margin-top: 0.5rem; }
</style>
