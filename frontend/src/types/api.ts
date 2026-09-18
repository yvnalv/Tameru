// Mirrors the backend response envelope (docs/API_SPEC.md) and the MVP DTOs the client consumes.

export interface ApiError {
  code: string;
  details?: { field: string; message: string }[] | null;
  traceId?: string | null;
}

export interface ApiSuccess<T> {
  success: true;
  data: T;
  message: null;
  error: null;
}

export interface ApiFailure {
  success: false;
  data: null;
  message: string;
  error: ApiError;
}

export type ApiResponse<T> = ApiSuccess<T> | ApiFailure;

export interface Paged<T> {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
  totalPages: number;
}

// --- Accounts ---------------------------------------------------------------

export type AccountType = 'Cash' | 'Bank' | 'EWallet' | 'Investment' | 'Blocked';

export interface Account {
  id: string;
  name: string;
  groupId: string | null;
  groupName: string | null;
  type: string;
  openingBalance: number;
  balance: number;
  currencyCode: string;
  isActive: boolean;
  sortOrder: number;
}

export interface AccountGroup {
  id: string;
  name: string;
  sortOrder: number;
  accountCount: number;
  totalBalance: number;
}

// --- Ledger -----------------------------------------------------------------

export type TransactionType = 'Income' | 'Expense' | 'Transfer';
export type TransactionStatus = 'Cleared' | 'Uncleared';

export interface Transaction {
  id: string;
  type: string;
  date: string;
  title: string;
  amount: number;
  currencyCode: string;
  accountId: string;
  accountName?: string | null;
  toAccountId: string | null;
  budgetCategoryId: string | null;
  categoryId: string | null;
  categoryName?: string | null;
  subCategoryId: string | null;
  status: string;
  description: string | null;
}

// --- Categories -------------------------------------------------------------

export type CategoryLevel = 'Budget' | 'Category' | 'Sub';
export type CategoryFlow = 'Income' | 'Expense' | 'Transfer' | 'Any';

export interface Category {
  id: string;
  name: string;
  level: string;
  parentId: string | null;
  flow: string;
  isSystem: boolean;
  isActive: boolean;
  sortOrder: number;
}

// --- Budget -----------------------------------------------------------------

export interface BudgetPeriodSummary {
  id: string;
  year: number;
  month: number;
  note: string | null;
}

export interface BudgetLine {
  categoryId: string;
  categoryName: string | null;
  plan: number;
  actual: number;
  leftover: number;
}

export interface BudgetPeriod {
  id: string;
  year: number;
  month: number;
  note: string | null;
  lines: BudgetLine[];
  totalPlan: number;
  totalActual: number;
  totalLeftover: number;
  startDate?: string;
  endDate?: string;
}

// --- Master Plan ------------------------------------------------------------

export interface MasterPlanItem {
  id: string;
  sectionId: string;
  name: string;
  price: number;
  frequency: number;
  totalBudget: number;
  sortOrder: number;
}

export interface MasterPlanSection {
  id: string;
  name: string;
  targetPercent: number;
  sortOrder: number;
  items: MasterPlanItem[];
  total: number;
}

export interface MasterPlan {
  sections: MasterPlanSection[];
  grandTotal: number;
}

// --- Identity ---------------------------------------------------------------

export interface AuthUser {
  id: string;
  email: string;
  displayName: string;
  locale: string;
  budgetCycleStartDay?: number;
  apiToken?: string | null;
}

// --- Automations & Rules ----------------------------------------------------

export type RuleMatchField = 'Payee' | 'Description';
export type RuleMatchOperator = 'Contains' | 'Equals' | 'StartsWith' | 'Regex';

export interface RuleDto {
  id: string;
  name: string;
  pattern: string;
  matchField: RuleMatchField;
  matchOperator: RuleMatchOperator;
  targetCategoryId: string | null;
  targetBudgetCategoryId: string | null;
  targetSubCategoryId: string | null;
  targetStatus: string | null;
  priority: number;
  isActive: boolean;
  minAmount?: number | null;
  maxAmount?: number | null;
  accountId?: string | null;
  transactionType?: TransactionType | null;
  replaceTitle?: string | null;
  groupId?: string | null;
  scheduleExpression?: string | null;
  isTemplate?: boolean;
  tags?: string | null;
}

