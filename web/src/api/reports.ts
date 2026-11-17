// K. Rapor: ilerleme paneli verisi ve PDF (sd-6)
import { api, query } from './http'
import type { ProgressReport } from './models'

type Period = { from?: string; to?: string }
const path = (clientId: string) => `/clients/${encodeURIComponent(clientId)}`

export const reportsApi = {
  /** Ölçüm eğilimi, plan uyumu (%) ve kayıt sıklığı (%); varsayılan son 30 gün (FR-K1, FR-K3). */
  progress: (clientId: string, period: Period) => api.get<ProgressReport>(query(`${path(clientId)}/progress`, period)),
  /** PDF token ister: düz bağlantı yerine token'lı istekle indirilir (FR-K2). */
  downloadPdf: (clientId: string, period: Period) =>
    api.download(query(`${path(clientId)}/report.pdf`, period), 'ilerleme-raporu.pdf'),
}
