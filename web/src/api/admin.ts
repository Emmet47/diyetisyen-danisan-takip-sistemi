// L. Yönetici: /api/admin
import { api, query } from './http'
import type { AuditLog, DietitianAdmin, DietitianUsage, Package, Paged } from './models'

type Page = { page: number; pageSize: number }
export type AuditFilter = { from?: string; to?: string; userId?: string; action?: string }
export type PackageInput = Omit<Package, 'id'>

export const adminApi = {
  dietitians: (paging: Page) => api.get<Paged<DietitianAdmin>>(query('/admin/dietitians', paging)),
  activate: (id: string) => api.post(`/admin/dietitians/${encodeURIComponent(id)}/activate`),
  deactivate: (id: string) => api.post(`/admin/dietitians/${encodeURIComponent(id)}/deactivate`),
  packages: () => api.get<Package[]>('/admin/subscription-plans'),
  createPackage: (input: PackageInput) => api.post<Package>('/admin/subscription-plans', input),
  updatePackage: (id: string, input: PackageInput) => api.put<Package>(`/admin/subscription-plans/${encodeURIComponent(id)}`, input),
  usage: (paging: Page) => api.get<Paged<DietitianUsage>>(query('/admin/usage', paging)),
  auditLogs: (paging: Page, filter: AuditFilter) => api.get<Paged<AuditLog>>(query('/admin/audit-logs', { ...paging, ...filter })),
}
