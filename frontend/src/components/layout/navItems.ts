import { LayoutDashboard, ArrowLeftRight, Wallet, PieChart, Target, Tags, BarChart3, Settings } from 'lucide-vue-next';
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
  { key: 'reports', icon: BarChart3, route: 'reports' },
  { key: 'budget', icon: PieChart, route: 'budget' },
  { key: 'masterPlan', icon: Target, route: 'masterPlan' },
  { key: 'categories', icon: Tags, route: 'categories' },
  { key: 'settings', icon: Settings, route: 'settings' },
];

export function iconForRoute(name: string): Component | undefined {
  return navItems.find((i) => i.route === name)?.icon;
}

// The mobile bottom-nav pill shows four destinations plus a "More" button (five slots, for thumb
// reach). Everything not in the pill must stay reachable from the More sheet — the sidebar is
// hidden below `md`, so an item in neither place has no navigation path at all on a phone.
export const MOBILE_PRIMARY_COUNT = 4;
export const mobileNavItems: NavItem[] = navItems.slice(0, MOBILE_PRIMARY_COUNT);
export const mobileMoreItems: NavItem[] = navItems.slice(MOBILE_PRIMARY_COUNT);
