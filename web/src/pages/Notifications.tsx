import { useState } from 'react'
import { notificationsApi } from '../api/notifications'
import { dateLabel, timeLabel } from '../format'
import { Badge, Empty, Message, Pager, Title, Wait, useAction, useLoad } from '../ui'
import type { PageProps } from '../ui'

/** J. Bildirimler: randevu talepleri ve onay/iptal/erteleme bildirimleri (sd-4, sd-7). */
export default function Notifications({ changed }: PageProps & { changed: () => void }) {
  const [page, setPage] = useState(1)
  const list = useLoad(() => notificationsApi.list(page, 20), 'notifications:' + page)
  const action = useAction(() => { list.reload(); changed() })
  return <><Title title="Bildirimler" subtitle="Randevu taleplerini ve hatırlatmaları takip et." />
    <Message>{action.error}</Message>
    {!list.data ? <Wait error={list.error} reload={list.reload} /> : <section className="panel padded stack">
      {list.data.items.map(item => <article className="record list-line" key={item.id}><div><small className="muted">{dateLabel(item.createdAt)} · {timeLabel(item.createdAt)}</small><h3>{item.title}</h3><p>{item.body}</p></div>{item.isRead ? <Badge tone="gray">Okundu</Badge> : <button className="button secondary" disabled={action.busy} onClick={() => void action.run(() => notificationsApi.markRead(item.id))}>Okundu işaretle</button>}</article>)}
      {!list.data.items.length && <Empty>Bildirim bulunmuyor.</Empty>}
      <Pager {...list.data} change={setPage} />
    </section>}
  </>
}
