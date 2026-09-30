import { Component, inject, output, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ShortUrlService } from '../../core/short-url.service';
import { errorMessage } from '../../core/error-message';
import { copyToClipboard } from '../../core/clipboard';
import { CreateShortUrlRequest, ShortUrl } from '../../models/short-url';

interface ExpirationOption {
  label: string;
  minutes: number;
}

@Component({
  selector: 'app-shorten-form',
  imports: [ReactiveFormsModule],
  templateUrl: './shorten-form.html',
  styleUrl: './shorten-form.css',
})
export class ShortenForm {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly service = inject(ShortUrlService);

  /** Emitido quando uma URL é criada ou acessada (a lista usa para recarregar). */
  readonly changed = output<void>();

  readonly expirationOptions: ExpirationOption[] = [
    { label: '5 minutos (padrão)', minutes: 5 },
    { label: '1 hora', minutes: 60 },
    { label: '1 dia', minutes: 60 * 24 },
    { label: '7 dias', minutes: 60 * 24 * 7 },
    { label: '30 dias', minutes: 60 * 24 * 30 },
  ];

  readonly form = this.fb.group({
    originalUrl: ['', [Validators.required, Validators.pattern(/^https?:\/\/\S+$/i)]],
    customAlias: ['', [Validators.pattern(/^[A-Za-z0-9_-]{3,12}$/)]],
    expirationMinutes: [5],
  });

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly result = signal<ShortUrl | null>(null);
  readonly copied = signal(false);

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { originalUrl, customAlias, expirationMinutes } = this.form.getRawValue();
    const request: CreateShortUrlRequest = { originalUrl: originalUrl.trim() };

    if (customAlias.trim()) {
      request.customAlias = customAlias.trim();
    }

    const minutes = Number(expirationMinutes);
    if (minutes !== 5) {
      request.expirationDate = new Date(Date.now() + minutes * 60_000).toISOString();
    }

    this.loading.set(true);
    this.error.set(null);
    this.copied.set(false);

    this.service.create(request).subscribe({
      next: (created) => {
        this.result.set(created);
        this.form.reset({ originalUrl: '', customAlias: '', expirationMinutes: 5 });
        this.loading.set(false);
        this.changed.emit();
      },
      error: (err) => {
        this.error.set(errorMessage(err));
        this.loading.set(false);
      },
    });
  }

  async copyResult(): Promise<void> {
    const current = this.result();
    if (!current) return;
    await copyToClipboard(current.shortUrl);
    this.copied.set(true);
    setTimeout(() => this.copied.set(false), 2000);
  }

  open(item: ShortUrl): void {
    this.error.set(null);
    this.service.openInNewTab(item.id).subscribe({
      next: () => this.changed.emit(),
      error: (err) => this.error.set(errorMessage(err)),
    });
  }

  showError(control: 'originalUrl' | 'customAlias'): boolean {
    const c = this.form.controls[control];
    return c.invalid && (c.touched || c.dirty);
  }
}
