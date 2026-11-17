import { useState } from 'react'
import { clientsApi } from '../api/clients'
import { goalNames, sexNames } from '../api/models'
import type { ClientDetail, Goal, NewClientInput, Restriction, Sex, Usage } from '../api/models'
import { subscriptionApi } from '../api/subscription'
import { dateKey, dateLabel, initials } from '../format'
import { FoodPicker } from './Catalog'
import { Badge, Empty, Field, Message, Modal, Table, Title, Wait, useAction, useLoad, values } from '../ui'
import type { PageProps } from '../ui'

export function ClientIdentity({ client, index = 0 }: { client: { fullName: string; email?: string | null }; index?: number }) {
  return <span className="identity"><span className={'avatar avatar-' + index % 4}>{initials(client.fullName)}</span><span><strong>{client.fullName}</strong>{client.email && <small>{client.email}</small>}</span></span>
}

/** KVKK aydınlatma metni (gereksinimler 2.5): danışan eklenirken diyetisyen panelinde gösterilir. */
export function KvkkNotice() {
  return <details className="record">
    <summary><strong>KVKK aydınlatma metni</strong></summary>
    <p className="muted">Danışanın ad, iletişim, doğum tarihi, cinsiyet, hedef, beslenme tercihi, alerji/kısıt, ölçüm, plan, öğün kaydı ve fotoğraf bilgileri yalnızca beslenme takibi amacıyla işlenir. Sağlık verisi olduğu için hukuki dayanak danışanın açık rızasıdır. Verileri yalnızca danışan ve bağlı olduğu diyetisyen görür; platform yöneticisi göremez. İlişki bitince kayıt pasife alınır; danışan isterse tüm kayıtlar ve fotoğraflar kalıcı silinir (en geç 30 gün içinde). Danışan verisine erişme, düzeltme ve silinmesini isteme haklarına sahiptir.</p>
  </details>
}

export default function Clients(_props: PageProps) {
  const [filter, setFilter] = useState('all')
  const [query, setQuery] = useState('')
  const [selected, select] = useState<string | null>(null)
  const [form, setForm] = useState<ClientDetail | 'new' | null>(null)
  const [added, setAdded] = useState<Usage | null>(null)
  const list = useLoad(() => clientsApi.list(), 'clients')
  const quota = useLoad(() => subscriptionApi.get(), 'client-quota')
  const detail = useLoad(() => selected ? clientsApi.get(selected) : Promise.resolve(null), selected ?? '')
  const usage = added ?? quota.data?.usage
  const client = detail.data
  const refresh = () => { list.reload(); detail.reload(); quota.reload() }
  const rows = list.data?.filter(item => item.fullName.toLocaleLowerCase('tr').includes(query.toLocaleLowerCase('tr').trim()) && (filter === 'all' || item.isActive === (filter === 'active')))
  return <>
    <Title title="Danışanlar" subtitle="Danışan profillerini, hedeflerini ve kısıtlarını yönet." action={<button className="button primary" onClick={() => setForm('new')}>＋ Danışan ekle</button>} />
    {usage?.nearLimit && <Message type="info">Paket kullanımı {usage.activeClients}/{usage.maxClients}. Aktif danışan limitine yaklaştınız; gerekirse paketini yükselt.</Message>}
    {quota.data && quota.data.status !== 'Active' && <Message type="info">Aboneliğin aktif değil. Yeni danışan eklemek için aboneliğini yenile.</Message>}
    {!list.data ? <Wait error={list.error} reload={list.reload} /> : <section className="panel">
      <div className="toolbar"><input className="search" type="search" placeholder="Danışan ara…" aria-label="Danışan ara" value={query} onChange={event => setQuery(event.target.value)} /><select aria-label="Danışan durumu" value={filter} onChange={event => setFilter(event.target.value)}><option value="all">Tüm durumlar</option><option value="active">Aktif</option><option value="inactive">Pasif</option></select></div>
      <Table heads={['Danışan', 'Hedef', 'Telefon', 'Durum', '']}>{rows?.map((item, i) => <tr key={item.id}><td><ClientIdentity client={item} index={i} /></td><td>{goalNames[item.goalType]}</td><td>{item.phone || '—'}</td><td><Badge tone={item.isActive ? 'green' : 'gray'}>{item.isActive ? 'Aktif' : 'Pasif'}</Badge></td><td className="right"><button className="text-button" onClick={() => select(item.id)}>Profili aç →</button></td></tr>)}</Table>
      {!rows?.length && <Empty>Bu filtreye uygun danışan bulunamadı.</Empty>}
    </section>}
    {selected && !form && <Modal title={client?.fullName ?? 'Danışan profili'} close={() => select(null)}>
      {client ? <ClientDetailView key={client.id} client={client} edit={() => setForm(client)} refresh={refresh} close={() => { select(null); list.reload(); quota.reload() }} /> : <Wait error={detail.error} reload={detail.reload} />}
    </Modal>}
    {form && <ClientForm client={form === 'new' ? undefined : form} close={() => setForm(null)} save={async (payload, profile) => {
      if (profile) {
        const { fullName, phone, goalType, targetWeightKg, preferences, allergyNotes } = payload
        await clientsApi.update(profile.id, { fullName, phone, goalType, targetWeightKg, preferences, allergyNotes })
      } else {
        const result = await clientsApi.add(payload)
        setAdded(result.usage)
      }
      refresh()
      setForm(null)
    }} />}
  </>
}

