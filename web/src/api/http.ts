// API çağrıları için tek ortak istemci. Bütün modül dosyaları (clients.ts, plans.ts ...) bunu kullanır.
// Tüm adresler /api ile başlar; geliştirmede Vite proxy'si, Docker'da nginx bunları .NET API'ye iletir.
// Yerel çalıştırma rehberi: web/README.md

/** API'den gelen hata. ProblemDetails (RFC 9457) gövdesindeki bilgileri taşır. */
export class ApiError extends Error {
  /** HTTP durum kodu: 400, 401, 403, 404, 409, 429, 503 ... */
  readonly status: number
  /** 400'de alan bazlı hatalar: { "genel": ["...", "..."] } */
  readonly errors?: Record<string, string[]>

  constructor(status: number, message: string, errors?: Record<string, string[]>) {
    super(message)
    this.status = status
    this.errors = errors
  }
}

/** Sunucu "detail" göndermezse gösterilecek Türkçe mesajlar. */
const statusMessages: Record<number, string> = {
  400: 'Girdiğin bilgileri kontrol et.',
  401: 'Oturumun sona erdi. Tekrar giriş yap.',
  403: 'Bu işlem için yetkin yok.',
  404: 'Kayıt bulunamadı.',
  409: 'Bu işlem kaydın şu anki durumuyla çakışıyor. Sayfayı yenileyip tekrar dene.',
  429: 'Çok fazla deneme yaptın. Biraz sonra tekrar dene.',
  503: 'Dış kaynağa şu an ulaşılamıyor. Biraz sonra tekrar dene.',
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

/** Adres sonuna ?a=1&b=2 ekler; boş değerleri atlar. */
export function query(path: string, params: Record<string, string | number | boolean | null | undefined>): string {
  const search = new URLSearchParams()
  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== null && value !== '') search.set(key, String(value))
  }
  const text = search.toString()
  return text ? `${path}?${text}` : path
}

// ---- İstek ----
async function send(method: string, path: string, body?: unknown, accept = 'application/json'): Promise<Response> {
  const headers: Record<string, string> = { Accept: accept }
  if (body !== undefined) {
    headers['Content-Type'] = 'application/json'
  }

  const token = getToken()
  if (token) {
    headers.Authorization = `Bearer ${token}`
  }

  let response: Response
  try {
    response = await fetch(`/api${path}`, {
      method,
      headers,
      body: body === undefined ? undefined : JSON.stringify(body),
    })
  } catch {
    throw new ApiError(503, 'API’ye ulaşılamadı. .NET sunucusunun çalıştığını kontrol et.')
  }

  if (!response.ok) {
    // 401: token yok ya da süresi doldu → App oturumu kapatıp giriş ekranına döner.
    if (response.status === 401 && token && token === getToken() && !path.startsWith('/auth/')) {
      window.dispatchEvent(new Event('ddts:unauthorized'))
    }
    // Hatalar ProblemDetails biçiminde döner: { title, status, detail, errors }
    const problem = await response.json().catch(() => null)
    // 409'da başlık da anlamlı: "Bu saat az önce doldu. Lütfen başka bir saat seç." / "Paket limiti dolu. ..."
    const detail = response.status === 409 && problem?.detail && problem?.title && problem.title !== 'Çakışma'
      ? `${problem.title}. ${problem.detail}`
      : problem?.detail
    throw new ApiError(
      response.status,
      detail ?? statusMessages[response.status] ?? problem?.title ?? (response.status >= 500
        ? 'API yanıt vermiyor. .NET sunucusunun çalıştığını kontrol et.'
        : response.statusText),
      problem?.errors,
    )
  }
  return response
}

async function request<T>(method: string, path: string, body?: unknown): Promise<T> {
  const response = await send(method, path, body)

  // 204 No Content / 202 Accepted: gövde yok (ör. silme, parola sıfırlama)
  if (response.status === 204 || response.headers.get('Content-Length') === '0' || response.status === 202 && !response.headers.get('Content-Type')) {
    return undefined as T
  }

  if (!response.headers.get('Content-Type')?.includes('json')) {
    throw new ApiError(503, 'API yanıt vermiyor. .NET sunucusunun çalıştığını kontrol et.')
  }
  return (await response.json()) as T
}

/**
 * Örnek:
 *   const paketler = await api.get<Package[]>('/subscription-plans')
 *   const sonuc    = await api.post<{ id: string }>('/clients', { fullName: 'Deneme Danışan', ... })
 */
export const api = {
  get: <T>(path: string) => request<T>('GET', path),
  post: <T = void>(path: string, body?: unknown) => request<T>('POST', path, body),
  put: <T = void>(path: string, body?: unknown) => request<T>('PUT', path, body),
  delete: <T = void>(path: string) => request<T>('DELETE', path),
  /**
   * Dosya indirir (ör. PDF rapor). Token gerektiği için düz bağlantı kullanılamaz:
   * dosya token'lı istekle alınır, sonra tarayıcıya "indir" dedirtilir.
   */
  download: async (path: string, fallbackName: string) => {
    const response = await send('GET', path, undefined, '*/*')
    const blob = await response.blob()
    const disposition = response.headers.get('Content-Disposition') ?? ''
    const name = /filename\*?=(?:UTF-8'')?"?([^";]+)"?/i.exec(disposition)?.[1]
    const url = URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.href = url
    link.download = name ? decodeURIComponent(name) : fallbackName
    document.body.append(link)
    link.click()
    link.remove()
    setTimeout(() => URL.revokeObjectURL(url), 1000)
  },
}

/** /api/health düz metin ("Healthy") döndürür. */
export async function getHealth(): Promise<string> {
  const response = await fetch('/api/health')
  return response.ok ? await response.text() : `Hata (${response.status})`
}
