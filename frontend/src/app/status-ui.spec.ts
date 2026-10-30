import { TestBed } from '@angular/core/testing';
import { I18n, provideI18n } from '@cs/i18n/i18n';
import { StatusBadge, toneFor } from '@cs/ui/status-badge';

describe('status badge', () => {
  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({ providers: [provideI18n()] });
  });

  it('maps every order state to a tone', () => {
    expect(toneFor('Paid')).toBe('accent');
    expect(toneFor('Confirmed')).toBe('accent');
    expect(toneFor('UnderReview')).toBe('warn');
    expect(toneFor('Captured')).toBe('ok');
    expect(toneFor('Processing')).toBe('warn');
    expect(toneFor('Shipped')).toBe('info');
    expect(toneFor('Delivered')).toBe('ok');
    expect(toneFor('Cancelled')).toBe('bad');
    expect(toneFor('PaymentFailed')).toBe('bad');
    expect(toneFor('PaymentPending')).toBe('neutral');
    expect(toneFor('SomethingNew')).toBe('neutral');
  });

  it('renders a translated label and the matching class', async () => {
    const i18n = TestBed.inject(I18n);
    await i18n.use('es');
    const fixture = TestBed.createComponent(StatusBadge);
    fixture.componentRef.setInput('status', 'PaymentPending');
    fixture.detectChanges();
    const badge = fixture.nativeElement.querySelector('.badge') as HTMLElement;
    expect(badge.textContent?.trim()).toBe('Pago pendiente');

    fixture.componentRef.setInput('status', 'Paid');
    fixture.detectChanges();
    expect(badge.textContent?.trim()).toBe('Pagado');
    expect(badge.classList.contains('accent')).toBe(true);
  });

  it('follows the language and keeps showing a status it has no text for', async () => {
    const i18n = TestBed.inject(I18n);
    await i18n.use('en');
    const fixture = TestBed.createComponent(StatusBadge);
    fixture.componentRef.setInput('status', 'PaymentPending');
    fixture.detectChanges();
    const badge = fixture.nativeElement.querySelector('.badge') as HTMLElement;
    expect(badge.textContent?.trim()).toBe('Payment pending');

    await i18n.use('es');
    fixture.detectChanges();
    expect(badge.textContent?.trim()).toBe('Pago pendiente');

    fixture.componentRef.setInput('status', 'BrandNewState');
    fixture.detectChanges();
    expect(badge.textContent?.trim()).toBe('Brand new state');
  });
});
