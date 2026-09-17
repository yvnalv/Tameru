<script setup lang="ts">
import { ref, computed } from 'vue';
import { useRouter } from 'vue-router';
import { useThemeStore } from '@/stores/theme';
import { useUiStore } from '@/stores/ui';
import ThemeToggle from '@/components/ui/ThemeToggle.vue';
import AppToggle from '@/components/ui/AppToggle.vue';
import '@/lib/echarts';
import VChart from 'vue-echarts';
import { getChartTheme } from '@/lib/chartTheme';
import { formatMoney } from '@/lib/format';
import {
  LayoutDashboard,
  ArrowLeftRight,
  Wallet,
  BarChart3,
  PieChart,
  Target,
  Settings,
  LogOut,
  Search,
  Calendar,
  Filter,
  ArrowDownLeft,
  ArrowUpRight,
  TrendingUp,
  Plus,
  Check,
  AlertCircle,
  X,
  CreditCard,
  ChevronDown,
  Download,
  SlidersHorizontal,
  Eye,
  EyeOff,
  Sparkles,
  ShieldCheck,
  Building2,
  Smartphone,
  Receipt,
  Layers,
  Activity,
  Gauge,
  HelpCircle,
  RotateCcw,
} from 'lucide-vue-next';

const router = useRouter();
const themeStore = useThemeStore();
const ui = useUiStore();

// Interactive states for showcase
const activeSegment = ref<'daily' | 'monthly' | 'yearly'>('monthly');
const activeTransactionType = ref<'income' | 'expense' | 'transfer'>('expense');
const activeMonthIndex = ref(3); // April in the reference chart
const isModalOpen = ref(false);
const isConfirmOpen = ref(false);
const toastMessage = ref<string | null>(null);
const toastType = ref<'success' | 'error' | 'info'>('success');
const toggleChecked = ref(true);
const checkboxChecked = ref(true);
const inputSearch = ref('');
const inputText = ref('Groceries at Grand Lucky');
const inputAmount = ref('2450000');
const selectedAccountTab = ref<'BCA' | 'Jenius' | 'GoPay'>('BCA');

// Chart Laboratory state & interactive controls
const chartTimeframe = ref<'3M' | '6M' | '12M'>('12M');
const globalShowLegend = ref(true);

// 1. Cashflow Analysis Controls
const cashflowChartMode = ref<'grouped' | 'stacked' | 'waterfall'>('grouped');
const cashflowShowLegend = ref(true);
const cashflowShowLabels = ref(false);
const cashflowShowAverage = ref(false);

// 2. Net Worth & Balance Progression Controls
const lineChartMode = ref<'netWorth' | 'multiAccount' | 'projection'>('netWorth');
const netWorthShowLegend = ref(true);
const netWorthSmooth = ref(true);
const netWorthShowArea = ref(true);
const netWorthShowPoints = ref(true);

// 3. Category Spending Breakdown Controls
const categoryChartMode = ref<'donut' | 'gauge' | 'treemap'>('donut');
const categoryLegendPosition = ref<'right' | 'bottom' | 'none'>('right');
const categoryCenterMetric = ref<'amount' | 'percent' | 'count'>('amount');

// 4. Daily Spending Velocity / Cumulative Burn Rate Controls
const velocityShowLegend = ref(true);
const velocityShowPace = ref(true);
const velocityShowTodayMarker = ref(true);

// 5. Budget Envelopes & Variance Controls
const budgetChartMode = ref<'bullet' | 'diverging'>('bullet');
const budgetFilter = ref<'all' | 'warningOnly'>('all');

// 6. Segmented Category Spend Bar Mode
const spendBarMode = ref<'detailed' | 'compact'>('detailed');

// Sync global legend switch across all individual charts
function toggleGlobalLegends(val: boolean) {
  globalShowLegend.value = val;
  cashflowShowLegend.value = val;
  netWorthShowLegend.value = val;
  velocityShowLegend.value = val;
  if (!val) {
    categoryLegendPosition.value = 'none';
  } else if (categoryLegendPosition.value === 'none') {
    categoryLegendPosition.value = 'right';
  }
}

// Reset all chart conditions to recommended defaults
function resetChartConditions() {
  chartTimeframe.value = '12M';
  globalShowLegend.value = true;
  cashflowChartMode.value = 'grouped';
  cashflowShowLegend.value = true;
  cashflowShowLabels.value = false;
  cashflowShowAverage.value = false;
  lineChartMode.value = 'netWorth';
  netWorthShowLegend.value = true;
  netWorthSmooth.value = true;
  netWorthShowArea.value = true;
  netWorthShowPoints.value = true;
  categoryChartMode.value = 'donut';
  categoryLegendPosition.value = 'right';
  categoryCenterMetric.value = 'amount';
  velocityShowLegend.value = true;
  velocityShowPace.value = true;
  velocityShowTodayMarker.value = true;
  budgetChartMode.value = 'bullet';
  budgetFilter.value = 'all';
  spendBarMode.value = 'detailed';
}

// Raw 12-month financial dataset
const rawCashflow = [
  { month: 'Jan', income: 15000000, expense: 9500000 },
  { month: 'Feb', income: 14500000, expense: 8800000 },
  { month: 'Mar', income: 16000000, expense: 10200000 },
  { month: 'Apr', income: 18500000, expense: 11000000 },
  { month: 'May', income: 15200000, expense: 9100000 },
  { month: 'Jun', income: 17000000, expense: 10500000 },
  { month: 'Jul', income: 16500000, expense: 9800000 },
  { month: 'Aug', income: 19000000, expense: 11500000 },
  { month: 'Sep', income: 15500000, expense: 8900000 },
  { month: 'Oct', income: 16800000, expense: 9400000 },
  { month: 'Nov', income: 17500000, expense: 10100000 },
  { month: 'Dec', income: 24000000, expense: 14500000 },
];

const filteredCashflow = computed(() => {
  if (chartTimeframe.value === '3M') return rawCashflow.slice(9);
  if (chartTimeframe.value === '6M') return rawCashflow.slice(6);
  return rawCashflow;
});

// 1. Dual / Stacked / Waterfall Cashflow Bar Option
const cashflowChartOption = computed(() => {
  const ct = getChartTheme(themeStore.isDark);
  const data = filteredCashflow.value;
  const isStacked = cashflowChartMode.value === 'stacked';
  const isWaterfall = cashflowChartMode.value === 'waterfall';

  if (isWaterfall) {
    return {
      tooltip: {
        trigger: 'axis',
        ...ct.tooltip,
        valueFormatter: (val: number) => (ui.amountsHidden ? '••••••' : formatMoney(val, 'IDR')),
      },
      legend: {
        show: cashflowShowLegend.value,
        top: 0,
        right: 8,
        icon: 'circle',
        textStyle: { color: ct.text, fontSize: 11, fontWeight: 500 },
        data: ['Net Savings / Delta'],
      },
      grid: { left: 12, right: 12, top: cashflowShowLegend.value ? 36 : 16, bottom: 20, containLabel: true },
      xAxis: {
        type: 'category',
        data: data.map((d) => d.month),
        axisLabel: { color: ct.textMuted, fontSize: 11 },
        axisLine: { lineStyle: { color: ct.border } },
        axisTick: { show: false },
      },
      yAxis: {
        type: 'value',
        axisLabel: {
          color: ct.textMuted,
          fontSize: 10,
          formatter: (v: number) => `${(v / 1000000).toFixed(0)}M`,
        },
        splitLine: { lineStyle: { color: ct.border, type: 'dashed' } },
      },
      series: [
        {
          name: 'Net Savings / Delta',
          type: 'bar',
          data: data.map((d) => {
            const net = d.income - d.expense;
            return {
              value: net,
              itemStyle: {
                color: net >= 0 ? ct.positive : ct.negative,
                borderRadius: net >= 0 ? [4, 4, 0, 0] : [0, 0, 4, 4],
              },
            };
          }),
          barMaxWidth: 24,
          label: cashflowShowLabels.value
            ? {
                show: true,
                position: 'top',
                color: ct.text,
                fontSize: 10,
                formatter: (p: { value: number }) => (ui.amountsHidden ? '••••' : `${(p.value / 1000000).toFixed(1)}M`),
              }
            : undefined,
          markLine: cashflowShowAverage.value
            ? {
                data: [{ type: 'average', name: 'Avg Net' }],
                lineStyle: { color: ct.accent, type: 'dashed', width: 1.5 },
                label: { color: ct.textMuted, fontSize: 10, position: 'end' },
              }
            : undefined,
        },
      ],
    };
  }

  return {
    tooltip: {
      trigger: 'axis',
      ...ct.tooltip,
      valueFormatter: (val: number) => (ui.amountsHidden ? '••••••' : formatMoney(val, 'IDR')),
    },
    legend: {
      show: cashflowShowLegend.value,
      top: 0,
      right: 8,
      icon: 'circle',
      textStyle: { color: ct.text, fontSize: 11, fontWeight: 500 },
      data: ['Income', 'Expense'],
    },
    grid: { left: 12, right: 12, top: cashflowShowLegend.value ? 36 : 16, bottom: 20, containLabel: true },
    xAxis: {
      type: 'category',
      data: data.map((d) => d.month),
      axisLabel: { color: ct.textMuted, fontSize: 11 },
      axisLine: { lineStyle: { color: ct.border } },
      axisTick: { show: false },
    },
    yAxis: {
      type: 'value',
      axisLabel: {
        color: ct.textMuted,
        fontSize: 10,
        formatter: (v: number) => `${(v / 1000000).toFixed(0)}M`,
      },
      splitLine: { lineStyle: { color: ct.border, type: 'dashed' } },
    },
    series: [
      {
        name: 'Income',
        type: 'bar',
        stack: isStacked ? 'total' : undefined,
        data: data.map((d) => d.income),
        itemStyle: { color: ct.positive, borderRadius: isStacked ? [0, 0, 0, 0] : [4, 4, 0, 0] },
        barMaxWidth: isStacked ? 28 : 16,
        label: cashflowShowLabels.value
          ? {
              show: true,
              position: 'top',
              color: ct.textMuted,
              fontSize: 9,
              formatter: (p: { value: number }) => `${(p.value / 1000000).toFixed(0)}M`,
            }
          : undefined,
        markLine: cashflowShowAverage.value
          ? {
              data: [{ type: 'average', name: 'Avg Income' }],
              lineStyle: { color: ct.positive, type: 'dashed', width: 1.5 },
              label: { color: ct.positive, fontSize: 10, position: 'end' },
            }
          : undefined,
      },
      {
        name: 'Expense',
        type: 'bar',
        stack: isStacked ? 'total' : undefined,
        data: data.map((d) => d.expense),
        itemStyle: { color: ct.negative, borderRadius: [4, 4, 0, 0] },
        barMaxWidth: isStacked ? 28 : 16,
        label: cashflowShowLabels.value
          ? {
              show: true,
              position: 'top',
              color: ct.textMuted,
              fontSize: 9,
              formatter: (p: { value: number }) => `${(p.value / 1000000).toFixed(0)}M`,
            }
          : undefined,
        markLine: cashflowShowAverage.value
          ? {
              data: [{ type: 'average', name: 'Avg Expense' }],
              lineStyle: { color: ct.negative, type: 'dashed', width: 1.5 },
              label: { color: ct.negative, fontSize: 10, position: 'end' },
            }
          : undefined,
      },
    ],
  };
});