function ClientForm({ client, save, close }: { client?: ClientDetail; save: (data: NewClientInput, client?: ClientDetail) => Promise<void>; close: () => void }) {
  const action = useAction()
  return <Modal title={client ? 'Danışanı düzenle' : 'Yeni danışan'} close={close}><form className="form" onSubmit={event => {
    const data = values(event)
    void action.run(() => save({
      fullName: data.fullName.trim(), phone: data.phone.trim() || null, goalType: data.goalType as Goal,
      targetWeightKg: data.targetWeightKg ? Number(data.targetWeightKg) : null, preferences: data.preferences.trim() || null, allergyNotes: data.allergyNotes.trim() || null,
      email: client?.email ?? data.email.trim().toLowerCase(), birthDate: client?.birthDate ?? data.birthDate, sex: client?.sex ?? data.sex as Sex, kvkkConsentGiven: data.kvkk === 'on',
    }, client))
  }}>
    <div className="form-grid">
      <Field label="Ad soyad"><input name="fullName" defaultValue={client?.fullName} maxLength={100} required /></Field>
      <Field label="Telefon"><input name="phone" type="tel" defaultValue={client?.phone ?? ''} maxLength={20} /></Field>
      {!client && <><Field label="E-posta"><input name="email" type="email" maxLength={254} required /></Field><Field label="Doğum tarihi"><input name="birthDate" type="date" max={dateKey()} required /></Field><Field label="Cinsiyet"><select name="sex">{Object.entries(sexNames).map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select></Field></>}
      <Field label="Hedef"><select name="goalType" defaultValue={client?.goalType}>{Object.entries(goalNames).map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select></Field>
      <Field label="Hedef kilo (kg)"><input name="targetWeightKg" type="number" min="20" max="350" step="0.1" defaultValue={client?.targetWeightKg ?? ''} /></Field>
    </div>
    <Field label="Beslenme tercihleri"><textarea name="preferences" defaultValue={client?.preferences ?? ''} maxLength={500} /></Field>
    <Field label="Alerji notu"><textarea name="allergyNotes" defaultValue={client?.allergyNotes ?? ''} maxLength={500} /></Field>
    {!client && <><KvkkNotice /><label className="check"><input type="checkbox" name="kvkk" required /> Aydınlatma metni danışana okundu ve danışandan açık rıza alındı.</label>
      <p className="muted">Danışana mobil uygulama parolasını oluşturması için davet e-postası gönderilir.</p></>}
    <Message>{action.error}</Message><div className="form-actions"><button type="button" className="button secondary" onClick={close}>Vazgeç</button><button className="button primary" disabled={action.busy}>{action.busy ? 'Kaydediliyor…' : 'Kaydet'}</button></div>
  </form></Modal>
}

function ClientDetailView({ client, edit, refresh, close }: { client: ClientDetail; edit: () => void; refresh: () => void; close: () => void }) {
  const [tab, setTab] = useState('profile')
  const [restrictions, setRestrictions] = useState<Restriction[]>(client.restrictions)
  const [type, setType] = useState<Restriction['type']>('Allergy')
  const [saved, setSaved] = useState('')
  const history = useLoad(() => tab === 'history' ? clientsApi.history(client.id) : Promise.resolve([]), tab + client.id)
  const action = useAction(() => { refresh(); history.reload() })
  const rows = history.data
  return <div className="stack">
    <div className="tabs">{[['profile', 'Profil'], ['restrictions', 'Alerji ve kısıtlar'], ['history', 'Geçmiş']].map(([id, name]) => <button key={id} className={tab === id ? 'selected' : ''} onClick={() => { setTab(id); setSaved('') }}>{name}</button>)}</div>
    <Message>{action.error}</Message><Message type="success">{saved}</Message>
    {tab === 'profile' && <><dl className="details">{[['E-posta', client.email], ['Telefon', client.phone], ['Doğum tarihi', dateLabel(client.birthDate)], ['Cinsiyet', sexNames[client.sex]], ['Hedef', goalNames[client.goalType]], ['Hedef kilo', client.targetWeightKg ? client.targetWeightKg + ' kg' : null], ['Durum', client.isActive ? 'Aktif' : 'Pasif'], ['Açık rıza', dateLabel(client.kvkkConsentAt)], ['Tercihler', client.preferences], ['Alerji notu', client.allergyNotes]].map(([label, value]) => <div key={label}><dt>{label}</dt><dd>{value || '—'}</dd></div>)}</dl><div className="form-actions"><button className="button danger" disabled={action.busy} onClick={() => {
      if (!confirm(client.fullName + ' ve ona bağlı tüm kayıtlar (ölçüm, plan, öğün kaydı, fotoğraf, randevu) kalıcı silinsin mi?')) return
      void action.run(async () => { await clientsApi.remove(client.id); close() })
    }}>Kalıcı sil</button><button className="button secondary" disabled={action.busy} onClick={() => void action.run(() => client.isActive ? clientsApi.deactivate(client.id) : clientsApi.activate(client.id))}>{client.isActive ? 'Pasife al' : 'Aktifleştir'}</button><button className="button primary" disabled={action.busy} onClick={edit}>Düzenle</button></div></>}
    {tab === 'restrictions' && <>
      <p className="muted">“Alerji” işaretli besin plana eklenemez; “Sevmiyor” işaretli besin uyarı verir.</p>
      {!restrictions.length && <Empty>Kısıt kaydı bulunmuyor.</Empty>}
      {restrictions.map(row => <div className="list-line" key={row.foodItemId}><span>{row.foodName} · {row.type === 'Allergy' ? 'Alerji' : 'Sevmiyor'}</span><button className="text-button" onClick={() => setRestrictions(restrictions.filter(item => item.foodItemId !== row.foodItemId))}>Kaldır</button></div>)}
      <div className="toolbar flush"><select aria-label="Kısıt türü" value={type} onChange={event => setType(event.target.value as Restriction['type'])}><option value="Allergy">Alerji</option><option value="Dislike">Sevmiyor</option></select></div>
      <FoodPicker label="Kısıtlanacak besin" pick={food => { if (!restrictions.some(item => item.foodItemId === food.id)) setRestrictions([...restrictions, { foodItemId: food.id!, foodName: food.name, type }]) }} />
      <div className="form-actions"><button className="button primary" disabled={action.busy} onClick={() => void action.run(async () => {
        await clientsApi.setRestrictions(client.id, restrictions.map(({ foodItemId, type }) => ({ foodItemId, type })))
        setSaved('Kısıtlar kaydedildi.')
      })}>Kısıtları kaydet</button></div>
    </>}
    {tab === 'history' && (!rows ? <Wait error={history.error} reload={history.reload} /> : rows.length ? <div className="stack">{rows.map((row, index) => <div className="record" key={row.at + index}><strong>{row.action}</strong><p className="muted">{dateLabel(row.at)}</p>{row.changes.map(change => <p key={change.field}>{change.field} · {change.oldValue || '—'} → {change.newValue || '—'}</p>)}</div>)}</div> : <Empty>Geçmiş kaydı bulunmuyor.</Empty>)}
  </div>
}
