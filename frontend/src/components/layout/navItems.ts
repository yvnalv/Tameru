import {
  LayoutDashboard,
  ArrowLeftRight,
  Wallet,
  PieChart,
  Target,
  Tags,
  BarChart3,
  Settings,
  Sparkles,
  CreditCard,
  Repeat,
  Flag,
} from 'lucide-vue-next';
import type { Component } from 'vue';

export interface NavItem {
  /** i18n key under `nav.*`, also the router route name. */
  key: string;
  icon: Component;
  route: string;
  /** True until the real screen ships; the route shows a "coming soon" placeholder. */
  placeholder?: boolean;
}

export interface NavSection {
  titleKey: string;
  items: NavItem[];
}

export const navSections: NavSection[] = [
  {
    titleKey: 'nav.sectionCore',
    items: [
      { key: 'dashboard', icon: LayoutDashboard, route: 'dashboard' },
      { key: 'transactions', icon: ArrowLeftRight, route: 'transactions' },
      { key: 'accounts', icon: Wallet, route: 'accounts' },
    ],
  },
  {
    titleKey: 'nav.sectionPlanning',
    items: [
      { key: 'budget', icon: PieChart, route: 'budget' },
      { key: 'masterPlan', icon: Target, route: 'masterPlan' },
      { key: 'debts', icon: CreditCard, route: 'debts' },
      { key: 'recurring', icon: Repeat, route: 'recurring' },
      { key: 'goals', icon: Flag, route: 'goals', placeholder: true },
    ],
  },
  {
    titleKey: 'nav.sectionIntelligence',
    items: [
      { key: 'insights', icon: Sparkles, route: 'insights' },
      { key: 'reports', icon: BarChart3, route: 'reports' },
    ],
  },
  {
    titleKey: 'nav.sectionSystem',
    items: [
      { key: 'categories', icon: Tags, route: 'categories' },
      { key: 'settings', icon: Settings, route: 'settings' },
    ],
  },
];

// Flat list of all nav items for routes, command palette, and lookups
export const navItems: NavItem[] = navSections.flatMap((s) => s.items);

export function iconForRoute(name: string): Component | undefined {
  return navItems.find((i) => i.route === name)?.icon;
}

// The mobile bottom-nav pill shows four destinations plus a "More" button (five slots, for thumb
// reach). Everything not in the pill must stay reachable from the More sheet.
export const mobileNavItems: NavItem[] = [
  { key: 'dashboard', icon: LayoutDashboard, route: 'dashboard' },
  { key: 'transactions', icon: ArrowLeftRight, route: 'transactions' },
  { key: 'accounts', icon: Wallet, route: 'accounts' },
  { key: 'insights', icon: Sparkles, route: 'insights' },
];

export const mobileMoreItems: NavItem[] = navItems.filter(
  (item) => !mobileNavItems.some((m) => m.route === item.route),
);
