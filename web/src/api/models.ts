export type Role = 'Dietitian' | 'Admin'
export type LoginResult = { accessToken: string; expiresAt: string; role: Role | 'Client' }
export type Session = { role: Role; email: string; demo: boolean; expiresAt?: string }
export type Goal = 'LoseWeight' | 'Maintain' | 'GainWeight'
export type Restriction = { foodItemId: string; foodName: string; type: 'Allergy' | 'Dislike' }
export type Client = {
  id: string; fullName: string; email?: string; phone?: string | null; birthDate?: string;
  sex?: 'Female' | 'Male'; goalType: Goal; targetWeightKg?: number | null;
  preferences?: string | null; allergyNotes?: string | null; isActive: boolean; kvkkConsentAt?: string;
  restrictions?: Restriction[];
}
export type History = { at: string; action: string; userId?: string; changes?: { field: string; oldValue?: string; newValue?: string }[] }
export type Package = { id: string; name: string; maxClients: number; monthlyPrice: number; isActive: boolean }
export type Usage = { activeClients: number; maxClients: number; nearLimit: boolean }
export type Subscription = { id?: string; planId: string; planName: string; monthlyPrice: number; status: string; startDate?: string; endDate: string; usage: Usage }
export type Dietitian = { id?: string; dietitianId?: string; userId?: string; fullName: string; email?: string; isActive?: boolean; planName?: string; subscriptionStatus?: string; subscriptionEndDate?: string; usage: Usage }
export type Audit = { id: string | number; at: string; action: string; entityType?: string; userId?: string }
export type Paged<T> = { items: T[]; page: number; pageSize: number; totalCount: number }

export type Food = {
  id: string; name: string; portionName: string; portionGrams: number; energyKcalPer100g: number;
  proteinPer100g: number; carbsPer100g: number; fatPer100g: number;
  sourceName: string; sourceUpdatedAt: string; editable?: boolean;
}
export type Measurement = { id: string; clientId: string; date: string; weight: number; height: number; waist: number; hip?: number; fat?: number; note?: string; weightChange?: number | null }
export type MeasurementInput = {
  measuredOn: string; weightKg: number; heightCm: number; waistCm: number;
  hipCm: number | null; bodyFatPercent: number | null; note: string | null;
}
export type MeasurementHistory = {
  targetWeightKg: number | null;
  remainingKg: number | null;
  items: (MeasurementInput & { id: string; weightChangeKg: number | null })[];
}
export type FoodLine = { id: string; foodId: string; quantity: number; alternativeFor?: string }
export type Meal = { id: string; day: number; name: string; time: string; lines: FoodLine[] }
export type Plan = {
  id: string; clientId?: string; title: string; days: number; state: 'Draft' | 'Published' | 'Archived'; meals: Meal[];
  targetKcal?: number; energyFormula?: string; publishedDate?: string; archivedDate?: string;
}
export type Appointment = { id: string; clientId: string; startsAt: string; status: 'Requested' | 'Confirmed' | 'Completed' | 'Cancelled' }
export type Notice = { id: string; title: string; detail: string; at: string; read: boolean }
export type MealLog = { id: string; clientId: string; date: string; mealId: string; meal: string; status: 'Ate' | 'Partial' | 'Skipped'; note?: string }
export type WorkHour = { day: number; start: string; end: string }
export const goalNames: Record<Goal, string> = { LoseWeight: 'Kilo verme', Maintain: 'Kilo koruma', GainWeight: 'Kilo alma' }
