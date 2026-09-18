<script setup lang="ts">
import { useRouter } from 'vue-router';
import { useUiStore } from '@/stores/ui';
import { useTransactionModalStore } from '@/stores/transactionModal';
import type { ChatAction, InsightDto, Transaction } from '@/types/api';
import { formatMoney } from '@/lib/format';
import TameruAssistantIcon from '@/components/brand/TameruAssistantIcon.vue';
import {
  CheckCircle2,
  AlertTriangle,
  AlertOctagon,
  ArrowRight,
  Receipt,
  Calculator,
  Pencil,
} from 'lucide-vue-next';

const props = defineProps<{
  action?: ChatAction | null;
  insights?: InsightDto[] | null;
}>();

const router = useRouter();
const uiStore = useUiStore();
const transactionModal = useTransactionModalStore();

function goToTransactions(): void {
  void router.push('/transactions');
}

function openSimulator(): void {
  uiStore.openSimulator();
}

function openEditTransaction(): void {
  if (!props.action?.data) return;
  const d = props.action.data;
  const tx: Transaction = {
    id: d.transactionId || d.id || '',
    title: d.title || props.action.summary || '',
    amount: typeof d.amount === 'number' ? d.amount : 0,
    type: d.type || 'Expense',
    date: d.date || new Date().toISOString().split('T')[0],
    currencyCode: d.currencyCode || 'IDR',
    accountId: d.accountId || '',
    accountName: d.accountName || null,
    toAccountId: d.toAccountId || null,
    categoryId: d.categoryId || null,
    categoryName: d.categoryName || null,
    budgetCategoryId: d.budgetCategoryId || null,
    subCategoryId: d.subCategoryId || null,
    status: d.status || 'Cleared',
    description: d.description || null,
  };
  transactionModal.openEdit(tx);
}
</script>

