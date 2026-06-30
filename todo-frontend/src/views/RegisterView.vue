<template>
  <div class="auth-page">
    <div class="auth-card">
      <h1>Create account</h1>
      <form @submit.prevent="handleSubmit">
        <div class="form-group">
          <label for="username">Username</label>
          <input id="username" v-model="username" autocomplete="username" required maxlength="50" />
          <span v-if="fieldErrors.username" class="error-msg">{{ fieldErrors.username }}</span>
        </div>
        <div class="form-group">
          <label for="password">Password</label>
          <input id="password" v-model="password" type="password" autocomplete="new-password" required minlength="6" />
          <span v-if="fieldErrors.password" class="error-msg">{{ fieldErrors.password }}</span>
        </div>
        <p v-if="errorMsg" class="error-msg">{{ errorMsg }}</p>
        <button type="submit" class="btn-primary" :disabled="loading" style="width:100%">
          {{ loading ? 'Creating account…' : 'Register' }}
        </button>
      </form>
      <p class="auth-footer">Already have an account? <RouterLink to="/login">Sign in</RouterLink></p>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue'
import { useRouter, RouterLink } from 'vue-router'
import { useAuth } from '../composables/useAuth'
import { ApiError } from '../lib/apiClient'

const router = useRouter()
const { register } = useAuth()

const username = ref('')
const password = ref('')
const loading = ref(false)
const errorMsg = ref('')
const fieldErrors = reactive<Record<string, string>>({})

async function handleSubmit() {
  errorMsg.value = ''
  Object.keys(fieldErrors).forEach(k => delete fieldErrors[k])
  loading.value = true
  try {
    await register(username.value, password.value)
    router.push('/')
  } catch (e: unknown) {
    if (e instanceof ApiError && e.details?.length) {
      e.details.forEach(d => { fieldErrors[d.field.toLowerCase()] = d.message })
    } else {
      errorMsg.value = e instanceof ApiError ? e.message : 'Registration failed'
    }
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
