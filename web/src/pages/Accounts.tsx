import { useState } from 'react'
import { adminApi } from '../api/admin'
import type { AuditFilter, PackageInput } from '../api/admin'
import { subscriptionNames } from '../api/models'
import type { AuditLog, DietitianAdmin, DietitianUsage, Package, Paged, SubscriptionStatus } from '../api/models'
import { subscriptionApi } from '../api/subscription'
import { dateLabel, timeLabel } from '../format'
import { Badge, Empty, Field, Message, Modal, Pager, Table, Title, Wait, useAction, useLoad, values } from '../ui'
import type { PageProps } from '../ui'

const price = (value: number) => value.toLocaleString('tr-TR', { style: 'currency', currency: 'TRY' })
const status = (value: SubscriptionStatus | null | undefined) => value ? subscriptionNames[value] : '—'
const pageSize = 20

export function SubscriptionPage(_props: PageProps) {
  const sub = useLoad(() => subscriptionApi.get(), 'sub')
  const plans = useLoad(() => subscriptionApi.packages(), 'packages')
  const [notice, setNotice] = useState('')
  const data = sub.data
  const options = plans.data
  const action = useAction(sub.reload)
  return <><Title title="Aboneliğim" subtitle="Paketini, bitiş tarihini ve danışan limitini takip et." />
    {!data ? <Wait error={sub.error} reload={sub.reload} /> : <>
      <section className="panel subscription-card"><div><Badge tone={data.status === 'Active' ? 'green' : 'amber'}>{status(data.status)}</Badge><h2>{data.planName}</h2><p className="muted">{price(data.monthlyPrice)} / ay · Bitiş {dateLabel(data.endDate)}</p></div><div><strong>{data.usage.activeClients}/{data.usage.maxClients} aktif danışan</strong><progress value={data.usage.activeClients} max={data.usage.maxClients || 1} /></div></section>
      <Message type="info">{data.usage.nearLimit && 'Aktif danışan limitine yaklaştınız (%80).'}{data.status !== 'Active' && ' Abonelik aktif değilken yeni danışan ve yeni plan eklenemez; mevcut verileri görebilirsin.'}</Message>
      <Message>{action.error}</Message><Message type="success">{notice}</Message>
      <section className="panel padded"><h2>Paket değiştir</h2>{!options ? <Wait error={plans.error} reload={plans.reload} /> : <form className="toolbar flush" onSubmit={event => {
        const { id } = values(event)
        void action.run(async () => { await subscriptionApi.change(id); setNotice('Paketin değiştirildi.') })
      }}><select name="id" aria-label="Yeni paket" defaultValue="" required><option value="">Paket seç</option>{options.filter(item => item.id !== data.planId).map(item => <option value={item.id} key={item.id}>{item.name} · {item.maxClients} danışan · {price(item.monthlyPrice)}</option>)}</select><button className="button primary" disabled={action.busy}>Paketi değiştir</button></form>}</section>
      <section className="panel padded"><h2>Aboneliği yenile</h2><p className="muted">Ödeme simülasyonuyla aboneliğini bir ay uzat. Kart bilgisi alınmaz.</p><button className="button secondary" disabled={action.busy || data.status === 'Cancelled'} onClick={() => void action.run(async () => { await subscriptionApi.renew(); setNotice('Aboneliğin bir ay uzatıldı.') })}>Bir ay yenile</button></section>
    </>}
  </>
}

type Section = 'dietitians' | 'packages' | 'usage' | 'audit'
const headings: Record<Section, [string, string]> = {
  dietitians: ['Diyetisyenler', 'Diyetisyen hesaplarını, paketlerini ve erişim durumlarını yönet.'],
  packages: ['Abonelik paketleri', 'Paket ücretlerini, danışan limitlerini ve satış durumunu düzenle.'],
  usage: ['Paket kullanımı', 'Diyetisyenlerin aktif danışan sayısını ve paket limitini izle.'],
  audit: ['Denetim kayıtları', 'Platformdaki işlem kayıtlarını görüntüle (sağlık verisi ayrıntısı olmadan).'],
}

