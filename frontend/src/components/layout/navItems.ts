import { LayoutDashboard, ArrowLeftRight, Wallet, PieChart, Target, Tags } from 'lucide-vue-next';
import type { Component } from 'vue';

export interface NavItem {
  /** i18n key under `nav.*`, also the router route name. */
  key: string;
  icon: Component;
  route: string;
  /** True until the real screen ships; the route shows a "coming soon" placeholder. */
  placeholder?: boolean;
}

// Mirrors the workbook menu (CLAUDE.md → Modules & Menu). All MVP screens are live.
export const navItems: NavItem[] = [
  { key: 'dashboard', icon: LayoutDashboard, route: 'dashboard' },
  { key: 'transactions', icon: ArrowLeftRight, route: 'transactions' },
  { key: 'accounts', icon: Wallet, route: 'accounts' },
  { key: 'budget', icon: PieChart, route: 'budget' },
  { key: 'masterPlan', icon: Target, route: 'masterPlan' },
  { key: 'categories', icon: Tags, route: 'categories' },
];

export function iconForRoute(name: string): Component | undefined {
  return navItems.find((i) => i.route === name)?.icon;
}

// The subset shown in the mobile bottom-nav pill (keep it to five for thumb reach).
export const mobileNavItems: NavItem[] = navItems.slice(0, 5);
