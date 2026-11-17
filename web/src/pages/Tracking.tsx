import { useState } from 'react'
import { api } from '../api/http'
import { measurementsApi } from '../api/measurements'
import type { Client, Measurement, MeasurementInput } from '../api/models'
import { dateKey, dateLabel, moveDate, newId, plannedMeals } from '../demo'
import type { DemoProps } from './Clients'
import { Trend } from './Overview'
import { Badge, Empty, Field, Message, Modal, Table, Title, Wait, useAction, useLoad, values } from '../ui'

const decimal = (n: number) => n.toLocaleString('tr-TR', { maximumFractionDigits: 2 })
const logStatuses = { Ate: 'Yedim', Partial: 'Kısmen yedim', Skipped: 'Yemedim' }
function ClientPicker({ clients, clientId, change, disabled }: { clients: Client[]; clientId?: string; change: (id: string) => void; disabled: boolean }) {
  return <select aria-label="Danışan seç" value={clientId ?? ''} disabled={disabled} onChange={event => change(event.target.value)}>{!clients.length && <option value="">Danışan bulunamadı</option>}{clients.map(item => <option value={item.id} key={item.id}>{item.fullName}</option>)}</select>
}

type TrackingProps = DemoProps & { mode: 'tracking' | 'journal' | 'energy' }

export default function Tracking(props: TrackingProps) {
  const { session, demo } = props
  const liveClients = useLoad(() => session.demo ? Promise.resolve([]) : api.get<Client[]>('/clients'), 'tracking-clients')
  const clients = session.demo ? demo.clients : liveClients.data
  if (!clients) return <><Title title="Ölçümler ve gelişim" /><Wait error={liveClients.error} reload={liveClients.reload} /></>
  return <TrackingContent {...props} clients={clients} />
}

