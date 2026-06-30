<template>
  <div>
    <p v-if="tasks.length === 0" class="empty-msg">No tasks yet. Create one!</p>
    <ul v-else class="task-list">
      <li v-for="task in tasks" :key="task.id">
        <TaskItem :task="task" @edit="emit('edit', task)" @delete="emit('delete', task)" />
      </li>
    </ul>
  </div>
</template>

<script setup lang="ts">
import TaskItem from './TaskItem.vue'
import type { Task } from '../types/api'

defineProps<{ tasks: Task[] }>()
const emit = defineEmits<{
  edit: [task: Task]
  delete: [task: Task]
}>()
</script>

<style scoped>
.task-list { list-style: none; display: flex; flex-direction: column; gap: 0.5rem; }
.empty-msg { text-align: center; color: var(--color-text-muted); padding: 3rem 0; }
</style>
