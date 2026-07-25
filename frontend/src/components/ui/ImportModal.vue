<script setup lang="ts">
import { ref, computed } from 'vue';
import { useI18n } from 'vue-i18n';
import { X, Upload, Download, Check, AlertTriangle } from 'lucide-vue-next';
import { parseCsv } from '@/lib/csvParse';
import { downloadTemplate, type ImportConfig } from '@/lib/import';
import { errorMessage } from '@/lib/errorMessage';
import AppButton from '@/components/ui/AppButton.vue';

const props = defineProps<{ title: string; config: ImportConfig }>();
const emit = defineEmits<{ close: []; done: [] }>();
const { t, te } = useI18n();

type Row = { record: Record<string, string>; error: string | null };

const stage = ref<'upload' | 'preview' | 'importing' | 'report'>('upload');
const rows = ref<Row[]>([]);
const fileName = ref('');
const progress = ref(0);
const failures = ref<{ summary: string; error: string }[]>([]);
const createdCount = ref(0);

const validRows = computed(() => rows.value.filter((r) => !r.error));
const invalidCount = computed(() => rows.value.length - validRows.value.length);

async function onFile(event: Event): Promise<void> {
  const input = event.target as HTMLInputElement;
  const file = input.files?.[0];
  if (!file) return;
  fileName.value = file.name;
  const text = await file.text();
  const { records } = parseCsv(text);
  rows.value = records.map((record) => ({ record, error: props.config.validate(record) }));
  stage.value = 'preview';
}

async function runImport(): Promise<void> {
  stage.value = 'importing';
  progress.value = 0;
  failures.value = [];
  createdCount.value = 0;
  const targets = validRows.value;
  for (let i = 0; i < targets.length; i++) {
    const { record } = targets[i];
    try {
      await props.config.importRecord(record);
      createdCount.value++;
    } catch (error) {
      failures.value.push({ summary: props.config.summary(record), error: errorMessage(t, te, error) });
    }
    progress.value = Math.round(((i + 1) / targets.length) * 100);
  }
  stage.value = 'report';
  if (createdCount.value > 0) emit('done');
}

function reset(): void {
  stage.value = 'upload';
  rows.value = [];
  fileName.value = '';
  failures.value = [];
  createdCount.value = 0;
}
</script>

<template>
  <Teleport to="body">
    <div class="fixed inset-0 z-50 flex items-end justify-center bg-black/60 p-0 sm:items-center sm:p-4" @click.self="emit('close')">
      <div class="flex max-h-[90vh] w-full max-w-2xl flex-col rounded-t-card border border-border bg-surface shadow-lift sm:rounded-card" role="dialog" aria-modal="true">
        <header class="flex items-center justify-between border-b border-border px-5 py-4">
          <h2 class="text-base font-semibold">{{ title }}</h2>
          <button class="rounded-control p-1 text-text-muted hover:bg-surface-2 hover:text-text" @click="emit('close')"><X :size="18" /></button>
        </header>

        <div class="scroll-slim flex-1 overflow-y-auto px-5 py-4">
          <!-- Upload -->
          <template v-if="stage === 'upload'">
            <p class="text-[13px] text-text-muted">{{ t('import.expectedColumns') }}</p>
            <code class="mt-2 block overflow-x-auto rounded-control bg-surface-2 px-3 py-2 text-[12px] text-text">{{ config.columns.join(', ') }}</code>
            <div class="mt-4 flex flex-wrap items-center gap-2">
              <label class="inline-flex cursor-pointer items-center gap-2 rounded-control bg-accent px-4 py-2 text-sm font-medium text-accent-contrast hover:bg-accent-hover">
                <Upload :size="16" />{{ t('import.chooseFile') }}
                <input type="file" accept=".csv,text/csv" class="hidden" @change="onFile" />
              </label>
              <AppButton variant="ghost" @click="downloadTemplate(config)"><Download :size="16" />{{ t('import.downloadTemplate') }}</AppButton>
            </div>
          </template>

          <!-- Preview -->
          <template v-else-if="stage === 'preview'">
            <div class="mb-3 flex flex-wrap items-center gap-x-4 gap-y-1 text-[13px]">
              <span class="text-text-muted">{{ fileName }}</span>
              <span class="text-positive">{{ t('import.validRows', { n: validRows.length }) }}</span>
              <span v-if="invalidCount" class="text-negative">{{ t('import.invalidRows', { n: invalidCount }) }}</span>
            </div>
            <div class="scroll-slim max-h-[45vh] overflow-auto rounded-control border border-border">
              <table class="w-full text-sm">
                <thead class="sticky top-0 bg-surface-2 text-left text-[12px] uppercase text-text-muted">
                  <tr><th class="px-3 py-2 font-medium">#</th><th class="px-3 py-2 font-medium">{{ t('import.record') }}</th><th class="px-3 py-2 font-medium">{{ t('import.status') }}</th></tr>
                </thead>
                <tbody>
                  <tr v-for="(row, i) in rows" :key="i" class="border-t border-border">
                    <td class="px-3 py-1.5 tnum text-text-muted">{{ i + 1 }}</td>
                    <td class="px-3 py-1.5">{{ config.summary(row.record) }}</td>
                    <td class="px-3 py-1.5">
                      <span v-if="!row.error" class="inline-flex items-center gap-1 text-positive"><Check :size="14" /></span>
                      <span v-else class="inline-flex items-center gap-1 text-negative"><AlertTriangle :size="14" />{{ row.error }}</span>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </template>

          <!-- Importing -->
          <template v-else-if="stage === 'importing'">
            <p class="text-sm text-text-muted">{{ t('import.importing') }} {{ progress }}%</p>
            <div class="mt-3 h-2 w-full overflow-hidden rounded-full bg-surface-2">
              <div class="h-full bg-accent transition-[width]" :style="{ width: `${progress}%` }" />
            </div>
          </template>

          <!-- Report -->
          <template v-else>
            <p class="text-sm"><span class="font-semibold text-positive">{{ t('import.created', { n: createdCount }) }}</span></p>
            <p v-if="failures.length" class="mt-1 text-sm font-semibold text-negative">{{ t('import.failed', { n: failures.length }) }}</p>
            <ul v-if="failures.length" class="scroll-slim mt-3 max-h-[40vh] space-y-1 overflow-auto rounded-control border border-border p-3 text-[13px]">
              <li v-for="(f, i) in failures" :key="i" class="flex gap-2">
                <span class="text-text-muted">{{ f.summary }}</span><span class="text-negative">{{ f.error }}</span>
              </li>
            </ul>
          </template>
        </div>

        <footer class="flex justify-end gap-2 border-t border-border px-5 py-4">
          <template v-if="stage === 'preview'">
            <AppButton variant="secondary" @click="reset">{{ t('import.chooseAnother') }}</AppButton>
            <AppButton :disabled="!validRows.length" @click="runImport">{{ t('import.importN', { n: validRows.length }) }}</AppButton>
          </template>
          <template v-else-if="stage === 'report'">
            <AppButton variant="secondary" @click="reset">{{ t('import.importAnother') }}</AppButton>
            <AppButton @click="emit('close')">{{ t('common.close') }}</AppButton>
          </template>
          <AppButton v-else-if="stage === 'upload'" variant="secondary" @click="emit('close')">{{ t('common.cancel') }}</AppButton>
        </footer>
      </div>
    </div>
  </Teleport>
</template>
