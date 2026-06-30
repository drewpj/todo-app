import { ref } from 'vue'
import type { Task, TaskRequest, TaskListResponse } from '../types/api'
import { api } from '../lib/apiClient'

const tasks = ref<Task[]>([])
const isLoading = ref(false)
const error = ref<string | null>(null)

export function useTasks() {
  async function fetchTasks(params?: { status?: string; sortBy?: string; sortOrder?: string }): Promise<void> {
    isLoading.value = true
    error.value = null
    try {
      const query = new URLSearchParams()
      if (params?.status) query.set('status', params.status)
      if (params?.sortBy) query.set('sortBy', params.sortBy)
      if (params?.sortOrder) query.set('sortOrder', params.sortOrder)
      const qs = query.toString() ? `?${query.toString()}` : ''
      const data = await api.get<TaskListResponse>(`/api/tasks${qs}`)
      tasks.value = data.tasks
    } catch (e: unknown) {
      error.value = e instanceof Error ? e.message : 'Failed to load tasks'
    } finally {
      isLoading.value = false
    }
  }

  async function createTask(request: TaskRequest): Promise<Task> {
    const task = await api.post<Task>('/api/tasks', request)
    tasks.value = [task, ...tasks.value]
    return task
  }

  async function updateTask(id: number, request: TaskRequest): Promise<Task> {
    const task = await api.put<Task>(`/api/tasks/${id}`, request)
    tasks.value = tasks.value.map(t => (t.id === id ? task : t))
    return task
  }

  async function deleteTask(id: number): Promise<void> {
    await api.delete(`/api/tasks/${id}`)
    tasks.value = tasks.value.filter(t => t.id !== id)
  }

  return { tasks, isLoading, error, fetchTasks, createTask, updateTask, deleteTask }
}