function TrackingContent({ session, demo, update, mode, clients }: TrackingProps & { clients: Client[] }) {
  const [selection, setSelection] = useState('')
  const [from, setFrom] = useState(() => session.demo ? moveDate(dateKey(), -35) : '')
  const [to, setTo] = useState(() => session.demo ? dateKey() : '')
  const [edit, setEdit] = useState<Measurement | 'new' | null>(null)
  const [print, setPrint] = useState(false)
  const [result, setResult] = useState<{ clientId: string; bmr: number; daily: number; target: number } | null>(null)
  const [revision, setRevision] = useState(0)
  const action = useAction()
  const client = clients.find(item => item.id === selection) ?? clients[0]
  const liveClientId = session.demo ? undefined : client?.id
  const live = useLoad(() => liveClientId ? measurementsApi.list(liveClientId) : Promise.resolve(null), `${liveClientId ?? ''}:${revision}`)
  const history = live.data
  const ready = session.demo || Boolean(history)
  const measurementRows: Measurement[] = session.demo
    ? demo.measurements.filter(item => item.clientId === client?.id).sort((a, b) => a.date.localeCompare(b.date))
    : history?.items.map(row => ({
        id: row.id, clientId: client.id, date: row.measuredOn, weight: row.weightKg, height: row.heightCm,
        waist: row.waistCm, hip: row.hipCm ?? undefined, fat: row.bodyFatPercent ?? undefined,
        note: row.note ?? undefined, weightChange: row.weightChangeKg,
      })) ?? []
  const measurements = measurementRows.filter(item => (!from || item.date >= from) && (!to || item.date <= to))
  const latest = measurementRows.at(-1)
  const target = session.demo ? client?.targetWeightKg : history?.targetWeightKg
  const remaining = session.demo && latest && target != null ? Math.abs(latest.weight - target) : history?.remainingKg
  const logs = session.demo ? demo.mealLogs.filter(item => item.clientId === client?.id && item.date >= from && item.date <= to) : []
  const currentPlan = session.demo ? demo.plans.find(item => item.clientId === client?.id && item.state === 'Published') : undefined
  const numDays = from && to && from <= to ? Math.round((Date.parse(to) - Date.parse(from)) / 86400000) + 1 : 0
  const loggedDays = new Set(logs.map(row => row.date)).size
  const scheduled = session.demo && client && numDays ? plannedMeals(demo.plans, client.id, from, to) : []
  const entries = new Map(logs.map(row => [row.date + ':' + row.mealId, row]))
  const journal = scheduled.map(row => ({ ...row, log: entries.get(row.date + ':' + row.meal.id) }))
  const adherence = scheduled.length ? journal.reduce((sum, row) => sum + (row.log?.status === 'Ate' ? 1 : row.log?.status === 'Partial' ? .5 : 0), 0) / scheduled.length * 100 : null
  const frequency = numDays ? loggedDays / numDays * 100 : 0
  const labels = { tracking: 'Ölçümler ve gelişim', journal: 'Öğün günlüğü', energy: 'Enerji hesabı' }
  const caption = { tracking: 'Ölçümleri karşılaştır ve dönem raporunu hazırla.', journal: 'Planlanan öğünleri danışanın kayıtlarıyla karşılaştır.', energy: 'Son ölçümle bazal metabolizma, günlük ihtiyaç ve hedef enerjiyi hesapla.' }

  function changeClient(id: string) {
    setSelection(id)
    setEdit(null)
    setPrint(false)
    action.clearError()
  }

  function removeMeasurement(row: Measurement) {
    if (!confirm('Ölçüm silinsin mi?')) return
    void action.run(async () => {
      if (session.demo) {
        update(data => ({ ...data, measurements: data.measurements.filter(item => item.id !== row.id) }))
      } else {
        await measurementsApi.remove(client.id, row.id)
        setRevision(value => value + 1)
      }
    })
  }

  async function saveMeasurement(row: Omit<Measurement, 'id' | 'clientId'>) {
    if (!session.demo) {
      const payload: MeasurementInput = {
        measuredOn: row.date, weightKg: row.weight, heightCm: row.height, waistCm: row.waist,
        hipCm: row.hip ?? null, bodyFatPercent: row.fat ?? null, note: row.note ?? null,
      }
      if (edit === 'new') await measurementsApi.add(client.id, payload)
      else if (edit) await measurementsApi.update(client.id, edit.id, payload)
      setRevision(value => value + 1)
    } else {
      const updated = { ...row, clientId: client.id, id: edit && edit !== 'new' ? edit.id : newId() }
      if (measurementRows.some(item => item.id !== updated.id && item.date === updated.date)) {
        throw new Error('Bu tarihte zaten bir ölçüm var.')
      }
      update(data => ({ ...data, measurements: edit === 'new' ? [...data.measurements, updated] : data.measurements.map(item => item.id === updated.id ? updated : item) }))
    }
    setEdit(null)
  }

  return <>
    <Title title={labels[mode]} subtitle={caption[mode]} action={mode === 'tracking' && <button className="button primary" disabled={!client || !ready || action.busy} onClick={() => setEdit('new')}>＋ Ölçüm ekle</button>} />
    <section className="panel toolbar tracking-filters"><Field label="Danışan"><ClientPicker clientId={client?.id} clients={clients} change={changeClient} disabled={action.busy} /></Field>
      {mode !== 'energy' && <>
        <Field label="Başlangıç"><input aria-label="Başlangıç tarihi" type="date" value={from} max={to || undefined} onChange={event => {
          const date = event.target.value
          if (date ? !to || date <= to : !session.demo) setFrom(date)
        }} /></Field>
        <Field label="Bitiş"><input aria-label="Bitiş tarihi" type="date" value={to} min={from || undefined} max={session.demo ? dateKey() : undefined} onChange={event => {
          const date = event.target.value
          if (date ? (!from || date >= from) && (!session.demo || date <= dateKey()) : !session.demo) setTo(date)
        }} /></Field>
      </>}
      {mode === 'tracking' && <button className="button secondary" disabled={!client || (session.demo ? !numDays : !measurements.length)} onClick={() => setPrint(true)}>PDF raporu</button>}
    </section>
    {!client ? <Empty>Önce bir danışan ekle.</Empty> : !ready ? <Wait error={live.error} reload={live.reload} /> : <>
    {mode === 'tracking' && <>
      <section className="panel padded"><div className="summary-grid">
        <div><span>Son kilo</span><strong>{latest ? decimal(latest.weight) + ' kg' : '—'}</strong></div>
        <div><span>Hedefe fark</span><strong>{remaining == null ? '—' : decimal(remaining) + ' kg'}</strong></div>
        {session.demo ? <><div><span>Plan uyumu (demo)</span><strong>{adherence === null ? '—' : '%' + decimal(adherence)}</strong></div><div><span>Kayıt sıklığı</span><strong>{'%' + decimal(frequency)}</strong></div></>
          : <div><span>Hedef kilo</span><strong>{target == null ? '—' : decimal(target) + ' kg'}</strong></div>}
      </div></section>
      <div className="overview-grid equal"><section className="panel padded"><h2>Kilo eğilimi</h2><Trend data={measurements.map(row => ({ date: row.date, value: row.weight }))} target={target ?? undefined} /></section><section className="panel padded"><h2>Bel çevresi eğilimi</h2><Trend unit="cm" data={measurements.map(row => ({ date: row.date, value: row.waist }))} /></section></div>
      <section className="panel"><header className="section-header"><h2>Ölçüm geçmişi</h2></header><Message>{action.error}</Message>
        <Table heads={['Tarih', 'Kilo', 'Değişim', 'Boy', 'Bel', 'Kalça', 'Yağ', 'Not', '']}>{measurements.map(row => {
          const previous = session.demo ? measurementRows[measurementRows.indexOf(row) - 1] : null
          const change = previous ? row.weight - previous.weight : row.weightChange
          return <tr key={row.id}>
            <td>{dateLabel(row.date)}</td><td>{decimal(row.weight)} kg</td>
            <td>{change == null ? '—' : change.toLocaleString('tr-TR', { signDisplay: 'always', maximumFractionDigits: 2 }) + ' kg'}</td>
            <td>{decimal(row.height)} cm</td><td>{decimal(row.waist)} cm</td>
            <td>{row.hip === undefined ? '—' : decimal(row.hip) + ' cm'}</td>
            <td>{row.fat === undefined ? '—' : '%' + decimal(row.fat)}</td><td>{row.note ?? '—'}</td>
            <td className="row-actions"><button className="text-button" disabled={action.busy} onClick={() => setEdit(row)}>Düzenle</button><button className="text-button danger-text" disabled={action.busy} onClick={() => removeMeasurement(row)}>Sil</button></td>
          </tr>
        })}</Table>{!measurements.length && <Empty>Bu dönemde ölçüm bulunamadı.</Empty>}</section>
    </>}
    {mode === 'journal' && <section className="panel"><header className="section-header"><div><h2>{client.fullName}</h2><p className="muted">{currentPlan ? currentPlan.title : 'Yayımlanmış plan bulunmuyor'}</p></div></header><Table heads={['Tarih', 'Planlanan öğün', 'Gerçekleşen', 'Not']}>{journal.map(({ key, date, meal, log }) => <tr key={key}><td>{dateLabel(date)}</td><td>{meal.name} · {meal.time}</td><td><Badge tone={log?.status === 'Ate' ? 'green' : log?.status === 'Partial' ? 'amber' : 'gray'}>{log ? logStatuses[log.status] : 'Kayıt yok'}</Badge></td><td>{log?.note || '—'}</td></tr>)}</Table>{!journal.length && <Empty>Seçili dönemde planlanmış öğün bulunamadı.</Empty>}</section>}
    {mode === 'energy' && <section className="panel padded"><h2>Hesaplama parametreleri</h2>{!latest && <Message type="info">Bu danışan için önce bir ölçüm ekle.</Message>}<form className="form" onSubmit={event => {
      const inputs = values(event)
      if (!latest || !client.birthDate || !client.sex) return
      const today = dateKey()
      const age = Number(today.slice(0, 4)) - Number(client.birthDate.slice(0, 4)) - Number(today.slice(5) < client.birthDate.slice(5))
      const male = client.sex === 'Male'
      const bmr = inputs.formula === 'harris'
        ? male ? 88.362 + 13.397 * latest.weight + 4.799 * latest.height - 5.677 * age : 447.593 + 9.247 * latest.weight + 3.098 * latest.height - 4.330 * age
        : 10 * latest.weight + 6.25 * latest.height - 5 * age + (male ? 5 : -161)
      const daily = bmr * Number(inputs.activity)
      setResult({ clientId: client.id, bmr, daily, target: daily + Number(inputs.adjustment) })
    }}><div className="form-grid"><Field label="Formül"><select name="formula"><option value="mifflin">Mifflin–St Jeor</option><option value="harris">Harris–Benedict</option></select></Field><Field label="Aktivite katsayısı"><input name="activity" type="number" min="1.2" max="1.9" step=".01" defaultValue="1.55" required /></Field><Field label="Hedef düzeltmesi (kcal)"><input name="adjustment" type="number" min="-1500" max="1500" defaultValue="0" required /></Field></div><div><button className="button primary" disabled={!latest || !client.birthDate || !client.sex}>Hesapla</button></div></form>
    {result?.clientId === client.id && <div className="summary-grid space-top">{[['Bazal metabolizma', result.bmr], ['Günlük ihtiyaç', result.daily], ['Hedef enerji', result.target]].map(([name, amount]) => <div key={name}><span>{name}</span><strong>{decimal(Number(amount))} kcal</strong></div>)}</div>}<p className="muted">Hesaplamalar tahminidir, profesyonel değerlendirme yerine geçmez.</p></section>}
    </>}
    {edit && client && <MeasurementForm measurement={edit === 'new' ? undefined : edit} latestDate={session.demo ? dateKey() : undefined} close={() => setEdit(null)} save={saveMeasurement} />}
    {print && client && <Modal title="Dönemsel ilerleme raporu" close={() => setPrint(false)}>
      <div className="report-body"><h2>{client.fullName}</h2>
        <p>{dateLabel(from || (measurements[0]?.date ?? ''))} – {dateLabel(to || (measurements.at(-1)?.date ?? ''))}</p>
        {session.demo && <p>Plan uyumu {adherence === null ? '—' : '%' + decimal(adherence)} · Kayıt sıklığı %{decimal(frequency)}</p>}
        <Table heads={['Tarih','Kilo (kg)','Boy (cm)','Bel (cm)']}>{measurements.map(row => <tr key={row.id}><td>{dateLabel(row.date)}</td><td>{decimal(row.weight)}</td><td>{decimal(row.height)}</td><td>{decimal(row.waist)}</td></tr>)}</Table>
      </div><div className="form-actions"><button className="button primary" onClick={() => window.print()}>Yazdır / PDF olarak kaydet</button></div>
    </Modal>}
  </>
}

