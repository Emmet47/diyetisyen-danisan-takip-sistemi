// A. Kimlik: /api/auth (herkese açık uçlar)
import { api } from './http'
import type { LoginResult, Package } from './models'

export const authApi = {
  login: (email: string, password: string) => api.post<LoginResult>('/auth/login', { email, password }),
  /** Diyetisyen hesabı açar ve paket seçer; yanıt girişle aynıdır (FR-A1). */
  register: (input: { fullName: string; email: string; password: string; phone: string | null; subscriptionPlanId: string }) =>
    api.post<LoginResult>('/auth/register', input),
  /** E-posta kayıtlı olsun olmasın 202 döner (FR-A3). */
  forgotPassword: (email: string) => api.post('/auth/forgot-password', { email }),
  /** Davet ve sıfırlama bağlantısındaki token ile yeni parola. */
  resetPassword: (token: string, newPassword: string) => api.post('/auth/reset-password', { token, newPassword }),
  /** Satıştaki paketler (kayıt ekranı). */
  packages: () => api.get<Package[]>('/subscription-plans'),
}
