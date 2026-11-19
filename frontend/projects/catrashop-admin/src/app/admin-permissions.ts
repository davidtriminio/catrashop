import type { IconName } from './ui/icon';

/** Permission strings the admin console gates on (mirror of the backend module permissions). */
export const PERMISSIONS = {
  catalog: 'catalog.products.manage',
  inventory: 'inventory.manage',
  ordersRead: 'orders.read_all',
  ordersManage: 'orders.manage',
  promotions: 'promotions.manage',
  notifications: 'notifications.manage',
} as const;

export interface NavItem { labelKey: string; path: string; icon: IconName; permission?: string; }

export const ADMIN_NAV: NavItem[] = [
  { labelKey: 'admin.nav.dashboard', path: '/dashboard', icon: 'dashboard' },
  { labelKey: 'admin.nav.catalog', path: '/catalog', icon: 'package', permission: PERMISSIONS.catalog },
  { labelKey: 'admin.nav.inventory', path: '/inventory', icon: 'warehouse', permission: PERMISSIONS.inventory },
  { labelKey: 'admin.nav.orders', path: '/orders', icon: 'bag', permission: PERMISSIONS.ordersRead },
  { labelKey: 'admin.nav.payments', path: '/payments', icon: 'card', permission: PERMISSIONS.ordersManage },
  { labelKey: 'admin.nav.promotions', path: '/promotions', icon: 'tag', permission: PERMISSIONS.promotions },
  { labelKey: 'admin.nav.shipping', path: '/shipping', icon: 'truck', permission: PERMISSIONS.ordersManage },
  { labelKey: 'admin.nav.notifications', path: '/notifications', icon: 'bell', permission: PERMISSIONS.notifications },
];
