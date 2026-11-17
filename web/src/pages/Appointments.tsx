import { useState } from 'react'
import { appointmentsApi } from '../api/appointments'
import { clientsApi } from '../api/clients'
import { appointmentLabels } from '../api/models'
import type { Appointment, ClientSummary, WorkingHour } from '../api/models'
import { dateKey, dateLabel, dayStartUtc, fullTime, localTime, moveDate, shortTime, timeLabel } from '../format'
import { Badge, Empty, Field, Message, Modal, Table, Title, Wait, useAction, useLoad, values } from '../ui'
import type { PageProps } from '../ui'

const weekDays = ['Pazartesi', 'Salı', 'Çarşamba', 'Perşembe', 'Cuma', 'Cumartesi', 'Pazar']
const minutes = (value: string) => Number(value.slice(0, 2)) * 60 + Number(value.slice(3, 5))
const isActive = (row: Appointment) => row.status === 'Requested' || row.status === 'Confirmed'
/** 1 = Pazartesi … 7 = Pazar (API'deki weekday ile aynı). */
const weekday = (date: string) => (new Date(date + 'T12:00:00Z').getUTCDay() + 6) % 7 + 1

/**
 * Seçilen gün için boş 30 dakikalık slotlar: çalışma saati → slotlar, sonra geçmiş ve dolu (talep edildi/onaylı)
 * slotlar çıkarılır (FR-I1). Son kontrolü yine API yapar; arada biri aynı saati alırsa 409 "Bu saat az önce doldu".
 */
function freeSlots(date: string, hours: WorkingHour[], taken: Appointment[], now: number, ignoreId?: string) {
  return hours.filter(row => row.weekday === weekday(date)).flatMap(row => {
    const start = minutes(row.startTime), end = minutes(row.endTime)
    return Array.from({ length: Math.floor((end - start) / 30) }, (_, i) => {
      const value = start + i * 30
      const time = String(Math.floor(value / 60)).padStart(2, '0') + ':' + String(value % 60).padStart(2, '0')
      return { time, at: localTime(date, time) }
    })
  }).filter(slot => Date.parse(slot.at) > now && !taken.some(row => row.id !== ignoreId && isActive(row) && Math.abs(Date.parse(row.startsAt) - Date.parse(slot.at)) < 30 * 60000))
}

export default function Appointments(_props: PageProps) {
  const today = dateKey()
  const [date, setDate] = useState('')
  const [edit, setEdit] = useState<Appointment | 'new' | null>(null)
  const [hours, setHours] = useState(false)
  const [notice, setNotice] = useState('')
  // Gün seçilmediyse geçen haftadan (tamamlanacak randevular için) 30 gün sonrasına kadar.
  const from = date || moveDate(today, -7)
  const to = date ? moveDate(date, 1) : moveDate(today, 31)
  const list = useLoad(() => appointmentsApi.list(dayStartUtc(from), dayStartUtc(to)), 'appointments:' + from + to)
  const working = useLoad(() => appointmentsApi.workingHours(), 'working-hours')
  const clients = useLoad(() => clientsApi.list(true), 'appointment-clients')
  const [now] = useState(() => Date.now())
  const action = useAction(list.reload)
  const rows = [...(list.data ?? [])].sort((a, b) => a.startsAt.localeCompare(b.startsAt))
  function change(task: () => Promise<unknown>, text: string) {
    setNotice('')
    void action.run(async () => { await task(); setNotice(text + ' Danışana bildirim gönderildi.') })
  }
  return <><Title title="Randevular" subtitle="Görüşmeleri planla, talepleri onayla ve çalışma saatlerini belirle." action={<div className="row-actions"><button className="button secondary" onClick={() => setHours(true)}>Çalışma saatleri</button><button className="button primary" onClick={() => setEdit('new')}>＋ Randevu oluştur</button></div>} />
    <Message>{action.error}</Message><Message type="success">{notice}</Message>
    {working.data && !working.data.length && <Message type="info">Henüz çalışma saati tanımlamadın. Danışanlar boş saat göremez; önce “Çalışma saatleri”ni doldur.</Message>}
    <section className="panel"><div className="toolbar"><Field label="Randevu günü"><input type="date" value={date} onChange={event => setDate(event.target.value)} /></Field>{date && <button className="text-button" onClick={() => setDate('')}>Tüm günler</button>}<span className="muted">Saatler Türkiye saatiyle gösterilir.</span></div>
      {!list.data ? <Wait error={list.error} reload={list.reload} /> : <><Table heads={['Danışan', 'Tarih', 'Saat', 'Durum', '']}>{rows.map(row => <tr key={row.id}><td><strong>{row.clientName ?? '—'}</strong>{row.note && <small className="cell-note">{row.note}</small>}</td><td>{dateLabel(row.startsAt)}</td><td>{timeLabel(row.startsAt)}–{timeLabel(row.endsAt)}</td><td><Badge tone={row.status === 'Requested' ? 'amber' : row.status === 'Cancelled' ? 'gray' : 'green'}>{appointmentLabels[row.status]}</Badge></td><td className="row-actions">
        {row.status === 'Requested' && <button className="text-button" disabled={action.busy} onClick={() => change(() => appointmentsApi.confirm(row.id), 'Randevu onaylandı.')}>Onayla</button>}
        {row.status === 'Confirmed' && Date.parse(row.endsAt) <= now && <button className="text-button" disabled={action.busy} onClick={() => { setNotice(''); void action.run(async () => { await appointmentsApi.complete(row.id); setNotice('Randevu tamamlandı.') }) }}>Tamamla</button>}
        {isActive(row) && <><button className="text-button" onClick={() => setEdit(row)}>Ertele</button><button className="text-button danger-text" disabled={action.busy} onClick={() => { if (confirm('Randevu iptal edilsin mi?')) change(() => appointmentsApi.cancel(row.id), 'Randevu iptal edildi.') }}>İptal</button></>}
      </td></tr>)}</Table>{!rows.length && <Empty>Randevu bulunamadı.</Empty>}</>}</section>
    {edit && <AppointmentForm item={edit === 'new' ? undefined : edit} clients={clients.data ?? []} hours={working.data ?? []} now={now} close={() => setEdit(null)} saved={text => { setEdit(null); setNotice(text); list.reload() }} />}
    {hours && (working.data ? <HoursForm current={working.data} close={() => setHours(false)} saved={() => { setHours(false); setNotice('Çalışma saatleri kaydedildi.'); working.reload() }} /> : <Modal title="Haftalık çalışma saatleri" close={() => setHours(false)}><Wait error={working.error} reload={working.reload} /></Modal>)}
  </>
}

