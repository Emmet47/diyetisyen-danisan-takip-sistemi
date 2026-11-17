import { useState } from 'react'
import { api } from '../api/http'
import type { Audit, Dietitian, Package, Paged, Subscription } from '../api/models'
import { addMonth, dateLabel, dateKey, newId, subscriptionFor } from '../demo'
import type { DemoProps } from './Clients'
import { Badge, Empty, Field, Message, Modal, Pager, Table, Title, Wait, useAction, useLoad, values } from '../ui'

const price = (value: number) => value.toLocaleString('tr-TR', { style: 'currency', currency: 'TRY' })
const status = (value: string | undefined) => ({ Active: 'Aktif', Expired: 'Süresi doldu', Cancelled: 'İptal edildi' })[value ?? ''] ?? '—'
const pageSize = 20

export function SubscriptionPage({ session, demo, update }: DemoProps) {
  const sub = useLoad(() => session.demo ? Promise.resolve(null) : api.get<Subscription>('/subscription'), 'sub')
  const plans = useLoad(() => session.demo ? Promise.resolve([]) : api.get<Package[]>('/subscription-plans'), 'packages')
  const data = session.demo ? subscriptionFor(demo) : sub.data
  const options = session.demo ? demo.packages.filter(item => item.isActive) : plans.data
  const action = useAction(sub.reload)
  return <><Title title="Aboneliğim" subtitle="Paketini, bitiş tarihini ve danışan limitini takip et." />
    {!data ? <Wait error={sub.error} reload={sub.reload} /> : <>
      <section className="panel subscription-card"><div><Badge tone={data.status === 'Active' ? 'green' : 'amber'}>{status(data.status)}</Badge><h2>{data.planName}</h2><p className="muted">{price(data.monthlyPrice)} / ay · Bitiş {dateLabel(data.endDate)}</p></div><div><strong>{data.usage.activeClients}/{data.usage.maxClients} aktif danışan</strong><progress value={data.usage.activeClients} max={data.usage.maxClients || 1} /></div></section>
      <Message type="info">{data.usage.nearLimit && 'Aktif danışan limitine yaklaştınız.'}</Message>
      <Message>{action.error}</Message>
      <section className="panel padded"><h2>Paket değiştir</h2>{!options ? <Wait error={plans.error} reload={plans.reload} /> : <form className="toolbar flush" onSubmit={event => {
        const { id } = values(event)
        void action.run(async () => {
          if (!session.demo) return api.post('/subscription/change', { subscriptionPlanId: id })
          const plan = options.find(item => item.id === id)
          if (!plan || plan.maxClients < data.usage.activeClients) throw new Error('Aktif danışan sayısı seçilen paketin limitini aşıyor.')
          update(d => ({ ...d, subscription: { ...data, planId: id, planName: plan.name, monthlyPrice: plan.monthlyPrice, endDate: addMonth(dateKey()), status: 'Active', usage: { ...data.usage, maxClients: plan.maxClients } } }))
        })
      }}><select name="id" aria-label="Yeni paket" defaultValue="" required><option value="">Paket seç</option>{options.filter(item => item.id !== data.planId).map(item => <option value={item.id} key={item.id}>{item.name} · {item.maxClients} danışan · {price(item.monthlyPrice)}</option>)}</select><button className="button primary" disabled={action.busy}>Paketi değiştir</button></form>}</section>
      <section className="panel padded"><h2>Aboneliği yenile</h2><p className="muted">Ödeme simülasyonuyla aboneliğini bir ay uzat.</p><button className="button secondary" disabled={action.busy || data.status === 'Cancelled'} onClick={() => void action.run(async () => {
        if (!session.demo) return api.post('/subscription/renew')
        update(d => ({ ...d, subscription: { ...data, status: 'Active', endDate: addMonth(data.endDate > dateKey() ? data.endDate : dateKey()) } }))
      })}>Bir ay yenile</button></section>
    </>}
  </>
}

type Section = 'dietitians' | 'packages' | 'usage' | 'audit'
const headings: Record<Section, [string, string]> = {
  dietitians: ['Diyetisyenler', 'Diyetisyen hesaplarını, paketlerini ve erişim durumlarını yönet.'],
  packages: ['Abonelik paketleri', 'Paket ücretlerini, danışan limitlerini ve satış durumunu düzenle.'],
  usage: ['Paket kullanımı', 'Diyetisyenlerin aktif danışan sayısını ve paket limitini izle.'],
  audit: ['Denetim kayıtları', 'Platformdaki işlem kayıtlarını görüntüle.'],
}

