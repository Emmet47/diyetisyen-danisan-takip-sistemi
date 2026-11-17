import { useEffect, useState } from 'react'
import type { Session } from './api/models'
import { clearSession, readSession } from './api/session'
import { dateLabel, initials, readDemo, saveDemo, subscriptionFor, timeLabel } from './demo'
import type { DemoData } from './demo'
import Auth from './pages/Auth'
import Overview from './pages/Overview'
import Clients from './pages/Clients'
import { AdminPage, SubscriptionPage } from './pages/Accounts'
import Catalog from './pages/Catalog'
import Plans from './pages/Plans'
import Tracking from './pages/Tracking'
import Appointments from './pages/Appointments'
import { Badge, Brand, Empty, Message, Title } from './ui'

type NavItem = { id: string; label: string; symbol: string; demoOnly?: boolean }
const dietitian: NavItem[] = [
  { id: 'overview', label: 'Genel bakış', symbol: '▦' },
  { id: 'clients', label: 'Danışanlar', symbol: '♙' },
  { id: 'tracking', label: 'Ölçümler ve gelişim', symbol: '↗' },
  { id: 'plans', label: 'Beslenme planları', symbol: '▤', demoOnly: true },
  { id: 'templates', label: 'Plan şablonları', symbol: '▧', demoOnly: true },
  { id: 'catalog', label: 'Besin kataloğu', symbol: '♧', demoOnly: true },
  { id: 'journal', label: 'Öğün günlüğü', symbol: '◫', demoOnly: true },
  { id: 'energy', label: 'Enerji hesabı', symbol: 'ϟ', demoOnly: true },
  { id: 'appointments', label: 'Randevular', symbol: '⊞', demoOnly: true },
  { id: 'notifications', label: 'Bildirimler', symbol: '◉', demoOnly: true },
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
  const [demo, setDemo] = useState(readDemo)
  const [menu, setMenu] = useState(false)
  const nav = session?.role === 'Admin' ? administrator : dietitian
  const selected = nav.find(item => item.id === page) ?? nav[0]
  const unread = session?.demo && session.role === 'Dietitian' ? demo.notices.filter(item => !item.read).length : 0
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
  function update(fn: (data: DemoData) => DemoData) {
    setDemo(previous => fn(previous))
  }
  useEffect(() => { saveDemo(demo) }, [demo])
  useEffect(() => {
    const changed = () => { setPage(location.hash.slice(1) || 'overview'); setMenu(false) }
    window.addEventListener('hashchange', changed)
    return () => window.removeEventListener('hashchange', changed)
  }, [])
  useEffect(() => {
    if (!session || session.demo || !session.expiresAt) return
    const timeout = window.setTimeout(() => logout(true), Math.max(0, Date.parse(session.expiresAt) - Date.now()))
    const unauthorized = () => logout(true)
    window.addEventListener('ddts:unauthorized', unauthorized)
    return () => { clearTimeout(timeout); window.removeEventListener('ddts:unauthorized', unauthorized) }
  }, [session])

  if (!session) return <Auth onLogin={login} expired={expired} />
  const props = { session, demo, update }
  const roleName = session.role === 'Dietitian' ? 'Diyetisyen' : 'Yönetici'
  const account = session.demo ? session.role === 'Dietitian' ? 'Ayşe Örnek' : 'Demo Yönetici' : session.email
  const sub = session.role === 'Dietitian' && session.demo ? subscriptionFor(demo) : null
  let content
  if (selected.demoOnly && !session.demo) content = <><Title title={selected.label} /><Message type="info">Bu modül için sunucu uç noktası henüz hazır değil. Örnek verilerle denemek için diyetisyen demosunu açabilirsin.</Message></>
  else switch (selected.id) {
    case 'clients': content = <Clients {...props} />; break
    case 'subscription': content = <SubscriptionPage {...props} />; break
    case 'dietitians': case 'packages': case 'usage': case 'audit': content = <AdminPage key={selected.id} {...props} section={selected.id} />; break
    case 'catalog': content = <Catalog {...props} />; break
    case 'plans': case 'templates': content = <Plans key={selected.id} {...props} templates={selected.id === 'templates'} />; break
    case 'tracking': case 'energy': case 'journal': content = <Tracking key={selected.id} {...props} mode={selected.id} />; break
    case 'appointments': content = <Appointments {...props} />; break
    case 'notifications': content = <><Title title="Bildirimler" subtitle="Randevu taleplerini ve hatırlatmaları takip et." /><section className="panel padded stack">{demo.notices.map(item => <article className="record list-line" key={item.id}><div><small className="muted">{dateLabel(item.at)} · {timeLabel(item.at)}</small><h3>{item.title}</h3><p>{item.detail}</p></div>{item.read ? <Badge tone="gray">Okundu</Badge> : <button className="button secondary" onClick={() => update(data => ({ ...data, notices: data.notices.map(row => row.id === item.id ? { ...row, read: true } : row) }))}>Okundu işaretle</button>}</article>)}{!demo.notices.length && <Empty>Bildirim bulunmuyor.</Empty>}</section></>; break
    default: content = <Overview {...props} navigate={navigate} />
  }
  return <div className="shell">
    {menu && <button className="nav-overlay" aria-label="Menüyü kapat" onClick={() => setMenu(false)} />}
    <aside className={'sidebar' + (menu ? ' is-open' : '')}>
      <a className="home-link" href="#overview" onClick={() => navigate('overview')}><Brand /></a>
      <p className="nav-label">{roleName} paneli</p>
      <nav aria-label="Panel menüsü">{nav.map(item => <a key={item.id} href={'#' + item.id} aria-current={selected.id === item.id ? 'page' : undefined} onClick={() => navigate(item.id)}><span className="nav-icon" aria-hidden="true">{item.symbol}</span>{item.label}{item.id === 'notifications' && unread > 0 && <span className="nav-count">{unread}</span>}</a>)}</nav>
      <div className="sidebar-footer">{sub && <div className="sidebar-plan"><span>{sub.planName} paketi</span><progress aria-label="Aktif danışan kullanımı" value={sub.usage.activeClients} max={sub.usage.maxClients || 1} /><small>{sub.usage.activeClients} / {sub.usage.maxClients} aktif danışan</small></div>}<button className="logout" onClick={() => logout()}><span aria-hidden="true">↪</span> Çıkış yap</button></div>
    </aside>
    <div className="workspace"><header className="topbar"><div className="top-context"><button className="button secondary menu-button" aria-label="Menüyü aç" aria-expanded={menu} onClick={() => setMenu(!menu)}>☰</button><span>{roleName} paneli</span><span aria-hidden="true">/</span><strong>{selected.label}</strong></div><div className="top-actions">{session.demo && <Badge tone="gray">Demo</Badge>}{session.demo && session.role === 'Dietitian' && <button className="icon-button notification-button" aria-label={unread ? 'Bildirimler, ' + unread + ' okunmamış' : 'Bildirimler'} onClick={() => navigate('notifications')}><span aria-hidden="true">◉</span>{unread > 0 && <i />}</button>}<div className="account"><span className="avatar">{session.demo ? initials(account) : account[0]?.toUpperCase()}</span><div><strong>{account}</strong><small>{roleName}</small></div></div></div></header>
      <main className="workspace-main" id="content">{content}</main></div>
  </div>
}