function AppointmentForm({ item, clients, hours, now, close, saved }: { item?: Appointment; clients: ClientSummary[]; hours: WorkingHour[]; now: number; close: () => void; saved: (text: string) => void }) {
  const action = useAction()
  const [date, setDate] = useState(() => item && dateKey(new Date(item.startsAt)) > dateKey() ? dateKey(new Date(item.startsAt)) : dateKey())
  const taken = useLoad(() => appointmentsApi.list(dayStartUtc(date), dayStartUtc(moveDate(date, 1))), 'taken:' + date)
  const available = taken.data ? freeSlots(date, hours, taken.data, now, item?.id) : []
  return <Modal title={item ? 'Randevuyu ertele' : 'Yeni randevu (doğrudan onaylı)'} close={close}><form className="form" onSubmit={event => {
    const data = values(event)
    void action.run(async () => {
      if (item) {
        await appointmentsApi.reschedule(item.id, data.startsAt)
        saved('Randevu ertelendi. Danışana bildirim gönderildi.')
      } else {
        await appointmentsApi.create(data.clientId, data.startsAt)
        saved('Randevu oluşturuldu.')
      }
    })
  }}>
    {item ? <p>{item.clientName} · şu anki saat {dateLabel(item.startsAt)} {timeLabel(item.startsAt)}</p> : <Field label="Danışan"><select name="clientId" required defaultValue=""><option value="">Danışan seç</option>{clients.map(row => <option key={row.id} value={row.id}>{row.fullName}</option>)}</select></Field>}
    <Field label="Tarih"><input type="date" min={dateKey()} value={date} onChange={event => { if (event.target.value) setDate(event.target.value) }} required /></Field>
    {!taken.data ? <Wait error={taken.error} reload={taken.reload} /> : <Field label="Saat (Türkiye, 30 dakika)"><select key={date} name="startsAt" defaultValue="" required><option value="">{available.length ? 'Boş saat seç' : 'Uygun saat yok'}</option>{available.map(slot => <option value={slot.at} key={slot.at}>{slot.time}</option>)}</select></Field>}
    <Message>{action.error}</Message><div className="form-actions"><button className="button primary" disabled={action.busy || !available.length}>Kaydet</button></div>
  </form></Modal>
}

function HoursForm({ current, close, saved }: { current: WorkingHour[]; close: () => void; saved: () => void }) {
  const action = useAction()
  return <Modal title="Haftalık çalışma saatleri" close={close}><form className="form" onSubmit={event => {
    const data = values(event)
    void action.run(async () => {
      const rows = weekDays.flatMap((name, i) => {
        const day = i + 1
        if (!data['day' + day]) return []
        const start = data['start' + day], end = data['end' + day]
        if (!start || !end || minutes(end) - minutes(start) < 30 || minutes(start) % 30 || minutes(end) % 30) throw new Error(name + ' için yarım saat sınırlarında geçerli bir aralık gir.')
        return [{ weekday: day, startTime: fullTime(start), endTime: fullTime(end) }]
      })
      await appointmentsApi.setWorkingHours(rows)
      saved()
    })
  }}><p className="muted">Her gün için tek aralık girilir; randevular 30 dakikalık slotlara bölünür.</p>{weekDays.map((name, i) => { const day = i + 1; const row = current.find(item => item.weekday === day)
    return <div className="hours-line" key={name}><label className="check"><input name={'day' + day} type="checkbox" defaultChecked={!!row} />{name}</label><input aria-label={name + ' başlangıç'} type="time" name={'start' + day} step={1800} defaultValue={row ? shortTime(row.startTime) : '09:00'} /><input aria-label={name + ' bitiş'} type="time" name={'end' + day} step={1800} defaultValue={row ? shortTime(row.endTime) : '17:00'} /></div>
  })}<Message>{action.error}</Message><div className="form-actions"><button className="button primary" disabled={action.busy}>Kaydet</button></div></form></Modal>
}
