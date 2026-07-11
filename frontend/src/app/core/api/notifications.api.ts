import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './api-base-url';
import { Notification, NotificationPreferences, Paged, UnreadCount, UpdateChannelPreferenceRequest } from './models';

@Injectable({ providedIn: 'root' })
export class NotificationsApi {
  private readonly http = inject(HttpClient);
  private readonly base = inject(API_BASE_URL) + '/api/v1/notifications';

  list(unreadOnly = false, page = 1, pageSize = 20): Observable<Paged<Notification>> {
    const params = new HttpParams().set('unreadOnly', unreadOnly).set('page', page).set('pageSize', pageSize);
    return this.http.get<Paged<Notification>>(`${this.base}/`, { params });
  }
  unreadCount(): Observable<UnreadCount> { return this.http.get<UnreadCount>(`${this.base}/unread-count`); }
  markRead(id: string): Observable<void> { return this.http.post<void>(`${this.base}/${id}/read`, {}); }
  markAllRead(): Observable<{ updated: number }> { return this.http.post<{ updated: number }>(`${this.base}/read-all`, {}); }
  preferences(): Observable<NotificationPreferences> { return this.http.get<NotificationPreferences>(`${this.base}/preferences`); }
  updatePreference(request: UpdateChannelPreferenceRequest): Observable<NotificationPreferences> {
    return this.http.put<NotificationPreferences>(`${this.base}/preferences`, request);
  }
}