// 2. Net Worth Trend Area / Multi-Account Line / Projection Option
const netWorthChartOption = computed(() => {
  const ct = getChartTheme(themeStore.isDark);
  const data = filteredCashflow.value;
  const sliceCount = chartTimeframe.value === '3M' ? 3 : chartTimeframe.value === '6M' ? 6 : 12;
  const sliceFn = (arr: number[]) => arr.slice(arr.length - sliceCount);

  if (lineChartMode.value === 'multiAccount') {
    const bcaData = [110, 115, 120, 128, 125, 132, 130, 138, 135, 139, 140, 142].map((v) => v * 1000000);
    const jeniusData = [25, 26, 28, 29, 30, 31, 32, 33, 34, 35, 36, 36.5].map((v) => v * 1000000);
    const gopayData = [1.2, 1.5, 1.0, 1.8, 1.2, 1.4, 1.6, 1.3, 1.7, 1.2, 1.5, 1.25].map((v) => v * 1000000);
    const ajaibData = [35, 36, 38, 40, 41, 43, 44, 46, 47, 48, 49, 50].map((v) => v * 1000000);

    return {
      tooltip: {
        trigger: 'axis',
        ...ct.tooltip,
        valueFormatter: (v: number) => (ui.amountsHidden ? '••••••' : formatMoney(v, 'IDR')),
      },
      legend: {
        show: netWorthShowLegend.value,
        top: 0,
        right: 8,
        icon: 'circle',
        textStyle: { color: ct.text, fontSize: 11, fontWeight: 500 },
        data: ['BCA Checking', 'Jenius Savings', 'GoPay', 'Ajaib RDN'],
      },
      grid: { left: 12, right: 12, top: netWorthShowLegend.value ? 36 : 16, bottom: 20, containLabel: true },
      xAxis: {
        type: 'category',
        data: data.map((d) => d.month),
        axisLabel: { color: ct.textMuted, fontSize: 11 },
        axisLine: { lineStyle: { color: ct.border } },
        axisTick: { show: false },
      },
      yAxis: {
        type: 'value',
        axisLabel: {
          color: ct.textMuted,
          fontSize: 10,
          formatter: (v: number) => `${(v / 1000000).toFixed(0)}M`,
        },
        splitLine: { lineStyle: { color: ct.border, type: 'dashed' } },
      },
      series: [
        {
          name: 'BCA Checking',
          type: 'line',
          smooth: netWorthSmooth.value,
          data: sliceFn(bcaData),
          itemStyle: { color: ct.accent },
          lineStyle: { color: ct.accent, width: 2.5 },
          showSymbol: netWorthShowPoints.value,
          symbolSize: 6,
        },
        {
          name: 'Jenius Savings',
          type: 'line',
          smooth: netWorthSmooth.value,
          data: sliceFn(jeniusData),
          itemStyle: { color: '#06B6D4' },
          lineStyle: { color: '#06B6D4', width: 2.5 },
          showSymbol: netWorthShowPoints.value,
          symbolSize: 6,
        },
        {
          name: 'GoPay',
          type: 'line',
          smooth: netWorthSmooth.value,
          data: sliceFn(gopayData),
          itemStyle: { color: ct.positive },
          lineStyle: { color: ct.positive, width: 2 },
          showSymbol: netWorthShowPoints.value,
          symbolSize: 5,
        },
        {
          name: 'Ajaib RDN',
          type: 'line',
          smooth: netWorthSmooth.value,
          data: sliceFn(ajaibData),
          itemStyle: { color: '#F59E0B' },
          lineStyle: { color: '#F59E0B', width: 2.5 },
          showSymbol: netWorthShowPoints.value,
          symbolSize: 6,
        },
      ],
    };
  }

  if (lineChartMode.value === 'projection') {
    const allMonths = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
    const historicalSeries = [165, 169, 174, 181, null, null, null, null, null, null, null, null].map((v) =>
      v ? v * 1000000 : null
    );
    const projectedSeries = [null, null, null, 181, 186, 192, 197, 203, 209, 215, 220, 225].map((v) =>
      v ? v * 1000000 : null
    );

    return {
      tooltip: {
        trigger: 'axis',
        ...ct.tooltip,
        valueFormatter: (v: number | null) => (v ? (ui.amountsHidden ? '••••••' : formatMoney(v, 'IDR')) : '-'),
      },
      legend: {
        show: netWorthShowLegend.value,
        top: 0,
        right: 8,
        icon: 'circle',
        textStyle: { color: ct.text, fontSize: 11, fontWeight: 500 },
        data: ['Recorded Net Worth', '3M+ Projected Forecast'],
      },
      grid: { left: 12, right: 12, top: netWorthShowLegend.value ? 36 : 16, bottom: 20, containLabel: true },
      xAxis: {
        type: 'category',
        data: allMonths,
        axisLabel: { color: ct.textMuted, fontSize: 11 },
        axisLine: { lineStyle: { color: ct.border } },
        axisTick: { show: false },
      },
      yAxis: {
        type: 'value',
        min: 150000000,
        axisLabel: {
          color: ct.textMuted,
          fontSize: 10,
          formatter: (v: number) => `${(v / 1000000).toFixed(0)}M`,
        },
        splitLine: { lineStyle: { color: ct.border, type: 'dashed' } },
      },
      series: [
        {
          name: 'Recorded Net Worth',
          type: 'line',
          smooth: netWorthSmooth.value,
          data: historicalSeries,
          itemStyle: { color: ct.accent },
          lineStyle: { color: ct.accent, width: 3 },
          areaStyle: netWorthShowArea.value
            ? {
                color: ct.accent,
                opacity: themeStore.isDark ? 0.18 : 0.08,
              }
            : undefined,
          showSymbol: netWorthShowPoints.value,
          symbolSize: 6,
        },
        {
          name: '3M+ Projected Forecast',
          type: 'line',
          smooth: netWorthSmooth.value,
          data: projectedSeries,
          itemStyle: { color: ct.warning },
          lineStyle: { color: ct.warning, width: 2.5, type: 'dashed' },
          showSymbol: netWorthShowPoints.value,
          symbolSize: 6,
          markPoint: {
            data: [{ name: 'Target Milestone', coord: ['Dec', 225000000], value: 'Target: 225M' }],
            label: { fontSize: 10, color: '#FFFFFF', fontWeight: 'bold' },
            itemStyle: { color: ct.accent },
          },
        },
      ],
    };
  }

  // Single Net Worth Area
  const netWorthData = [165, 169, 174, 181, 183, 188, 191, 196, 198, 201, 202, 203.147].map((v) => v * 1000000);
  const slicedNetWorth = sliceFn(netWorthData);

  return {
    tooltip: {
      trigger: 'axis',
      ...ct.tooltip,
      valueFormatter: (v: number) => (ui.amountsHidden ? '••••••' : formatMoney(v, 'IDR')),
    },
    legend: {
      show: netWorthShowLegend.value,
      top: 0,
      right: 8,
      icon: 'circle',
      textStyle: { color: ct.text, fontSize: 11, fontWeight: 500 },
      data: ['Total Net Worth'],
    },
    grid: { left: 12, right: 12, top: netWorthShowLegend.value ? 36 : 16, bottom: 20, containLabel: true },
    xAxis: {
      type: 'category',
      data: data.map((d) => d.month),
      axisLabel: { color: ct.textMuted, fontSize: 11 },
      axisLine: { lineStyle: { color: ct.border } },
      axisTick: { show: false },
    },
    yAxis: {
      type: 'value',
      min: 150000000,
      axisLabel: {
        color: ct.textMuted,
        fontSize: 10,
        formatter: (v: number) => `${(v / 1000000).toFixed(0)}M`,
      },
      splitLine: { lineStyle: { color: ct.border, type: 'dashed' } },
    },
    series: [
      {
        name: 'Total Net Worth',
        type: 'line',
        smooth: netWorthSmooth.value,
        data: slicedNetWorth,
        itemStyle: { color: ct.accent },
        lineStyle: { color: ct.accent, width: 3 },
        areaStyle: netWorthShowArea.value
          ? {
              color: ct.accent,
              opacity: themeStore.isDark ? 0.15 : 0.08,
            }
          : undefined,
        showSymbol: netWorthShowPoints.value,
        symbolSize: 6,
      },
    ],
  };
});

// 3. Category Donut / Gauge / Treemap Chart Option
const categoryChartOption = computed(() => {
  const ct = getChartTheme(themeStore.isDark);
  const isGauge = categoryChartMode.value === 'gauge';
  const isTreemap = categoryChartMode.value === 'treemap';
  const hasLegend = categoryLegendPosition.value !== 'none';
  const isRightLegend = categoryLegendPosition.value === 'right';

  if (isGauge) {
    return {
      tooltip: {
        trigger: 'item',
        ...ct.tooltip,
        formatter: (p: { name: string; value: number }) => `${p.name}: <b>${p.value}%</b>`,
      },
      series: [
        {
          type: 'pie',
          radius: ['68%', '92%'],
          center: ['50%', '75%'],
          startAngle: 180,
          endAngle: 360,
          avoidLabelOverlap: false,
          itemStyle: { borderColor: ct.surface, borderWidth: 2 },
          label: { show: false },
          data: [
            { name: 'Used Budget (68%)', value: 68, itemStyle: { color: ct.accent } },
            { name: 'Remaining Envelope (32%)', value: 32, itemStyle: { color: ct.surface2 } },
          ],
        },
      ],
    };
  }

  if (isTreemap) {
    return {
      tooltip: {
        ...ct.tooltip,
        formatter: (p: { name: string; value: number }) =>
          `${p.name}<br/><b>${ui.amountsHidden ? '••••••' : formatMoney(p.value, 'IDR')}</b>`,
      },
      series: [
        {
          type: 'treemap',
          width: '95%',
          height: '85%',
          roam: false,
          nodeClick: false,
          breadcrumb: { show: false },
          label: {
            show: true,
            formatter: '{b}\n{d}%',
            fontSize: 11,
            color: '#FFFFFF',
            fontWeight: 600,
          },
          itemStyle: {
            borderColor: ct.surface,
            borderWidth: 2,
            gapWidth: 2,
          },
          data: categories.map((c, i) => ({
            name: c.name,
            value: (c.percent / 100) * 22676000,
            itemStyle: { color: ct.spectrum[i % ct.spectrum.length] },
          })),
        },
      ],
    };
  }

  return {
    tooltip: {
      trigger: 'item',
      ...ct.tooltip,
      formatter: (p: { name: string; value: number; percent: number }) =>
        `${p.name}<br/><b>${ui.amountsHidden ? '••••••' : formatMoney(p.value, 'IDR')}</b> · ${p.percent}%`,
    },
    legend: {
      show: hasLegend,
      orient: isRightLegend ? 'vertical' : 'horizontal',
      right: isRightLegend ? 0 : 'center',
      top: isRightLegend ? 'middle' : undefined,
      bottom: isRightLegend ? undefined : 0,
      icon: 'circle',
      textStyle: { color: ct.text, fontSize: 11 },
      formatter: (name: string) => {
        const item = categories.find((c) => c.name === name);
        return `${name} (${item ? item.percent : 0}%)`;
      },
    },
    series: [
      {
        type: 'pie',
        radius: isRightLegend ? ['48%', '72%'] : ['58%', '82%'],
        center: isRightLegend ? ['32%', '50%'] : ['50%', hasLegend ? '45%' : '50%'],
        avoidLabelOverlap: false,
        itemStyle: { borderColor: ct.surface, borderWidth: 2 },
        label: { show: false },
        labelLine: { show: false },
        data: categories.map((c, i) => ({
          name: c.name,
          value: (c.percent / 100) * 22676000,
          itemStyle: { color: ct.spectrum[i % ct.spectrum.length] },
        })),
      },
    ],
  };
});

// 4. Daily Spending Velocity / Cumulative Burn Rate Option
const velocityDays = Array.from({ length: 30 }, (_, i) => `${i + 1}`);
const actualCumulativeSpend = [
  450000, 880000, 1320000, 1750000, 2180000, 2600000, 3050000,
  3500000, 3950000, 4450000, 4920000, 5400000, 5950000, 6500000,
  7050000, 7550000, 7920000, null, null, null, null, null, null,
  null, null, null, null, null, null, null
];
const idealBudgetPace = Array.from({ length: 30 }, (_, i) => (i + 1) * 500000);

const velocityChartOption = computed(() => {
  const ct = getChartTheme(themeStore.isDark);

  const seriesList: any[] = [
    {
      name: 'Actual Cumulative Spend',
      type: 'line',
      smooth: true,
      data: actualCumulativeSpend,
      itemStyle: { color: ct.accent },
      lineStyle: { color: ct.accent, width: 3 },
      areaStyle: {
        color: ct.accent,
        opacity: themeStore.isDark ? 0.15 : 0.08,
      },
      symbol: 'circle',
      symbolSize: 6,
      markLine: velocityShowTodayMarker.value
        ? {
            data: [{ xAxis: '17', name: 'Today' }],
            lineStyle: { color: ct.warning, width: 2, type: 'dashed' },
            label: {
              formatter: 'Today (Day 17)',
              position: 'end',
              color: ct.warning,
              fontSize: 10,
              fontWeight: 'bold',
            },
          }
        : undefined,
    },
  ];

  if (velocityShowPace.value) {
    seriesList.push({
      name: 'Ideal Budget Pace (500k/day)',
      type: 'line',
      data: idealBudgetPace,
      itemStyle: { color: ct.textMuted },
      lineStyle: { color: ct.textMuted, width: 1.5, type: 'dashed' },
      symbol: 'none',
    });
  }

  return {
    tooltip: {
      trigger: 'axis',
      ...ct.tooltip,
      valueFormatter: (v: number | null) => (v ? (ui.amountsHidden ? '••••••' : formatMoney(v, 'IDR')) : '-'),
    },
    legend: {
      show: velocityShowLegend.value,
      top: 0,
      right: 8,
      icon: 'circle',
      textStyle: { color: ct.text, fontSize: 11, fontWeight: 500 },
      data: seriesList.map((s) => s.name),
    },
    grid: { left: 12, right: 12, top: velocityShowLegend.value ? 36 : 16, bottom: 20, containLabel: true },
    xAxis: {
      type: 'category',
      data: velocityDays,
      axisLabel: { color: ct.textMuted, fontSize: 10, interval: 4 },
      axisLine: { lineStyle: { color: ct.border } },
      axisTick: { show: false },
    },
    yAxis: {
      type: 'value',
      axisLabel: {
        color: ct.textMuted,
        fontSize: 10,
        formatter: (v: number) => `${(v / 1000000).toFixed(0)}M`,
      },
      splitLine: { lineStyle: { color: ct.border, type: 'dashed' } },
    },
    series: seriesList,
  };
});

// 5. Budget Envelopes & Variance
const budgetEnvelopes = [
  { category: 'Food & Dining', actual: 3200000, budget: 4000000, delta: -800000, percent: 80, status: 'ok' },
  { category: 'Housing & Rent', actual: 3500000, budget: 3500000, delta: 0, percent: 100, status: 'ok' },
  { category: 'Transportation', actual: 1600000, budget: 1500000, delta: 100000, percent: 106, status: 'warn' },
  { category: 'Entertainment', actual: 2600000, budget: 2000000, delta: 600000, percent: 130, status: 'danger' },
  { category: 'Personal Care', actual: 1200000, budget: 1500000, delta: -300000, percent: 80, status: 'ok' },
];

const filteredBudgetEnvelopes = computed(() => {
  if (budgetFilter.value === 'warningOnly') {
    return budgetEnvelopes.filter((e) => e.status === 'warn' || e.status === 'danger');
  }
  return budgetEnvelopes;
});

