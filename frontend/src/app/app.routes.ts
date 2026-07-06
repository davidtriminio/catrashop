import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'products' },

  // Catalog (public)
  {
    path: 'products',
    loadComponent: () => import('./features/catalog/product-list').then((m) => m.ProductList),
  },
  {
    path: 'products/:slug',
    loadComponent: () => import('./features/catalog/product-detail').then((m) => m.ProductDetail),
  },

  // Cart & checkout
  {
    path: 'cart',
    loadComponent: () => import('./features/cart/cart-page').then((m) => m.CartPage),
  },
  {
    path: 'checkout',
    canActivate: [authGuard],
    loadComponent: () => import('./features/checkout/checkout-page').then((m) => m.CheckoutPage),
  },

  // Orders (auth)
  {
    path: 'orders',
    canActivate: [authGuard],
    loadComponent: () => import('./features/orders/order-list').then((m) => m.OrderList),
  },
  {
    path: 'orders/:id',
    canActivate: [authGuard],
    loadComponent: () => import('./features/orders/order-detail').then((m) => m.OrderDetailPage),
  },

  // Account (auth)
  {
    path: 'account',
    canActivate: [authGuard],
    loadComponent: () => import('./features/account/account-page').then((m) => m.AccountPage),
  },

  // Notifications (auth)
  {
    path: 'notifications',
    canActivate: [authGuard],
    loadComponent: () => import('./features/notifications/notifications-page').then((m) => m.NotificationsPage),
  },

  // Tracking (public)
  {
    path: 'track',
    loadComponent: () => import('./features/tracking/tracking-page').then((m) => m.TrackingPage),
  },

  // Auth
  { path: 'login', loadComponent: () => import('./features/auth/login').then((m) => m.Login) },
  { path: 'register', loadComponent: () => import('./features/auth/register').then((m) => m.Register) },

  { path: '**', redirectTo: 'products' },
];
