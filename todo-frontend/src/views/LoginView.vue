<template>
  <div class="auth-page">
    <div class="auth-card">
      <h1>Sign in</h1>
      <form @submit.prevent="handleSubmit">
        <div class="form-group">
          <label for="username">Username</label>
          <input id="username" v-model="username" autocomplete="username" required />
        </div>
        <div class="form-group">
          <label for="password">Password</label>
          <input id="password" v-model="password" type="password" autocomplete="current-password" required />
        </div>
        <p v-if="errorMsg" class="error-msg">{{ errorMsg }}</p>
        <button type="submit" class="btn-primary" :disabled="loading" style="width:100%">
          {{ loading ? 'Signing in…' : 'Sign in' }}
        </button>
      </form>
      <p class="auth-footer">No account? <RouterLink to="/register">Register</RouterLink></p>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useRouter, RouterLink } from 'vue-router'
import { useAuth } from '../composables/useAuth'
import { ApiError } from '../lib/apiClient'

const router = useRouter()
const { login } = useAuth()

const username = ref('')
const password = ref('')
const loading = ref(false)
const errorMsg = ref('')

async function handleSubmit() {
  errorMsg.value = ''
  loading.value = true
  try {
    await login(username.value, password.value)
    router.push('/tasks')
  } catch (e: unknown) {
    errorMsg.value = e instanceof ApiError ? e.message : 'Login failed'
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.auth-page {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: var(--color-bg);
}
.auth-card {
  background: var(--color-surface);
  padding: 2rem;
  border-radius: var(--radius);
  box-shadow: var(--shadow);
  width: 100%;
  max-width: 360px;
}
h1 { font-size: 1.5rem; margin-bottom: 1.5rem; }
.auth-footer { margin-top: 1rem; text-align: center; font-size: 0.875rem; }
</style>
