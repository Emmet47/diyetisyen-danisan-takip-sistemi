// E. Plan: /api/plans (Builder, sd-2; yayımlama sd-4) ve F. Şablon: /api/templates (Prototype, sd-3)
import { api, query } from './http'
import type { PlanCreated, PlanInput, PlanListItem, PlanSummary, TemplateInput, TemplateListItem, TemplateSummary } from './models'

const plan = (id: string) => `/plans/${encodeURIComponent(id)}`
const template = (id: string) => `/templates/${encodeURIComponent(id)}`

export const plansApi = {
  list: (clientId?: string) => api.get<PlanListItem[]>(query('/plans', { clientId })),
  get: (id: string) => api.get<PlanSummary>(plan(id)),
  /** Hatalı plan 400 döner, bütün hatalar errors.genel'de; uyarılar 201 yanıtının warnings listesinde. */
  create: (input: PlanInput & { clientId: string }) => api.post<PlanCreated>('/plans', input),
  /** Yalnızca taslak düzenlenir (değilse 409). */
  update: (id: string, input: PlanInput) => api.put<PlanCreated>(plan(id), input),
  remove: (id: string) => api.delete(plan(id)),
  /** Alerjen içeren plan 400; eski yayımlı plan arşivlenir ve danışana bildirim gider. */
  publish: (id: string) => api.post<PlanSummary>(`${plan(id)}/publish`),
  archive: (id: string) => api.post<PlanSummary>(`${plan(id)}/archive`),
}

export const templatesApi = {
  list: () => api.get<TemplateListItem[]>('/templates'),
  get: (id: string) => api.get<TemplateSummary>(template(id)),
  create: (input: TemplateInput) => api.post<{ id: string }>('/templates', input),
  update: (id: string, input: TemplateInput) => api.put(template(id), input),
  remove: (id: string) => api.delete(template(id)),
  /** Şablonun bağımsız kopyası taslak plan olur; özgün şablon değişmez (FR-F2, FR-F3). */
  copyToClient: (id: string, clientId: string, title: string | null) =>
    api.post<PlanCreated>(`${template(id)}/copy-to-client`, { clientId, title }),
}
