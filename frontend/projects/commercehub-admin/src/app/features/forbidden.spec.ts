import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Forbidden } from './forbidden';

describe('Forbidden component', () => {
  it('renders the access-denied message and a link back to the dashboard', () => {
    TestBed.configureTestingModule({
      imports: [Forbidden],
      providers: [provideRouter([])],
    });

    const fixture = TestBed.createComponent(Forbidden);
    fixture.detectChanges();
    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';

    expect(text).toContain('Access denied');

    const backLink = (fixture.nativeElement as HTMLElement).querySelector('a[href="/dashboard"]');
    expect(backLink).not.toBeNull();
  });
});
