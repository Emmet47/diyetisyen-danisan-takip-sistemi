import { useState } from 'react'
import { api } from '../api/http'
import { goalNames } from '../api/models'
import type { Client, Goal, History, Session, Subscription } from '../api/models'
import { dateKey, dateLabel, initials, newId, subscriptionFor } from '../demo'
import type { DemoData } from '../demo'
import { Badge, Empty, Field, Message, Modal, Table, Title, Wait, useAction, useLoad, values } from '../ui'

export type DemoProps = { session: Session; demo: DemoData; update: (fn: (data: DemoData) => DemoData) => void }
export function ClientIdentity({ client, index = 0 }: { client: Client; index?: number }) {
  return <span className="identity"><span className={'avatar avatar-' + index % 4}>{initials(client.fullName)}</span><span><strong>{client.fullName}</strong>{client.email && <small>{client.email}</small>}</span></span>
}

export default function Clients({ session, demo, update }: DemoProps) {
  const [filter, setFilter] = useState('all')
  const [query, setQuery] = useState('')
  const [selected, select] = useState<string | null>(null)
  const [form, setForm] = useState<Client | 'new' | null>(null)
  const live = useLoad(() => session.demo ? Promise.resolve([]) : api.get<Client[]>('/clients'), 'clients')
  const quota = useLoad(() => session.demo ? Promise.resolve(null) : api.get<Subscription>('/subscription'), 'client-quota')
  const detail = useLoad(() => !session.demo && selected ? api.get<Client>('/clients/' + selected) : Promise.resolve(null), selected ?? '')
  const list = session.demo ? demo.clients : live.data
  const usage = (session.demo ? subscriptionFor(demo) : quota.data)?.usage
  const client = session.demo ? list?.find(item => item.id === selected) : detail.data
  const refresh = () => { live.reload(); detail.reload(); quota.reload() }
  const rows = list?.filter(item => item.fullName.toLocaleLowerCase('tr').includes(query.toLocaleLowerCase('tr').trim()) && (filter === 'all' || item.isActive === (filter === 'active')))
  return <>
    <Title title="Danışanlar" subtitle="Danışan profillerini, hedeflerini ve kısıtlarını yönet." action={<button className="button primary" onClick={() => setForm('new')}>＋ Danışan ekle</button>} />
    {usage?.nearLimit && <Message type="info">Paket kullanımı {usage.activeClients}/{usage.maxClients}. Aktif danışan limitine yaklaştınız.</Message>}
    {!list ? <Wait error={live.error} reload={live.reload} /> : <section className="panel">
      <div className="toolbar"><input className="search" type="search" placeholder="Danışan ara…" aria-label="Danışan ara" value={query} onChange={event => setQuery(event.target.value)} /><select aria-label="Danışan durumu" value={filter} onChange={event => setFilter(event.target.value)}><option value="all">Tüm durumlar</option><option value="active">Aktif</option><option value="inactive">Pasif</option></select></div>
      <Table heads={['Danışan', 'Hedef', 'Telefon', 'Durum', '']}>{rows?.map((item, i) => <tr key={item.id}><td><ClientIdentity client={item} index={i} /></td><td>{goalNames[item.goalType]}</td><td>{item.phone || '—'}</td><td><Badge tone={item.isActive ? 'green' : 'gray'}>{item.isActive ? 'Aktif' : 'Pasif'}</Badge></td><td className="right"><button className="text-button" onClick={() => select(item.id)}>Profili aç →</button></td></tr>)}</Table>
      {!rows?.length && <Empty>Bu filtreye uygun danışan bulunamadı.</Empty>}
    </section>}
    {selected && !form && <Modal title={client?.fullName ?? 'Danışan profili'} close={() => select(null)}>
      {client ? <ClientDetail key={client.id} client={client} session={session} demo={demo} update={update} edit={() => setForm(client)} refresh={refresh} close={() => select(null)} /> : <Wait error={detail.error} reload={detail.reload} />}
    </Modal>}
    {form && <ClientForm client={form === 'new' ? undefined : form} session={session} close={() => setForm(null)} save={async (payload, profile) => {
      if (!session.demo) {
        if (profile) await api.put('/clients/' + profile.id, payload)
        else await api.post('/clients', payload)
      } else {
        const sub = subscriptionFor(demo)
        if (!profile && (sub.status !== 'Active' || sub.usage.activeClients >= sub.usage.maxClients)) throw new Error('Aktif abonelik ve kullanılabilir danışan limiti gerekli.')
        if (!profile && demo.clients.some(item => item.email === payload.email)) throw new Error('Bu e-posta adresi kullanılıyor.')
        const { kvkkConsentGiven, ...clientFields } = payload
        const at = new Date().toISOString()
        const row = { ...profile, ...clientFields, id: profile?.id ?? newId(), isActive: profile?.isActive ?? true, kvkkConsentAt: kvkkConsentGiven ? at : profile?.kvkkConsentAt } as Client
        const fields = Object.keys(clientFields) as (keyof Client)[]
        const changes = profile ? fields.filter(field => profile[field] !== row[field]).map(field => ({ field, oldValue: String(profile[field] ?? ''), newValue: String(row[field] ?? '') })) : []
        update(data => ({ ...data, clients: profile ? data.clients.map(item => item.id === profile.id ? row : item) : [...data.clients, row], history: { ...data.history, [row.id]: [{ at, action: profile ? 'ClientUpdated' : 'ClientAdded', changes }, ...(data.history[row.id] ?? [])] } }))
      }
      refresh()
      setForm(null)
    }} />}
  </>
}
function ClientForm({ client, session, save, close }: { client?: Client; session: Session; save: (data: Record<string, unknown>, client?: Client) => Promise<void>; close: () => void }) {
  const action = useAction()
  return <Modal title={client ? 'Danışanı düzenle' : 'Yeni danışan'} close={close}><form className="form" onSubmit={event => {
    const data = values(event)
    void action.run(() => save({
      fullName: data.fullName.trim(), phone: data.phone.trim() || null, goalType: data.goalType as Goal,
      targetWeightKg: data.targetWeightKg ? Number(data.targetWeightKg) : null, preferences: data.preferences.trim() || null, allergyNotes: data.allergyNotes.trim() || null,
      ...(client ? {} : { email: data.email.trim().toLowerCase(), birthDate: data.birthDate, sex: data.sex, kvkkConsentGiven: data.kvkk === 'on' }),
    }, client))
  }}>
    <div className="form-grid">
      <Field label="Ad soyad"><input name="fullName" defaultValue={client?.fullName} maxLength={100} required /></Field>
      <Field label="Telefon"><input name="phone" type="tel" defaultValue={client?.phone ?? ''} maxLength={20} /></Field>
      {!client && <><Field label="E-posta"><input name="email" type="email" maxLength={254} required /></Field><Field label="Doğum tarihi"><input name="birthDate" type="date" max={dateKey()} required /></Field><Field label="Cinsiyet"><select name="sex"><option value="Female">Kadın</option><option value="Male">Erkek</option></select></Field></>}
      <Field label="Hedef"><select name="goalType" defaultValue={client?.goalType}>{Object.entries(goalNames).map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select></Field>
      <Field label="Hedef kilo (kg)"><input name="targetWeightKg" type="number" min="20" max="350" step="0.1" defaultValue={client?.targetWeightKg ?? ''} /></Field>
    </div>
    <Field label="Beslenme tercihleri"><textarea name="preferences" defaultValue={client?.preferences ?? ''} maxLength={500} /></Field>
    <Field label="Alerji notu"><textarea name="allergyNotes" defaultValue={client?.allergyNotes ?? ''} maxLength={500} /></Field>
    {!client && <label className="check"><input type="checkbox" name="kvkk" required /> Danışandan açık rıza alındı.</label>}
    {!client && !session.demo && <p className="muted">Danışana parola oluşturması için davet e-postası gönderilir.</p>}
    <Message>{action.error}</Message><div className="form-actions"><button type="button" className="button secondary" onClick={close}>Vazgeç</button><button className="button primary" disabled={action.busy}>{action.busy ? 'Kaydediliyor…' : 'Kaydet'}</button></div>
  </form></Modal>
}
function ClientDetail({ client, session, demo, update, edit, refresh, close }: DemoProps & { client: Client; edit: () => void; refresh: () => void; close: () => void }) {
  const [tab, setTab] = useState('profile')
  const [restrictions, setRestrictions] = useState(client.restrictions ?? [])
  const history = useLoad(() => tab === 'history' && !session.demo ? api.get<History[]>('/clients/' + client.id + '/history') : Promise.resolve([]), tab + client.id)
  const action = useAction(() => { refresh(); history.reload() })
  const rows = session.demo ? demo.history[client.id] ?? [] : history.data
  function stateChange() {
    void action.run(async () => {
      if (!session.demo) return api.post('/clients/' + client.id + (client.isActive ? '/deactivate' : '/activate'))
      const sub = subscriptionFor(demo)
      if (!client.isActive && (sub.status !== 'Active' || sub.usage.activeClients >= sub.usage.maxClients)) throw new Error('Aktif abonelik ve kullanılabilir danışan limiti gerekli.')
      update(data => ({ ...data, clients: data.clients.map(item => item.id === client.id ? { ...item, isActive: !item.isActive } : item) }))
    })
  }
  return <div className="stack">
    <div className="tabs">{[['profile','Profil'],['restrictions','Alerji ve kısıtlar'],['history','Geçmiş']].map(([id, name]) => <button key={id} className={tab === id ? 'selected' : ''} onClick={() => setTab(id)}>{name}</button>)}</div>
    <Message>{action.error}</Message>
    {tab === 'profile' && <><dl className="details">{[['E-posta', client.email], ['Telefon', client.phone], ['Doğum tarihi', client.birthDate && dateLabel(client.birthDate)], ['Hedef', goalNames[client.goalType]], ['Hedef kilo', client.targetWeightKg ? client.targetWeightKg + ' kg' : null], ['Durum', client.isActive ? 'Aktif' : 'Pasif'], ['Tercihler', client.preferences], ['Alerji notu', client.allergyNotes]].map(([label, value]) => <div key={label}><dt>{label}</dt><dd>{value || '—'}</dd></div>)}</dl><div className="form-actions"><button className="button danger" disabled={action.busy} onClick={() => {
      if (!confirm(client.fullName + ' ve ona bağlı tüm kayıtlar kalıcı silinsin mi?')) return
      void action.run(async () => { if (session.demo) update(data => ({ ...data, clients: data.clients.filter(item => item.id !== client.id), plans: data.plans.filter(item => item.clientId !== client.id), appointments: data.appointments.filter(item => item.clientId !== client.id), measurements: data.measurements.filter(item => item.clientId !== client.id), mealLogs: data.mealLogs.filter(item => item.clientId !== client.id), history: Object.fromEntries(Object.entries(data.history).filter(([id]) => id !== client.id)) })); else await api.delete('/clients/' + client.id); close() })
    }}>Kalıcı sil</button><button className="button secondary" disabled={action.busy} onClick={stateChange}>{client.isActive ? 'Pasife al' : 'Aktifleştir'}</button><button className="button primary" disabled={action.busy} onClick={edit}>Düzenle</button></div></>}
    {tab === 'restrictions' && <>
      {!restrictions.length && <Empty>Kısıt kaydı bulunmuyor.</Empty>}
      {restrictions.map(row => <div className="list-line" key={row.foodItemId}><span>{row.foodName} · {row.type === 'Allergy' ? 'Alerji' : 'Sevmiyor'}</span><button className="text-button" onClick={() => setRestrictions(restrictions.filter(item => item.foodItemId !== row.foodItemId))}>Kaldır</button></div>)}
      {session.demo ? <form className="toolbar flush" onSubmit={event => { const data = values(event); const food = demo.foods.find(item => item.id === data.food); if (food && !restrictions.some(item => item.foodItemId === food.id)) setRestrictions([...restrictions, { foodItemId: food.id, foodName: food.name, type: data.type === 'Allergy' ? 'Allergy' : 'Dislike' }]) }}>
        <select name="food" aria-label="Besin" required><option value="">Besin seç</option>{demo.foods.map(food => <option key={food.id} value={food.id}>{food.name}</option>)}</select><select name="type" aria-label="Kısıt türü"><option value="Allergy">Alerji</option><option value="Dislike">Sevmiyor</option></select><button className="button secondary">Ekle</button>
      </form> : <Message type="info">Yeni kısıt eklemek için besin arama API’si gerekiyor. Mevcut kısıtları kaldırıp kaydedebilirsin.</Message>}
      <div className="form-actions"><button className="button primary" disabled={action.busy} onClick={() => void action.run(() => session.demo ? update(data => ({ ...data, clients: data.clients.map(item => item.id === client.id ? { ...item, restrictions } : item) })) : api.put('/clients/' + client.id + '/restrictions', { restrictions: restrictions.map(({ foodItemId, type }) => ({ foodItemId, type })) }))}>Kısıtları kaydet</button></div>
    </>}
    {tab === 'history' && (!rows ? <Wait error={history.error} reload={history.reload} /> : rows.length ? <div className="stack">{rows.map((row, index) => <div className="record" key={row.at + index}><strong>{row.action}</strong><p className="muted">{dateLabel(row.at)} {row.userId ? ' · ' + row.userId : ''}</p>{row.changes?.map(change => <p key={change.field}>{change.field} · {change.oldValue || '—'} → {change.newValue || '—'}</p>)}</div>)}</div> : <Empty>Geçmiş kaydı bulunmuyor.</Empty>)}
  </div>
}
