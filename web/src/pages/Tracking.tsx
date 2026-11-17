import { useState } from 'react'
import { clientsApi } from '../api/clients'
import { energyApi } from '../api/energy'
import { mealLogsApi } from '../api/mealLogs'
import { measurementsApi } from '../api/measurements'
import { formulaNames, mealLogNames } from '../api/models'
import type { ClientSummary, EnergyFormula, EnergyResult, Measurement, MeasurementInput } from '../api/models'
import { reportsApi } from '../api/reports'
import { dateKey, dateLabel, decimal, moveDate, shortTime } from '../format'
import { Trend } from './Overview'
import { Badge, Empty, Field, Message, Modal, Table, Title, Wait, useAction, useLoad, values } from '../ui'
import type { PageProps } from '../ui'

const twoDigits = (n: number) => decimal(n, 2)
function ClientPicker({ clients, clientId, change, disabled }: { clients: ClientSummary[]; clientId?: string; change: (id: string) => void; disabled: boolean }) {
  return <select aria-label="Danışan seç" value={clientId ?? ''} disabled={disabled} onChange={event => change(event.target.value)}>{!clients.length && <option value="">Danışan bulunamadı</option>}{clients.map(item => <option value={item.id} key={item.id}>{item.fullName}{item.isActive ? '' : ' (pasif)'}</option>)}</select>
}

type Mode = 'tracking' | 'journal' | 'energy'
type TrackingProps = PageProps & { mode: Mode }

const labels: Record<Mode, string> = { tracking: 'Ölçümler ve gelişim', journal: 'Öğün günlüğü', energy: 'Enerji hesabı' }
const captions: Record<Mode, string> = {
  tracking: 'Ölçümleri karşılaştır, plan uyumunu izle ve dönem raporunu PDF olarak indir.',
  journal: 'Planlanan öğünleri danışanın kayıtlarıyla gün gün karşılaştır.',
  energy: 'Son ölçümle bazal metabolizma, günlük ihtiyaç ve hedef enerjiyi hesapla.',
}

export default function Tracking(props: TrackingProps) {
  const clients = useLoad(() => clientsApi.list(), 'tracking-clients')
  if (!clients.data) return <><Title title={labels[props.mode]} /><Wait error={clients.error} reload={clients.reload} /></>
  return <TrackingContent {...props} clients={clients.data} />
}

function TrackingContent({ mode, clients }: TrackingProps & { clients: ClientSummary[] }) {
  const today = dateKey()
  const [selection, setSelection] = useState('')
  // Rapor ve günlük için varsayılan dönem: ölçümde son 30 gün, günlükte son 7 gün (API'nin varsayılanlarıyla aynı).
  const [from, setFrom] = useState(() => moveDate(today, mode === 'journal' ? -6 : -29))
  const [to, setTo] = useState(today)
  const client = clients.find(item => item.id === selection) ?? clients.find(item => item.isActive) ?? clients[0]
  function changeClient(id: string) { setSelection(id) }
  return <>
    <Title title={labels[mode]} subtitle={captions[mode]} />
    <section className="panel toolbar tracking-filters"><Field label="Danışan"><ClientPicker clientId={client?.id} clients={clients} change={changeClient} disabled={false} /></Field>
      {mode !== 'energy' && <>
        <Field label="Başlangıç"><input aria-label="Başlangıç tarihi" type="date" value={from} max={to} onChange={event => { if (event.target.value && event.target.value <= to) setFrom(event.target.value) }} /></Field>
        <Field label="Bitiş"><input aria-label="Bitiş tarihi" type="date" value={to} min={from} onChange={event => { if (event.target.value && event.target.value >= from) setTo(event.target.value) }} /></Field>
      </>}
    </section>
    {!client ? <Empty>Önce bir danışan ekle.</Empty>
      : mode === 'tracking' ? <MeasurementsView key={client.id} client={client} from={from} to={to} />
      : mode === 'journal' ? <JournalView key={client.id + from + to} client={client} from={from} to={to} />
      : <EnergyView key={client.id} client={client} />}
  </>
}

