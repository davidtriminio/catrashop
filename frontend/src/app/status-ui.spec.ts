import { TestBed } from '@angular/core/testing';
import { StatusBadge, toneFor } from '@ch/ui/status-badge';

describe('status badge', () => {
  it('maps every order state to a tone', () => {
    expect(toneFor('Paid')).toBe('accent');
    expect(toneFor('Processing')).toBe('warn');
    expect(toneFor('Shipped')).toBe('info');
    expect(toneFor('Delivered')).toBe('ok');
    expect(toneFor('Cancelled')).toBe('bad');
    expect(toneFor('PaymentFailed')).toBe('bad');
    expect(toneFor('PaymentPending')).toBe('neutral');
    expect(toneFor('SomethingNew')).toBe('neutral');
  });

  it('renders a readable label and the matching class', () => {
    const fixture = TestBed.createComponent(StatusBadge);
    fixture.componentRef.setInput('status', 'PaymentPending');
    fixture.detectChanges();
    const badge = fixture.nativeElement.querySelector('.badge') as HTMLElement;
    expect(badge.textContent?.trim()).toBe('Payment pending');

    fixture.componentRef.setInput('status', 'Paid');
    fixture.detectChanges();
    expect(badge.classList.contains('accent')).toBe(true);
  });
});
