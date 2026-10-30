import { PERMISSIONS, ADMIN_NAV } from './admin-permissions';

describe('admin navigation', () => {
  it('gates every non-dashboard section on a permission', () => {
    const gated = ADMIN_NAV.filter((n) => n.path !== '/dashboard');
    expect(gated.length).toBeGreaterThan(0);
    expect(gated.every((n) => !!n.permission)).toBe(true);
  });

  it('maps to the backend permission strings', () => {
    expect(PERMISSIONS.catalog).toBe('catalog.products.manage');
    expect(PERMISSIONS.ordersManage).toBe('orders.manage');
  });
});