export function AdminPage({ session, demo, update, section }: DemoProps & { section: Section }) {
  const [page, setPage] = useState(1)
  const [query, setQuery] = useState('')
  const [filters, setFilters] = useState({ from: '', to: '', action: '', userId: '' })
  const [edit, setEdit] = useState<Package | 'new' | null>(null)
  const queryString = new URLSearchParams({ page: String(page), pageSize: String(pageSize) })
  if (section === 'audit') Object.entries(filters).forEach(([key, val]) => { if (val) queryString.set(key, val) })
  const path = { dietitians: 'dietitians', packages: 'subscription-plans', usage: 'usage', audit: 'audit-logs' }[section]
  type Row = Dietitian | Package | Audit
  const remote = useLoad(() => session.demo ? Promise.resolve(null) : api.get<Paged<Row> | Row[]>('/admin/' + path + '?' + queryString), path + queryString)
  const pageData = remote.data && !Array.isArray(remote.data) ? remote.data : null
  const demoAudit = demo.audit.filter(row => (!filters.from || row.at.slice(0, 10) >= filters.from) && (!filters.to || row.at.slice(0, 10) <= filters.to) && (!filters.action || row.action.toLowerCase().includes(filters.action.toLowerCase())) && (!filters.userId || row.userId === filters.userId))
  const action = useAction(remote.reload)
  const rows = session.demo ? section === 'packages' ? demo.packages : section === 'audit' ? demoAudit : demo.dietitians : Array.isArray(remote.data) ? remote.data : pageData?.items
  const accounts = (rows as Dietitian[] | undefined)?.filter(row => !query || [row.fullName, row.email].join(' ').toLocaleLowerCase('tr').includes(query.trim().toLocaleLowerCase('tr')))
  const [title, subtitle] = headings[section]
  return <>
    <Title title={title} subtitle={subtitle} action={section === 'packages' && <button className="button primary" onClick={() => setEdit('new')}>＋ Paket ekle</button>} />
    <Message>{action.error}</Message>
    {section === 'audit' && <form className="panel toolbar" onSubmit={event => { const data = values(event); setFilters({ from: data.from, to: data.to, action: data.action.trim(), userId: data.userId.trim() }); setPage(1) }}><Field label="Başlangıç"><input type="date" name="from" /></Field><Field label="Bitiş"><input type="date" name="to" /></Field><Field label="İşlem"><input name="action" maxLength={50} placeholder="Örn. LoginFailed" /></Field><Field label="Kullanıcı kimliği"><input name="userId" /></Field><button className="button secondary">Filtrele</button></form>}
    {!rows ? <Wait error={remote.error} reload={remote.reload} /> : <section className="panel">
      {section === 'dietitians' && <div className="toolbar"><input className="search" type="search" placeholder="Bu sayfada diyetisyen ara…" aria-label="Diyetisyen ara" value={query} onChange={event => setQuery(event.target.value)} /></div>}
      {section === 'packages' && <Table heads={['Paket', 'Danışan limiti', 'Aylık ücret', 'Satış durumu', '']}>{(rows as Package[]).map(row => <tr key={row.id}><td><strong>{row.name}</strong></td><td>{row.maxClients}</td><td>{price(row.monthlyPrice)}</td><td><Badge tone={row.isActive ? 'green' : 'gray'}>{row.isActive ? 'Satışta' : 'Kapalı'}</Badge></td><td className="right"><button className="text-button" onClick={() => setEdit(row)}>Düzenle</button></td></tr>)}</Table>}
      {(section === 'dietitians' || section === 'usage') && <Table heads={section === 'dietitians' ? ['Diyetisyen', 'Paket', 'Kullanım', 'Hesap durumu', ''] : ['Diyetisyen', 'Paket', 'Aktif danışan / limit']}>{accounts?.map(row => {
        const id = row.dietitianId ?? row.id
        return <tr key={id}><td><strong>{row.fullName}</strong>{row.email && <small className="cell-note">{row.email}</small>}</td><td>{row.planName ?? '—'}{section === 'dietitians' && row.subscriptionStatus && <small className="cell-note">{status(row.subscriptionStatus)}</small>}</td><td>{row.usage.activeClients} / {row.usage.maxClients}</td>{section === 'dietitians' && <><td><Badge tone={row.isActive ? 'green' : 'gray'}>{row.isActive ? 'Aktif' : 'Pasif'}</Badge></td><td className="right"><button className="text-button" disabled={action.busy || !id} onClick={() => {
          if (row.isActive && !confirm(row.fullName + ' hesabı pasife alınsın mı?')) return
          void action.run(async () => {
            if (!session.demo) return api.post('/admin/dietitians/' + id + (row.isActive ? '/deactivate' : '/activate'))
            update(data => ({ ...data, dietitians: data.dietitians.map(item => item.id === id ? { ...item, isActive: !item.isActive } : item), audit: [{ id: newId(), at: new Date().toISOString(), action: row.isActive ? 'DietitianDeactivated' : 'DietitianActivated', entityType: 'Dietitian' }, ...data.audit] }))
          })
        }}>{row.isActive ? 'Pasife al' : 'Aktifleştir'}</button></td></>}</tr>
      })}</Table>}
      {section === 'audit' && <Table heads={['Zaman', 'İşlem', 'Varlık türü', 'Kullanıcı']}>{(rows as Audit[]).map(row => <tr key={row.id}><td>{dateLabel(row.at)} · {new Date(row.at).toLocaleTimeString('tr-TR', { timeZone: 'Europe/Istanbul', hour: '2-digit', minute: '2-digit' })}</td><td>{row.action}</td><td>{row.entityType ?? '—'}</td><td>{row.userId ?? '—'}</td></tr>)}</Table>}
      {!(section === 'dietitians' ? accounts?.length : rows.length) && <Empty>Kayıt bulunamadı.</Empty>}
      {pageData && <Pager {...pageData} change={setPage} />}
    </section>}
    {edit && <PackageForm item={edit === 'new' ? undefined : edit} close={() => setEdit(null)} save={async payload => {
      if (session.demo) {
        if (demo.packages.some(item => item.id !== payload.id && item.name.toLocaleLowerCase('tr') === payload.name.toLocaleLowerCase('tr'))) throw new Error('Bu paket adı zaten kullanılıyor.')
        update(data => {
          const exists = data.packages.some(item => item.id === payload.id)
          const subscription = data.subscription.planId === payload.id ? { ...data.subscription, planName: payload.name, monthlyPrice: payload.monthlyPrice, usage: { ...data.subscription.usage, maxClients: payload.maxClients } } : data.subscription
          return { ...data, subscription, packages: exists ? data.packages.map(item => item.id === payload.id ? payload : item) : [...data.packages, payload] }
        })
      } else {
        const { id, ...input } = payload
        if (edit === 'new') await api.post('/admin/subscription-plans', input)
        else await api.put('/admin/subscription-plans/' + id, input)
      }
      remote.reload(); setEdit(null)
    }} />}
  </>
}
function PackageForm({ item, save, close }: { item?: Package; close: () => void; save: (item: Package) => Promise<void> }) {
  const action = useAction()
  return <Modal title={item ? 'Paketi düzenle' : 'Yeni paket'} close={close}><form className="form" onSubmit={event => {
    const data = values(event)
    void action.run(() => save({ id: item?.id ?? newId(), name: data.name.trim(), maxClients: Number(data.maxClients), monthlyPrice: Number(data.monthlyPrice), isActive: data.isActive === 'on' }))
  }}><Field label="Paket adı"><input name="name" maxLength={50} required defaultValue={item?.name} /></Field><div className="form-grid"><Field label="Danışan limiti"><input name="maxClients" type="number" min="1" step="1" required defaultValue={item?.maxClients} /></Field><Field label="Aylık ücret (TL)"><input name="monthlyPrice" type="number" min="0" max="999999.99" step=".01" required defaultValue={item?.monthlyPrice} /></Field></div><label className="check"><input type="checkbox" name="isActive" defaultChecked={item?.isActive ?? true} /> Satışta</label><Message>{action.error}</Message><div className="form-actions"><button className="button primary" disabled={action.busy}>Kaydet</button></div></form></Modal>
}
