// L. Abonelik (diyetisyen): /api/subscription
import { api } from './http'
import type { Package, Subscription } from './models'

export const subscriptionApi = {
  get: () => api.get<Subscription>('/subscription'),
  packages: () => api.get<Package[]>('/subscription-plans'),
  /** Paket yükseltme/düşürme; düşürmede aktif danışan yeni limiti aşıyorsa 409 (FR-L3). */
  change: (subscriptionPlanId: string) => api.post<Subscription>('/subscription/change', { subscriptionPlanId }),
  /** Ödeme simülasyonuyla 1 ay uzatma (FR-L4). */
  renew: () => api.post<Subscription>('/subscription/renew'),
}
