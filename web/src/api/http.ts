// API çağrıları için ortak yardımcı. Tüm adresler /api ile başlar;
// geliştirmede Vite proxy'si, Docker'da nginx bunları .NET API'ye iletir.
// Kullanım rehberi: web/API_REHBERI.md

/** API'den gelen hata. ProblemDetails (RFC 9457) gövdesindeki bilgileri taşır. */
export class ApiError extends Error {
  /** HTTP durum kodu: 400, 401, 403, 404, 409, 429 ... */
  readonly status: number
  /** 400'de alan bazlı hatalar: { "Email": ["Geçerli bir e-posta girin."] } */
  readonly errors?: Record<string, string[]>

  constructor(status: number, message: string, errors?: Record<string, string[]>) {
    super(message)
    this.status = status
    this.errors = errors
  }
}

// ---- Oturum (JWT) ----
// Girişte alınan token sekme kapanana kadar sessionStorage'da tutulur
// ve her isteğe "Authorization: Bearer <token>" başlığı olarak eklenir.
const TOKEN_KEY = 'ddts_token'

export function setToken(token: string | null): void {
  if (token) {
    sessionStorage.setItem(TOKEN_KEY, token)
  } else {
    sessionStorage.removeItem(TOKEN_KEY)
  }
}

export function getToken(): string | null {
  return sessionStorage.getItem(TOKEN_KEY)
}

// ---- İstek ----
async function request<T>(method: string, path: string, body?: unknown): Promise<T> {
  const headers: Record<string, string> = { Accept: 'application/json' }
  if (body !== undefined) {
    headers['Content-Type'] = 'application/json'
  }

  const token = getToken()
  if (token) {
    headers.Authorization = `Bearer ${token}`
  }

  const response = await fetch(`/api${path}`, {
    method,
    headers,
    body: body === undefined ? undefined : JSON.stringify(body),
  })

  if (!response.ok) {
    // Hatalar ProblemDetails biçiminde döner: { title, status, detail, errors }
    const problem = await response.json().catch(() => null)
    throw new ApiError(
      response.status,
      problem?.detail ?? problem?.title ?? response.statusText,
      problem?.errors,
    )
  }

  // 204 No Content: gövde yok (ör. silme)
  if (response.status === 204) {
    return undefined as T
  }

  return (await response.json()) as T
}

/**
 * Örnek:
 *   const paketler = await api.get<SubscriptionPlan[]>('/subscription-plans')
 *   const danisan  = await api.post<Client>('/clients', { fullName: 'Ayşe Yılmaz', ... })
 */
export const api = {
  get: <T>(path: string) => request<T>('GET', path),
  post: <T>(path: string, body?: unknown) => request<T>('POST', path, body),
  put: <T>(path: string, body?: unknown) => request<T>('PUT', path, body),
  delete: <T = void>(path: string) => request<T>('DELETE', path),
}

/** /api/health düz metin ("Healthy") döndürür. */
export async function getHealth(): Promise<string> {
  const response = await fetch('/api/health')
  return response.ok ? await response.text() : `Hata (${response.status})`
}
