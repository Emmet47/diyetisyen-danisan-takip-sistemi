// B. Danışan: /api/clients
import { api, query } from './http'
import type { ClientDetail, ClientInput, ClientSummary, HistoryEntry, NewClientInput, RestrictionType, Usage } from './models'

const path = (id: string) => `/clients/${encodeURIComponent(id)}`

export const clientsApi = {
  list: (onlyActive = false) => api.get<ClientSummary[]>(query('/clients', { active: onlyActive || undefined })),
  get: (id: string) => api.get<ClientDetail>(path(id)),
  /** Paket limiti doluysa 409 (FR-L2); yanıttaki usage ile %80 uyarısı gösterilir. */
  add: (input: NewClientInput) => api.post<{ id: string; usage: Usage }>('/clients', input),
  update: (id: string, input: ClientInput) => api.put(path(id), input),
  remove: (id: string) => api.delete(path(id)),
  setRestrictions: (id: string, restrictions: { foodItemId: string; type: RestrictionType }[]) =>
    api.put(`${path(id)}/restrictions`, { restrictions }),
  history: (id: string) => api.get<HistoryEntry[]>(`${path(id)}/history`),
  deactivate: (id: string) => api.post(`${path(id)}/deactivate`),
  activate: (id: string) => api.post(`${path(id)}/activate`),
}
