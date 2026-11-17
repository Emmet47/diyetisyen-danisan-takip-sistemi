// D. Besin: /api/foods (sd-8)
import { api, query } from './http'
import type { FoodInput, FoodSearchResult, ImportFoodInput } from './models'

export type FoodSource = '' | 'Yerel' | 'USDA'

export const foodsApi = {
  /**
   * Yerel kayıtlar hep aranır; external=true ise USDA da aranır. USDA çalışmazsa yine 200 gelir:
   * externalUnavailable=true ve message ile yalnızca yerel sonuçlar (FR-D4).
   */
  search: (q: string, source: FoodSource = '', external = false) =>
    api.get<FoodSearchResult>(query('/foods', { q: q.trim(), source, external: external || undefined })),
  create: (input: FoodInput) => api.post<{ id: string }>('/foods', input),
  /** Yalnızca kendi kaydı; ortak besinde 403, planda kullanılan besini silmede 409. */
  update: (id: string, input: FoodInput) => api.put(`/foods/${encodeURIComponent(id)}`, input),
  remove: (id: string) => api.delete(`/foods/${encodeURIComponent(id)}`),
  /** USDA sonucunu yerel veritabanına aktarır, yerel id döner (FR-D3). */
  import: (input: ImportFoodInput) => api.post<{ id: string }>('/foods/import', input),
}
