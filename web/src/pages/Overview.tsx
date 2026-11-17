import { useEffect, useRef, useState } from 'react'
import { api } from '../api/http'
import type { Client, Dietitian, Package, Paged, Subscription } from '../api/models'
import { dateKey, dateLabel, initials, moveDate, subscriptionFor, timeLabel } from '../demo'
import type { DemoProps } from './Clients'
import { ClientIdentity } from './Clients'
import { Badge, Empty, Table, Title, Wait, useLoad } from '../ui'

export const appointmentLabels = { Requested: 'Talep edildi', Confirmed: 'Onaylandı', Completed: 'Tamamlandı', Cancelled: 'İptal' }
export function Trend({ data, target, unit = 'kg' }: { data: { date: string; value: number }[]; target?: number; unit?: string }) {
  const canvas = useRef<HTMLCanvasElement>(null)
  useEffect(() => {
    const element = canvas.current
    if (!element || !data.length) return
    const draw = () => {
      const width = element.clientWidth
      const height = element.clientHeight
      if (!width || !height) return
      const scale = window.devicePixelRatio || 1
      element.width = width * scale
      element.height = height * scale
      const ctx = element.getContext('2d')
      if (!ctx) return
      ctx.scale(scale, scale)
      const all = [...data.map(point => point.value), ...(target === undefined ? [] : [target])]
      const min = Math.floor(Math.min(...all) - 0.5)
      const max = Math.ceil(Math.max(...all) + 0.5)
      const left = 39, top = 14, bottom = height - 29, last = width - 22
      const y = (value: number) => bottom - (value - min) * (bottom - top) / (max - min)
      const x = (i: number) => data.length === 1 ? (last + left) / 2 : left + (last - left) * i / (data.length - 1)
      ctx.font = '11px system-ui'
      ctx.textBaseline = 'middle'
      for (let i = 0; i < 5; i++) {
        const value = min + (max - min) * i / 4
        ctx.strokeStyle = '#e8ede8'; ctx.beginPath(); ctx.moveTo(left, y(value)); ctx.lineTo(last, y(value)); ctx.stroke()
        ctx.fillStyle = '#626f66'; ctx.fillText(value.toLocaleString('tr-TR', { maximumFractionDigits: 1 }), 1, y(value))
      }
      const gradient = ctx.createLinearGradient(0, top, 0, bottom)
      gradient.addColorStop(0, '#83ac7e58'); gradient.addColorStop(1, '#eef4e900')
      ctx.beginPath(); ctx.moveTo(x(0), bottom)
      data.forEach((point, i) => ctx.lineTo(x(i), y(point.value)))
      ctx.lineTo(x(data.length - 1), bottom); ctx.closePath(); ctx.fillStyle = gradient; ctx.fill()
      if (target !== undefined) {
        ctx.strokeStyle = '#ad793f'; ctx.setLineDash([4, 4]); ctx.beginPath(); ctx.moveTo(left, y(target)); ctx.lineTo(last, y(target)); ctx.stroke(); ctx.setLineDash([])
      }
      ctx.strokeStyle = '#347753'; ctx.lineWidth = 2.5; ctx.beginPath()
      data.forEach((point, i) => i ? ctx.lineTo(x(i), y(point.value)) : ctx.moveTo(x(i), y(point.value)))
      ctx.stroke()
      data.forEach((point, i) => {
        ctx.beginPath(); ctx.arc(x(i), y(point.value), 3.4, 0, Math.PI * 2); ctx.fillStyle = '#347753'; ctx.fill()
        if (i % Math.max(1, Math.ceil(data.length / (width < 370 ? 3 : 6))) === 0 || i === data.length - 1) {
          ctx.fillStyle = '#626f66'; ctx.textAlign = i === 0 ? 'left' : i === data.length - 1 ? 'right' : 'center'
          ctx.fillText(dateLabel(point.date).split(' ').slice(0, 2).join(' '), x(i), height - 10)
        }
      })
    }
    const observer = new ResizeObserver(draw)
    observer.observe(element)
    draw()
    return () => observer.disconnect()
  }, [data, target])
  if (!data.length) return <Empty>Seçili dönemde ölçüm yok.</Empty>
  return <canvas className="trend" ref={canvas} role="img" aria-label={data.map(point => dateLabel(point.date) + ': ' + point.value + ' ' + unit).join('; ') + (target === undefined ? '' : '; Hedef ' + target + ' ' + unit)} />
}
function Metric({ label, value, foot, symbol, tone }: { label: string; value: string | number; foot: string; symbol: string; tone: string }) {
  return <article className="panel metric"><div><p>{label}</p><strong>{value}</strong><small>{foot}</small></div><span className={'metric-icon ' + tone} aria-hidden="true">{symbol}</span></article>
}
export default function Overview({ session, demo, navigate }: DemoProps & { navigate: (page: string) => void }) {
  const clients = useLoad(() => session.demo || session.role === 'Admin' ? Promise.resolve([]) : api.get<Client[]>('/clients'), 'overview-clients')
  const sub = useLoad(() => session.demo || session.role === 'Admin' ? Promise.resolve(null) : api.get<Subscription>('/subscription'), 'overview-sub')
  const accounts = useLoad(() => session.demo || session.role !== 'Admin' ? Promise.resolve(null) : api.get<Paged<Dietitian>>('/admin/dietitians?page=1&pageSize=20'), 'overview-admin')
  const packages = useLoad(() => session.demo || session.role !== 'Admin' ? Promise.resolve([]) : api.get<Package[]>('/admin/subscription-plans'), 'overview-packages')
  const today = dateKey()
  const data = session.demo ? demo.clients : clients.data
  const membership = session.demo ? subscriptionFor(demo) : sub.data
  const appointments = demo.appointments.filter(item => dateKey(new Date(item.startsAt)) === today && item.status !== 'Cancelled').sort((a, b) => a.startsAt.localeCompare(b.startsAt))
  const [period, setPeriod] = useState(42)
  const [trendClient, setTrendClient] = useState('')
  const selectedClient = demo.clients.find(row => row.id === trendClient) ?? demo.clients[0]
  const trend = demo.measurements.filter(item => item.clientId === selectedClient?.id && item.date >= moveDate(today, -period)).sort((a, b) => a.date.localeCompare(b.date))
  const admin = session.role === 'Admin'
  const title = admin ? 'Yönetim özeti' : 'Genel bakış'
  const fullDate = new Intl.DateTimeFormat('tr-TR', { dateStyle: 'full', timeZone: 'Europe/Istanbul' }).format(new Date())
  const accountCount = session.demo ? demo.dietitians.length : accounts.data?.totalCount
  const packageCount = (session.demo ? demo.packages : packages.data)?.filter(item => item.isActive).length
  return <>
    <Title title={title} subtitle={admin ? 'Diyetisyen hesaplarını ve abonelik paketlerini takip et.' : 'Danışanlarının durumunu ve günün programını takip et.'} action={<span className="date-pill">{fullDate}</span>} />
    {admin ? <><div className="metrics admin-metrics"><Metric label="Diyetisyen hesabı" value={accountCount ?? '—'} foot="Tüm hesaplar" symbol="♙" tone="green" /><Metric label="Satıştaki paketler" value={packageCount ?? '—'} foot="Abonelik planları" symbol="▤" tone="sand" /></div>{accounts.error && <Wait error={accounts.error} reload={accounts.reload} />}{packages.error && <Wait error={packages.error} reload={packages.reload} />}<section className="panel padded"><h2>Yönetim</h2><div className="quick-grid">{[['dietitians', 'Diyetisyen hesapları'], ['packages', 'Abonelik paketleri'], ['usage', 'Paket kullanımı'], ['audit', 'Denetim kayıtları']].map(([id, label]) => <button className="button secondary" key={id} onClick={() => navigate(id)}>{label} →</button>)}</div></section></> : <>
      <div className="metrics">
        <Metric label="Aktif danışan" value={data?.filter(item => item.isActive).length ?? '—'} foot={membership ? membership.usage.activeClients + ' / ' + membership.usage.maxClients + ' paket kullanımı' : 'Aktif hesaplar'} symbol="♙" tone="green" />
        <Metric label="Yayımlanan plan" value={session.demo ? demo.plans.filter(item => item.state === 'Published').length : '—'} foot="Güncel beslenme planı" symbol="▤" tone="sand" />
        <Metric label="Bugünkü randevu" value={session.demo ? appointments.length : '—'} foot={session.demo ? appointments.filter(item => item.status === 'Requested').length + ' onay bekliyor' : 'Takvim'} symbol="▦" tone="blue" />
        <Metric label="Toplam danışan" value={data?.length ?? '—'} foot="Aktif ve pasif kayıtlar" symbol="◎" tone="lilac" />
      </div>
      {session.demo && <div className="overview-grid">
        <section className="panel chart-panel">
          <header className="section-header"><div><h2>Kilo eğilimi</h2>{demo.clients.length > 0 && <select className="inline-select" aria-label="Grafik danışanı" value={selectedClient?.id} onChange={event => setTrendClient(event.target.value)}>{demo.clients.map(client => <option key={client.id} value={client.id}>{client.fullName}</option>)}</select>}</div><select aria-label="Grafik dönemi" value={period} onChange={event => setPeriod(Number(event.target.value))}><option value={42}>Son 6 hafta</option><option value={21}>Son 3 hafta</option><option value={7}>Son 7 gün</option></select></header>
          <div className="chart-meta"><strong>{trend.length ? trend.at(-1)?.weight.toLocaleString('tr-TR') + ' kg' : '—'}</strong>{trend.length > 1 && <Badge tone="gray">{(trend.at(-1)!.weight - trend[0].weight).toLocaleString('tr-TR', { maximumFractionDigits: 1, signDisplay: 'always' })} kg</Badge>}{selectedClient?.targetWeightKg && <span className="target-label">Hedef {selectedClient.targetWeightKg} kg</span>}</div>
          <Trend data={trend.map(item => ({ date: item.date, value: item.weight }))} target={selectedClient?.targetWeightKg ?? undefined} />
        </section>
        <section className="panel agenda-panel"><header className="section-header"><div><h2>Günün randevuları</h2><p className="muted">{appointments.length} randevu</p></div><button className="text-button" onClick={() => navigate('appointments')}>Takvimi aç →</button></header>
          <div className="agenda-list">{appointments.map(item => { const name = demo.clients.find(client => client.id === item.clientId)?.fullName ?? 'Danışan'; return <div className="agenda-item" key={item.id}><time>{timeLabel(item.startsAt)}</time><span className="agenda-avatar">{initials(name)}</span><span><strong>{name}</strong><small>{appointmentLabels[item.status]}</small></span><span className={'agenda-dot ' + item.status} /></div> })}</div>
          {!appointments.length && <Empty>Bugün randevu yok.</Empty>}
        </section>
      </div>}
      <section className="panel"><header className="section-header"><div><h2>Danışanlar</h2>{data && <p className="muted">{data.length} kayıt</p>}</div><button className="text-button" onClick={() => navigate('clients')}>Tüm danışanlar →</button></header>
      {!data ? <Wait error={clients.error} reload={clients.reload} /> : data.length ? <Table heads={['Danışan', 'Durum', session.demo ? 'Hedef kilo' : 'Telefon']}>{data.slice(0, 4).map((row, index) => <tr key={row.id}><td><ClientIdentity client={row} index={index} /></td><td><Badge tone={row.isActive ? 'green' : 'gray'}>{row.isActive ? 'Aktif' : 'Pasif'}</Badge></td><td>{session.demo ? row.targetWeightKg ? row.targetWeightKg + ' kg' : '—' : row.phone || '—'}</td></tr>)}</Table> : <Empty>Henüz danışan eklenmedi.</Empty>}
      </section>
    </>}
  </>
}
