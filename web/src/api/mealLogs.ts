// G. Öğün günlüğü (diyetisyen tarafı): planlanan-gerçekleşen ve fotoğraf bağlantısı (sd-5)
import { api, query } from './http'
import type { MealComparison, PhotoUrl } from './models'

export const mealLogsApi = {
  /** Gün gün planlanan öğünler ve danışanın kaydı; varsayılan 7 gün, en fazla 31 gün (FR-G3). */
  comparison: (clientId: string, period: { from?: string; to?: string }) =>
    api.get<MealComparison>(query(`/clients/${encodeURIComponent(clientId)}/meal-logs`, period)),
  /** 10 dakika geçerli imzalı fotoğraf bağlantısı (FR-G2). */
  photoUrl: (mealLogId: string) => api.get<PhotoUrl>(`/meal-logs/${encodeURIComponent(mealLogId)}/photo-url`),
}