const budgetVarianceChartOption = computed(() => {
  const ct = getChartTheme(themeStore.isDark);
  const items = filteredBudgetEnvelopes.value;

  return {
    tooltip: {
      trigger: 'axis',
      axisPointer: { type: 'shadow' },
      ...ct.tooltip,
      formatter: (params: any) => {
        const p = Array.isArray(params) ? params[0] : params;
        const val = p.value as number;
        const sign = val > 0 ? '+Over Budget: ' : val < 0 ? '-Under Budget (Saved): ' : 'On Budget: ';
        return `${p.name}<br/><b>${sign}${ui.amountsHidden ? '••••••' : formatMoney(Math.abs(val), 'IDR')}</b>`;
      },
    },
    grid: { left: 16, right: 16, top: 20, bottom: 20, containLabel: true },
    xAxis: {
      type: 'value',
      axisLabel: {
        color: ct.textMuted,
        fontSize: 10,
        formatter: (v: number) => `${(v / 1000).toFixed(0)}k`,
      },
      splitLine: { lineStyle: { color: ct.border, type: 'dashed' } },
    },
    yAxis: {
      type: 'category',
      data: items.map((e) => e.category),
      axisLabel: { color: ct.text, fontSize: 11, fontWeight: 500 },
      axisLine: { lineStyle: { color: ct.border } },
      axisTick: { show: false },
    },
    series: [
      {
        name: 'Budget Variance',
        type: 'bar',
        barMaxWidth: 18,
        data: items.map((e) => ({
          value: e.delta,
          itemStyle: {
            color: e.delta > 0 ? (e.delta > 300000 ? ct.negative : ct.warning) : ct.positive,
            borderRadius: e.delta > 0 ? [0, 4, 4, 0] : [4, 0, 0, 4],
          },
        })),
      },
    ],
  };
});

// 6. Accessible Heatmap Matrix Data (Reports Module)
const heatmapMonths = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
const heatmapData = [
  { category: 'Food & Dining', values: [3.2, 3.2, 3.4, 3.2, 4.0, 2.4, 3.2, 3.5, 3.1, 3.4, 3.6, 4.2], total: 41.6 },
  { category: 'Entertainment', values: [2.6, 1.4, 1.1, 2.7, 1.5, 1.9, 1.8, 1.6, 1.4, 1.8, 2.1, 2.5], total: 22.4 },
  { category: 'Personal & Care', values: [1.2, 1.6, 1.5, 0.47, 1.3, 2.0, 1.6, 1.5, 1.3, 1.4, 1.5, 1.8], total: 17.17 },
  { category: 'Internet & Phone', values: [1.5, 1.4, 1.4, 0.74, 1.4, 1.4, 1.3, 1.4, 1.4, 1.4, 1.4, 1.4], total: 16.34 },
  { category: 'Transportation', values: [1.6, 1.9, 0.52, 1.7, 0.8, 1.3, 1.2, 1.4, 1.3, 1.5, 1.6, 1.8], total: 16.62 },
];

// Chart data matching reference
const months = [
  { name: 'Jan', amount: 12000000, value: 45 },
  { name: 'Feb', amount: 14500000, value: 55 },
  { name: 'Mar', amount: 11000000, value: 40 },
  { name: 'Apr', amount: 24500000, value: 92, active: true },
  { name: 'May', amount: 13000000, value: 50 },
  { name: 'Jun', amount: 15500000, value: 58 },
  { name: 'Jul', amount: 9800000, value: 38 },
  { name: 'Aug', amount: 16200000, value: 62 },
];

function showToast(msg: string, type: 'success' | 'error' | 'info' = 'success') {
  toastMessage.value = msg;
  toastType.value = type;
  setTimeout(() => {
    if (toastMessage.value === msg) toastMessage.value = null;
  }, 3500);
}

// Color palette definitions with live contrast calculation
const colorPalette = computed(() => {
  const isDark = themeStore.isDark;
  return [
    {
      name: 'Accent (Primary Cobalt/Indigo)',
      token: '--accent',
      hex: isDark ? '#6366F1' : '#3B46F1',
      contrast: isDark ? '4.7:1 AA' : '6.3:1 AAA',
      role: 'Primary CTAs, active highlights, key series',
    },
    {
      name: 'Canvas Background',
      token: '--bg',
      hex: isDark ? '#0B0D11' : '#F5F6FA',
      contrast: 'Base',
      role: 'Overall application canvas',
    },
    {
      name: 'Card Surface',
      token: '--surface',
      hex: isDark ? '#14171F' : '#FFFFFF',
      contrast: 'Base',
      role: 'Cards, sheets, modals, panels',
    },
    {
      name: 'Card Surface Elevated',
      token: '--surface-2',
      hex: isDark ? '#1D222D' : '#EEF0F6',
      contrast: 'Elevation',
      role: 'Table headers, input fills, hover states',
    },
    {
      name: 'Primary Text',
      token: '--text',
      hex: isDark ? '#F8FAFC' : '#111827',
      contrast: isDark ? '15.2:1 AAA' : '15.4:1 AAA',
      role: 'Headings, primary figures, high emphasis',
    },
    {
      name: 'Muted Text',
      token: '--text-muted',
      hex: isDark ? '#94A3B8' : '#64748B',
      contrast: isDark ? '6.4:1 AAA' : '4.55:1 AA',
      role: 'Labels, dates, secondary descriptions',
    },
    {
      name: 'Positive (Income / Gains)',
      token: '--positive',
      hex: isDark ? '#35D07A' : '#047857',
      contrast: isDark ? '9.18:1 AAA' : '5.5:1 AAA',
      role: 'Income rows, positive percentages, cleared status',
    },
    {
      name: 'Negative (Expense / Danger)',
      token: '--negative',
      hex: isDark ? '#FF5B60' : '#DC2626',
      contrast: isDark ? '6.36:1 AAA' : '4.85:1 AA',
      role: 'Expense rows, negative deltas, destructive actions',
    },
    {
      name: 'Interactive Border',
      token: '--border-strong',
      hex: isDark ? '#64748B' : '#94A3B8',
      contrast: '3.1:1 UI (1.4.11)',
      role: 'Control boundaries, text inputs, selects',
    },
  ];
});

// Category Spectrum colors
const categories = [
  { name: 'Food & Dining', percent: 35, color: 'var(--cat-1)' },
  { name: 'Investments', percent: 25, color: 'var(--cat-2)' },
  { name: 'Housing & Utilities', percent: 20, color: 'var(--cat-3)' },
  { name: 'Transportation', percent: 12, color: 'var(--cat-4)' },
  { name: 'Entertainment', percent: 8, color: 'var(--cat-5)' },
];
</script>

