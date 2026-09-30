/**
 * Prefixo usado para chamar a API .NET.
 * - Em desenvolvimento (npm start), o proxy.conf.json encaminha /api -> http://localhost:8080
 * - No Docker, o nginx.conf encaminha /api -> http://urlshortener-api:8080
 * Assim o navegador sempre chama a mesma origem e não há problema de CORS.
 */
export const API_BASE_URL = '/api';

/** Valor do header X-API-Key exigido pelo POST /v1/urls (Shortener:ApiKey no backend). */
export const API_KEY = 'itau';
