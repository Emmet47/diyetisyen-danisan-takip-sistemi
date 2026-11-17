import { useEffect, useState } from 'react'
import type { Session } from './api/models'
import { notificationsApi } from './api/notifications'
import { clearSession, readSession } from './api/session'
import { subscriptionApi } from './api/subscription'
import { initials } from './format'
import Auth from './pages/Auth'
import Overview from './pages/Overview'
import Clients from './pages/Clients'
import { AdminPage, SubscriptionPage } from './pages/Accounts'
import Catalog from './pages/Catalog'
import Plans from './pages/Plans'
import Tracking from './pages/Tracking'
import Appointments from './pages/Appointments'
import Notifications from './pages/Notifications'
import { Brand, useLoad } from './ui'

type NavItem = { id: string; label: string; symbol: string }
const dietitian: NavItem[] = [
  { id: 'overview', label: 'Genel bakış', symbol: '▦' },
  { id: 'clients', label: 'Danışanlar', symbol: '♙' },
  { id: 'tracking', label: 'Ölçümler ve gelişim', symbol: '↗' },
  { id: 'plans', label: 'Beslenme planları', symbol: '▤' },
  { id: 'templates', label: 'Plan şablonları', symbol: '▧' },
  { id: 'catalog', label: 'Besin kataloğu', symbol: '♧' },
  { id: 'journal', label: 'Öğün günlüğü', symbol: '◫' },
  { id: 'energy', label: 'Enerji hesabı', symbol: 'ϟ' },
  { id: 'appointments', label: 'Randevular', symbol: '⊞' },
  { id: 'notifications', label: 'Bildirimler', symbol: '◉' },
  { id: 'subscription', label: 'Aboneliğim', symbol: '◇' },
]
const administrator: NavItem[] = [
  { id: 'overview', label: 'Yönetim özeti', symbol: '▦' },
  { id: 'dietitians', label: 'Diyetisyenler', symbol: '♙' },
  { id: 'packages', label: 'Abonelik paketleri', symbol: '▤' },
  { id: 'usage', label: 'Paket kullanımı', symbol: '↗' },
  { id: 'audit', label: 'Denetim kayıtları', symbol: '◫' },
]

export default function App() {
  const [session, setSession] = useState(() => location.pathname === '/reset-password' ? null : readSession())
  const [expired, setExpired] = useState(false)
  const [page, setPage] = useState(() => location.hash.slice(1) || 'overview')
  const [menu, setMenu] = useState(false)
  const nav = session?.role === 'Admin' ? administrator : dietitian
  const selected = nav.find(item => item.id === page) ?? nav[0]
  function navigate(next: string) {
    location.hash = next
    setPage(next)
    setMenu(false)
    window.scrollTo(0, 0)
  }
  function logout(reason = false) {
    clearSession()
    setSession(null)
    setExpired(reason)
    navigate('overview')
  }
  function login(value: Session) {
    setExpired(false)
    setSession(value)
    navigate('overview')
  }
  useEffect(() => {
    const changed = () => { setPage(location.hash.slice(1) || 'overview'); setMenu(false) }
    window.addEventListener('hashchange', changed)
    return () => window.removeEventListener('hashchange', changed)
  }, [])
  // JWT 60 dakika geçerli ve refresh token yok: süre dolunca ya da API 401 dönünce girişe dönülür.
  useEffect(() => {
    if (!session) return
    const timeout = window.setTimeout(() => logout(true), Math.max(0, Date.parse(session.expiresAt) - Date.now()))
    const unauthorized = () => logout(true)
    window.addEventListener('ddts:unauthorized', unauthorized)
    return () => { clearTimeout(timeout); window.removeEventListener('ddts:unauthorized', unauthorized) }
  }, [session])

  const isDietitian = session?.role === 'Dietitian'
  // Okunmamış bildirim sayısı ve paket kullanımı: sayfa değiştikçe yenilenir.
  const notices = useLoad(() => isDietitian ? notificationsApi.list(1, 20) : Promise.resolve(null), 'nav-notices:' + session?.email + ':' + page)
  const sub = useLoad(() => isDietitian ? subscriptionApi.get() : Promise.resolve(null), 'nav-sub:' + session?.email + ':' + page)

  if (!session) return <Auth onLogin={login} expired={expired} />
  const props = { session }
  const roleName = isDietitian ? 'Diyetisyen' : 'Yönetici'
  const account = session.email
  const unread = notices.data?.items.filter(item => !item.isRead).length ?? 0
  const usage = sub.data
  let content
  switch (selected.id) {
    case 'clients': content = <Clients {...props} />; break
    case 'subscription': content = <SubscriptionPage {...props} />; break
    case 'dietitians': case 'packages': case 'usage': case 'audit': content = <AdminPage key={selected.id} {...props} section={selected.id} />; break
    case 'catalog': content = <Catalog {...props} />; break
    case 'plans': case 'templates': content = <Plans key={selected.id} {...props} templates={selected.id === 'templates'} />; break
    case 'tracking': case 'energy': case 'journal': content = <Tracking key={selected.id} {...props} mode={selected.id} />; break
    case 'appointments': content = <Appointments {...props} />; break
    case 'notifications': content = <Notifications {...props} changed={notices.reload} />; break
    default: content = <Overview {...props} navigate={navigate} />
  }
  return <div className="shell">
    {menu && <button className="nav-overlay" aria-label="Menüyü kapat" onClick={() => setMenu(false)} />}
    <aside className={'sidebar' + (menu ? ' is-open' : '')}>
      <a className="home-link" href="#overview" onClick={() => navigate('overview')}><Brand /></a>
      <p className="nav-label">{roleName} paneli</p>
      <nav aria-label="Panel menüsü">{nav.map(item => <a key={item.id} href={'#' + item.id} aria-current={selected.id === item.id ? 'page' : undefined} onClick={() => navigate(item.id)}><span className="nav-icon" aria-hidden="true">{item.symbol}</span>{item.label}{item.id === 'notifications' && unread > 0 && <span className="nav-count">{unread}</span>}</a>)}</nav>
      <div className="sidebar-footer">{usage && <div className="sidebar-plan"><span>{usage.planName} paketi</span><progress aria-label="Aktif danışan kullanımı" value={usage.usage.activeClients} max={usage.usage.maxClients || 1} /><small>{usage.usage.activeClients} / {usage.usage.maxClients} aktif danışan</small></div>}<button className="logout" onClick={() => logout()}><span aria-hidden="true">↪</span> Çıkış yap</button></div>
    </aside>
    <div className="workspace"><header className="topbar"><div className="top-context"><button className="button secondary menu-button" aria-label="Menüyü aç" aria-expanded={menu} onClick={() => setMenu(!menu)}>☰</button><span>{roleName} paneli</span><span aria-hidden="true">/</span><strong>{selected.label}</strong></div><div className="top-actions">{isDietitian && <button className="icon-button notification-button" aria-label={unread ? 'Bildirimler, ' + unread + ' okunmamış' : 'Bildirimler'} onClick={() => navigate('notifications')}><span aria-hidden="true">◉</span>{unread > 0 && <i />}</button>}<div className="account"><span className="avatar">{initials(account) || account[0]?.toUpperCase()}</span><div><strong>{account}</strong><small>{roleName}</small></div></div></div></header>
      <main className="workspace-main" id="content">{content}</main></div>
  </div>
}
