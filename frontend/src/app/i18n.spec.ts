import { TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import en from '@cs/i18n/en.json';
import es from '@cs/i18n/es.json';
import { I18n, provideI18n } from '@cs/i18n/i18n';
import { LocalDatePipe } from '@cs/util/local-date.pipe';
import { PluralPipe } from '@cs/util/plural.pipe';

type Tree = { [key: string]: string | Tree };

function flatten(tree: Tree, prefix = ''): Record<string, string> {
  const out: Record<string, string> = {};
  for (const [key, value] of Object.entries(tree)) {
    if (typeof value === 'string') out[prefix + key] = value;
    else Object.assign(out, flatten(value, `${prefix}${key}.`));
  }
  return out;
}

const params = (text: string) => [...text.matchAll(/\{\{\s*(\w+)\s*\}\}/g)].map((m) => m[1]).sort();

describe('translation files', () => {
  const spanish = flatten(es as Tree);
  const english = flatten(en as Tree);

  it('have the same keys in es and en', () => {
    const missingInEs = Object.keys(english).filter((k) => !(k in spanish));
    const missingInEn = Object.keys(spanish).filter((k) => !(k in english));
    expect(missingInEs).toEqual([]);
    expect(missingInEn).toEqual([]);
  });

  it('use the same {{params}} in both languages', () => {
    const mismatched = Object.keys(english).filter((k) => k in spanish && params(english[k]).join() !== params(spanish[k]).join());
    expect(mismatched).toEqual([]);
  });

  it('have no empty texts', () => {
    expect(Object.entries(spanish).filter(([, v]) => !v.trim()).map(([k]) => k)).toEqual([]);
    expect(Object.entries(english).filter(([, v]) => !v.trim()).map(([k]) => k)).toEqual([]);
  });

  it('translate every order, payment and shipment status the badge knows', () => {
    const statuses = [
      'pending', 'paymentPending', 'paid', 'paymentFailed', 'processing', 'shipped', 'delivered', 'cancelled',
      'refundRequested', 'refunded', 'labelCreated', 'pickedUp', 'inTransit', 'outForDelivery', 'failed', 'returned',
    ];
    for (const status of statuses) expect(spanish[`status.${status}`], status).toBeTruthy();
  });
});

describe('I18n at runtime', () => {
  let i18n: I18n;

  beforeEach(async () => {
    localStorage.clear();
    TestBed.configureTestingModule({ providers: [provideI18n()] });
    i18n = TestBed.inject(I18n);
    await i18n.use('es');
  });

  it('translates with parameters and switches language at runtime', async () => {
    expect(i18n.t('checkout.placed', { number: 'ORD-1' })).toBe('Pedido ORD-1 realizado');
    await i18n.use('en');
    expect(i18n.t('checkout.placed', { number: 'ORD-1' })).toBe('Order ORD-1 placed');
    expect(i18n.lang()).toBe('en');
    expect(document.documentElement.lang).toBe('en');
  });

  it('remembers the chosen language for the next visit', async () => {
    await i18n.use('en');
    expect(localStorage.getItem('cs_lang')).toBe('en');
    await i18n.init('es');
    expect(i18n.lang()).toBe('en');
  });

  it('shows the key text of an unknown status in readable form', () => {
    expect(i18n.tOr('status.somethingNew', 'Something new')).toBe('Something new');
    expect(i18n.tOr('status.paid', 'Paid')).toBe('Pagado');
  });

  it('prefers the server message and falls back to a translated one', () => {
    const withMessage = new HttpErrorResponse({ status: 409, error: { title: 'Conflict', detail: 'Ya existe.' } });
    expect(i18n.problem(withMessage, 'auth.login.error')).toBe('Ya existe.');
    expect(i18n.problem(new HttpErrorResponse({ status: 500, error: null }), 'auth.login.error')).toBe('Correo o contraseña incorrectos.');
    expect(i18n.problem(new HttpErrorResponse({ status: 0, error: null }))).toBe('No se pudo conectar con el servidor. ¿Está en marcha la API?');
  });

  it('picks the singular or plural text', () => {
    TestBed.runInInjectionContext(() => {
      const plural = new PluralPipe();
      expect(plural.transform('cart.items', 1)).toBe('1 artículo');
      expect(plural.transform('cart.items', 3)).toBe('3 artículos');
      expect(plural.transform('cart.items', 0)).toBe('0 artículos');
    });
  });

  it('formats dates in the active language', async () => {
    const pipe = TestBed.runInInjectionContext(() => new LocalDatePipe());
    const date = '2026-10-28T15:30:00';
    const spanishText = pipe.transform(date, 'date');
    expect(spanishText.toLowerCase()).toContain('oct');
    expect(spanishText).toContain('28');
    await i18n.use('en');
    expect(pipe.transform(date, 'date')).toMatch(/Oct 28, 2026/);
    expect(pipe.transform(null)).toBe('');
    expect(pipe.transform('not a date')).toBe('');
  });
});
