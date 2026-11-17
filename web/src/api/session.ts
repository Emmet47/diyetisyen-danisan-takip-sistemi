import { getToken, setToken } from './http'
import type { LoginResult, Session } from './models'

const key = 'ddts.web.session'
export function readSession(): Session | null {
  try {
    const saved = JSON.parse(sessionStorage.getItem(key) ?? 'null') as Session | null
    if (saved?.role === 'Admin' || saved?.role === 'Dietitian') {
      if (saved.demo || (getToken() && saved.expiresAt && Date.parse(saved.expiresAt) > Date.now())) return saved
    }
  } catch {
    // Ignore a corrupt browser session.
  }
  clearSession()
  return null
}

export function clearSession() {
  setToken(null)
  sessionStorage.removeItem(key)
}

export function startSession(email: string, result?: LoginResult, demoRole: Session['role'] = 'Dietitian'): Session {
  if (result?.role === 'Client') throw new Error('Danışan hesabınla mobil uygulamadan giriş yapabilirsin.')
  if (result && (!result.accessToken || !(Date.parse(result.expiresAt) > Date.now()) || !['Dietitian', 'Admin'].includes(result.role))) {
    throw new Error('Geçerli bir oturum alınamadı.')
  }
  const session: Session = { role: result?.role ?? demoRole, email, demo: !result, expiresAt: result?.expiresAt }
  setToken(result?.accessToken ?? null)
  sessionStorage.setItem(key, JSON.stringify(session))
  return session
}
