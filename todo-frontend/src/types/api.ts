export interface User {
  id: number
  username: string
}

export type TaskStatus = 'Todo' | 'InProgress' | 'Done'
export type TaskPriority = 'Low' | 'Medium' | 'High'

export interface Task {
  id: number
  title: string
  description?: string | null
  status: TaskStatus
  priority: TaskPriority
  dueDate?: string | null
  createdAt: string
  updatedAt: string
}

export interface TaskRequest {
  title: string
  description?: string | null
  status: TaskStatus
  priority: TaskPriority
  dueDate?: string | null
}

export interface AuthResponse {
  token: string
  user: User
}

export interface TaskListResponse {
  tasks: Task[]
}
