// C. Ölçüm: /api/clients/{id}/measurements
import { api } from './http'
import type { MeasurementHistory, MeasurementInput } from './models'

const path = (clientId: string) => `/clients/${encodeURIComponent(clientId)}/measurements`

export const measurementsApi = {
  list: (clientId: string) => api.get<MeasurementHistory>(path(clientId)),
  /** Aynı gün ikinci ölçüm 409 döner: o günkü ölçüm düzenlenmeli. */
  add: (clientId: string, input: MeasurementInput) => api.post<{ id: string }>(path(clientId), input),
  update: (clientId: string, measurementId: string, input: MeasurementInput) =>
    api.put(`${path(clientId)}/${encodeURIComponent(measurementId)}`, input),
  remove: (clientId: string, measurementId: string) =>
    api.delete(`${path(clientId)}/${encodeURIComponent(measurementId)}`),
}
