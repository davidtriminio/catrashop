/** Permission strings the admin console gates on (mirror of the backend module permissions). */
export const PERMISSIONS = {
  catalog: 'catalog.products.manage',
  inventory: 'inventory.manage',
  ordersRead: 'orders.read_all',
  ordersManage: 'orders.manage',
  promotions: 'promotions.manage',
  notifications: 'notifications.manage',
} as const;

export interface NavItem { label: string; path: string; permission?: string; }

export const ADMIN_NAV: NavItem[] = [
  { label: 'Dashboard', path: '/dashboard' },
  { label: 'Catalog', path: '/catalog', permission: PERMISSIONS.catalog },
  { label: 'Inventory', path: '/inventory', permission: PERMISSIONS.inventory },
  { label: 'Orders', path: '/orders', permission: PERMISSIONS.ordersRead },
  { label: 'Promotions', path: '/promotions', permission: PERMISSIONS.promotions },
  { label: 'Shipping', path: '/shipping', permission: PERMISSIONS.ordersManage },
  { label: 'Notifications', path: '/notifications', permission: PERMISSIONS.notifications },
];
