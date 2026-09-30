import { HttpErrorResponse } from '@angular/common/http';

interface ProblemDetails {
  title?: string;
  detail?: string;
}

/** Extrai uma mensagem amigável de um erro HTTP (ProblemDetails do backend). */
export function errorMessage(err: unknown): string {
  if (!(err instanceof HttpErrorResponse)) {
    return 'Erro inesperado.';
  }

  if (err.status === 0) {
    return 'Não foi possível conectar à API. Verifique se o backend está rodando na porta 8080.';
  }

  const problem = parseProblem(err.error);
  return problem?.detail || problem?.title || `Erro ${err.status}: ${err.statusText}`;
}

function parseProblem(body: unknown): ProblemDetails | null {
  if (typeof body === 'string') {
    try {
      const parsed: unknown = JSON.parse(body);
      return parsed && typeof parsed === 'object' ? (parsed as ProblemDetails) : { detail: body };
    } catch {
      return body ? { detail: body } : null;
    }
  }
  return body && typeof body === 'object' ? (body as ProblemDetails) : null;
}
