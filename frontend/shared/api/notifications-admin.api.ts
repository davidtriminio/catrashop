import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './api-base-url';
import { SendNotificationRequest } from './models';

/** Manual/operational notification send (permission: notifications.manage). */
@Injectable({ providedIn: 'root' })
export class NotificationsAdminApi {
  private readonly http = inject(HttpClient);
  private readonly base = inject(API_BASE_URL) + '/api/v1/notifications/admin';

  send(request: SendNotificationRequest): Observable<void> {
    return this.http.post<void>(`${this.base}/send`, request);
  }
}