export interface CreateRuleRequest {
  name: string;
  pattern: string;
  matchField?: RuleMatchField;
  matchOperator?: RuleMatchOperator;
  targetCategoryId?: string | null;
  targetBudgetCategoryId?: string | null;
  targetSubCategoryId?: string | null;
  targetStatus?: string | null;
  priority?: number;
  isActive?: boolean;
  minAmount?: number | null;
  maxAmount?: number | null;
  accountId?: string | null;
  transactionType?: TransactionType | null;
  replaceTitle?: string | null;
  groupId?: string | null;
  scheduleExpression?: string | null;
  isTemplate?: boolean;
  tags?: string | null;
}

export interface UpdateRuleRequest {
  name: string;
  pattern: string;
  matchField: RuleMatchField;
  matchOperator: RuleMatchOperator;
  targetCategoryId: string | null;
  targetBudgetCategoryId: string | null;
  targetSubCategoryId: string | null;
  targetStatus: string | null;
  priority: number;
  isActive: boolean;
  minAmount?: number | null;
  maxAmount?: number | null;
  accountId?: string | null;
  transactionType?: TransactionType | null;
  replaceTitle?: string | null;
  groupId?: string | null;
  scheduleExpression?: string | null;
  isTemplate?: boolean;
  tags?: string | null;
}

export interface IngestTransactionRequest {
  text?: string;
  amount?: number;
  title?: string;
  type?: TransactionType;
  accountName?: string;
  accountId?: string;
  categoryId?: string;
  date?: string;
  description?: string;
}

export interface IngestResultDto {
  transaction: Transaction;
  formattedConfirmation: string;
  appliedRuleName: string | null;
}

export interface DryRunRuleRequest {
  text?: string;
  payee?: string;
  description?: string;
  amount?: number | null;
  accountId?: string | null;
  transactionType?: TransactionType | null;
}

export interface DryRunMatchDto {
  rule: RuleDto;
  matched: boolean;
  matchedReason?: string | null;
  projectedCategoryId?: string | null;
  projectedBudgetCategoryId?: string | null;
  projectedSubCategoryId?: string | null;
  projectedStatus?: string | null;
  projectedTitle?: string | null;
}

export interface DryRunResultDto {
  hasMatch: boolean;
  winningMatch: DryRunMatchDto | null;
  allEvaluations: DryRunMatchDto[];
}

export interface RuleAuditLogDto {
  id: string;
  ruleId: string;
  ruleName: string;
  transactionId: string | null;
  transactionTitle: string;
  amount: number | null;
  matchedField: string;
  matchedValue: string;
  wasApplied: boolean;
  details: string | null;
  evaluatedAt: string;
}

export interface CreateFromTemplateRequest {
  name?: string;
  pattern?: string;
  targetCategoryId?: string | null;
  targetBudgetCategoryId?: string | null;
  targetSubCategoryId?: string | null;
  accountId?: string | null;
  minAmount?: number | null;
  maxAmount?: number | null;
  groupId?: string | null;
  scheduleExpression?: string | null;
  tags?: string | null;
}

// --- AI Assistant Chat ------------------------------------------------------

export interface ChatAction {
  type: string;
  summary: string;
  data?: any;
}

export interface AiProviderConfig {
  provider?: string;
  baseUrl?: string;
  apiKey?: string;
  model?: string;
}

export interface ChatRequest {
  message: string;
  conversationId?: string | null;
  provider?: AiProviderConfig | null;
}

export interface TestConnectionRequest {
  provider?: AiProviderConfig | null;
}

export interface TestConnectionResponse {
  success: boolean;
  message: string;
  model?: string | null;
}

export interface ChatResponse {
  message: string;
  conversationId: string;
  action: ChatAction | null;
  insights: InsightDto[] | null;
}

export interface ChatMessageItem {
  id: string;
  role: 'user' | 'assistant' | 'system';
  content: string;
  timestamp: string;
  action?: ChatAction | null;
  insights?: InsightDto[] | null;
}

