import { getToken, setToken } from './http'
import type { LoginResult, Session } from './models'

// Oturum bilgisi (rol, e-posta, bitiş zamanı) sekme kapanana kadar sessionStorage'da durur.
// JWT 60 dakika geçerli, refresh token yok: süre dolunca ya da 401 gelince giriş ekranına dönülür.
const key = 'ddts.web.session'

export function readSession(): Session | null {
  try {
    const saved = JSON.parse(sessionStorage.getItem(key) ?? 'null') as Session | null
    if ((saved?.role === 'Admin' || saved?.role === 'Dietitian') && getToken() && Date.parse(saved.expiresAt) > Date.now()) {
      return saved
    }
  } catch {
    // Bozuk oturum kaydı: yok sayıp girişe dön.
  }
  clearSession()
  return null
}

export function clearSession() {
  setToken(null)
  sessionStorage.removeItem(key)
}

/** Giriş yanıtından oturum açar. Danışan rolü web paneline alınmaz (rol bazlı yönlendirme). */
export function startSession(email: string, result: LoginResult): Session {
  if (result.role === 'Client') throw new Error('Danışan hesabınla mobil uygulamadan giriş yapabilirsin.')
  if (!result.accessToken || !(Date.parse(result.expiresAt) > Date.now())) {
    throw new Error('Geçerli bir oturum alınamadı.')
  }
  const session: Session = { role: result.role, email, expiresAt: result.expiresAt }
  setToken(result.accessToken)
  sessionStorage.setItem(key, JSON.stringify(session))
  return session
}
