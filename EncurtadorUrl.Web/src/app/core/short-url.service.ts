import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL, API_KEY } from './api.config';
import { CreateShortUrlRequest, ShortUrl } from '../models/short-url';

@Injectable({ providedIn: 'root' })
export class ShortUrlService {
  private readonly http = inject(HttpClient);

  /** POST /v1/urls (exige X-API-Key) */
  create(request: CreateShortUrlRequest): Observable<ShortUrl> {
    return this.http.post<ShortUrl>(`${API_BASE_URL}/v1/urls`, request, {
      headers: { 'X-API-Key': API_KEY },
    });
  }

  /** GET /v1/urls?page=&pageSize= */
  list(page: number, pageSize: number): Observable<ShortUrl[]> {
    return this.http.get<ShortUrl[]>(`${API_BASE_URL}/v1/urls`, {
      params: { page, pageSize },
    });
  }

  /** DELETE /v1/urls/{id} */
  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${API_BASE_URL}/v1/urls/${encodeURIComponent(id)}`);
  }
}
