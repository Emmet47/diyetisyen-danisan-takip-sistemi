import { useState } from 'react'
import type { Notification } from '../api/models'
import { notificationsApi } from '../api/notifications'
import { dateLabel, timeLabel } from '../format'
import { Badge, Empty, Message, Pager, Title, Wait, useAction, useLoad } from '../ui'
import type { PageProps } from '../ui'

/** Bildirim türüne göre açılacak panel sayfası. */
const targets: Record<string, [page: string, label: string]> = {
  AppointmentRequested: ['appointments', 'Randevulara git'],
  AppointmentConfirmed: ['appointments', 'Randevulara git'],
  AppointmentCancelled: ['appointments', 'Randevulara git'],
  AppointmentRescheduled: ['appointments', 'Randevulara git'],
  AppointmentReminder: ['appointments', 'Randevulara git'],
  PlanPublished: ['plans', 'Planlara git'],
  MealLogReminder: ['journal', 'Öğün günlüğüne git'],
  MeasurementReminder: ['tracking', 'Ölçümlere git'],
}

/** J. Bildirimler: randevu talepleri ve onay/iptal/erteleme bildirimleri (sd-4, sd-7). Tıklanınca okundu olur ve ilgili sayfa açılır. */
export default function Notifications({ changed, navigate }: PageProps & { changed: () => void; navigate: (page: string) => void }) {
  const [page, setPage] = useState(1)
  const list = useLoad(() => notificationsApi.list(page, 20), 'notifications:' + page)
  const action = useAction(() => { list.reload(); changed() })
  function open(item: Notification) {
    const target = targets[item.type]
    void action.run(async () => {
      if (!item.isRead) await notificationsApi.markRead(item.id)
      if (target) navigate(target[0])
    })
  }
  return <><Title title="Bildirimler" subtitle="Randevu taleplerini ve hatırlatmaları takip et. Bildirime tıklayınca ilgili sayfa açılır." />
    <Message>{action.error}</Message>
    {!list.data ? <Wait error={list.error} reload={list.reload} /> : <section className="panel padded stack">
      {list.data.items.map(item => <article className="record list-line clickable" key={item.id} role="button" tabIndex={0} onClick={() => open(item)} onKeyDown={event => { if (event.key === 'Enter') open(item) }}>
        <div><small className="muted">{dateLabel(item.createdAt)} · {timeLabel(item.createdAt)}</small><h3>{item.title}</h3><p>{item.body}</p>{targets[item.type] && <small className="cell-note">{targets[item.type][1]} →</small>}</div>
        {item.isRead ? <Badge tone="gray">Okundu</Badge> : <button className="button secondary" disabled={action.busy} onClick={event => { event.stopPropagation(); void action.run(() => notificationsApi.markRead(item.id)) }}>Okundu işaretle</button>}
      </article>)}
      {!list.data.items.length && <Empty>Bildirim bulunmuyor.</Empty>}
      <Pager {...list.data} change={setPage} />
    </section>}
  </>
}
