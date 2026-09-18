<script setup lang="ts">
import { ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { useAuthStore } from '@/stores/auth';
import { useUiStore } from '@/stores/ui';
import { ApiClientError } from '@/lib/api';
import LogoLockup from '@/components/brand/LogoLockup.vue';
import ThemeToggle from '@/components/ui/ThemeToggle.vue';
import {
  Mail,
  Lock,
  Eye,
  EyeOff,
  TrendingUp,
  PieChart,
} from 'lucide-vue-next';

const route = useRoute();
const router = useRouter();
const { t, te } = useI18n();
const auth = useAuthStore();
const ui = useUiStore();

const activeTab = ref<'signin' | 'signup'>('signin');
const email = ref('');
const password = ref('');
const showPassword = ref(false);
const submitting = ref(false);
const errorMessage = ref('');

function messageFor(code: string): string {
  return te(`errors.${code}`) ? t(`errors.${code}`) : t('errors.generic');
}

function quickFillDemo(): void {
  email.value = 'owner@tameru.local';
  password.value = 'ChangeMe!123';
}

async function onSubmit(): Promise<void> {
  if (activeTab.value === 'signup') {
    errorMessage.value = 'Registration is managed by organization admin. Please sign in with your provisioned account.';
    return;
  }

  errorMessage.value = '';
  submitting.value = true;
  try {
    await auth.login(email.value.trim(), password.value);
    const redirect = typeof route.query.redirect === 'string' ? route.query.redirect : undefined;
    router.push(redirect ?? { name: 'dashboard' });
  } catch (error) {
    errorMessage.value =
      error instanceof ApiClientError ? messageFor(error.code) : t('errors.generic');
  } finally {
    submitting.value = false;
  }
}
</script>

<template>
  <div class="flex min-h-screen items-center justify-center bg-bg p-4 sm:p-6 lg:p-8 text-text transition-colors duration-200">
    <div class="w-full max-w-5xl rounded-[28px] border border-border bg-surface shadow-2xl overflow-hidden grid grid-cols-1 lg:grid-cols-12 min-h-[660px]">
      
      <!-- Left Panel: Authentication Form -->
      <div class="lg:col-span-6 p-6 sm:p-10 lg:p-12 flex flex-col justify-between">
        <!-- Top Bar: Brand & Controls -->
        <div class="flex items-center justify-between">
          <LogoLockup size="md" />
          <div class="flex items-center gap-2">
            <ThemeToggle variant="segmented" :size="13" />
            <button
              type="button"
              class="rounded-lg border border-border bg-surface px-2.5 py-1 text-xs font-semibold uppercase text-text-muted hover:bg-surface-2 hover:text-text transition-colors"
              @click="ui.toggleLocale()"
            >
              {{ ui.locale }}
            </button>
          </div>
        </div>

        <!-- Main Form Content -->
        <div class="my-6 max-w-sm w-full mx-auto">
          <!-- Title & Subtitle -->
          <div class="text-center">
            <h1 class="text-2xl sm:text-3xl font-bold tracking-tight text-text">
              Welcome to Tameru
            </h1>
            <p class="mt-2 text-xs sm:text-sm text-text-muted leading-relaxed">
              Start your experience with Tameru by signing in or signing up.
            </p>
          </div>

          <!-- Segmented Tab Switch (Sign In / Sign Up) -->
          <div class="mt-6 rounded-xl bg-surface-2 p-1 border border-border flex items-center">
            <button
              type="button"
              class="flex-1 py-2 text-xs font-semibold rounded-lg transition-all"
              :class="activeTab === 'signin' ? 'bg-surface text-text shadow-sm' : 'text-text-muted hover:text-text'"
              @click="activeTab = 'signin'"
            >
              Sign In
            </button>
            <button
              type="button"
              class="flex-1 py-2 text-xs font-semibold rounded-lg transition-all"
              :class="activeTab === 'signup' ? 'bg-surface text-text shadow-sm' : 'text-text-muted hover:text-text'"
              @click="activeTab = 'signup'"
            >
              Sign Up
            </button>
          </div>

          <!-- Form -->
          <form class="mt-6 space-y-4" @submit.prevent="onSubmit">
            <div>
              <div class="flex items-center justify-between mb-1.5">
                <label for="email" class="block text-xs font-medium text-text">
                  {{ t('login.email') }}
                </label>
                <span class="text-accent text-xs font-bold">*</span>
              </div>
              <div class="relative flex items-center">
                <Mail class="absolute left-3.5 h-4 w-4 text-text-muted pointer-events-none" />
                <input
                  id="email"
                  v-model="email"
                  type="email"
                  autocomplete="username"
                  :placeholder="t('login.emailPlaceholder')"
                  required
                  class="w-full rounded-xl border border-border bg-surface pl-10 pr-3.5 py-2.5 text-xs text-text placeholder:text-text-muted/60 focus:border-accent focus:outline-none focus:ring-1 focus:ring-accent transition-all"
                />
              </div>
            </div>

            <div>
              <div class="flex items-center justify-between mb-1.5">
                <label for="password" class="block text-xs font-medium text-text">
                  {{ t('login.password') }}
                </label>
                <span class="text-accent text-xs font-bold">*</span>
              </div>
              <div class="relative flex items-center">
                <Lock class="absolute left-3.5 h-4 w-4 text-text-muted pointer-events-none" />
                <input
                  id="password"
                  v-model="password"
                  :type="showPassword ? 'text' : 'password'"
                  autocomplete="current-password"
                  :placeholder="t('login.passwordPlaceholder')"
                  required
                  class="w-full rounded-xl border border-border bg-surface pl-10 pr-10 py-2.5 text-xs text-text placeholder:text-text-muted/60 focus:border-accent focus:outline-none focus:ring-1 focus:ring-accent transition-all"
                />
                <button
                  type="button"
                  class="absolute right-3 text-text-muted hover:text-text transition-colors p-1"
                  :title="showPassword ? 'Hide password' : 'Show password'"
                  @click="showPassword = !showPassword"
                >
                  <EyeOff v-if="showPassword" class="h-4 w-4" />
                  <Eye v-else class="h-4 w-4" />
                </button>
              </div>
            </div>

            <p v-if="errorMessage" class="text-xs text-negative font-medium" role="alert">
              {{ errorMessage }}
            </p>

            <button
              type="submit"
              :disabled="submitting"
              class="w-full rounded-xl bg-accent py-2.5 text-xs font-semibold text-accent-contrast shadow-lg shadow-accent/25 hover:shadow-accent/40 hover:-translate-y-0.5 active:translate-y-0 active:scale-[0.99] transition-all disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {{ submitting ? t('login.submitting') : (activeTab === 'signin' ? t('login.submit') : 'Create Account') }}
            </button>
          </form>

          <!-- Divider -->
          <div class="relative my-5 flex items-center justify-center">
            <div class="w-full border-t border-border"></div>
            <span class="absolute bg-surface px-3 text-[11px] text-text-muted uppercase tracking-wider">
              Or continue with
            </span>
          </div>

          <!-- Social / Quick-Auth Buttons -->
          <div class="flex items-center justify-center gap-3">
            <!-- Google -->
            <button
              type="button"
              class="flex h-10 w-10 items-center justify-center rounded-xl border border-border bg-surface hover:bg-surface-2 transition-all hover:scale-105 active:scale-95 shadow-xs"
              title="Sign in with Google"
              @click="quickFillDemo"
            >
              <svg class="h-4 w-4" viewBox="0 0 24 24">
                <path fill="#4285F4" d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z" />
                <path fill="#34A853" d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z" />
                <path fill="#FBBC05" d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.06H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.94l2.85-2.22.81-.63z" />
                <path fill="#EA4335" d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.06l3.66 2.84c.87-2.6 3.3-4.52 6.16-4.52z" />
              </svg>
            </button>

            <!-- Apple -->
            <button
              type="button"
              class="flex h-10 w-10 items-center justify-center rounded-xl border border-border bg-surface hover:bg-surface-2 transition-all hover:scale-105 active:scale-95 shadow-xs text-text"
              title="Sign in with Apple"
              @click="quickFillDemo"
            >
              <svg class="h-4 w-4 fill-current" viewBox="0 0 24 24">
                <path d="M18.71 19.5c-.83 1.24-1.71 2.45-3.05 2.47-1.34.03-1.77-.79-3.29-.79-1.53 0-2 .77-3.27.82-1.31.05-2.3-1.32-3.14-2.53C4.25 17 2.94 12.45 4.7 9.39c.87-1.52 2.43-2.48 4.12-2.51 1.28-.02 2.5.87 3.29.87.78 0 2.26-1.07 3.81-.91.65.03 2.47.26 3.64 1.98-.09.06-2.17 1.28-2.15 3.81.03 3.02 2.65 4.03 2.68 4.04-.03.07-.42 1.44-1.38 2.83M15.97 6.37c.62-.75 1.04-1.8 0.92-2.85-.9.04-1.99.6-2.64 1.36-.58.67-1.08 1.74-.95 2.77 1.01.08 2.05-.53 2.67-1.28z" />
              </svg>
            </button>

            <!-- Facebook -->
            <button
              type="button"
              class="flex h-10 w-10 items-center justify-center rounded-xl border border-border bg-surface hover:bg-surface-2 transition-all hover:scale-105 active:scale-95 shadow-xs text-[#1877F2]"
              title="Sign in with Facebook"
              @click="quickFillDemo"
            >
              <svg class="h-4 w-4 fill-current" viewBox="0 0 24 24">
                <path d="M24 12.073c0-6.627-5.373-12-12-12s-12 5.373-12 12c0 5.99 4.388 10.954 10.125 11.854v-8.385H7.078v-3.47h3.047V9.43c0-3.007 1.792-4.669 4.533-4.669 1.312 0 2.686.235 2.686.235v2.953H15.83c-1.491 0-1.956.925-1.956 1.874v2.25h3.328l-.532 3.47h-2.796v8.385C19.612 23.027 24 18.062 24 12.073z" />
              </svg>
            </button>

            <!-- X / Twitter -->
            <button
              type="button"
              class="flex h-10 w-10 items-center justify-center rounded-xl border border-border bg-surface hover:bg-surface-2 transition-all hover:scale-105 active:scale-95 shadow-xs text-text"
              title="Sign in with X"
              @click="quickFillDemo"
            >
              <svg class="h-3.5 w-3.5 fill-current" viewBox="0 0 24 24">
                <path d="M18.244 2.25h3.308l-7.227 8.26 8.502 11.24H16.17l-5.214-6.817L4.99 21.75H1.68l7.73-8.835L1.254 2.25H8.08l4.713 6.231zm-1.161 17.52h1.833L7.084 4.126H5.117z" />
              </svg>
            </button>
          </div>

          <!-- Quick Fill Pill -->
          <div class="mt-3 text-center">
            <button
              type="button"
              class="text-[11px] text-accent hover:underline inline-flex items-center gap-1 font-medium"
              @click="quickFillDemo"
            >
              Use demo credentials (owner@tameru.local)
            </button>
          </div>
        </div>

        <!-- Footer -->
        <div class="pt-4 border-t border-border/50 flex flex-col sm:flex-row items-center justify-between gap-2 text-[11px] text-text-muted">
          <span>Copyright : Tameru, All Right Reserved</span>
          <div class="flex items-center gap-2">
            <a href="#" class="hover:text-text transition-colors">Term & Condition</a>
            <span>|</span>
            <a href="#" class="hover:text-text transition-colors">Privacy & Policy</a>
          </div>
        </div>
      </div>

      <!-- Right Panel: Rich Showcase Hub -->
      <div class="lg:col-span-6 bg-gradient-to-br from-slate-900 via-blue-950 to-slate-950 text-white relative p-6 sm:p-10 lg:p-12 flex flex-col justify-between overflow-hidden border-t lg:border-t-0 lg:border-l border-border/40">
        <!-- Subtle Background Grid Pattern Overlay -->
        <div
          class="absolute inset-0 opacity-[0.07] pointer-events-none"
          style="background-image: radial-gradient(circle, #38bdf8 1px, transparent 1px); background-size: 24px 24px;"
        />
        <!-- Ambient Glowing Gradients -->
        <div class="absolute -top-24 -right-24 h-80 w-80 rounded-full bg-accent/20 blur-3xl pointer-events-none" />
        <div class="absolute -bottom-24 -left-24 h-80 w-80 rounded-full bg-blue-600/15 blur-3xl pointer-events-none" />

        <!-- Floating Showcase Cards (Arranged cleanly like layout reference) -->
        <div class="relative z-10 space-y-3.5 my-auto max-w-md mx-auto w-full pt-4">
          <!-- Card 1: Financial Plan with Donut Chart -->
          <div class="rounded-2xl border border-white/10 bg-white/[0.06] backdrop-blur-md p-4 shadow-xl shadow-black/20">
            <div class="flex items-center justify-between pb-2 border-b border-white/10 text-[11px]">
              <span class="font-semibold text-slate-200 flex items-center gap-1.5">
                <PieChart class="h-3.5 w-3.5 text-accent" />
                Financial Plan
              </span>
              <span class="text-slate-400 font-mono">This Month ▼</span>
            </div>

            <div class="mt-3 flex items-center justify-between gap-4">
              <!-- SVG Donut Chart -->
              <div class="relative flex items-center justify-center shrink-0">
                <svg class="h-20 w-20 -rotate-90" viewBox="0 0 36 36">
                  <!-- Background circle -->
                  <circle cx="18" cy="18" r="14" fill="none" class="stroke-white/10" stroke-width="4" />
                  <!-- Budgeted Expenses Segment -->
                  <circle
                    cx="18"
                    cy="18"
                    r="14"
                    fill="none"
                    class="stroke-accent"
                    stroke-width="4"
                    stroke-dasharray="55 100"
                    stroke-dashoffset="0"
                  />
                  <!-- Additional Spending Segment -->
                  <circle
                    cx="18"
                    cy="18"
                    r="14"
                    fill="none"
                    class="stroke-amber-400"
                    stroke-width="4"
                    stroke-dasharray="25 100"
                    stroke-dashoffset="-55"
                  />
                  <!-- Available/In Stock Segment -->
                  <circle
                    cx="18"
                    cy="18"
                    r="14"
                    fill="none"
                    class="stroke-emerald-400"
                    stroke-width="4"
                    stroke-dasharray="20 100"
                    stroke-dashoffset="-80"
                  />
                </svg>
                <div class="absolute text-[10px] font-bold text-white tracking-tighter">
                  85%
                </div>
              </div>

              <!-- Available balance & legend -->
              <div class="flex-1 min-w-0">
                <div class="text-[11px] text-slate-400">Available</div>
                <div class="text-lg font-bold font-mono text-white tracking-tight">
                  Rp 32.500.000
                </div>
                <div class="mt-1.5 space-y-0.5 text-[10px] text-slate-300">
                  <div class="flex items-center gap-1.5 truncate">
                    <span class="h-1.5 w-1.5 rounded-full bg-accent shrink-0" />
                    <span>Budgeted Expenses (50%)</span>
                  </div>
                  <div class="flex items-center gap-1.5 truncate">
                    <span class="h-1.5 w-1.5 rounded-full bg-amber-400 shrink-0" />
                    <span>Additional Spending (25%)</span>
                  </div>
                  <div class="flex items-center gap-1.5 truncate">
                    <span class="h-1.5 w-1.5 rounded-full bg-emerald-400 shrink-0" />
                    <span>Liquid Reserve (25%)</span>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <!-- Card 2: Capital Allocations & Target Funds -->
          <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <!-- Capital Allocations -->
            <div class="rounded-2xl border border-white/10 bg-white/[0.06] backdrop-blur-md p-3.5 shadow-xl shadow-black/20">
              <div class="flex items-center justify-between text-[11px]">
                <span class="text-slate-300 font-medium">Capital Allocations</span>
                <span class="inline-flex items-center gap-0.5 text-[10px] text-emerald-400 font-semibold font-mono">
                  <TrendingUp class="h-3 w-3" /> +12%
                </span>
              </div>
              <div class="mt-1 text-base font-bold font-mono text-white">
                Rp 280.500.000
              </div>
              <div class="mt-2 space-y-1 text-[10px]">
                <div class="flex justify-between text-slate-300">
                  <span>Emergency Fund</span>
                  <span class="font-mono text-white">6.2 bln</span>
                </div>
                <div class="flex justify-between text-slate-300">
                  <span>Gold & Savings</span>
                  <span class="font-mono text-emerald-400">Target 80%</span>
                </div>
              </div>
            </div>

            <!-- Future Funds / Safe-to-Spend -->
            <div class="rounded-2xl border border-white/10 bg-white/[0.06] backdrop-blur-md p-3.5 shadow-xl shadow-black/20">
              <div class="flex items-center justify-between text-[11px]">
                <span class="text-slate-300 font-medium">Safe-to-Spend</span>
                <span class="text-[10px] text-accent font-semibold">Pacing</span>
              </div>
              <div class="mt-1 text-base font-bold font-mono text-white">
                Rp 13.300.000
              </div>
              <div class="mt-2 space-y-1">
                <div class="flex justify-between text-[10px] text-slate-300">
                  <span>Daily Allowance</span>
                  <span class="font-mono text-emerald-400">Rp 443k/hari</span>
                </div>
                <!-- Progress bar -->
                <div class="h-1.5 w-full rounded-full bg-white/10 overflow-hidden">
                  <div class="h-full rounded-full bg-gradient-to-r from-accent to-emerald-400 w-[72%]" />
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Center Bottom Text & Brand Statement -->
        <div class="relative z-10 text-center mt-6">
          <!-- Tameru Emblem -->
          <div class="h-11 w-11 rounded-2xl bg-accent/20 border border-accent/40 text-accent flex items-center justify-center mx-auto mb-3 shadow-lg shadow-accent/20">
            <svg class="h-6 w-6" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
              <rect x="3.5" y="4" width="17" height="3" rx="1.5" fill="currentColor" />
              <rect x="4.5" y="14" width="3.5" height="6" rx="1.75" fill="currentColor" opacity="0.75" />
              <rect x="10.25" y="7.5" width="3.5" height="12.5" rx="1.75" fill="currentColor" />
              <rect x="16" y="10.5" width="3.5" height="9.5" rx="1.75" fill="currentColor" opacity="0.9" />
            </svg>
          </div>

          <!-- Headline -->
          <h2 class="text-lg sm:text-xl font-bold text-white tracking-tight leading-snug max-w-sm mx-auto">
            A Unified Hub for Smarter Financial Decision-Making
          </h2>
          <!-- Subtext -->
          <p class="mt-1.5 text-xs text-slate-300/80 leading-relaxed max-w-md mx-auto">
            Tameru empowers you with a unified financial command center—delivering deep insights and a 360° view of your entire economic world.
          </p>

          <!-- Slider Pagination Dots -->
          <div class="mt-4 flex items-center justify-center gap-1.5">
            <span class="h-1 w-8 rounded-full bg-accent" />
            <span class="h-1 w-3 rounded-full bg-white/20" />
            <span class="h-1 w-3 rounded-full bg-white/20" />
            <span class="h-1 w-3 rounded-full bg-white/20" />
          </div>
        </div>

      </div>
    </div>
  </div>
</template>
