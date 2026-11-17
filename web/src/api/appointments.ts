// I. Randevu (diyetisyen tarafı): /api/working-hours ve /api/appointments (sd-7)
import { api, query } from './http'
import type { Appointment, WorkingHour } from './models'

const path = (id: string) => `/appointments/${encodeURIComponent(id)}`

export const appointmentsApi = {
  workingHours: () => api.get<WorkingHour[]>('/working-hours'),
  setWorkingHours: (hours: WorkingHour[]) => api.put('/working-hours', { hours }),
  /** [from, to) arasında başlayan randevular; zamanlar UTC. */
  list: (from: string, to: string) => api.get<Appointment[]>(query('/appointments', { from, to })),
  /** Doğrudan onaylı randevu; slot doluysa 409 "Bu saat az önce doldu". */
  create: (clientId: string, startsAt: string) => api.post<Appointment>('/appointments', { clientId, startsAt }),
  confirm: (id: string) => api.post<Appointment>(`${path(id)}/confirm`),
  cancel: (id: string) => api.post<Appointment>(`${path(id)}/cancel`),
  reschedule: (id: string, startsAt: string) => api.post<Appointment>(`${path(id)}/reschedule`, { startsAt }),
  complete: (id: string) => api.post<Appointment>(`${path(id)}/complete`),
}