// ---- C. Ölçüm + K. Rapor (sd-6) ----
function MeasurementsView({ client, from, to }: { client: ClientSummary; from: string; to: string }) {
  const [edit, setEdit] = useState<Measurement | 'new' | null>(null)
  const history = useLoad(() => measurementsApi.list(client.id), 'measurements:' + client.id)
  const progress = useLoad(() => reportsApi.progress(client.id, { from, to }), 'progress:' + client.id + from + to)
  const action = useAction(() => { history.reload(); progress.reload() })
  const rows = history.data?.items ?? []
  const measurements = rows.filter(item => item.measuredOn >= from && item.measuredOn <= to)
  const latest = rows.at(-1)
  const report = progress.data

  async function saveMeasurement(input: MeasurementInput) {
    if (edit === 'new') await measurementsApi.add(client.id, input)
    else if (edit) await measurementsApi.update(client.id, edit.id, input)
    setEdit(null)
    history.reload(); progress.reload()
  }

  if (!history.data) return <Wait error={history.error} reload={history.reload} />
  return <>
    <div className="form-actions"><button className="button secondary" disabled={action.busy} onClick={() => void action.run(() => reportsApi.downloadPdf(client.id, { from, to }))}>PDF raporu indir</button><button className="button primary" disabled={action.busy} onClick={() => setEdit('new')}>＋ Ölçüm ekle</button></div>
    <Message>{action.error}</Message>
    <section className="panel padded"><div className="summary-grid">
      <div><span>Son kilo</span><strong>{latest ? twoDigits(latest.weightKg) + ' kg' : '—'}</strong></div>
      <div><span>Hedef kilo</span><strong>{history.data.targetWeightKg == null ? '—' : twoDigits(history.data.targetWeightKg) + ' kg'}</strong></div>
      <div><span>Hedefe kalan</span><strong>{history.data.remainingKg == null ? '—' : twoDigits(history.data.remainingKg) + ' kg'}</strong></div>
      <div><span>Plan uyumu</span><strong>{report?.adherencePercent == null ? '—' : '%' + decimal(report.adherencePercent)}</strong></div>
      <div><span>Öğün kayıt sıklığı</span><strong>{report ? '%' + decimal(report.logFrequencyPercent) : '—'}</strong></div>
    </div>
    {report && <p className="muted space-top">Dönemde {report.plannedMeals} planlı öğün: {report.eatenMeals} yedim, {report.partialMeals} kısmen, {report.skippedMeals} yemedim · {report.loggedDays}/{report.dayCount} gün kayıt var. Plan uyumu = (Yedim + 0,5 × Kısmen) ÷ planlı öğün × 100.</p>}
    {progress.error && <Message>{progress.error}</Message>}</section>
    <div className="overview-grid equal"><section className="panel padded"><h2>Kilo eğilimi</h2><Trend data={measurements.map(row => ({ date: row.measuredOn, value: row.weightKg }))} target={history.data.targetWeightKg ?? undefined} /></section><section className="panel padded"><h2>Bel çevresi eğilimi</h2><Trend unit="cm" data={measurements.map(row => ({ date: row.measuredOn, value: row.waistCm }))} /></section></div>
    <section className="panel"><header className="section-header"><h2>Ölçüm geçmişi</h2></header>
      <Table heads={['Tarih', 'Kilo', 'Değişim', 'Boy', 'Bel', 'Kalça', 'Yağ', 'Not', '']}>{measurements.map(row => <tr key={row.id}>
        <td>{dateLabel(row.measuredOn)}</td><td>{twoDigits(row.weightKg)} kg</td>
        <td>{row.weightChangeKg == null ? '—' : row.weightChangeKg.toLocaleString('tr-TR', { signDisplay: 'always', maximumFractionDigits: 2 }) + ' kg'}</td>
        <td>{twoDigits(row.heightCm)} cm</td><td>{twoDigits(row.waistCm)} cm</td>
        <td>{row.hipCm == null ? '—' : twoDigits(row.hipCm) + ' cm'}</td>
        <td>{row.bodyFatPercent == null ? '—' : '%' + twoDigits(row.bodyFatPercent)}</td><td>{row.note ?? '—'}</td>
        <td className="row-actions"><button className="text-button" disabled={action.busy} onClick={() => setEdit(row)}>Düzenle</button><button className="text-button danger-text" disabled={action.busy} onClick={() => {
          if (confirm('Ölçüm silinsin mi?')) void action.run(() => measurementsApi.remove(client.id, row.id))
        }}>Sil</button></td>
      </tr>)}</Table>{!measurements.length && <Empty>Bu dönemde ölçüm bulunamadı.</Empty>}</section>
    {edit && <MeasurementForm measurement={edit === 'new' ? undefined : edit} close={() => setEdit(null)} save={saveMeasurement} />}
  </>
}

const measurementFields = [
  ['weightKg', 'Kilo (kg)', 20, 350, true],
  ['heightCm', 'Boy (cm)', 100, 250, true],
  ['waistCm', 'Bel (cm)', 30, 250, true],
  ['hipCm', 'Kalça (cm)', 30, 250, false],
  ['bodyFatPercent', 'Yağ oranı (%)', 2, 75, false],
] as const

function MeasurementForm({ measurement, close, save }: { measurement?: Measurement; close: () => void; save: (input: MeasurementInput) => Promise<void> }) {
  const action = useAction()
  return <Modal title={measurement ? 'Ölçümü düzenle' : 'Yeni ölçüm'} close={() => { if (!action.busy) close() }}><form className="form" onSubmit={event => {
    const data = values(event)
    void action.run(() => save({
      measuredOn: data.measuredOn, weightKg: Number(data.weightKg), heightCm: Number(data.heightCm), waistCm: Number(data.waistCm),
      hipCm: data.hipCm ? Number(data.hipCm) : null, bodyFatPercent: data.bodyFatPercent ? Number(data.bodyFatPercent) : null,
      note: data.note.trim() || null,
    }))
  }}>
    <Field label="Ölçüm tarihi"><input name="measuredOn" type="date" max={dateKey()} defaultValue={measurement?.measuredOn ?? dateKey()} required /></Field>
    <div className="form-grid">{measurementFields.map(([name, label, min, max, required]) => (
      <Field label={label} key={name}><input name={name} type="number" step="any" min={min} max={max} defaultValue={measurement?.[name] ?? undefined} required={required} /></Field>
    ))}</div>
    <Field label="Not"><textarea name="note" maxLength={300} defaultValue={measurement?.note ?? ''} /></Field>
    <p className="muted">Aynı gün için tek ölçüm girilir; o gün ölçüm varsa onu düzenle.</p>
    <Message>{action.error}</Message><div className="form-actions"><button className="button primary" disabled={action.busy}>{action.busy ? 'Kaydediliyor…' : 'Kaydet'}</button></div>
  </form></Modal>
}

