export interface ShortUrl {
  id: string;
  customAlias: string;
  shortUrl: string;
  originalUrl: string;
  createdAt: string;
  expirationDate: string | null;
  clickCount: number;
}

export interface CreateShortUrlRequest {
  originalUrl: string;
  customAlias?: string;
  expirationDate?: string;
}

export function isExpired(item: ShortUrl): boolean {
  return !!item.expirationDate && new Date(item.expirationDate).getTime() <= Date.now();
}