const measurementFields = [
  ['weight', 'Kilo (kg)', 20, 350, true],
  ['height', 'Boy (cm)', 100, 250, true],
  ['waist', 'Bel (cm)', 30, 250, true],
  ['hip', 'Kalça (cm)', 30, 250, false],
  ['fat', 'Yağ oranı (%)', 2, 75, false],
] as const

function MeasurementForm({ measurement, latestDate, close, save }: {
  measurement?: Measurement; latestDate?: string; close: () => void;
  save: (row: Omit<Measurement, 'id' | 'clientId'>) => Promise<void>;
}) {
  const action = useAction()
  return <Modal title={measurement ? 'Ölçümü düzenle' : 'Yeni ölçüm'} close={() => { if (!action.busy) close() }}><form className="form" onSubmit={event => {
    const data = values(event)
    void action.run(() => save({
      date: data.date, weight: Number(data.weight), height: Number(data.height), waist: Number(data.waist),
      hip: data.hip ? Number(data.hip) : undefined, fat: data.fat ? Number(data.fat) : undefined,
      note: data.note.trim() || undefined,
    }))
  }}>
    <Field label="Ölçüm tarihi"><input name="date" type="date" max={latestDate} defaultValue={measurement?.date ?? dateKey()} required /></Field>
    <div className="form-grid">{measurementFields.map(([name, label, min, max, required]) => (
      <Field label={label} key={name}><input name={name} type="number" step="any" min={min} max={max} defaultValue={measurement?.[name]} required={required} /></Field>
    ))}</div>
    <Field label="Not"><textarea name="note" maxLength={300} defaultValue={measurement?.note} /></Field>
    <Message>{action.error}</Message><div className="form-actions"><button className="button primary" disabled={action.busy}>{action.busy ? 'Kaydediliyor…' : 'Kaydet'}</button></div>
  </form></Modal>
}
