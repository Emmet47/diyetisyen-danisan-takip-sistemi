import { useState } from 'react'
import { api } from '../api/http'
import type { LoginResult, Package, Session } from '../api/models'
import { startSession } from '../api/session'
import { Brand, Field, Message, useAction, useLoad, values } from '../ui'

type Mode = 'login' | 'register' | 'forgot' | 'reset'
const titles: Record<Mode, string> = { login: 'Panele giriş', register: 'Diyetisyen hesabı aç', forgot: 'Parolamı unuttum', reset: 'Yeni parola belirle' }
const resetToken = () => location.pathname === '/reset-password' ? new URLSearchParams(location.search).get('token') : null

export default function Auth({ onLogin, expired }: { onLogin: (session: Session) => void; expired: boolean }) {
  const [token, setResetToken] = useState(resetToken)
  const [mode, setMode] = useState<Mode>(() => location.pathname === '/reset-password' ? 'reset' : 'login')
  const [feedback, setFeedback] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const action = useAction()
  const packages = useLoad(() => mode === 'register' ? api.get<Package[]>('/subscription-plans') : Promise.resolve([]), mode)
  function switchTo(next: Mode) {
    if (mode === 'reset') { history.replaceState(null, '', '/'); setResetToken(null) }
    setMode(next)
    setFeedback('')
    setShowPassword(false)
    action.clearError()
  }
  return <main className="auth-layout">
    <section className="auth-brand">
      <Brand />
      <div className="auth-art" aria-hidden="true"><span /><span /><span /><span /></div>
      <div className="auth-head"><h2>Danışan takibi<br />tek panelde.</h2><p>Beslenme planları, ölçümler ve randevular.</p></div>
    </section>
    <section className="auth-content">
      <div className="auth-card">
        <h1>{titles[mode]}</h1>
        <p className="muted">{mode === 'login' ? 'Diyetisyen veya yönetici hesabınla devam et.' : mode === 'reset' ? 'Davet veya sıfırlama bağlantınla parolanı oluştur.' : mode === 'forgot' ? 'Parola sıfırlama bağlantısı için e-posta adresini gir.' : 'Bilgilerini gir ve abonelik paketini seç.'}</p>
        <Message type="info">{expired && mode === 'login' ? 'Oturum süren doldu. Tekrar giriş yap.' : ''}</Message>
        <Message type="success">{feedback}</Message>
        <Message>{action.error}</Message>
        <Message>{mode === 'reset' && !token && 'Bu bağlantıda parola sıfırlama anahtarı bulunmuyor. Yeni bağlantı iste.'}</Message>
        <form className="form" key={mode} onSubmit={event => {
          const data = values(event)
          void action.run(async () => {
            if (mode === 'forgot') {
              await api.post('/auth/forgot-password', { email: data.email.trim() })
              setFeedback('Bu adres kayıtlıysa sıfırlama bağlantısı gönderildi.')
              return
            }
            if (mode === 'reset') {
              if (!token) throw new Error('Bağlantı geçersiz. Yeni bağlantı iste.')
              if (data.password !== data.confirm) throw new Error('Parolalar birbiriyle eşleşmiyor.')
              await api.post('/auth/reset-password', { token, newPassword: data.password })
              switchTo('login')
              setFeedback('Parolan güncellendi. Yeni parolanla giriş yapabilirsin.')
              return
            }
            const email = data.email.trim()
            const result = await api.post<LoginResult>('/auth/' + mode, { ...data, email })
            onLogin(startSession(email, result))
          })
        }}>
          {mode === 'register' && <><Field label="Ad soyad"><input name="fullName" autoComplete="name" maxLength={100} required /></Field><Field label="Telefon"><input name="phone" type="tel" autoComplete="tel" maxLength={20} /></Field></>}
          {mode !== 'reset' && <Field label="E-posta"><input name="email" placeholder="ornek@eposta.com" type="email" autoComplete="email" maxLength={254} required /></Field>}
          {mode !== 'forgot' && <Field label={mode === 'reset' ? 'Yeni parola' : 'Parola'}><span className="password"><input name="password" placeholder="••••••••" type={showPassword ? 'text' : 'password'} autoComplete={mode === 'login' ? 'current-password' : 'new-password'} minLength={mode === 'login' ? undefined : 8} maxLength={256} required /><button className="text-button" type="button" onClick={() => setShowPassword(!showPassword)}>{showPassword ? 'Gizle' : 'Göster'}</button></span></Field>}
          {mode === 'reset' && <Field label="Yeni parola tekrar"><input name="confirm" type="password" minLength={8} required /></Field>}
          {mode === 'register' && <Field label="Abonelik paketi"><select required name="subscriptionPlanId" defaultValue=""><option value="">{packages.data ? 'Paket seç' : 'Paketler yükleniyor…'}</option>{packages.data?.map(plan => <option key={plan.id} value={plan.id}>{plan.name} · {plan.maxClients} danışan · {plan.monthlyPrice} TL/ay</option>)}</select></Field>}
          {mode === 'register' && packages.error && <><Message>{packages.error}</Message><button className="text-button" type="button" onClick={packages.reload}>Paketleri tekrar yükle</button></>}
          {mode === 'login' && <button className="text-button auth-forgot" type="button" onClick={() => switchTo('forgot')} disabled={action.busy}>Parolamı unuttum</button>}
          <button className="button primary" disabled={action.busy || (mode === 'reset' && !token) || (mode === 'register' && !packages.data?.length)}>{action.busy ? 'İşleniyor…' : mode === 'login' ? 'Giriş yap' : mode === 'register' ? 'Hesap oluştur' : mode === 'reset' ? 'Parolayı kaydet' : 'Bağlantı gönder'}</button>
        </form>
        {mode === 'login' ? <><p className="auth-register">Yeni diyetisyen hesabı mı? <button className="text-button" onClick={() => switchTo('register')} disabled={action.busy}>Hesap oluştur</button></p><div className="demo-entry"><span>Örnek verilerle keşfet</span><button className="button secondary" disabled={action.busy} onClick={() => onLogin(startSession('ayse@ornek.test'))}>Diyetisyen demosu</button><button className="text-button" disabled={action.busy} onClick={() => onLogin(startSession('admin@ornek.test', undefined, 'Admin'))}>Yönetici demosu</button></div></> : <button className="text-button back-link" onClick={() => switchTo('login')} disabled={action.busy}>← Giriş ekranına dön</button>}
      </div>
    </section>
  </main>
}
