import { LayoutDashboard, ArrowLeftRight, Wallet, PieChart, Target, Tags } from 'lucide-vue-next';
import type { Component } from 'vue';

export interface NavItem {
  /** i18n key under `nav.*`. */
  key: string;
  icon: Component;
  /** Router route name; when omitted the item is a not-yet-built placeholder (M7). */
  route?: string;
}

// Mirrors the workbook menu (CLAUDE.md → Modules & Menu). Only Dashboard is wired in M6.
export const navItems: NavItem[] = [
  { key: 'dashboard', icon: LayoutDashboard, route: 'dashboard' },
  { key: 'transactions', icon: ArrowLeftRight },
  { key: 'accounts', icon: Wallet },
  { key: 'budget', icon: PieChart },
  { key: 'masterPlan', icon: Target },
  { key: 'categories', icon: Tags },
];

// The subset shown in the mobile bottom-nav pill (keep it to five for thumb reach).
export const mobileNavItems: NavItem[] = navItems.slice(0, 5);
