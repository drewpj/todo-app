import { ref, computed } from 'vue'
import type { User } from '../types/api'
import { api } from '../lib/apiClient'
import type { AuthResponse } from '../types/api'

const user = ref<User | null>(null)
const token = ref<string | null>(null)
const isAuthenticated = computed(() => user.value !== null)

export function useAuth() {
  async function register(username: string, password: string): Promise<void> {
    const data = await api.post<AuthResponse>('/api/auth/register', { username, password })
    token.value = data.token
    user.value = data.user
    localStorage.setItem('token', data.token)
  }

  async function login(username: string, password: string): Promise<void> {
    const data = await api.post<AuthResponse>('/api/auth/login', { username, password })
    token.value = data.token
    user.value = data.user
    localStorage.setItem('token', data.token)
  }

  function logout(): void {
    token.value = null
    user.value = null
    localStorage.removeItem('token')
  }

  async function restoreSession(): Promise<void> {
    const stored = localStorage.getItem('token')
    if (!stored) return
    token.value = stored
    try {
      const me = await api.get<User>('/api/auth/me')
      user.value = me
    } catch {
      logout()
    }
  }

  return { user, token, isAuthenticated, register, login, logout, restoreSession }
}
