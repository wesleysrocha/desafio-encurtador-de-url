import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ShortUrlService } from '../../core/short-url.service';
import { errorMessage } from '../../core/error-message';
import { copyToClipboard } from '../../core/clipboard';
import { ShortUrl, isExpired } from '../../models/short-url';

@Component({
  selector: 'app-url-list',
  imports: [DatePipe],
  templateUrl: './url-list.html',
  styleUrl: './url-list.css',
})
export class UrlList implements OnInit {
  private readonly service = inject(ShortUrlService);

  readonly pageSize = 10;
  readonly page = signal(1);
  readonly items = signal<ShortUrl[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly copiedId = signal<string | null>(null);
  readonly deletingId = signal<string | null>(null);

  /** A API não retorna o total; se veio uma página cheia, provavelmente há próxima. */
  readonly hasNext = computed(() => this.items().length === this.pageSize);
  readonly totalClicks = computed(() => this.items().reduce((sum, i) => sum + i.clickCount, 0));

  readonly isExpired = isExpired;

  ngOnInit(): void {
    this.load();
  }

  /** Recarrega a página atual (chamado pelo componente pai após criar/abrir um link). */
  reload(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);

    this.service.list(this.page(), this.pageSize).subscribe({
      next: (items) => {
        if (items.length === 0 && this.page() > 1) {
          this.page.update((p) => p - 1);
          this.load();
          return;
        }
        this.items.set(items);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(errorMessage(err));
        this.loading.set(false);
      },
    });
  }

  previous(): void {
    if (this.page() > 1) {
      this.page.update((p) => p - 1);
      this.load();
    }
  }

  next(): void {
    if (this.hasNext()) {
      this.page.update((p) => p + 1);
      this.load();
    }
  }

  async copy(item: ShortUrl): Promise<void> {
    await copyToClipboard(item.shortUrl);
    this.copiedId.set(item.id);
    setTimeout(() => {
      if (this.copiedId() === item.id) this.copiedId.set(null);
    }, 2000);
  }

  /** Abre o link curto (a API redireciona e conta o clique) e atualiza a lista logo depois. */
  open(item: ShortUrl): void {
    window.open(item.shortUrl, '_blank', 'noopener');
    setTimeout(() => this.load(), 1000);
  }

  remove(item: ShortUrl): void {
    if (!confirm(`Excluir o link ${item.shortUrl}?`)) return;

    this.deletingId.set(item.id);
    this.service.delete(item.id).subscribe({
      next: () => {
        this.deletingId.set(null);
        this.load();
      },
      error: (err) => {
        this.deletingId.set(null);
        this.error.set(errorMessage(err));
      },
    });
  }
}