export function AdminPage({ section }: PageProps & { section: Section }) {
  const [page, setPage] = useState(1)
  const [query, setQuery] = useState('')
  const [filters, setFilters] = useState<AuditFilter>({})
  const [edit, setEdit] = useState<Package | 'new' | null>(null)
  const paging = { page, pageSize }
  const remote = useLoad<Paged<DietitianAdmin | DietitianUsage | AuditLog> | Package[]>(() => {
    switch (section) {
      case 'dietitians': return adminApi.dietitians(paging)
      case 'usage': return adminApi.usage(paging)
      case 'audit': return adminApi.auditLogs(paging, filters)
      default: return adminApi.packages()
    }
  }, section + page + JSON.stringify(filters))
  const pageData = remote.data && !Array.isArray(remote.data) ? remote.data : null
  const action = useAction(remote.reload)
  const rows = Array.isArray(remote.data) ? remote.data : pageData?.items
  const accounts = section === 'dietitians' || section === 'usage'
    ? (rows as (DietitianAdmin | DietitianUsage)[] | undefined)?.filter(row => !query || [row.fullName, 'email' in row ? row.email : ''].join(' ').toLocaleLowerCase('tr').includes(query.trim().toLocaleLowerCase('tr')))
    : undefined
  const [title, subtitle] = headings[section]
  return <>
    <Title title={title} subtitle={subtitle} action={section === 'packages' && <button className="button primary" onClick={() => setEdit('new')}>＋ Paket ekle</button>} />
    <Message>{action.error}</Message>
    {section === 'audit' && <form className="panel toolbar" onSubmit={event => { const data = values(event); setFilters({ from: data.from, to: data.to, action: data.action.trim(), userId: data.userId.trim() }); setPage(1) }}><Field label="Başlangıç"><input type="date" name="from" /></Field><Field label="Bitiş"><input type="date" name="to" /></Field><Field label="İşlem"><input name="action" maxLength={50} placeholder="Örn. LoginFailed" /></Field><Field label="Kullanıcı kimliği"><input name="userId" pattern="[0-9a-fA-F-]{36}" title="Kullanıcı kimliği (GUID)" /></Field><button className="button secondary">Filtrele</button></form>}
    {!rows ? <Wait error={remote.error} reload={remote.reload} /> : <section className="panel">
      {section === 'dietitians' && <div className="toolbar"><input className="search" type="search" placeholder="Bu sayfada diyetisyen ara…" aria-label="Diyetisyen ara" value={query} onChange={event => setQuery(event.target.value)} /></div>}
      {section === 'packages' && <Table heads={['Paket', 'Danışan limiti', 'Aylık ücret', 'Satış durumu', '']}>{(rows as Package[]).map(row => <tr key={row.id}><td><strong>{row.name}</strong></td><td>{row.maxClients}</td><td>{price(row.monthlyPrice)}</td><td><Badge tone={row.isActive ? 'green' : 'gray'}>{row.isActive ? 'Satışta' : 'Kapalı'}</Badge></td><td className="right"><button className="text-button" onClick={() => setEdit(row)}>Düzenle</button></td></tr>)}</Table>}
      {section === 'dietitians' && <Table heads={['Diyetisyen', 'Paket', 'Kullanım', 'Hesap durumu', '']}>{(accounts as DietitianAdmin[]).map(row => <tr key={row.id}><td><strong>{row.fullName}</strong><small className="cell-note">{row.email}</small></td><td>{row.planName ?? '—'}{row.subscriptionStatus && <small className="cell-note">{status(row.subscriptionStatus)} · {dateLabel(row.subscriptionEndDate)}</small>}</td><td>{row.usage.activeClients} / {row.usage.maxClients}</td><td><Badge tone={row.isActive ? 'green' : 'gray'}>{row.isActive ? 'Aktif' : 'Pasif'}</Badge></td><td className="right"><button className="text-button" disabled={action.busy} onClick={() => {
        if (row.isActive && !confirm(row.fullName + ' hesabı pasife alınsın mı?')) return
        void action.run(() => row.isActive ? adminApi.deactivate(row.id) : adminApi.activate(row.id))
      }}>{row.isActive ? 'Pasife al' : 'Aktifleştir'}</button></td></tr>)}</Table>}
      {section === 'usage' && <Table heads={['Diyetisyen', 'Paket', 'Aktif danışan / limit']}>{(accounts as DietitianUsage[]).map(row => <tr key={row.dietitianId}><td><strong>{row.fullName}</strong></td><td>{row.planName ?? '—'}</td><td>{row.usage.activeClients} / {row.usage.maxClients}{row.usage.nearLimit && <> <Badge tone="amber">%80</Badge></>}</td></tr>)}</Table>}
      {section === 'audit' && <Table heads={['Zaman', 'İşlem', 'Varlık türü', 'Kullanıcı']}>{(rows as AuditLog[]).map(row => <tr key={row.id}><td>{dateLabel(row.at)} · {timeLabel(row.at)}</td><td>{row.action}</td><td>{row.entityType}</td><td>{row.userId ?? '—'}</td></tr>)}</Table>}
      {!(accounts ? accounts.length : rows.length) && <Empty>Kayıt bulunamadı.</Empty>}
      {pageData && <Pager {...pageData} change={setPage} />}
    </section>}
    {edit && <PackageForm item={edit === 'new' ? undefined : edit} close={() => setEdit(null)} save={async payload => {
      if (edit === 'new') await adminApi.createPackage(payload)
      else await adminApi.updatePackage(edit.id, payload)
      remote.reload(); setEdit(null)
    }} />}
  </>
}
function PackageForm({ item, save, close }: { item?: Package; close: () => void; save: (item: PackageInput) => Promise<void> }) {
  const action = useAction()
  return <Modal title={item ? 'Paketi düzenle' : 'Yeni paket'} close={close}><form className="form" onSubmit={event => {
    const data = values(event)
    void action.run(() => save({ name: data.name.trim(), maxClients: Number(data.maxClients), monthlyPrice: Number(data.monthlyPrice), isActive: data.isActive === 'on' }))
  }}><Field label="Paket adı"><input name="name" maxLength={50} required defaultValue={item?.name} /></Field><div className="form-grid"><Field label="Danışan limiti"><input name="maxClients" type="number" min="1" step="1" required defaultValue={item?.maxClients} /></Field><Field label="Aylık ücret (TL)"><input name="monthlyPrice" type="number" min="0" max="999999.99" step=".01" required defaultValue={item?.monthlyPrice} /></Field></div><label className="check"><input type="checkbox" name="isActive" defaultChecked={item?.isActive ?? true} /> Satışta</label><Message>{action.error}</Message><div className="form-actions"><button className="button primary" disabled={action.busy}>Kaydet</button></div></form></Modal>
}
