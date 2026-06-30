<template>
  <header class="app-header">
    <span class="app-logo">TodoApp</span>
    <span class="user-info">{{ user?.username }}</span>
    <button class="btn-secondary" @click="handleLogout">Sign out</button>
  </header>
</template>

<script setup lang="ts">
import { useRouter } from 'vue-router'
import { useAuth } from '../composables/useAuth'

const router = useRouter()
const { user, logout } = useAuth()

function handleLogout() {
  logout()
  router.push('/login')
}
</script>

<style scoped>
.app-header {
  background: var(--color-surface);
  border-bottom: 1px solid var(--color-border);
  padding: 0.75rem 1rem;
  display: flex;
  align-items: center;
  gap: 0.75rem;
}
.app-logo { font-weight: 700; font-size: 1rem; flex: 1; }
.user-info { font-size: 0.875rem; color: var(--color-text-muted); }
</style>
