// H. Enerji: /api/energy/calculate (Strategy)
import { api } from './http'
import type { EnergyFormula, EnergyResult } from './models'

export const energyApi = {
  /** Danışanın son ölçümü ve yaşıyla BMH, günlük ihtiyaç ve hedef (FR-H1). Ölçüm yoksa 400. */
  calculate: (input: { clientId: string; formula: EnergyFormula; activityFactor: number; adjustmentKcal: number }) =>
    api.post<EnergyResult>('/energy/calculate', input),
}