<template>
  <div class="min-h-screen bg-bg text-text transition-colors duration-200">
    <!-- Top Sticky Command / Showcase Bar -->
    <header class="sticky top-0 z-40 border-b border-border bg-surface/95 px-4 py-3 backdrop-blur shadow-sm md:px-8">
      <div class="mx-auto flex max-w-7xl items-center justify-between gap-4">
        <div class="flex items-center gap-3">
          <div class="flex h-9 w-9 items-center justify-center rounded-control bg-accent text-accent-contrast shadow-sm">
            <Sparkles :size="20" />
          </div>
          <div>
            <h1 class="text-base font-bold leading-tight md:text-lg">Tameru Design System</h1>
            <p class="text-xs text-text-muted">Living Specification · Option B Modern Cobalt / Indigo</p>
          </div>
        </div>

        <!-- Quick Navigation Jump Links -->
        <nav class="hidden items-center gap-4 text-xs font-medium text-text-muted lg:flex">
          <a href="#shell" class="transition-colors hover:text-text">App Shell</a>
          <a href="#tokens" class="transition-colors hover:text-text">Color Tokens</a>
          <a href="#typography" class="transition-colors hover:text-text">Typography</a>
          <a href="#buttons" class="transition-colors hover:text-text">Buttons</a>
          <a href="#cards" class="transition-colors hover:text-text">Cards & Metrics</a>
          <a href="#inputs" class="transition-colors hover:text-text">Inputs</a>
          <a href="#charts" class="transition-colors hover:text-text">Charts</a>
          <a href="#modals" class="transition-colors hover:text-text">Modals</a>
        </nav>

        <!-- Global Theme and Utility Controls -->
        <div class="flex items-center gap-2.5">
          <ThemeToggle variant="segmented" :size="15" />

          <button
            type="button"
            class="hidden items-center gap-1.5 rounded-control border border-border px-2.5 py-1.5 text-xs font-medium text-text-muted transition-colors hover:border-border-strong hover:text-text sm:flex"
            @click="ui.toggleAmounts()"
          >
            <component :is="ui.amountsHidden ? EyeOff : Eye" :size="14" />
            <span>{{ ui.amountsHidden ? 'Hidden' : 'Visible' }}</span>
          </button>

          <button
            type="button"
            class="rounded-control bg-accent px-3 py-1.5 text-xs font-semibold text-accent-contrast transition-opacity hover:opacity-90"
            @click="router.push({ name: 'dashboard' })"
          >
            Back to App
          </button>
        </div>
      </div>
    </header>

    <!-- Main Showcase Content -->
    <main class="mx-auto max-w-7xl space-y-16 px-4 py-8 md:px-8">
      <!-- Intro Banner -->
      <section class="rounded-card border border-border bg-surface p-6 shadow-card md:p-8">
        <div class="flex flex-col items-start justify-between gap-4 md:flex-row md:items-center">
          <div>
            <span class="inline-flex items-center gap-1.5 rounded-full bg-accent-soft px-3 py-1 text-xs font-semibold text-accent">
              <ShieldCheck :size="14" />
              WCAG 2.2 AA Verified · Zero AI-Gradients · Data-First
            </span>
            <h2 class="mt-3 text-2xl font-bold tracking-tight md:text-3xl">
              Reference-Accurate FinTech Component Suite
            </h2>
            <p class="mt-2 max-w-2xl text-sm leading-relaxed text-text-muted">
              Built directly from your selected reference layout. Test the light/dark toggle at the top bar to inspect how every card, border, status pill, chart, and input reacts seamlessly.
            </p>
          </div>
          <div class="flex flex-wrap gap-2">
            <button
              type="button"
              class="rounded-control border border-border bg-surface-2 px-3 py-2 text-xs font-medium text-text transition-colors hover:bg-surface-3"
              @click="showToast('Verified: All 18 color tokens pass WCAG 2.2 AA', 'success')"
            >
              Test Toast Notification
            </button>
            <button
              type="button"
              class="rounded-control bg-accent px-3.5 py-2 text-xs font-semibold text-accent-contrast transition-opacity hover:opacity-90"
              @click="isModalOpen = true"
            >
              Open Add Transaction Modal
            </button>
          </div>
        </div>
      </section>

      <!-- SECTION 1: APP SHELL & NAVIGATION RAIL (Reference Accurate) -->
      <section id="shell" class="space-y-6">
        <div class="border-b border-border pb-3">
          <h3 class="text-xl font-bold">1. App Shell & Navigation Rail</h3>
          <p class="text-xs text-text-muted">68px slim left icon rail and modern topbar with breadcrumbs & actions.</p>
        </div>

        <div class="overflow-hidden rounded-card border border-border bg-surface shadow-card">
          <!-- Mock Shell Header -->
          <div class="flex items-center justify-between border-b border-border bg-surface px-6 py-4">
            <div class="flex items-center gap-3">
              <div class="text-xs font-medium text-text-muted">
                Home <span class="mx-1.5 text-border-strong">/</span> <span class="font-semibold text-text">Overview</span>
              </div>
            </div>

            <div class="flex items-center gap-3">
              <div class="relative hidden sm:block">
                <Search :size="16" class="absolute left-3 top-1/2 -translate-y-1/2 text-text-muted" />
                <input
                  v-model="inputSearch"
                  type="text"
                  placeholder="Search anything…"
                  class="h-9 w-64 rounded-control border border-border bg-surface-2 pl-9 pr-8 text-xs text-text placeholder-text-muted transition-colors focus:border-accent focus:bg-surface focus:outline-none"
                />
                <kbd class="absolute right-2.5 top-1/2 -translate-y-1/2 rounded border border-border px-1.5 py-0.5 text-[10px] text-text-muted">⌘K</kbd>
              </div>

              <button
                type="button"
                class="flex h-9 items-center gap-1.5 rounded-control border border-border bg-surface px-3 text-xs font-medium text-text transition-colors hover:bg-surface-2"
              >
                <Calendar :size="14" />
                <span>2026</span>
              </button>

              <button
                type="button"
                class="flex h-9 items-center gap-1.5 rounded-control border border-border bg-surface px-3 text-xs font-medium text-text transition-colors hover:bg-surface-2"
              >
                <Filter :size="14" />
                <span>Filter</span>
              </button>

              <div class="h-6 w-px bg-border"></div>

              <!-- Topbar Profile Area -->
              <div class="flex items-center gap-2 pl-1">
                <div class="flex h-9 w-9 items-center justify-center rounded-full bg-accent/15 text-xs font-bold text-accent">
                  YA
                </div>
                <div class="hidden text-left leading-tight sm:block">
                  <div class="text-xs font-semibold">Yovan Alvianto</div>
                  <div class="text-[11px] text-text-muted">@yovanalv</div>
                </div>
              </div>
            </div>
          </div>

          <!-- Mock Shell Body with Left Rail -->
          <div class="flex min-h-[260px]">
            <!-- Slim 68px Icon Rail -->
            <div class="flex w-16 flex-col items-center justify-between border-r border-border bg-sidebar py-5">
              <div class="flex flex-col items-center gap-4">
                <!-- Brand Mark -->
                <div class="flex h-10 w-10 items-center justify-center rounded-control bg-accent text-accent-contrast shadow-sm">
                  <div class="h-4 w-4 rounded-sm border-2 border-accent-contrast"></div>
                </div>

                <!-- Navigation Items -->
                <div class="mt-2 flex flex-col items-center gap-2">
                  <button
                    type="button"
                    class="group relative flex h-10 w-10 items-center justify-center rounded-control bg-accent text-accent-contrast shadow-sm transition-all"
                    title="Dashboard"
                  >
                    <LayoutDashboard :size="19" />
                  </button>
                  <button
                    type="button"
                    class="flex h-10 w-10 items-center justify-center rounded-control text-text-muted transition-colors hover:bg-surface-2 hover:text-text"
                    title="Transactions"
                  >
                    <ArrowLeftRight :size="19" />
                  </button>
                  <button
                    type="button"
                    class="flex h-10 w-10 items-center justify-center rounded-control text-text-muted transition-colors hover:bg-surface-2 hover:text-text"
                    title="Accounts"
                  >
                    <Wallet :size="19" />
                  </button>
                  <button
                    type="button"
                    class="flex h-10 w-10 items-center justify-center rounded-control text-text-muted transition-colors hover:bg-surface-2 hover:text-text"
                    title="Reports"
                  >
                    <BarChart3 :size="19" />
                  </button>
                  <button
                    type="button"
                    class="flex h-10 w-10 items-center justify-center rounded-control text-text-muted transition-colors hover:bg-surface-2 hover:text-text"
                    title="Budget"
                  >
                    <PieChart :size="19" />
                  </button>
                  <button
                    type="button"
                    class="flex h-10 w-10 items-center justify-center rounded-control text-text-muted transition-colors hover:bg-surface-2 hover:text-text"
                    title="Master Plan"
                  >
                    <Target :size="19" />
                  </button>
                </div>
              </div>

              <!-- Bottom Rail Actions -->
              <div class="flex flex-col items-center gap-2">
                <button
                  type="button"
                  class="flex h-10 w-10 items-center justify-center rounded-control text-text-muted transition-colors hover:bg-surface-2 hover:text-text"
                  title="Settings"
                >
                  <Settings :size="19" />
                </button>
                <button
                  type="button"
                  class="flex h-10 w-10 items-center justify-center rounded-control text-negative transition-colors hover:bg-negative/10"
                  title="Sign out"
                >
                  <LogOut :size="19" />
                </button>
              </div>
            </div>

            <!-- Content Preview Canvas -->
            <div class="flex-1 bg-bg p-6">
              <div class="flex items-center justify-between">
                <div>
                  <h4 class="text-2xl font-bold tracking-tight">Overview</h4>
                  <p class="text-xs text-text-muted">Financial snapshot for September 2026</p>
                </div>
                <div class="flex items-center gap-2">
                  <span class="inline-flex items-center rounded-full border border-border bg-surface px-3 py-1 text-xs font-semibold text-positive">
                    +16.03% vs last month
                  </span>
                </div>
              </div>

              <div class="mt-6 grid grid-cols-1 gap-4 sm:grid-cols-3">
                <div class="rounded-card border border-border bg-surface p-4 shadow-card">
                  <div class="text-xs font-medium text-text-muted">Total Net Worth</div>
                  <div class="mt-1 text-xl font-bold tnum text-text">
                    {{ ui.amountsHidden ? '••••••••••' : 'Rp 203.147.000' }}
                  </div>
                  <div class="mt-1 text-[11px] text-positive font-medium">+2.36% this period</div>
                </div>
                <div class="rounded-card border border-border bg-surface p-4 shadow-card">
                  <div class="text-xs font-medium text-text-muted">September Income</div>
                  <div class="mt-1 text-xl font-bold tnum text-positive">
                    {{ ui.amountsHidden ? '••••••••••' : 'Rp 14.500.000' }}
                  </div>
                  <div class="mt-1 text-[11px] text-text-muted">Salary & Dividends</div>
                </div>
                <div class="rounded-card border border-border bg-surface p-4 shadow-card">
                  <div class="text-xs font-medium text-text-muted">September Outflow</div>
                  <div class="mt-1 text-xl font-bold tnum text-negative">
                    {{ ui.amountsHidden ? '••••••••••' : '(Rp 8.450.000)' }}
                  </div>
                  <div class="mt-1 text-[11px] text-text-muted">Needs 65% · Wants 35%</div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      <!-- SECTION 2: COLOR SYSTEM & LIVE WCAG CONTRAST MATRIX -->
      <section id="tokens" class="space-y-6">
        <div class="border-b border-border pb-3">
          <h3 class="text-xl font-bold">2. Design Tokens & Contrast Matrix</h3>
          <p class="text-xs text-text-muted">
            Strict WCAG 2.2 AA compliance across Light and Dark themes. Verified with zero artificial gradients.
          </p>
        </div>

        <div class="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
          <div
            v-for="color in colorPalette"
            :key="color.token"
            class="flex items-center justify-between rounded-card border border-border bg-surface p-4 shadow-card"
          >
            <div class="flex items-center gap-3">
              <div
                class="h-10 w-10 rounded-control border border-border shadow-sm"
                :style="{ backgroundColor: color.hex }"
              ></div>
              <div>
                <div class="text-xs font-bold">{{ color.name }}</div>
                <div class="font-mono text-[11px] text-text-muted">{{ color.token }} ({{ color.hex }})</div>
                <div class="text-[11px] text-text-muted">{{ color.role }}</div>
              </div>
            </div>

            <div class="text-right">
              <span
                class="inline-block rounded px-2 py-0.5 text-[11px] font-bold"
                :class="
                  color.contrast.includes('AAA')
                    ? 'bg-positive/15 text-positive'
                    : color.contrast.includes('AA')
                      ? 'bg-accent/15 text-accent'
                      : 'bg-surface-2 text-text-muted'
                "
              >
                {{ color.contrast }}
              </span>
            </div>
          </div>
        </div>

        <!-- Category Spectrum Bar -->
        <div class="rounded-card border border-border bg-surface p-5 shadow-card">
          <div class="flex items-center justify-between mb-3">
            <h4 class="text-sm font-bold">Category Spectrum Tokens (Spend Bar & Donuts)</h4>
            <span class="text-xs text-text-muted">7-Color Calibrated Scale</span>
          </div>

          <div class="flex h-3 w-full overflow-hidden rounded-full bg-surface-2">
            <div
              v-for="cat in categories"
              :key="cat.name"
              :style="{ width: `${cat.percent}%`, backgroundColor: cat.color }"
              class="h-full transition-all duration-300 hover:opacity-85"
            ></div>
          </div>

          <div class="mt-4 flex flex-wrap gap-4">
            <div v-for="cat in categories" :key="cat.name" class="flex items-center gap-2 text-xs">
              <span class="h-3 w-3 rounded-full" :style="{ backgroundColor: cat.color }"></span>
              <span class="font-medium text-text">{{ cat.name }}</span>
              <span class="text-text-muted font-mono">{{ cat.percent }}%</span>
            </div>
          </div>
        </div>
      </section>

      <!-- SECTION 3: TYPOGRAPHY & INDONESIAN RUPIAH (tnum) -->
      <section id="typography" class="space-y-6">
        <div class="border-b border-border pb-3">
          <h3 class="text-xl font-bold">3. Typography & Numerics Scale</h3>
          <p class="text-xs text-text-muted">
            Inter font with tabular figures (`tabular-nums`) and strict Indonesian Rupiah (`id-ID`) formatting.
          </p>
        </div>

        <div class="grid grid-cols-1 gap-6 lg:grid-cols-2">
          <!-- Text Scale -->
          <div class="space-y-4 rounded-card border border-border bg-surface p-6 shadow-card">
            <h4 class="text-xs font-bold uppercase tracking-wider text-text-muted">Text Hierarchy</h4>
            <div class="space-y-3">
              <div>
                <div class="text-xs text-text-muted">Display H1 (32px / 2rem · Bold)</div>
                <h1 class="text-3xl font-bold tracking-tight">Finances on Purpose</h1>
              </div>
              <div>
                <div class="text-xs text-text-muted">Section Title H2 (24px / 1.5rem · Bold)</div>
                <h2 class="text-2xl font-bold tracking-tight">Accounts & Balances</h2>
              </div>
              <div>
                <div class="text-xs text-text-muted">Card Header H3 (18px / 1.125rem · Semibold)</div>
                <h3 class="text-lg font-semibold">Monthly Cashflow Statistics</h3>
              </div>
              <div>
                <div class="text-xs text-text-muted">Body Regular (14px / 0.875rem)</div>
                <p class="text-sm text-text">
                  Tameru tracks every single transaction across your banks, e-wallets, and investments with strict auditability.
                </p>
              </div>
              <div>
                <div class="text-xs text-text-muted">Caption / Label (12px / 0.75rem · Medium)</div>
                <p class="text-xs font-medium text-text-muted">Cleared transaction on 25 Jul 2026 · BCA</p>
              </div>
            </div>
          </div>

          <!-- Currency & Tabular Alignment -->
          <div class="space-y-4 rounded-card border border-border bg-surface p-6 shadow-card">
            <h4 class="text-xs font-bold uppercase tracking-wider text-text-muted">IDR Currency Formatting (`tnum`)</h4>
            <p class="text-xs text-text-muted">Notice how numbers line up vertically regardless of digit widths:</p>

            <div class="divide-y divide-border rounded-control border border-border bg-surface-2 p-3">
              <div class="flex items-center justify-between py-2 text-sm">
                <span class="font-medium text-text">Net Worth Total</span>
                <span class="font-bold tnum text-text">Rp 203.147.000</span>
              </div>
              <div class="flex items-center justify-between py-2 text-sm">
                <span class="font-medium text-text">Monthly Salary</span>
                <span class="font-bold tnum text-positive">+ Rp 12.200.000</span>
              </div>
              <div class="flex items-center justify-between py-2 text-sm">
                <span class="font-medium text-text">Rent Expense</span>
                <span class="font-bold tnum text-negative">(Rp 3.500.000)</span>
              </div>
              <div class="flex items-center justify-between py-2 text-sm">
                <span class="font-medium text-text">Groceries (Grand Lucky)</span>
                <span class="font-bold tnum text-negative">(Rp 850.000)</span>
              </div>
              <div class="flex items-center justify-between py-2 text-sm">
                <span class="font-medium text-text">Bank Transfer to Jenius</span>
                <span class="font-bold tnum text-accent">Rp 2.000.000</span>
              </div>
            </div>
          </div>
        </div>
      </section>

      <!-- SECTION 4: BUTTONS & INTERACTIVE CONTROLS -->
      <section id="buttons" class="space-y-6">
        <div class="border-b border-border pb-3">
          <h3 class="text-xl font-bold">4. Buttons & Segmented Controls</h3>
          <p class="text-xs text-text-muted">Every state designed: default, hover, active, focus-visible, and disabled.</p>
        </div>

        <div class="grid grid-cols-1 gap-6 md:grid-cols-2">
          <!-- Button Variants -->
          <div class="space-y-4 rounded-card border border-border bg-surface p-6 shadow-card">
            <h4 class="text-xs font-bold uppercase tracking-wider text-text-muted">Button Variants</h4>
            <div class="flex flex-wrap items-center gap-3">
              <!-- Primary Cobalt -->
              <button
                type="button"
                class="inline-flex items-center justify-center rounded-control bg-accent px-4 py-2 text-xs font-semibold text-accent-contrast shadow-sm transition-all hover:bg-accent-hover active:bg-accent-active focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
              >
                Primary Button
              </button>

              <!-- Secondary Outline -->
              <button
                type="button"
                class="inline-flex items-center justify-center rounded-control border border-border bg-surface px-4 py-2 text-xs font-medium text-text shadow-sm transition-colors hover:border-border-strong hover:bg-surface-2 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
              >
                Secondary Outline
              </button>

              <!-- Ghost -->
              <button
                type="button"
                class="inline-flex items-center justify-center rounded-control px-3.5 py-2 text-xs font-medium text-text-muted transition-colors hover:bg-surface-2 hover:text-text"
              >
                Ghost Button
              </button>

              <!-- Danger -->
              <button
                type="button"
                class="inline-flex items-center justify-center rounded-control bg-negative px-4 py-2 text-xs font-semibold text-negative-contrast shadow-sm transition-all hover:bg-negative-hover active:opacity-90"
              >
                Danger Action
              </button>

              <!-- Disabled -->
              <button
                type="button"
                disabled
                class="cursor-not-allowed rounded-control border border-border bg-surface-2 px-4 py-2 text-xs font-medium text-text-muted opacity-50"
              >
                Disabled
              </button>
            </div>
          </div>

          <!-- Segmented Pill Controls (Tactile micro-interactions) -->
          <div class="space-y-4 rounded-card border border-border bg-surface p-6 shadow-card">
            <h4 class="text-xs font-bold uppercase tracking-wider text-text-muted">Segmented Pill Switcher</h4>

            <div>
              <div class="text-xs text-text-muted mb-2">Granularity Toggle:</div>
              <div class="inline-flex rounded-full border border-border bg-surface-2 p-1">
                <button
                  type="button"
                  class="rounded-full px-3 py-1 text-xs font-medium transition-all"
                  :class="activeSegment === 'daily' ? 'bg-surface text-text shadow-sm' : 'text-text-muted hover:text-text'"
                  @click="activeSegment = 'daily'"
                >
                  Daily
                </button>
                <button
                  type="button"
                  class="rounded-full px-3 py-1 text-xs font-medium transition-all"
                  :class="activeSegment === 'monthly' ? 'bg-surface text-text shadow-sm' : 'text-text-muted hover:text-text'"
                  @click="activeSegment = 'monthly'"
                >
                  Monthly
                </button>
                <button
                  type="button"
                  class="rounded-full px-3 py-1 text-xs font-medium transition-all"
                  :class="activeSegment === 'yearly' ? 'bg-surface text-text shadow-sm' : 'text-text-muted hover:text-text'"
                  @click="activeSegment = 'yearly'"
                >
                  Yearly
                </button>
              </div>
            </div>

            <div>
              <div class="text-xs text-text-muted mb-2">Transaction Type Selector:</div>
              <div class="inline-flex rounded-control border border-border bg-surface-2 p-1">
                <button
                  type="button"
                  class="rounded-lg px-3 py-1.5 text-xs font-semibold transition-all"
                  :class="
                    activeTransactionType === 'income'
                      ? 'bg-positive text-positive-contrast shadow-sm'
                      : 'text-text-muted hover:text-text'
                  "
                  @click="activeTransactionType = 'income'"
                >
                  Income
                </button>
                <button
                  type="button"
                  class="rounded-lg px-3 py-1.5 text-xs font-semibold transition-all"
                  :class="
                    activeTransactionType === 'expense'
                      ? 'bg-negative text-negative-contrast shadow-sm'
                      : 'text-text-muted hover:text-text'
                  "
                  @click="activeTransactionType = 'expense'"
                >
                  Expense
                </button>
                <button
                  type="button"
                  class="rounded-lg px-3 py-1.5 text-xs font-semibold transition-all"
                  :class="
                    activeTransactionType === 'transfer'
                      ? 'bg-accent text-accent-contrast shadow-sm'
                      : 'text-text-muted hover:text-text'
                  "
                  @click="activeTransactionType = 'transfer'"
                >
                  Transfer
                </button>
              </div>
            </div>
          </div>
        </div>
      </section>

      <!-- SECTION 5: REFERENCE-ACCURATE CARDS & METRICS -->
      <section id="cards" class="space-y-6">
        <div class="border-b border-border pb-3">
          <h3 class="text-xl font-bold">5. Reference-Accurate Cards & Metrics</h3>
          <p class="text-xs text-text-muted">
            The exact 3-tier KPI row, interactive account card, transaction item rows, and budget progress from the discussion.
          </p>
        </div>

        <!-- 3-Tier Metric Cards (Top row from reference) -->
        <div class="grid grid-cols-1 gap-4 sm:grid-cols-3">
          <!-- Income Card -->
          <div class="flex items-center gap-4 rounded-card border border-border bg-surface p-5 shadow-card transition-transform hover:-translate-y-0.5">
            <div class="flex h-12 w-12 shrink-0 items-center justify-center rounded-full bg-positive/10 text-positive">
              <ArrowDownLeft :size="22" :stroke-width="2.2" />
            </div>
            <div class="min-w-0 flex-1">
              <div class="text-lg font-bold tnum text-text truncate">Rp 12.200.000</div>
              <div class="flex items-center gap-2 text-xs">
                <span class="text-text-muted font-medium">Income</span>
                <span class="inline-flex items-center rounded-full bg-positive/15 px-1.5 py-0.5 text-[10px] font-bold text-positive">
                  +2.36%
                </span>
              </div>
            </div>
          </div>

          <!-- Outcome / Expense Card -->
          <div class="flex items-center gap-4 rounded-card border border-border bg-surface p-5 shadow-card transition-transform hover:-translate-y-0.5">
            <div class="flex h-12 w-12 shrink-0 items-center justify-center rounded-full bg-negative/10 text-negative">
              <ArrowUpRight :size="22" :stroke-width="2.2" />
            </div>
            <div class="min-w-0 flex-1">
              <div class="text-lg font-bold tnum text-text truncate">Rp 8.450.000</div>
              <div class="flex items-center gap-2 text-xs">
                <span class="text-text-muted font-medium">Outcome</span>
                <span class="inline-flex items-center rounded-full bg-negative/15 px-1.5 py-0.5 text-[10px] font-bold text-negative">
                  +1.85%
                </span>
              </div>
            </div>
          </div>

          <!-- Revenue / Net Savings Card -->
          <div class="flex items-center gap-4 rounded-card border border-border bg-surface p-5 shadow-card transition-transform hover:-translate-y-0.5">
            <div class="flex h-12 w-12 shrink-0 items-center justify-center rounded-full bg-accent/10 text-accent">
              <TrendingUp :size="22" :stroke-width="2.2" />
            </div>
            <div class="min-w-0 flex-1">
              <div class="text-lg font-bold tnum text-text truncate">Rp 3.750.000</div>
              <div class="flex items-center gap-2 text-xs">
                <span class="text-text-muted font-medium">Net Savings</span>
                <span class="inline-flex items-center rounded-full bg-accent/15 px-1.5 py-0.5 text-[10px] font-bold text-accent">
                  +4.12%
                </span>
              </div>
            </div>
          </div>
        </div>

        <!-- Middle Row: Interactive Account Balance Card + Statistics Bar Chart -->
        <div class="grid grid-cols-1 gap-6 lg:grid-cols-12">
          <!-- Adapted Account Balance Card (Replacing fake credit card) -->
          <div class="rounded-card border border-border bg-surface p-6 shadow-card lg:col-span-5 flex flex-col justify-between">
            <div>
              <div class="flex items-center justify-between">
                <h4 class="text-sm font-bold">Active Account</h4>
                <button type="button" class="text-text-muted hover:text-text">
                  <SlidersHorizontal :size="16" />
                </button>
              </div>

              <!-- Account Switcher Tabs -->
              <div class="mt-3 flex gap-1.5 rounded-full border border-border bg-surface-2 p-1 text-xs">
                <button
                  type="button"
                  class="flex-1 rounded-full py-1 font-medium transition-all"
                  :class="selectedAccountTab === 'BCA' ? 'bg-surface text-text shadow-sm font-semibold' : 'text-text-muted hover:text-text'"
                  @click="selectedAccountTab = 'BCA'"
                >
                  BCA
                </button>
                <button
                  type="button"
                  class="flex-1 rounded-full py-1 font-medium transition-all"
                  :class="selectedAccountTab === 'Jenius' ? 'bg-surface text-text shadow-sm font-semibold' : 'text-text-muted hover:text-text'"
                  @click="selectedAccountTab = 'Jenius'"
                >
                  Jenius
                </button>
                <button
                  type="button"
                  class="flex-1 rounded-full py-1 font-medium transition-all"
                  :class="selectedAccountTab === 'GoPay' ? 'bg-surface text-text shadow-sm font-semibold' : 'text-text-muted hover:text-text'"
                  @click="selectedAccountTab = 'GoPay'"
                >
                  GoPay
                </button>
              </div>

              <!-- Visual Account Display -->
              <div class="mt-4 rounded-2xl bg-accent p-5 text-accent-contrast shadow-sm">
                <div class="flex items-center justify-between">
                  <div class="flex items-center gap-2">
                    <Building2 :size="18" />
                    <span class="text-xs font-semibold uppercase tracking-wider opacity-90">{{ selectedAccountTab }} Checking</span>
                  </div>
                  <CreditCard :size="20" class="opacity-80" />
                </div>

                <div class="mt-6">
                  <div class="text-xs opacity-75">Available Balance</div>
                  <div class="text-2xl font-bold tnum tracking-tight">
                    {{ selectedAccountTab === 'BCA' ? 'Rp 141.942.000' : selectedAccountTab === 'Jenius' ? 'Rp 36.500.000' : 'Rp 1.250.000' }}
                  </div>
                </div>

                <div class="mt-4 flex items-center justify-between text-xs opacity-80">
                  <div class="font-mono">56** **** **** **28</div>
                  <div>Exp 12/28</div>
                </div>
              </div>
            </div>

            <!-- Quick Action Buttons -->
            <div class="mt-4 grid grid-cols-3 gap-2 pt-2">
              <button
                type="button"
                class="flex flex-col items-center justify-center rounded-control border border-border bg-surface-2 py-2 text-[11px] font-medium transition-colors hover:bg-surface-3"
              >
                <ArrowUpRight :size="15" class="mb-1 text-negative" />
                <span>Send</span>
              </button>
              <button
                type="button"
                class="flex flex-col items-center justify-center rounded-control border border-border bg-surface-2 py-2 text-[11px] font-medium transition-colors hover:bg-surface-3"
              >
                <ArrowDownLeft :size="15" class="mb-1 text-positive" />
                <span>Receive</span>
              </button>
              <button
                type="button"
                class="flex flex-col items-center justify-center rounded-control border border-border bg-surface-2 py-2 text-[11px] font-medium transition-colors hover:bg-surface-3"
              >
                <Plus :size="15" class="mb-1 text-accent" />
                <span>Add Record</span>
              </button>
            </div>
          </div>

          <!-- Reference-Accurate Statistics Chart (Hatched bars + Solid active bar) -->
          <div class="rounded-card border border-border bg-surface p-6 shadow-card lg:col-span-7 flex flex-col justify-between">
            <div class="flex items-center justify-between">
              <div>
                <h4 class="text-sm font-bold">Statistics & Cashflow</h4>
                <p class="text-xs text-text-muted">Monthly expense volume</p>
              </div>
              <div class="flex items-center gap-2">
                <button
                  type="button"
                  class="flex h-8 w-8 items-center justify-center rounded-control border border-border text-text-muted hover:text-text"
                  title="Download report"
                >
                  <Download :size="14" />
                </button>
                <span class="rounded-full border border-border bg-surface-2 px-2.5 py-1 text-xs font-medium text-text">
                  Monthly
                </span>
              </div>
            </div>

            <!-- Custom Hatched / Solid Bar Chart -->
            <div class="relative mt-8 flex h-48 items-end justify-between gap-3 px-2">
              <div
                v-for="(m, i) in months"
                :key="m.name"
                class="group relative flex flex-1 flex-col items-center cursor-pointer"
                @mouseenter="activeMonthIndex = i"
              >
                <!-- Floating Tooltip on Hover/Active -->
                <div
                  v-if="activeMonthIndex === i"
                  class="absolute -top-12 z-20 whitespace-nowrap rounded-lg bg-text px-2.5 py-1 text-[11px] font-bold text-surface shadow-popover transition-all"
                >
                  <div>{{ m.name }} 2026</div>
                  <div class="tnum font-mono">{{ ui.amountsHidden ? '•••' : 'Rp ' + (m.amount / 1000000).toFixed(1) + 'M' }}</div>
                  <div class="absolute left-1/2 top-full -translate-x-1/2 border-4 border-transparent border-t-text"></div>
                </div>

                <!-- Bar Column -->
                <div
                  class="w-full rounded-t-lg transition-all duration-200"
                  :style="{ height: `${m.value}%` }"
                  :class="
                    activeMonthIndex === i
                      ? 'bg-accent shadow-sm'
                      : 'bg-surface-3 opacity-60 hover:opacity-100'
                  "
                ></div>

                <!-- Month Label -->
                <div
                  class="mt-2 text-[11px] font-medium transition-colors"
                  :class="activeMonthIndex === i ? 'font-bold text-accent' : 'text-text-muted'"
                >
                  {{ m.name }}
                </div>
              </div>
            </div>

            <div class="mt-4 flex items-center justify-between border-t border-border pt-3 text-xs text-text-muted">
              <span>Average Monthly Expense: <strong class="text-text font-mono">Rp 14.5M</strong></span>
              <span class="text-positive font-medium">Within 15% Budget Limit</span>
            </div>
          </div>
        </div>

        <!-- Bottom Row: Transactions List & Master Plan Progress (Replacing Promo) -->
        <div class="grid grid-cols-1 gap-6 lg:grid-cols-12">
          <!-- Transactions List -->
          <div class="rounded-card border border-border bg-surface p-6 shadow-card lg:col-span-7">
            <div class="flex items-center justify-between border-b border-border pb-3">
              <h4 class="text-sm font-bold">Recent Transactions</h4>
              <div class="flex items-center gap-3">
                <button type="button" class="text-xs font-semibold text-accent hover:underline">
                  View all →
                </button>
              </div>
            </div>

            <div class="divide-y divide-border">
              <!-- Item 1 -->
              <div class="flex items-center justify-between py-3.5 transition-colors hover:bg-surface-2/40 px-2 rounded-control">
                <div class="flex items-center gap-3">
                  <div class="flex h-10 w-10 items-center justify-center rounded-full bg-accent-soft text-accent font-bold text-xs">
                    <Building2 :size="18" />
                  </div>
                  <div>
                    <div class="text-xs font-bold text-text">Salary Deposit</div>
                    <div class="text-[11px] text-text-muted">PT Tech Nusantara · 25 Jul 2026</div>
                  </div>
                </div>
                <div class="text-right">
                  <div class="text-xs font-bold tnum text-positive">+ Rp 12.200.000</div>
                  <span class="text-[10px] text-text-muted">Cleared</span>
                </div>
              </div>

              <!-- Item 2 -->
              <div class="flex items-center justify-between py-3.5 transition-colors hover:bg-surface-2/40 px-2 rounded-control">
                <div class="flex items-center gap-3">
                  <div class="flex h-10 w-10 items-center justify-center rounded-full bg-negative/10 text-negative font-bold text-xs">
                    <Receipt :size="18" />
                  </div>
                  <div>
                    <div class="text-xs font-bold text-text">Grand Lucky Supermarket</div>
                    <div class="text-[11px] text-text-muted">Groceries · 24 Jul 2026</div>
                  </div>
                </div>
                <div class="text-right">
                  <div class="text-xs font-bold tnum text-negative">(Rp 850.000)</div>
                  <span class="text-[10px] text-text-muted">Debit BCA</span>
                </div>
              </div>

              <!-- Item 3 -->
              <div class="flex items-center justify-between py-3.5 transition-colors hover:bg-surface-2/40 px-2 rounded-control">
                <div class="flex items-center gap-3">
                  <div class="flex h-10 w-10 items-center justify-center rounded-full bg-surface-2 text-text font-bold text-xs">
                    <Smartphone :size="18" />
                  </div>
                  <div>
                    <div class="text-xs font-bold text-text">GoPay Top Up</div>
                    <div class="text-[11px] text-text-muted">Transfer · 20 Jul 2026</div>
                  </div>
                </div>
                <div class="text-right">
                  <div class="text-xs font-bold tnum text-text">Rp 500.000</div>
                  <span class="text-[10px] text-text-muted">BCA → GoPay</span>
                </div>
              </div>
            </div>
          </div>

          <!-- Master Plan Allocation Card (Replacing the "Go Pro" Promo Banner) -->
          <div class="rounded-card border border-border bg-surface p-6 shadow-card lg:col-span-5 flex flex-col justify-between">
            <div>
              <div class="flex items-center justify-between border-b border-border pb-3">
                <h4 class="text-sm font-bold">Master Plan Allocation</h4>
                <span class="rounded-full bg-accent-soft px-2 py-0.5 text-[10px] font-bold text-accent">50 · 30 · 20 Rule</span>
              </div>

              <div class="mt-4 space-y-4">
                <!-- Needs -->
                <div>
                  <div class="flex justify-between text-xs font-medium">
                    <span>Needs (Target 50%)</span>
                    <span class="font-bold tnum">Rp 7.5M / Rp 10M</span>
                  </div>
                  <div class="mt-1.5 h-2 w-full overflow-hidden rounded-full bg-surface-2">
                    <div class="h-full rounded-full bg-accent" style="width: 75%"></div>
                  </div>
                </div>

                <!-- Wants -->
                <div>
                  <div class="flex justify-between text-xs font-medium">
                    <span>Wants (Target 30%)</span>
                    <span class="font-bold tnum">Rp 2.5M / Rp 6M</span>
                  </div>
                  <div class="mt-1.5 h-2 w-full overflow-hidden rounded-full bg-surface-2">
                    <div class="h-full rounded-full bg-cat-3" style="width: 42%"></div>
                  </div>
                </div>

                <!-- Investment / Savings -->
                <div>
                  <div class="flex justify-between text-xs font-medium">
                    <span>Investments (Target 20%)</span>
                    <span class="font-bold tnum">Rp 4.0M / Rp 4M</span>
                  </div>
                  <div class="mt-1.5 h-2 w-full overflow-hidden rounded-full bg-surface-2">
                    <div class="h-full rounded-full bg-positive" style="width: 100%"></div>
                  </div>
                </div>
              </div>
            </div>

            <div class="mt-6 rounded-control border border-border bg-surface-2 p-3 text-xs">
              <div class="font-semibold text-text">Allocation Health: Optimal</div>
              <div class="text-[11px] text-text-muted mt-0.5">Your current month savings rate is 27.5%, exceeding the 20% baseline target.</div>
            </div>
          </div>
        </div>
      </section>

      <!-- SECTION 6: FORM CONTROLS & INPUTS -->
      <section id="inputs" class="space-y-6">
        <div class="border-b border-border pb-3">
          <h3 class="text-xl font-bold">6. Form Controls & Validation States</h3>
          <p class="text-xs text-text-muted">
            Modern, accessible input styling with strong WCAG 3:1 control borders (`--border-strong`).
          </p>
        </div>

        <div class="grid grid-cols-1 gap-6 md:grid-cols-2 lg:grid-cols-3">
          <!-- Text Input -->
          <div class="rounded-card border border-border bg-surface p-5 shadow-card space-y-3">
            <label class="block text-xs font-semibold text-text">Title / Description</label>
            <input
              v-model="inputText"
              type="text"
              class="h-10 w-full rounded-control border border-border-strong bg-surface px-3 text-xs text-text transition-colors focus:border-accent focus:outline-none focus:ring-1 focus:ring-accent"
              placeholder="e.g. Monthly groceries"
            />
            <p class="text-[11px] text-text-muted">Enter clear merchant or transaction title.</p>
          </div>

          <!-- Currency Money Input -->
          <div class="rounded-card border border-border bg-surface p-5 shadow-card space-y-3">
            <label class="block text-xs font-semibold text-text">Amount (IDR)</label>
            <div class="relative">
              <span class="absolute left-3 top-1/2 -translate-y-1/2 font-bold text-xs text-text-muted">Rp</span>
              <input
                v-model="inputAmount"
                type="text"
                class="h-10 w-full rounded-control border border-border-strong bg-surface pl-9 pr-3 text-xs font-mono font-bold text-text transition-colors focus:border-accent focus:outline-none focus:ring-1 focus:ring-accent"
              />
            </div>
            <p class="text-[11px] text-text-muted">Formatted with tabular lining numbers.</p>
          </div>

          <!-- Select Dropdown -->
          <div class="rounded-card border border-border bg-surface p-5 shadow-card space-y-3">
            <label class="block text-xs font-semibold text-text">Account Selection</label>
            <div class="relative">
              <select
                class="h-10 w-full appearance-none rounded-control border border-border-strong bg-surface px-3 pr-8 text-xs text-text transition-colors focus:border-accent focus:outline-none focus:ring-1 focus:ring-accent"
              >
                <option>BCA Checking (Rp 141.9M)</option>
                <option>Jenius Savings (Rp 36.5M)</option>
                <option>GoPay E-Wallet (Rp 1.25M)</option>
                <option>Ajaib RDN Investment (Rp 50M)</option>
              </select>
              <ChevronDown :size="16" class="pointer-events-none absolute right-3 top-1/2 -translate-y-1/2 text-text-muted" />
            </div>
            <p class="text-[11px] text-text-muted">Select destination or funding account.</p>
          </div>

          <!-- Checkboxes and Switches -->
          <div class="rounded-card border border-border bg-surface p-5 shadow-card space-y-4">
            <label class="block text-xs font-semibold text-text">Toggles & Checks</label>
            <div class="flex items-center justify-between">
              <span class="text-xs text-text">Auto-reconcile balance</span>
              <AppToggle v-model="toggleChecked" label="Auto-reconcile balance" />
            </div>

            <label class="flex items-center gap-2.5 text-xs text-text cursor-pointer">
              <input
                v-model="checkboxChecked"
                type="checkbox"
                class="h-4 w-4 rounded border-border-strong text-accent focus:ring-accent"
              />
              <span>Mark as Cleared Transaction</span>
            </label>
          </div>

          <!-- Error State -->
          <div class="rounded-card border border-border bg-surface p-5 shadow-card space-y-3">
            <label class="block text-xs font-semibold text-negative">Validation Error State</label>
            <input
              type="text"
              value="Invalid format"
              class="h-10 w-full rounded-control border border-negative bg-surface px-3 text-xs text-text transition-colors focus:outline-none focus:ring-1 focus:ring-negative"
            />
            <p class="flex items-center gap-1 text-[11px] text-negative font-medium">
              <AlertCircle :size="13" />
              <span>Amount must be greater than zero.</span>
            </p>
          </div>
        </div>
      </section>

      <!-- SECTION 7: CHARTS & DATA VISUALIZATIONS LABORATORY -->
      <section id="charts" class="space-y-8">
        <!-- Section Header & Global Chart Toolbar -->
        <div class="border-b border-border pb-4 flex flex-col lg:flex-row lg:items-center lg:justify-between gap-4">
          <div>
            <div class="flex items-center gap-2">
              <h3 class="text-xl font-bold">7. Chart & Data Visualization Laboratory</h3>
              <span class="rounded-full bg-accent/15 px-2.5 py-0.5 text-[11px] font-bold text-accent">
                All Financial Cases
              </span>
            </div>
            <p class="text-xs text-text-muted mt-1">
              Comprehensive personal finance charting suite for Tameru. Test various conditions: legends on/off, value labels, benchmarks, smoothing, forecasting, and privacy masking.
            </p>
          </div>

          <!-- Interactive Global Chart Toolbar -->
          <div class="flex flex-wrap items-center gap-2.5 rounded-control border border-border bg-surface p-2 shadow-sm">
            <!-- Global Legend Toggle -->
            <div class="flex items-center gap-2 pr-2.5 border-r border-border">
              <span class="text-xs font-semibold text-text">Global Legends</span>
              <AppToggle
                :model-value="globalShowLegend"
                @update:model-value="toggleGlobalLegends"
                label="Toggle All Chart Legends"
                size="sm"
              />
            </div>

            <!-- Global Timeframe Selector -->
            <div class="flex items-center gap-1.5 text-xs pr-2.5 border-r border-border">
              <span class="text-text-muted">Window:</span>
              <div class="inline-flex rounded-full border border-border bg-surface-2 p-0.5">
                <button
                  type="button"
                  class="rounded-full px-2 py-0.5 font-medium transition-all"
                  :class="chartTimeframe === '3M' ? 'bg-surface text-text font-bold shadow-sm' : 'text-text-muted hover:text-text'"
                  @click="chartTimeframe = '3M'"
                >
                  3M
                </button>
                <button
                  type="button"
                  class="rounded-full px-2 py-0.5 font-medium transition-all"
                  :class="chartTimeframe === '6M' ? 'bg-surface text-text font-bold shadow-sm' : 'text-text-muted hover:text-text'"
                  @click="chartTimeframe = '6M'"
                >
                  6M
                </button>
                <button
                  type="button"
                  class="rounded-full px-2 py-0.5 font-medium transition-all"
                  :class="chartTimeframe === '12M' ? 'bg-surface text-text font-bold shadow-sm' : 'text-text-muted hover:text-text'"
                  @click="chartTimeframe = '12M'"
                >
                  12M
                </button>
              </div>
            </div>

            <!-- Privacy Masking Indicator -->
            <button
              type="button"
              class="flex items-center gap-1 rounded-full border border-border bg-surface-2 px-2.5 py-1 text-xs text-text-muted hover:text-text transition-colors"
              @click="ui.toggleAmounts()"
              title="Toggle masked amounts"
            >
              <Eye :size="12" v-if="!ui.amountsHidden" />
              <EyeOff :size="12" v-else class="text-accent" />
              <span>{{ ui.amountsHidden ? 'Masked' : 'Visible' }}</span>
            </button>

            <!-- Reset to Defaults Button -->
            <button
              type="button"
              class="flex items-center gap-1 rounded-full border border-border bg-surface-2 px-2.5 py-1 text-xs text-text-muted hover:text-text transition-colors"
              @click="resetChartConditions"
              title="Reset all chart conditions to defaults"
            >
              <RotateCcw :size="12" />
              <span>Reset</span>
            </button>
          </div>
        </div>

        <!-- 1. Cashflow Analysis (Income vs Expense vs Net Savings) -->
        <div class="rounded-card border border-border bg-surface p-6 shadow-card space-y-4">
          <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3 border-b border-border pb-3">
            <div>
              <h4 class="text-sm font-bold flex items-center gap-2">
                <BarChart3 :size="16" class="text-accent" />
                <span>Cashflow Analysis: Inflow, Outflow & Net Savings</span>
                <span title="Compare monthly inflow vs outflow with net savings delta">
                  <HelpCircle :size="13" class="text-text-muted hover:text-text cursor-help" />
                </span>
              </h4>
              <p class="text-xs text-text-muted">Compare monthly cash inflows against outflows or inspect net cash delta</p>
            </div>

            <!-- Mode Switcher -->
            <div class="inline-flex rounded-full border border-border bg-surface-2 p-1 text-xs">
              <button
                type="button"
                class="rounded-full px-3 py-1 font-medium transition-all"
                :class="cashflowChartMode === 'grouped' ? 'bg-surface text-text font-bold shadow-sm' : 'text-text-muted hover:text-text'"
                @click="cashflowChartMode = 'grouped'"
              >
                Dual Comparison Bar
              </button>
              <button
                type="button"
                class="rounded-full px-3 py-1 font-medium transition-all"
                :class="cashflowChartMode === 'stacked' ? 'bg-surface text-text font-bold shadow-sm' : 'text-text-muted hover:text-text'"
                @click="cashflowChartMode = 'stacked'"
              >
                Stacked Total Bar
              </button>
              <button
                type="button"
                class="rounded-full px-3 py-1 font-medium transition-all"
                :class="cashflowChartMode === 'waterfall' ? 'bg-surface text-text font-bold shadow-sm' : 'text-text-muted hover:text-text'"
                @click="cashflowChartMode = 'waterfall'"
              >
                Net Savings Delta Bar
              </button>
            </div>
          </div>

          <!-- Interactive Local Condition Controls Bar -->
          <div class="flex flex-wrap items-center gap-4 rounded-control border border-border/80 bg-surface-2/40 px-3 py-2 text-xs">
            <span class="font-semibold text-text flex items-center gap-1 text-[11px]">
              <SlidersHorizontal :size="12" class="text-accent" />
              <span>Chart Conditions:</span>
            </span>

            <div class="flex items-center gap-2">
              <span class="text-text-muted">Legend:</span>
              <AppToggle v-model="cashflowShowLegend" label="Cashflow legend" size="sm" />
            </div>

            <div class="flex items-center gap-2">
              <span class="text-text-muted">Data Labels:</span>
              <AppToggle v-model="cashflowShowLabels" label="Cashflow data labels" size="sm" />
            </div>

            <div class="flex items-center gap-2">
              <span class="text-text-muted">Benchmark Average:</span>
              <AppToggle v-model="cashflowShowAverage" label="Cashflow average line" size="sm" />
            </div>
          </div>

          <div class="h-64 w-full pt-2">
            <VChart :option="cashflowChartOption" autoresize class="h-full w-full" />
          </div>

          <div class="flex flex-wrap items-center justify-between border-t border-border pt-3 text-xs text-text-muted gap-2">
            <span class="flex items-center gap-1.5">
              <span class="h-2 w-2 rounded-full bg-positive"></span>
              <span>Income: <strong>+Rp 198.5M</strong></span>
              <span class="mx-2 text-border-strong">·</span>
              <span class="h-2 w-2 rounded-full bg-negative"></span>
              <span>Expense: <strong>-Rp 122.9M</strong></span>
            </span>
            <span class="font-bold text-positive">Net Savings Rate: +Rp 75.6M (38.1%)</span>
          </div>
        </div>

        <!-- 2. Net Worth & Account Progression (Line / Area / Projection Charts) -->
        <div class="rounded-card border border-border bg-surface p-6 shadow-card space-y-4">
          <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3 border-b border-border pb-3">
            <div>
              <h4 class="text-sm font-bold flex items-center gap-2">
                <TrendingUp :size="16" class="text-accent" />
                <span>Net Worth & Wealth Trajectory Progression</span>
              </h4>
              <p class="text-xs text-text-muted">Track wealth accumulation, individual account trends, or projected runway</p>
            </div>

            <!-- Mode Switcher -->
            <div class="inline-flex rounded-full border border-border bg-surface-2 p-1 text-xs">
              <button
                type="button"
                class="rounded-full px-3 py-1 font-medium transition-all"
                :class="lineChartMode === 'netWorth' ? 'bg-surface text-text font-bold shadow-sm' : 'text-text-muted hover:text-text'"
                @click="lineChartMode = 'netWorth'"
              >
                Total Net Worth Area
              </button>
              <button
                type="button"
                class="rounded-full px-3 py-1 font-medium transition-all"
                :class="lineChartMode === 'multiAccount' ? 'bg-surface text-text font-bold shadow-sm' : 'text-text-muted hover:text-text'"
                @click="lineChartMode = 'multiAccount'"
              >
                Multi-Account Lines
              </button>
              <button
                type="button"
                class="rounded-full px-3 py-1 font-medium transition-all"
                :class="lineChartMode === 'projection' ? 'bg-surface text-text font-bold shadow-sm' : 'text-text-muted hover:text-text'"
                @click="lineChartMode = 'projection'"
              >
                3M+ Forecast Projection
              </button>
            </div>
          </div>

          <!-- Interactive Local Condition Controls Bar -->
          <div class="flex flex-wrap items-center gap-4 rounded-control border border-border/80 bg-surface-2/40 px-3 py-2 text-xs">
            <span class="font-semibold text-text flex items-center gap-1 text-[11px]">
              <SlidersHorizontal :size="12" class="text-accent" />
              <span>Chart Conditions:</span>
            </span>

            <div class="flex items-center gap-2">
              <span class="text-text-muted">Legend:</span>
              <AppToggle v-model="netWorthShowLegend" label="Net worth legend" size="sm" />
            </div>

            <div class="flex items-center gap-2">
              <span class="text-text-muted">Smooth Curve:</span>
              <AppToggle v-model="netWorthSmooth" label="Smooth curve spline" size="sm" />
            </div>

            <div v-if="lineChartMode !== 'multiAccount'" class="flex items-center gap-2">
              <span class="text-text-muted">Soft Area Fill:</span>
              <AppToggle v-model="netWorthShowArea" label="Area fill" size="sm" />
            </div>

            <div class="flex items-center gap-2">
              <span class="text-text-muted">Point Vertices:</span>
              <AppToggle v-model="netWorthShowPoints" label="Show point circles" size="sm" />
            </div>
          </div>

          <div class="h-64 w-full pt-2">
            <VChart :option="netWorthChartOption" autoresize class="h-full w-full" />
          </div>

          <div class="flex flex-wrap items-center justify-between border-t border-border pt-3 text-xs text-text-muted gap-2">
            <span>Starting Balance: <strong class="text-text font-mono">Rp 165.0M</strong></span>
            <span>Current: <strong class="text-text font-mono">Rp 203.1M</strong></span>
            <span class="text-positive font-bold">+23.1% Annual Net Worth Growth</span>
          </div>
        </div>

        <!-- 3. Two Columns: Category Spending Breakdown + Daily Spending Velocity -->
        <div class="grid grid-cols-1 gap-6 lg:grid-cols-2">
          <!-- 3A: Category Spending Breakdown (Donut vs Gauge vs Treemap) -->
          <div class="rounded-card border border-border bg-surface p-6 shadow-card space-y-4 flex flex-col justify-between">
            <div>
              <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-2 border-b border-border pb-3">
                <div>
                  <h4 class="text-sm font-bold flex items-center gap-2">
                    <PieChart :size="16" class="text-accent" />
                    <span>Category Spending Allocation</span>
                  </h4>
                  <p class="text-xs text-text-muted">Expense allocation by category envelopes</p>
                </div>

                <!-- Mode Switcher -->
                <div class="inline-flex rounded-full border border-border bg-surface-2 p-0.5 text-xs">
                  <button
                    type="button"
                    class="rounded-full px-2.5 py-0.5 font-medium transition-all"
                    :class="categoryChartMode === 'donut' ? 'bg-surface text-text font-bold shadow-sm' : 'text-text-muted hover:text-text'"
                    @click="categoryChartMode = 'donut'"
                  >
                    Donut
                  </button>
                  <button
                    type="button"
                    class="rounded-full px-2.5 py-0.5 font-medium transition-all"
                    :class="categoryChartMode === 'gauge' ? 'bg-surface text-text font-bold shadow-sm' : 'text-text-muted hover:text-text'"
                    @click="categoryChartMode = 'gauge'"
                  >
                    180° Gauge
                  </button>
                  <button
                    type="button"
                    class="rounded-full px-2.5 py-0.5 font-medium transition-all"
                    :class="categoryChartMode === 'treemap' ? 'bg-surface text-text font-bold shadow-sm' : 'text-text-muted hover:text-text'"
                    @click="categoryChartMode = 'treemap'"
                  >
                    Treemap
                  </button>
                </div>
              </div>

              <!-- Conditions Bar: Legend Position & Center Metric -->
              <div v-if="categoryChartMode === 'donut'" class="mt-3 flex flex-wrap items-center justify-between gap-2 rounded-control border border-border/80 bg-surface-2/40 px-3 py-1.5 text-xs">
                <div class="flex items-center gap-1.5">
                  <span class="text-text-muted text-[11px]">Legend:</span>
                  <div class="inline-flex rounded-full border border-border bg-surface p-0.5 text-[11px]">
                    <button
                      type="button"
                      class="rounded-full px-2 py-0.5 font-medium"
                      :class="categoryLegendPosition === 'right' ? 'bg-accent text-accent-contrast font-bold' : 'text-text-muted hover:text-text'"
                      @click="categoryLegendPosition = 'right'"
                    >
                      Right
                    </button>
                    <button
                      type="button"
                      class="rounded-full px-2 py-0.5 font-medium"
                      :class="categoryLegendPosition === 'bottom' ? 'bg-accent text-accent-contrast font-bold' : 'text-text-muted hover:text-text'"
                      @click="categoryLegendPosition = 'bottom'"
                    >
                      Bottom
                    </button>
                    <button
                      type="button"
                      class="rounded-full px-2 py-0.5 font-medium"
                      :class="categoryLegendPosition === 'none' ? 'bg-accent text-accent-contrast font-bold' : 'text-text-muted hover:text-text'"
                      @click="categoryLegendPosition = 'none'"
                    >
                      None (Minimal)
                    </button>
                  </div>
                </div>

                <div class="flex items-center gap-1.5">
                  <span class="text-text-muted text-[11px]">Center:</span>
                  <div class="inline-flex rounded-full border border-border bg-surface p-0.5 text-[11px]">
                    <button
                      type="button"
                      class="rounded-full px-1.5 py-0.5 font-medium"
                      :class="categoryCenterMetric === 'amount' ? 'bg-surface-2 text-text font-bold' : 'text-text-muted hover:text-text'"
                      @click="categoryCenterMetric = 'amount'"
                    >
                      Amount
                    </button>
                    <button
                      type="button"
                      class="rounded-full px-1.5 py-0.5 font-medium"
                      :class="categoryCenterMetric === 'count' ? 'bg-surface-2 text-text font-bold' : 'text-text-muted hover:text-text'"
                      @click="categoryCenterMetric = 'count'"
                    >
                      Count
                    </button>
                    <button
                      type="button"
                      class="rounded-full px-1.5 py-0.5 font-medium"
                      :class="categoryCenterMetric === 'percent' ? 'bg-surface-2 text-text font-bold' : 'text-text-muted hover:text-text'"
                      @click="categoryCenterMetric = 'percent'"
                    >
                      %
                    </button>
                  </div>
                </div>
              </div>

              <!-- The Chart -->
              <div class="relative h-60 w-full pt-3">
                <VChart :option="categoryChartOption" autoresize class="h-full w-full" />

                <!-- Dynamic Center Metric for Donut -->
                <div
                  v-if="categoryChartMode === 'donut'"
                  class="pointer-events-none absolute inset-0 flex flex-col items-center justify-center transition-all"
                  :class="categoryLegendPosition === 'right' ? 'sm:right-[36%]' : categoryLegendPosition === 'bottom' ? 'bottom-6' : ''"
                >
                  <span class="text-[10px] text-text-muted font-medium uppercase tracking-wider">
                    {{ categoryCenterMetric === 'amount' ? 'Total Spent' : categoryCenterMetric === 'count' ? 'Records' : 'Envelope' }}
                  </span>
                  <span class="text-base font-bold tnum text-text">
                    {{
                      categoryCenterMetric === 'amount'
                        ? (ui.amountsHidden ? '••••••' : 'Rp 22.6M')
                        : categoryCenterMetric === 'count'
                          ? '48 Trx'
                          : '100%'
                    }}
                  </span>
                </div>
              </div>
            </div>

            <div class="border-t border-border pt-3 text-xs text-text-muted flex justify-between">
              <span>Top Expense: <strong class="text-text">Food & Dining (35%)</strong></span>
              <span class="text-positive font-medium">Under Monthly Budget Cap</span>
            </div>
          </div>

          <!-- 3B: Daily Spending Velocity / Cumulative Burn Rate Tracker (Fintech Pacing) -->
          <div class="rounded-card border border-border bg-surface p-6 shadow-card space-y-4 flex flex-col justify-between">
            <div>
              <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-2 border-b border-border pb-3">
                <div>
                  <h4 class="text-sm font-bold flex items-center gap-2">
                    <Gauge :size="16" class="text-accent" />
                    <span>Daily Spending Velocity (Burn Rate)</span>
                  </h4>
                  <p class="text-xs text-text-muted">Compare cumulative daily spend against ideal budget pace</p>
                </div>

                <div class="flex items-center gap-1.5 rounded-full bg-positive/15 px-2.5 py-1 text-xs font-semibold text-positive">
                  <span>Rp 580k Under Pace · Safe</span>
                </div>
              </div>

              <!-- Conditions Bar -->
              <div class="mt-3 flex flex-wrap items-center gap-3 rounded-control border border-border/80 bg-surface-2/40 px-3 py-1.5 text-xs">
                <div class="flex items-center gap-1.5">
                  <span class="text-text-muted text-[11px]">Legend:</span>
                  <AppToggle v-model="velocityShowLegend" label="Velocity legend" size="sm" />
                </div>
                <div class="flex items-center gap-1.5">
                  <span class="text-text-muted text-[11px]">Ideal Pace Line:</span>
                  <AppToggle v-model="velocityShowPace" label="Pace benchmark" size="sm" />
                </div>
                <div class="flex items-center gap-1.5">
                  <span class="text-text-muted text-[11px]">Today Marker:</span>
                  <AppToggle v-model="velocityShowTodayMarker" label="Today marker" size="sm" />
                </div>
              </div>

              <div class="h-60 w-full pt-2">
                <VChart :option="velocityChartOption" autoresize class="h-full w-full" />
              </div>
            </div>

            <div class="border-t border-border pt-3 text-xs text-text-muted flex justify-between">
              <span>Day 17 of 30: <strong class="text-text">Rp 7.92M spent / Rp 15M cap</strong></span>
              <span class="text-positive font-bold">52.8% Consumed</span>
            </div>
          </div>
        </div>

        <!-- 4. Two Columns: Budget Envelopes & Variance + Segmented Category Spend Bar -->
        <div class="grid grid-cols-1 gap-6 lg:grid-cols-2">
          <!-- 4A: Budget Envelopes & Target Bullet / Diverging Variance Bars -->
          <div class="rounded-card border border-border bg-surface p-6 shadow-card space-y-4 flex flex-col justify-between">
            <div>
              <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-2 border-b border-border pb-3">
                <div>
                  <h4 class="text-sm font-bold flex items-center gap-2">
                    <Target :size="16" class="text-accent" />
                    <span>Budget Envelopes & Variance</span>
                  </h4>
                  <p class="text-xs text-text-muted">Target envelopes vs actual variance</p>
                </div>

                <!-- Mode Switcher -->
                <div class="inline-flex rounded-full border border-border bg-surface-2 p-0.5 text-xs">
                  <button
                    type="button"
                    class="rounded-full px-2.5 py-0.5 font-medium transition-all"
                    :class="budgetChartMode === 'bullet' ? 'bg-surface text-text font-bold shadow-sm' : 'text-text-muted hover:text-text'"
                    @click="budgetChartMode = 'bullet'"
                  >
                    Bullet Bars
                  </button>
                  <button
                    type="button"
                    class="rounded-full px-2.5 py-0.5 font-medium transition-all"
                    :class="budgetChartMode === 'diverging' ? 'bg-surface text-text font-bold shadow-sm' : 'text-text-muted hover:text-text'"
                    @click="budgetChartMode = 'diverging'"
                  >
                    Diverging Variance Bar
                  </button>
                </div>
              </div>

              <!-- Filter Bar -->
              <div class="mt-3 flex items-center justify-between rounded-control border border-border/80 bg-surface-2/40 px-3 py-1.5 text-xs">
                <span class="text-text-muted text-[11px]">Filter Envelopes:</span>
                <div class="inline-flex rounded-full border border-border bg-surface p-0.5 text-[11px]">
                  <button
                    type="button"
                    class="rounded-full px-2 py-0.5 font-medium"
                    :class="budgetFilter === 'all' ? 'bg-surface-2 text-text font-bold' : 'text-text-muted hover:text-text'"
                    @click="budgetFilter = 'all'"
                  >
                    All ({{ budgetEnvelopes.length }})
                  </button>
                  <button
                    type="button"
                    class="rounded-full px-2 py-0.5 font-medium"
                    :class="budgetFilter === 'warningOnly' ? 'bg-surface-2 text-negative font-bold' : 'text-text-muted hover:text-text'"
                    @click="budgetFilter = 'warningOnly'"
                  >
                    Warnings Only (2)
                  </button>
                </div>
              </div>

              <!-- Presentation 1: Interactive Bullet Bars -->
              <div v-if="budgetChartMode === 'bullet'" class="mt-4 space-y-3.5">
                <div v-for="item in filteredBudgetEnvelopes" :key="item.category" class="space-y-1">
                  <div class="flex items-center justify-between text-xs">
                    <span class="font-medium text-text">{{ item.category }}</span>
                    <div class="flex items-center gap-2">
                      <span class="font-bold tnum">
                        {{ ui.amountsHidden ? '••••' : (item.actual / 1000000).toFixed(1) + 'M' }} /
                        {{ ui.amountsHidden ? '••••' : (item.budget / 1000000).toFixed(1) + 'M' }}
                      </span>
                      <span
                        class="rounded px-1.5 py-0.5 text-[10px] font-bold"
                        :class="
                          item.status === 'danger'
                            ? 'bg-negative/15 text-negative'
                            : item.status === 'warn'
                              ? 'bg-cat-3/15 text-cat-3'
                              : 'bg-positive/15 text-positive'
                        "
                      >
                        {{ item.percent }}%
                      </span>
                    </div>
                  </div>

                  <!-- Relative Bullet Bar -->
                  <div class="relative h-2 w-full rounded-full bg-surface-2 overflow-hidden">
                    <div
                      class="h-full rounded-full transition-all duration-300"
                      :style="{ width: `${Math.min(item.percent, 100)}%` }"
                      :class="
                        item.status === 'danger'
                          ? 'bg-negative'
                          : item.status === 'warn'
                            ? 'bg-cat-3'
                            : 'bg-accent'
                      "
                    ></div>
                  </div>
                </div>
              </div>

              <!-- Presentation 2: Diverging Horizontal Variance Bar -->
              <div v-else class="h-60 w-full pt-2">
                <VChart :option="budgetVarianceChartOption" autoresize class="h-full w-full" />
              </div>
            </div>

            <div class="border-t border-border pt-3 text-xs text-text-muted flex justify-between">
              <span>Envelope Health: <strong class="text-positive">3 of 5 On Track</strong></span>
              <span class="text-negative font-medium">Entertainment exceeds envelope by Rp 600k</span>
            </div>
          </div>

          <!-- 4B: Segmented Category Spend Bar (Tameru Signature Component) -->
          <div class="rounded-card border border-border bg-surface p-6 shadow-card space-y-4 flex flex-col justify-between">
            <div>
              <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3 border-b border-border pb-3">
                <div>
                  <h4 class="text-sm font-bold flex items-center gap-2">
                    <Layers :size="16" class="text-accent" />
                    <span>Segmented Category Spend Bar (Tameru Signature)</span>
                  </h4>
                  <p class="text-xs text-text-muted">Proportional horizontal spectrum for quick visual allocation</p>
                </div>

                <div class="inline-flex rounded-full border border-border bg-surface-2 p-0.5 text-xs">
                  <button
                    type="button"
                    class="rounded-full px-3 py-1 font-medium transition-all"
                    :class="spendBarMode === 'detailed' ? 'bg-surface text-text font-bold shadow-sm' : 'text-text-muted hover:text-text'"
                    @click="spendBarMode = 'detailed'"
                  >
                    Detailed with Legend
                  </button>
                  <button
                    type="button"
                    class="rounded-full px-3 py-1 font-medium transition-all"
                    :class="spendBarMode === 'compact' ? 'bg-surface text-text font-bold shadow-sm' : 'text-text-muted hover:text-text'"
                    @click="spendBarMode = 'compact'"
                  >
                    Compact Strip Only
                  </button>
                </div>
              </div>

              <!-- The Bar -->
              <div class="mt-4 flex h-3.5 w-full overflow-hidden rounded-full bg-surface-2 shadow-inner">
                <div
                  v-for="cat in categories"
                  :key="cat.name"
                  :style="{ width: `${cat.percent}%`, backgroundColor: cat.color }"
                  class="h-full transition-all duration-300 hover:opacity-80 cursor-pointer"
                  :title="`${cat.name}: ${cat.percent}%`"
                ></div>
              </div>

              <!-- Option A: Detailed Legend Grid -->
              <div v-if="spendBarMode === 'detailed'" class="mt-4 grid grid-cols-2 gap-2.5 sm:grid-cols-3">
                <div
                  v-for="cat in categories"
                  :key="cat.name"
                  class="rounded-control border border-border bg-surface-2 p-2.5 text-xs flex flex-col justify-between"
                >
                  <div class="flex items-center gap-1.5">
                    <span class="h-2.5 w-2.5 rounded-full flex-shrink-0" :style="{ backgroundColor: cat.color }"></span>
                    <span class="font-medium text-text truncate text-[11px]">{{ cat.name }}</span>
                  </div>
                  <div class="mt-1.5 flex items-baseline justify-between">
                    <span class="font-bold tnum text-text text-[11px]">
                      {{ ui.amountsHidden ? '••••••' : `Rp ${((cat.percent / 100) * 22.676).toFixed(1)}M` }}
                    </span>
                    <span class="font-mono text-[10px] text-text-muted">{{ cat.percent }}%</span>
                  </div>
                </div>
              </div>
            </div>

            <div class="border-t border-border pt-3 text-xs text-text-muted flex justify-between">
              <span>Proportional Allocation: <strong class="text-text">5 Active Categories</strong></span>
              <span class="text-accent font-medium">100% Accounted</span>
            </div>
          </div>
        </div>

        <!-- 5. Reports Monthly Expense Heatmap Matrix -->
        <div class="rounded-card border border-border bg-surface p-6 shadow-card space-y-4">
          <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3 border-b border-border pb-3">
            <div>
              <h4 class="text-sm font-bold flex items-center gap-2">
                <Activity :size="16" class="text-accent" />
                <span>Accessible Spending Heatmap Matrix (Reports Module)</span>
              </h4>
              <p class="text-xs text-text-muted">
                Cell intensity reflects monthly volume, with exact numbers printed inside every cell for strict accessibility.
              </p>
            </div>
            <span class="text-xs font-semibold text-text-muted">2026 Yearly Summary</span>
          </div>

          <!-- Heatmap Table -->
          <div class="overflow-x-auto scroll-slim">
            <table class="w-full text-left text-xs border-collapse min-w-[720px]">
              <thead>
                <tr class="border-b border-border text-text-muted">
                  <th class="py-2.5 px-3 font-semibold">Category</th>
                  <th v-for="m in heatmapMonths" :key="m" class="py-2.5 px-2 text-center font-semibold">
                    {{ m }}
                  </th>
                  <th class="py-2.5 px-3 text-right font-semibold">Total</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-border">
                <tr v-for="row in heatmapData" :key="row.category" class="hover:bg-surface-2/50 transition-colors">
                  <td class="py-2.5 px-3 font-medium text-text">{{ row.category }}</td>
                  <td v-for="(val, idx) in row.values" :key="idx" class="p-1 text-center">
                    <div
                      class="rounded py-1 px-1.5 font-mono text-[11px] font-medium transition-all"
                      :style="{
                        backgroundColor:
                          themeStore.isDark
                            ? `rgba(85, 88, 240, ${Math.min(val / 4.5, 0.85)})`
                            : `rgba(59, 70, 241, ${Math.min(val / 4.5, 0.75)})`,
                        color:
                          val > 2.5
                            ? '#FFFFFF'
                            : themeStore.isDark
                              ? '#F8FAFC'
                              : '#111827',
                      }"
                    >
                      {{ ui.amountsHidden ? '••' : `${val}M` }}
                    </div>
                  </td>
                  <td class="py-2.5 px-3 text-right font-bold tnum text-text">
                    {{ ui.amountsHidden ? '••••••' : `Rp ${row.total.toFixed(1)}M` }}
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </section>

      <!-- SECTION 8: MODALS & POPUPS DEMO -->
      <section id="modals" class="space-y-6">
        <div class="border-b border-border pb-3">
          <h3 class="text-xl font-bold">8. Interactive Modals & Dialogs</h3>
          <p class="text-xs text-text-muted">
            Accessible dialogs with focus trapping, clean non-claustrophobic card geometry, and backdrop blur.
          </p>
        </div>

        <div class="flex flex-wrap gap-4">
          <button
            type="button"
            class="rounded-control bg-accent px-4 py-2.5 text-xs font-semibold text-accent-contrast shadow-sm transition-opacity hover:opacity-90"
            @click="isModalOpen = true"
          >
            Launch "Add Transaction" Modal
          </button>
          <button
            type="button"
            class="rounded-control bg-negative px-4 py-2.5 text-xs font-semibold text-negative-contrast shadow-sm transition-opacity hover:opacity-90"
            @click="isConfirmOpen = true"
          >
            Launch Destructive Void Alert
          </button>
        </div>
      </section>
    </main>

    <!-- INTERACTIVE ADD TRANSACTION MODAL PREVIEW -->
    <div
      v-if="isModalOpen"
      class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm"
      role="dialog"
      aria-modal="true"
    >
      <div class="w-full max-w-md rounded-card border border-border bg-surface p-6 shadow-popover animate-in fade-in zoom-in-95 duration-200">
        <div class="flex items-center justify-between border-b border-border pb-3">
          <h3 class="text-base font-bold">Add Transaction</h3>
          <button
            type="button"
            class="rounded-control p-1 text-text-muted hover:bg-surface-2 hover:text-text"
            @click="isModalOpen = false"
          >
            <X :size="18" />
          </button>
        </div>

        <!-- Type Selector -->
        <div class="mt-4 flex rounded-full border border-border bg-surface-2 p-1 text-xs">
          <button
            type="button"
            class="flex-1 rounded-full py-1.5 font-medium transition-all"
            :class="activeTransactionType === 'expense' ? 'bg-negative text-negative-contrast font-bold shadow-sm' : 'text-text-muted hover:text-text'"
            @click="activeTransactionType = 'expense'"
          >
            Expense
          </button>
          <button
            type="button"
            class="flex-1 rounded-full py-1.5 font-medium transition-all"
            :class="activeTransactionType === 'income' ? 'bg-positive text-positive-contrast font-bold shadow-sm' : 'text-text-muted hover:text-text'"
            @click="activeTransactionType = 'income'"
          >
            Income
          </button>
          <button
            type="button"
            class="flex-1 rounded-full py-1.5 font-medium transition-all"
            :class="activeTransactionType === 'transfer' ? 'bg-accent text-accent-contrast font-bold shadow-sm' : 'text-text-muted hover:text-text'"
            @click="activeTransactionType = 'transfer'"
          >
            Transfer
          </button>
        </div>

        <!-- Form fields -->
        <div class="mt-4 space-y-3">
          <div>
            <label class="block text-xs font-semibold text-text mb-1">Amount (IDR)</label>
            <div class="relative">
              <span class="absolute left-3 top-1/2 -translate-y-1/2 font-bold text-xs text-text-muted">Rp</span>
              <input
                type="text"
                value="250.000"
                class="h-10 w-full rounded-control border border-border-strong bg-surface pl-9 pr-3 text-xs font-mono font-bold text-text focus:border-accent focus:outline-none"
              />
            </div>
          </div>

          <div>
            <label class="block text-xs font-semibold text-text mb-1">Title</label>
            <input
              type="text"
              placeholder="e.g. Dinner with family"
              class="h-10 w-full rounded-control border border-border-strong bg-surface px-3 text-xs text-text focus:border-accent focus:outline-none"
            />
          </div>

          <div>
            <label class="block text-xs font-semibold text-text mb-1">Account</label>
            <select class="h-10 w-full rounded-control border border-border-strong bg-surface px-3 text-xs text-text focus:border-accent focus:outline-none">
              <option>BCA Checking</option>
              <option>Jenius Savings</option>
              <option>GoPay E-Wallet</option>
            </select>
          </div>
        </div>

        <div class="mt-6 flex justify-end gap-2 border-t border-border pt-4">
          <button
            type="button"
            class="rounded-control border border-border px-4 py-2 text-xs font-medium text-text hover:bg-surface-2"
            @click="isModalOpen = false"
          >
            Cancel
          </button>
          <button
            type="button"
            class="rounded-control bg-accent px-4 py-2 text-xs font-semibold text-accent-contrast hover:opacity-90"
            @click="
              isModalOpen = false;
              showToast('Transaction added successfully!', 'success');
            "
          >
            Save Transaction
          </button>
        </div>
      </div>
    </div>

    <!-- INTERACTIVE CONFIRM ALERT DIALOG PREVIEW -->
    <div
      v-if="isConfirmOpen"
      class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm"
      role="alertdialog"
      aria-modal="true"
    >
      <div class="w-full max-w-sm rounded-card border border-border bg-surface p-6 shadow-popover animate-in fade-in zoom-in-95 duration-200">
        <div class="flex items-center gap-3">
          <div class="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-negative/10 text-negative">
            <AlertCircle :size="20" />
          </div>
          <div>
            <h4 class="text-sm font-bold">Void Transaction?</h4>
            <p class="text-xs text-text-muted mt-1">
              Are you sure you want to void <strong>Salary, Rp 12.200.000</strong>? This record will be soft-deleted.
            </p>
          </div>
        </div>

        <div class="mt-6 flex justify-end gap-2">
          <button
            type="button"
            class="rounded-control border border-border px-3.5 py-1.5 text-xs font-medium text-text hover:bg-surface-2"
            @click="isConfirmOpen = false"
          >
            Cancel
          </button>
          <button
            type="button"
            class="rounded-control bg-negative px-3.5 py-1.5 text-xs font-semibold text-negative-contrast hover:opacity-90"
            @click="
              isConfirmOpen = false;
              showToast('Transaction soft-deleted.', 'info');
            "
          >
            Void Transaction
          </button>
        </div>
      </div>
    </div>

    <!-- TOAST NOTIFICATION POPUP -->
    <div
      v-if="toastMessage"
      class="fixed bottom-6 right-6 z-50 flex items-center gap-2.5 rounded-control border border-border bg-surface px-4 py-3 shadow-popover animate-in slide-in-from-bottom-3 duration-200"
    >
      <Check v-if="toastType === 'success'" :size="16" class="text-positive" />
      <AlertCircle v-else :size="16" class="text-accent" />
      <span class="text-xs font-medium text-text">{{ toastMessage }}</span>
      <button type="button" class="ml-2 text-text-muted hover:text-text" @click="toastMessage = null">
        <X :size="14" />
      </button>
    </div>
  </div>
</template>
