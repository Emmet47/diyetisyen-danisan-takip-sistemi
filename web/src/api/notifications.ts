// J. Bildirim: /api/notifications (sd-4)
import { api, query } from './http'
import type { Notification, Paged } from './models'

export const notificationsApi = {
  /** En yenisi önce, sayfalı. */
  list: (page = 1, pageSize = 20) => api.get<Paged<Notification>>(query('/notifications', { page, pageSize })),
  markRead: (id: string) => api.post(`/notifications/${encodeURIComponent(id)}/read`),
}
