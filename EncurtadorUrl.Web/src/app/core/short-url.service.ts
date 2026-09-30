import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map, tap } from 'rxjs';
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

  /** GET /{id} - retorna a URL original e incrementa o clickCount */
  resolve(id: string): Observable<string> {
    return this.http
      .get(`${API_BASE_URL}/${encodeURIComponent(id)}`, { responseType: 'text' })
      .pipe(map((url) => url.trim().replace(/^"|"$/g, '')));
  }

  /**
   * Abre a URL original em uma nova aba passando pela API (GET /{id}),
   * para que o clique seja contabilizado e URLs expiradas sejam bloqueadas.
   * A aba é aberta antes da requisição para não ser bloqueada pelo navegador.
   */
  openInNewTab(id: string): Observable<string> {
    const win = window.open('', '_blank');
    return this.resolve(id).pipe(
      tap({
        next: (url) => {
          if (win) {
            win.opener = null;
            win.location.href = url;
          } else {
            window.open(url, '_blank', 'noopener');
          }
        },
        error: () => win?.close(),
      }),
    );
  }

  /** DELETE /v1/urls/{id} */
  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${API_BASE_URL}/v1/urls/${encodeURIComponent(id)}`);
  }
}