export interface AuthTokens {
  accessToken: string;
  accessTokenExpiresAt: string;
  refreshToken: string;
  user: AuthUser;
}

// --- Reporting (M5) ---------------------------------------------------------

export interface AccountBalance {
  accountId: string;
  name: string;
  groupName: string | null;
  type: string;
  balance: number;
  currencyCode: string;
}

export interface NetWorthReport {
  total: number;
  currencyCode: string;
  accounts: AccountBalance[];
}

export interface MonthlyCashflow {
  month: number;
  income: number;
  expense: number;
  net: number;
  savingsRate?: number;
}

export interface CashflowReport {
  year: number;
  month: number;
  income: number;
  expense: number;
  net: number;
  trend: MonthlyCashflow[];
}

export interface OverviewRow {
  categoryId: string;
  months: number[];
  total: number;
}

export interface OverviewReport {
  year: number;
  categories: OverviewRow[];
  monthlyTotals: number[];
  total: number;
}

export interface CategoryTrackerRow {
  categoryId: string;
  amounts: number[];
  total: number;
}

export interface CategoryTrackerReport {
  granularity: string;
  flow?: string;
  from: string;
  to: string;
  periods: string[];
  categories: CategoryTrackerRow[];
  periodTotals: number[];
  total: number;
}

export interface FinancialHealthReport {
  year: number;
  month: number;
  savingsRate: number;
  previousSavingsRate: number | null;
  runwayMonths: number;
  trailing3MonthAvgExpense: number;
  dailyBurnRate: number;
  projectedMonthEndExpense: number;
  daysPassed: number;
  totalDaysInMonth: number;
  momIncomePercent: number | null;
  momExpensePercent: number | null;
  momNetPercent: number | null;
  healthStatus: 'Excellent' | 'Healthy' | 'Low' | 'Deficit';
}

export interface EnvelopeItem {
  budgetCategoryId: string | null;
  amount: number;
  percent: number;
}

export interface EnvelopeReport {
  year: number;
  month: number | null;
  totalExpense: number;
  envelopes: EnvelopeItem[];
}

export interface SafeToSpendDto {
  liquidCash: number;
  unpaidObligations: number;
  safetyBuffer: number;
  safeToSpend: number;
  daysRemaining: number;
  dailyAllowance: number;
  cycleStart: string;
  cycleEnd: string;
  nextPayday: string;
  upcomingObligations: ObligationItemDto[];
  liquidAccounts: LiquidAccountDto[];
}

export interface LiquidAccountDto {
  id: string;
  name: string;
  type: string;
  balance: number;
}

export interface ObligationItemDto {
  id: string;
  name: string;
  amount: number;
  section: string;
  dueDate: string | null;
  isDueBeforePayday: boolean;
}

export interface SimulatePurchaseRequest {
  amount: number;
  categoryId?: string | null;
  description?: string | null;
}

export interface SurplusCategoryDto {
  categoryId: string;
  categoryName: string;
  availableSurplus: number;
}

export interface SimulatePurchaseResultDto {
  verdict: 'Safe' | 'Warning' | 'Risky';
  amount: number;
  categoryId: string | null;
  categoryName: string | null;
  currentSafeToSpend: number;
  newSafeToSpend: number;
  currentDailyAllowance: number;
  newDailyAllowance: number;
  daysRemaining: number;
  categoryPlan: number | null;
  categoryActual: number | null;
  categoryLeftover: number | null;
  categoryLeftoverAfterPurchase: number | null;
  surplusCategories: SurplusCategoryDto[];
  impactSummary: string;
}

// --- Proactive Insights -----------------------------------------------------

export type InsightType = 'spending_velocity' | 'anomaly' | 'budget_pacing' | 'savings_trend' | 'runway' | 'payday';
export type InsightSeverity = 'info' | 'warning' | 'critical';

export interface InsightDto {
  id: string;
  type: InsightType;
  severity: InsightSeverity;
  title: string;
  message: string;
  actionRoute: string | null;
  value: number | null;
  categoryName: string | null;
  generatedAt: string;
}


