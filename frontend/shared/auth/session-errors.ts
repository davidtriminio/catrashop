import { HttpErrorResponse } from '@angular/common/http';

/** Only an explicit rejection of the credentials ends the session; network errors, 429 and 5xx keep it. */
export function isSessionRejected(error: unknown): boolean {
  return error instanceof HttpErrorResponse && (error.status === 400 || error.status === 401 || error.status === 403);
}