// ---- G. Planlanan–gerçekleşen öğünler (FR-G3) ve fotoğraf (sd-5) ----
function JournalView({ client, from, to }: { client: ClientSummary; from: string; to: string }) {
  const comparison = useLoad(() => mealLogsApi.comparison(client.id, { from, to }), 'journal:' + client.id + from + to)
  const action = useAction()
  const rows = comparison.data?.days.flatMap(day => day.meals.map(meal => ({ date: day.date, ...meal }))) ?? []
  function openPhoto(logId: string) {
    // Pencereyi tıklama anında açıyoruz (açılır pencere engelleyicisine takılmasın), bağlantı gelince adresini veriyoruz.
    const tab = window.open('', '_blank')
    void action.run(async () => {
      try {
        const photo = await mealLogsApi.photoUrl(logId)
        if (tab) tab.location.href = photo.url
        else window.location.assign(photo.url)
      } catch (error) {
        tab?.close()
        throw error
      }
    })
  }
  if (!comparison.data) return <Wait error={comparison.error} reload={comparison.reload} />
  return <section className="panel"><header className="section-header"><div><h2>{client.fullName}</h2><p className="muted">{dateLabel(comparison.data.from)} – {dateLabel(comparison.data.to)} · en fazla 31 gün</p></div></header>
    <Message>{action.error}</Message>
    <Table heads={['Tarih', 'Planlanan öğün', 'Gerçekleşen', 'Not', 'Fotoğraf']}>{rows.map(row => <tr key={row.date + row.mealId}>
      <td>{dateLabel(row.date)}</td><td>{row.name} · {shortTime(row.time)}</td>
      <td><Badge tone={row.log?.status === 'Eaten' ? 'green' : row.log?.status === 'Partial' ? 'amber' : 'gray'}>{row.log ? mealLogNames[row.log.status] : 'Kayıt yok'}</Badge></td>
      <td>{row.log?.note || '—'}</td>
      <td>{row.log?.hasPhoto ? <button className="text-button" disabled={action.busy} onClick={() => openPhoto(row.log!.id)}>Fotoğrafı aç</button> : '—'}</td>
    </tr>)}</Table>{!rows.length && <Empty>Seçili dönemde yayımlanmış plandan planlanmış öğün bulunamadı.</Empty>}
    <p className="muted padded">Fotoğraf bağlantısı 10 dakika geçerlidir; süresi dolunca yeniden aç.</p>
  </section>
}

// ---- H. Enerji (Strategy) ----
function EnergyView({ client }: { client: ClientSummary }) {
  const [result, setResult] = useState<EnergyResult | null>(null)
  const action = useAction()
  return <section className="panel padded"><h2>Hesaplama parametreleri</h2><form className="form" onSubmit={event => {
    const inputs = values(event)
    void action.run(async () => setResult(await energyApi.calculate({ clientId: client.id, formula: inputs.formula as EnergyFormula, activityFactor: Number(inputs.activity), adjustmentKcal: Number(inputs.adjustment) })))
  }}><div className="form-grid"><Field label="Formül"><select name="formula">{Object.entries(formulaNames).map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select></Field><Field label="Aktivite katsayısı (1,2–1,9)"><input name="activity" type="number" min="1.2" max="1.9" step=".01" defaultValue="1.55" required /></Field><Field label="Hedef düzeltmesi (kcal, ±1500)"><input name="adjustment" type="number" min="-1500" max="1500" defaultValue="0" required /></Field></div><div><button className="button primary" disabled={action.busy}>Hesapla</button></div></form>
    <Message>{action.error}</Message>
    {result && <><p className="muted space-top">{formulaNames[result.formula]} · Son ölçüm: {decimal(result.input.weightKg)} kg, {decimal(result.input.heightCm)} cm · {result.input.ageYears} yaş</p><div className="summary-grid space-top">{([['Bazal metabolizma (BMH)', result.bmrKcal], ['Günlük ihtiyaç', result.dailyNeedKcal], ['Hedef enerji', result.targetKcal]] as const).map(([name, amount]) => <div key={name}><span>{name}</span><strong>{decimal(amount)} kcal</strong></div>)}</div></>}
    <p className="muted space-top">{result?.disclaimer ?? 'Hesaplamalar tahminidir, profesyonel değerlendirme yerine geçmez.'}</p></section>
}
