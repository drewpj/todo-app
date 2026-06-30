import { createRouter, createWebHistory } from 'vue-router'
import { useAuth } from '../composables/useAuth'
import LoginView from '../views/LoginView.vue'
import RegisterView from '../views/RegisterView.vue'
import TaskListView from '../views/TaskListView.vue'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', component: TaskListView, meta: { requiresAuth: true } },
    { path: '/login', component: LoginView },
    { path: '/register', component: RegisterView },
  ],
})

router.beforeEach((to) => {
  const { user } = useAuth()
  if (to.meta.requiresAuth && !user.value) {
    return '/login'
  }
  if ((to.path === '/login' || to.path === '/register') && user.value) {
    return '/'
  }
})

export default router
