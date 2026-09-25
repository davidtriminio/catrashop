import { Route } from '@angular/router';
import { routes } from './app.routes';
import { ADMIN_NAV } from './admin-permissions';

const byPath = (path: string): Route | undefined => routes.find((r) => r.path === path);

describe('admin route configuration', () => {
  it('exposes login and forbidden as public routes (no guard)', () => {
    for (const path of ['login', 'forbidden']) {
      const route = byPath(path);
      expect(route, `missing route '${path}'`).toBeDefined();
      expect(route!.canActivate ?? []).toHaveLength(0);
    }
  });

  it('guards every navigable section', () => {
    for (const nav of ADMIN_NAV) {
      const path = nav.path.replace(/^\//, '');
      const route = byPath(path);
      expect(route, `missing route for nav '${nav.label}'`).toBeDefined();
      expect(route!.canActivate?.length ?? 0, `route '${path}' is not guarded`).toBeGreaterThan(0);
      expect(typeof route!.loadComponent).toBe('function');
    }
  });

  it('redirects the empty and wildcard paths to the dashboard', () => {
    expect(byPath('')?.redirectTo).toBe('dashboard');
    expect(byPath('**')?.redirectTo).toBe('dashboard');
  });
});
