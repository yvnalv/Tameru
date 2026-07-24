import {
  createRouter,
  createWebHistory,
  type RouteLocationNormalized,
  type RouteRecordRaw,
} from 'vue-router';
import { useAuthStore } from '@/stores/auth';

const routes: RouteRecordRaw[] = [
  {
    path: '/login',
    name: 'login',
    component: () => import('@/views/LoginView.vue'),
    meta: { public: true },
  },
  {
    path: '/',
    component: () => import('@/layouts/AppShell.vue'),
    children: [
      { path: '', redirect: { name: 'dashboard' } },
      {
        path: 'dashboard',
        name: 'dashboard',
        component: () => import('@/views/DashboardView.vue'),
      },
      { path: 'transactions', name: 'transactions', component: () => import('@/views/transactions/TransactionsView.vue') },
      { path: 'accounts', name: 'accounts', component: () => import('@/views/accounts/AccountsView.vue') },
      // Navigable placeholders until the next increment builds these screens.
      { path: 'budget', name: 'budget', component: () => import('@/views/PlaceholderView.vue') },
      { path: 'master-plan', name: 'masterPlan', component: () => import('@/views/PlaceholderView.vue') },
      { path: 'categories', name: 'categories', component: () => import('@/views/PlaceholderView.vue') },
    ],
  },
  { path: '/:pathMatch(.*)*', redirect: { name: 'dashboard' } },
];

export const router = createRouter({
  history: createWebHistory(),
  routes,
  scrollBehavior: () => ({ top: 0 }),
});

// Auth guard: unauthenticated users are sent to /login (with ?redirect=); an authenticated user who
// lands on /login is bounced to the dashboard. Exported for unit testing.
export function authGuard(to: RouteLocationNormalized) {
  const auth = useAuthStore();
  const isPublic = to.meta.public === true;

  if (!isPublic && !auth.isAuthenticated) {
    return { name: 'login', query: { redirect: to.fullPath } };
  }

  if (isPublic && auth.isAuthenticated) {
    return { name: 'dashboard' };
  }

  return true;
}

router.beforeEach(authGuard);
