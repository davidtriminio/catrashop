import { Routes } from '@angular/router';
import { authGuard } from '@ch/auth/auth.guard';
import { permissionGuard } from '@ch/auth/permission.guard';
import { PERMISSIONS } from './admin-permissions';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
  { path: 'login', loadComponent: () => import('./features/login').then((m) => m.AdminLogin) },
  { path: 'forbidden', loadComponent: () => import('./features/forbidden').then((m) => m.Forbidden) },

  {
    path: 'dashboard',
    canActivate: [authGuard],
    loadComponent: () => import('./features/dashboard').then((m) => m.Dashboard),
  },
  {
    path: 'catalog',
    canActivate: [permissionGuard(PERMISSIONS.catalog)],
    loadComponent: () => import('./features/catalog-admin').then((m) => m.CatalogAdmin),
  },
  {
    path: 'inventory',
    canActivate: [permissionGuard(PERMISSIONS.inventory)],
    loadComponent: () => import('./features/inventory-admin').then((m) => m.InventoryAdmin),
  },
  {
    path: 'orders',
    canActivate: [permissionGuard(PERMISSIONS.ordersRead)],
    loadComponent: () => import('./features/orders-admin').then((m) => m.OrdersAdmin),
  },
  {
    path: 'orders/:id',
    canActivate: [permissionGuard(PERMISSIONS.ordersRead)],
    loadComponent: () => import('./features/order-detail-admin').then((m) => m.OrderDetailAdmin),
  },
  {
    path: 'promotions',
    canActivate: [permissionGuard(PERMISSIONS.promotions)],
    loadComponent: () => import('./features/promotions-admin').then((m) => m.PromotionsAdmin),
  },
  {
    path: 'shipping',
    canActivate: [permissionGuard(PERMISSIONS.ordersManage)],
    loadComponent: () => import('./features/shipping-admin').then((m) => m.ShippingAdmin),
  },
  {
    path: 'notifications',
    canActivate: [permissionGuard(PERMISSIONS.notifications)],
    loadComponent: () => import('./features/notifications-admin').then((m) => m.NotificationsAdmin),
  },

  { path: '**', redirectTo: 'dashboard' },
];
