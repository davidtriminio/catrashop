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

export interface NavItem { label: string; path: string; icon: IconName; permission?: string; }

export const ADMIN_NAV: NavItem[] = [
  { label: 'Dashboard', path: '/dashboard', icon: 'dashboard' },
  { label: 'Catalog', path: '/catalog', icon: 'package', permission: PERMISSIONS.catalog },
  { label: 'Inventory', path: '/inventory', icon: 'warehouse', permission: PERMISSIONS.inventory },
  { label: 'Orders', path: '/orders', icon: 'bag', permission: PERMISSIONS.ordersRead },
  { label: 'Payments', path: '/payments', icon: 'card', permission: PERMISSIONS.ordersManage },
  { label: 'Promotions', path: '/promotions', icon: 'tag', permission: PERMISSIONS.promotions },
  { label: 'Shipping', path: '/shipping', icon: 'truck', permission: PERMISSIONS.ordersManage },
  { label: 'Notifications', path: '/notifications', icon: 'bell', permission: PERMISSIONS.notifications },
];
