import { HttpErrorResponse } from '@angular/common/http';
import { problemMessage } from '@ch/util/problem';

describe('problemMessage', () => {
  it('prefers the first validation error when present', () => {
    const error = new HttpErrorResponse({
      status: 400,
      error: { title: 'Validation failed', errors: { Name: ['Name is required.'] } },
    });
    expect(problemMessage(error)).toBe('Name is required.');
  });

  it('falls back to detail, then title', () => {
    const withDetail = new HttpErrorResponse({ status: 409, error: { title: 'Conflict', detail: 'Already exists.' } });
    expect(problemMessage(withDetail)).toBe('Already exists.');

    const withTitle = new HttpErrorResponse({ status: 409, error: { title: 'Conflict' } });
    expect(problemMessage(withTitle)).toBe('Conflict');
  });

  it('uses a plain string error body verbatim', () => {
    const error = new HttpErrorResponse({ status: 500, error: 'Boom' });
    expect(problemMessage(error)).toBe('Boom');
  });

  it('reports an unreachable server for status 0', () => {
    const error = new HttpErrorResponse({ status: 0, error: null });
    expect(problemMessage(error)).toBe('Cannot reach the server. Is the API running?');
  });

  it('returns the fallback for non-HTTP errors', () => {
    expect(problemMessage(new Error('nope'), 'default message')).toBe('default message');
  });
});
