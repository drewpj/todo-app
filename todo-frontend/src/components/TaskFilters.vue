<template>
  <div class="filters">
    <select :value="filters.status" @change="emit('change', { ...filters, status: ($event.target as HTMLSelectElement).value })">
      <option value="">All statuses</option>
      <option value="Todo">To Do</option>
      <option value="InProgress">In Progress</option>
      <option value="Done">Done</option>
    </select>
    <select :value="filters.sortBy" @change="emit('change', { ...filters, sortBy: ($event.target as HTMLSelectElement).value })">
      <option value="createdAt">Date created</option>
      <option value="dueDate">Due date</option>
      <option value="priority">Priority</option>
      <option value="title">Title</option>
    </select>
    <select :value="filters.sortOrder" @change="emit('change', { ...filters, sortOrder: ($event.target as HTMLSelectElement).value })">
      <option value="desc">Descending</option>
      <option value="asc">Ascending</option>
    </select>
  </div>
</template>

<script setup lang="ts">
defineProps<{
  filters: { status: string; sortBy: string; sortOrder: string }
}>()

const emit = defineEmits<{
  change: [filters: { status: string; sortBy: string; sortOrder: string }]
}>()
</script>

<style scoped>
.filters {
  display: flex;
  gap: 0.75rem;
  margin-bottom: 1rem;
  flex-wrap: wrap;
}
.filters select { width: auto; flex: 1; min-width: 120px; }
</style>
