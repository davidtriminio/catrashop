import { HttpErrorResponse } from '@angular/common/http';
import { ProblemDetails } from '../api/models';

/**
 * Extracts a human-readable message from an API error (RFC 7807 problem details or validation errors).
 * `fallback` is used when the server sent nothing usable; `offline` when it could not be reached at all.
 */
export function problemMessage(
  error: unknown,
  fallback = 'Something went wrong. Please try again.',
  offline = 'Cannot reach the server. Is the API running?',
): string {
  if (error instanceof HttpErrorResponse) {
    const problem = error.error as ProblemDetails | string | null;
    if (typeof problem === 'string' && problem.trim()) return problem;
    if (problem && typeof problem === 'object') {
      if (problem.errors) {
        const first = Object.values(problem.errors)[0];
        if (first && first.length) return first[0];
      }
      if (problem.detail) return problem.detail;
      if (problem.title) return problem.title;
    }
    if (error.status === 0) return offline;
  }
  return fallback;
}