<template>
  <div class="mt-2.5 space-y-2">
    <!-- Transaction Created Card -->
    <div
      v-if="action?.type === 'transaction_created'"
      class="rounded-xl border border-emerald-500/20 bg-emerald-500/5 p-3 text-xs"
    >
      <div class="flex items-center justify-between gap-2 pb-1.5 border-b border-emerald-500/10">
        <div class="flex items-center gap-1.5 font-medium text-emerald-400">
          <Receipt class="h-3.5 w-3.5" />
          <span>Transaction Logged</span>
        </div>
        <span
          v-if="action.data?.type"
          class="rounded px-1.5 py-0.5 text-[10px] font-medium uppercase tracking-wider bg-emerald-500/20 text-emerald-300"
        >
          {{ action.data.type }}
        </span>
      </div>

      <div class="mt-2 flex items-baseline justify-between gap-2">
        <span class="font-medium text-slate-200 truncate">{{ action.data?.title || action.summary }}</span>
        <span class="font-mono font-semibold text-emerald-400 tabular-nums">
          {{ action.data?.amount ? formatMoney(action.data.amount) : '' }}
        </span>
      </div>

      <!-- Smart Fields Display: Account, Category, Budget, Status -->
      <div class="mt-2 flex flex-wrap items-center gap-1.5 text-[10px]">
        <span
          v-if="action.data?.accountName"
          class="rounded bg-slate-800 border border-slate-700/60 px-1.5 py-0.5 text-slate-300 font-medium"
        >
          💳 {{ action.data.accountName }}
        </span>
        <span
          v-if="action.data?.categoryName"
          class="rounded bg-indigo-500/10 border border-indigo-500/20 px-1.5 py-0.5 text-indigo-300 font-medium"
        >
          📁 {{ action.data.categoryName }}
        </span>
        <span
          v-if="action.data?.budgetName"
          class="rounded bg-sky-500/10 border border-sky-500/20 px-1.5 py-0.5 text-sky-300 font-medium"
        >
          🎯 {{ action.data.budgetName }}
        </span>
        <span
          v-if="action.data?.status"
          class="rounded bg-slate-800 border border-slate-700/60 px-1.5 py-0.5 text-emerald-400 font-medium"
        >
          ✓ {{ action.data.status }}
        </span>
      </div>

      <!-- Action Buttons: Edit in Modal & View in Transactions -->
      <div class="mt-3 flex items-center justify-between gap-2 pt-2 border-t border-emerald-500/10">
        <button
          type="button"
          class="inline-flex items-center gap-1 text-[11px] font-medium text-slate-300 hover:text-white transition-colors"
          @click="openEditTransaction"
        >
          <Pencil class="h-3 w-3 text-slate-400" />
          <span>Edit in Modal</span>
        </button>

        <button
          type="button"
          class="inline-flex items-center gap-1 text-[11px] font-medium text-emerald-400 hover:text-emerald-300 transition-colors"
          @click="goToTransactions"
        >
          <span>View in Transactions</span>
          <ArrowRight class="h-3 w-3" />
        </button>
      </div>
    </div>

    <!-- Purchase Simulation Card -->
    <div
      v-else-if="action?.type === 'simulation_run'"
      class="rounded-xl border p-3 text-xs"
      :class="{
        'border-emerald-500/20 bg-emerald-500/5': action.data?.verdict === 'Safe',
        'border-amber-500/20 bg-amber-500/5': action.data?.verdict === 'Warning',
        'border-rose-500/20 bg-rose-500/5': action.data?.verdict === 'Risky',
      }"
    >
      <div class="flex items-center justify-between gap-2 pb-1.5 border-b border-slate-700/50">
        <div class="flex items-center gap-1.5 font-medium">
          <CheckCircle2 v-if="action.data?.verdict === 'Safe'" class="h-3.5 w-3.5 text-emerald-400" />
          <AlertTriangle v-else-if="action.data?.verdict === 'Warning'" class="h-3.5 w-3.5 text-amber-400" />
          <AlertOctagon v-else class="h-3.5 w-3.5 text-rose-400" />
          <span
            :class="{
              'text-emerald-400': action.data?.verdict === 'Safe',
              'text-amber-400': action.data?.verdict === 'Warning',
              'text-rose-400': action.data?.verdict === 'Risky',
            }"
          >
            Simulation Verdict: {{ action.data?.verdict }}
          </span>
        </div>
        <span class="font-mono text-[11px] text-slate-400 tabular-nums">
          {{ action.data?.amount ? formatMoney(action.data.amount) : '' }}
        </span>
      </div>

      <p class="mt-2 text-slate-300 leading-relaxed">
        {{ action.summary }}
      </p>

      <div class="mt-2.5 flex items-center justify-end">
        <button
          type="button"
          class="inline-flex items-center gap-1 text-[11px] font-medium text-slate-300 hover:text-white transition-colors"
          @click="openSimulator"
        >
          <Calculator class="h-3 w-3" />
          <span>Interactive Sandbox</span>
        </button>
      </div>
    </div>

    <!-- Proactive Insights List -->
    <div v-if="insights && insights.length > 0" class="space-y-1.5 pt-1">
      <div
        v-for="item in insights"
        :key="item.id"
        class="rounded-lg border p-2.5 text-xs transition-colors"
        :class="{
          'border-rose-500/20 bg-rose-500/5': item.severity === 'critical',
          'border-amber-500/20 bg-amber-500/5': item.severity === 'warning',
          'border-indigo-500/20 bg-indigo-500/5': item.severity === 'info',
        }"
      >
        <div class="flex items-center justify-between gap-1.5">
          <div class="flex items-center gap-1.5 font-medium text-slate-200 truncate">
            <TameruAssistantIcon :size="12" class="text-indigo-400 shrink-0" />
            <span class="truncate">{{ item.title }}</span>
          </div>
          <span
            class="shrink-0 text-[10px] uppercase font-semibold px-1.5 py-0.2 rounded"
            :class="{
              'bg-rose-500/20 text-rose-300': item.severity === 'critical',
              'bg-amber-500/20 text-amber-300': item.severity === 'warning',
              'bg-indigo-500/20 text-indigo-300': item.severity === 'info',
            }"
          >
            {{ item.severity }}
          </span>
        </div>
        <p class="mt-1 text-slate-400 text-[11px] leading-relaxed">
          {{ item.message }}
        </p>
      </div>
    </div>
  </div>
</template>
