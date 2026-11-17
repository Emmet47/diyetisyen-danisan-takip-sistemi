import { useState } from 'react'
import type { Appointment, WorkHour } from '../api/models'
import { dateKey, dateLabel, localTime, newId, timeLabel } from '../demo'
import type { DemoProps } from './Clients'
import { appointmentLabels } from './Overview'
import { Badge, Empty, Field, Message, Modal, Table, Title, useAction, values } from '../ui'

const weekDays = ['Pazartesi', 'Salı', 'Çarşamba', 'Perşembe', 'Cuma', 'Cumartesi', 'Pazar']
const minutes = (value: string) => Number(value.slice(0, 2)) * 60 + Number(value.slice(3))
const active = (row: Appointment) => row.status === 'Requested' || row.status === 'Confirmed'
const dayIndex = (value: string) => (new Date(value + 'T12:00:00Z').getUTCDay() + 6) % 7 + 1

export default function Appointments({ demo, update }: DemoProps) {
  const [date, setDate] = useState('')
  const [edit, setEdit] = useState<Appointment | 'new' | null>(null)
  const [hours, setHours] = useState(false)
  const rows = demo.appointments.filter(item => !date || dateKey(new Date(item.startsAt)) === date).sort((a, b) => a.startsAt.localeCompare(b.startsAt))
  function change(row: Appointment, status: Appointment['status']) {
    update(data => ({ ...data, appointments: data.appointments.map(item => item.id === row.id ? { ...item, status } : item) }))
  }
  return <><Title title="Randevular" subtitle="Görüşmeleri planla, talepleri onayla ve çalışma saatlerini belirle." action={<div className="row-actions"><button className="button secondary" onClick={() => setHours(true)}>Çalışma saatleri</button><button className="button primary" onClick={() => setEdit('new')}>＋ Randevu oluştur</button></div>} />
    <section className="panel"><div className="toolbar"><Field label="Randevu günü"><input type="date" value={date} onChange={event => setDate(event.target.value)} /></Field>{date && <button className="text-button" onClick={() => setDate('')}>Tüm günler</button>}</div><Table heads={['Danışan', 'Tarih', 'Saat', 'Durum', '']}>{rows.map(row => <tr key={row.id}><td><strong>{demo.clients.find(client => client.id === row.clientId)?.fullName ?? '—'}</strong></td><td>{dateLabel(row.startsAt)}</td><td>{timeLabel(row.startsAt)}</td><td><Badge tone={row.status === 'Requested' ? 'amber' : row.status === 'Cancelled' ? 'gray' : 'green'}>{appointmentLabels[row.status]}</Badge></td><td className="row-actions">
      {row.status === 'Requested' && <button className="text-button" onClick={() => change(row, 'Confirmed')}>Onayla</button>}
      {row.status === 'Confirmed' && Date.parse(row.startsAt) + 30 * 60000 <= Date.now() && <button className="text-button" onClick={() => change(row, 'Completed')}>Tamamla</button>}
      {active(row) && <><button className="text-button" onClick={() => setEdit(row)}>Ertele</button><button className="text-button danger-text" onClick={() => { if (confirm('Randevu iptal edilsin mi?')) change(row, 'Cancelled') }}>İptal</button></>}
    </td></tr>)}</Table>{!rows.length && <Empty>Randevu bulunamadı.</Empty>}</section>
    {edit && <AppointmentForm item={edit === 'new' ? undefined : edit} demo={demo} close={() => setEdit(null)} save={row => {
      update(data => ({ ...data, appointments: edit === 'new' ? [...data.appointments, row] : data.appointments.map(item => item.id === row.id ? row : item) }))
      setEdit(null)
    }} />}
    {hours && <HoursForm current={demo.hours} close={() => setHours(false)} save={data => { update(demo => ({ ...demo, hours: data })); setHours(false) }} />}
  </>
}
function AppointmentForm({ item, demo, close, save }: { item?: Appointment; demo: DemoProps['demo']; close: () => void; save: (row: Appointment) => void }) {
  const action = useAction()
  const [date, setDate] = useState(() => item && dateKey(new Date(item.startsAt)) > dateKey() ? dateKey(new Date(item.startsAt)) : dateKey())
  const available = demo.hours.filter(row => row.day === dayIndex(date)).flatMap(row => {
    const start = minutes(row.start), end = minutes(row.end)
    return Array.from({ length: Math.floor((end - start) / 30) }, (_, i) => {
      const value = start + i * 30
      const time = String(Math.floor(value / 60)).padStart(2, '0') + ':' + String(value % 60).padStart(2, '0')
      return { time, at: localTime(date, time) }
    })
  }).filter(slot => Date.parse(slot.at) > Date.now() && !demo.appointments.some(row => row.id !== item?.id && active(row) && Math.abs(Date.parse(row.startsAt) - Date.parse(slot.at)) < 30 * 60000))
  return <Modal title={item ? 'Randevuyu ertele' : 'Yeni randevu'} close={close}><form className="form" onSubmit={event => {
    const data = values(event)
    void action.run(() => {
      if (!available.some(slot => slot.at === data.startsAt)) throw new Error('Uygun bir zaman dilimi seç.')
      save({ id: item?.id ?? newId(), clientId: item?.clientId ?? data.clientId, startsAt: data.startsAt, status: 'Confirmed' })
    })
  }}>
    {!item && <Field label="Danışan"><select name="clientId" required defaultValue=""><option value="">Danışan seç</option>{demo.clients.filter(row => row.isActive).map(row => <option key={row.id} value={row.id}>{row.fullName}</option>)}</select></Field>}
    <Field label="Tarih"><input type="date" min={dateKey()} value={date} onChange={event => { if (event.target.value) setDate(event.target.value) }} required /></Field>
    <Field label="Saat (Türkiye)"><select key={date} name="startsAt" defaultValue="" required><option value="">{available.length ? 'Boş saat seç' : 'Uygun saat yok'}</option>{available.map(slot => <option value={slot.at} key={slot.at}>{slot.time}</option>)}</select></Field>
    <Message>{action.error}</Message><div className="form-actions"><button className="button primary" disabled={action.busy || !available.length}>Kaydet</button></div>
  </form></Modal>
}
function HoursForm({ current, close, save }: { current: WorkHour[]; close: () => void; save: (rows: WorkHour[]) => void }) {
  const action = useAction()
  return <Modal title="Haftalık çalışma saatleri" close={close}><form className="form" onSubmit={event => {
    const data = values(event)
    void action.run(() => {
      const rows = weekDays.flatMap((name, i) => {
        const day = i + 1
        if (!data['day' + day]) return []
        const start = data['start' + day], end = data['end' + day]
        if (!start || !end || minutes(end) - minutes(start) < 30 || minutes(start) % 30 || minutes(end) % 30) throw new Error(name + ' için yarım saat sınırlarında geçerli bir aralık gir.')
        return [{ day, start, end }]
      })
      save(rows)
    })
  }}>{weekDays.map((name, i) => { const day = i + 1; const row = current.find(item => item.day === day)
    return <div className="hours-line" key={name}><label className="check"><input name={'day' + day} type="checkbox" defaultChecked={!!row} />{name}</label><input aria-label={name + ' başlangıç'} type="time" name={'start' + day} step={1800} defaultValue={row?.start ?? '09:00'} /><input aria-label={name + ' bitiş'} type="time" name={'end' + day} step={1800} defaultValue={row?.end ?? '17:00'} /></div>
  })}<Message>{action.error}</Message><div className="form-actions"><button className="button primary" disabled={action.busy}>Kaydet</button></div></form></Modal>
}
