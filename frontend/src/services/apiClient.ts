import type { ApiProblemDetails } from '../types/auth';

export class ApiError extends Error {
  readonly status: number;
  readonly problem?: ApiProblemDetails;

  constructor(message: string, status: number, problem?: ApiProblemDetails) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.problem = problem;
  }
}

export async function apiFetch<T>(
  input: RequestInfo | URL,
  init: RequestInit = {},
): Promise<T> {
  const headers = new Headers(init.headers);
  if (init.body && !(init.body instanceof FormData) && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json');
  }

  const response = await fetch(input, {
    ...init,
    headers,
  });

  if (response.status === 204) {
    return undefined as T;
  }

  const contentType = response.headers.get('content-type') ?? '';
  const isJson = /json/i.test(contentType);
  const payload = isJson ? await response.json() : undefined;

  if (!response.ok) {
    const problem = payload as ApiProblemDetails | undefined;
    throw new ApiError(
      problem?.detail ?? problem?.title ?? `Request failed with status ${response.status}`,
      response.status,
      problem,
    );
  }

  return payload as T;
}
