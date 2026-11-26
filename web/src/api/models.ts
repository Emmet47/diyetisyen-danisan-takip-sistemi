// API'nin gönderip aldığı JSON şekilleri. Alan adları backend DTO'larıyla birebir aynı (camelCase),
// enum'lar metin olarak gelir. Ayrıntı: docs/api.md ve http://localhost:8080/scalar

// ---- A. Kimlik ----
export type Role = 'Admin' | 'Dietitian' | 'Client'
export type LoginResult = { accessToken: string; expiresAt: string; role: Role }
/** Web paneline yalnızca diyetisyen ve yönetici girer; danışan mobil uygulamayı kullanır. */
export type Session = { role: 'Admin' | 'Dietitian'; email: string; expiresAt: string }

// ---- B. Danışan ----
export type Sex = 'Female' | 'Male'
export type Goal = 'LoseWeight' | 'Maintain' | 'GainWeight'
export type RestrictionType = 'Allergy' | 'Dislike'
export type Restriction = { foodItemId: string; foodName: string; type: RestrictionType }
export type ClientSummary = { id: string; fullName: string; phone: string | null; goalType: Goal; isActive: boolean }
export type ClientDetail = ClientSummary & {
  email: string; birthDate: string; sex: Sex; targetWeightKg: number | null;
  preferences: string | null; allergyNotes: string | null; kvkkConsentAt: string; restrictions: Restriction[];
}
export type ClientInput = {
  fullName: string; phone: string | null; goalType: Goal; targetWeightKg: number | null;
  preferences: string | null; allergyNotes: string | null;
}
export type NewClientInput = ClientInput & { email: string; birthDate: string; sex: Sex; kvkkConsentGiven: boolean }
export type HistoryEntry = { at: string; action: string; userId: string | null; changes: { field: string; oldValue: string | null; newValue: string | null }[] }

// ---- L. Abonelik ve yönetici ----
export type SubscriptionStatus = 'Active' | 'Expired' | 'Cancelled'
export type Package = { id: string; name: string; maxClients: number; monthlyPrice: number; isActive: boolean }
export type Usage = { activeClients: number; maxClients: number; nearLimit: boolean }
export type Subscription = {
  id: string; planId: string; planName: string; monthlyPrice: number; status: SubscriptionStatus;
  startDate: string; endDate: string; usage: Usage;
}
export type DietitianAdmin = {
  id: string; userId: string; fullName: string; email: string; phone: string | null; isActive: boolean;
  planName: string | null; subscriptionStatus: SubscriptionStatus | null; subscriptionEndDate: string | null; usage: Usage;
}
export type DietitianUsage = { dietitianId: string; fullName: string; planName: string | null; usage: Usage }
export type AuditLog = { id: number; at: string; userId: string | null; action: string; entityType: string; entityId: string | null; ipAddress: string | null }
/** Sayfalı liste yanıtı (PagedResult): varsayılan 20, en fazla 100 satır. */
export type Paged<T> = { items: T[]; page: number; pageSize: number; totalCount: number }

// ---- C. Ölçüm ve K. Rapor ----
export type MeasurementInput = {
  measuredOn: string; weightKg: number; heightCm: number; waistCm: number;
  hipCm: number | null; bodyFatPercent: number | null; note: string | null;
}
export type Measurement = MeasurementInput & {
  id: string; weightChangeKg: number | null; waistChangeCm: number | null;
  hipChangeCm: number | null; bodyFatChangePercent: number | null;
}
export type MeasurementHistory = { targetWeightKg: number | null; remainingKg: number | null; items: Measurement[] }
export type ProgressReport = {
  clientId: string; clientName: string; from: string; to: string; generatedAt: string;
  measurements: Measurement[]; targetWeightKg: number | null; remainingKg: number | null;
  weightChangeKg: number | null; waistChangeCm: number | null;
  plannedMeals: number; eatenMeals: number; partialMeals: number; skippedMeals: number;
  adherencePercent: number | null; loggedDays: number; dayCount: number; logFrequencyPercent: number; disclaimer: string;
}

// ---- D. Besin (NutritionItem: sistemin standart besin modeli) ----
export type Food = {
  /** Yerel kayıtta dolu; dış kaynaktan (PlateRank, USDA) yeni gelen satırda null: önce içe aktarılmalı. */
  id: string | null; name: string; portionName: string; portionGrams: number;
  energyKcalPer100g: number; proteinPer100g: number; carbsPer100g: number; fatPer100g: number;
  sourceName: string; externalId: string | null; retrievedAt: string | null;
  /**
   * Kaynak porsiyonun gramını vermiyorsa (PlateRank'teki Türk yemekleri) true: portionGrams 0'dır ve
   * enerji/makro alanları 100 g için değil 1 porsiyon içindir. Aktarırken gram girilir, API 100 g'a çevirir.
   */
  valuesPerPortion: boolean
}
export type FoodSearchResult = { items: Food[]; externalUnavailable: boolean; message: string | null }
export type FoodInput = Pick<Food, 'name' | 'portionName' | 'portionGrams' | 'energyKcalPer100g' | 'proteinPer100g' | 'carbsPer100g' | 'fatPer100g'>
/** source: arama sonucundaki sourceName ("PlateRank", "USDA FoodData Central"); boşsa API USDA sayar. */
export type ImportFoodInput = { externalId: string; source: string; name: string | null; portionName: string | null; portionGrams: number | null }

// ---- H. Enerji ----
export type EnergyFormula = 'MifflinStJeor' | 'HarrisBenedict'
export type EnergyResult = {
  formula: EnergyFormula; input: { sex: Sex; ageYears: number; weightKg: number; heightCm: number };
  bmrKcal: number; dailyNeedKcal: number; targetKcal: number; disclaimer: string;
}

// ---- E. Plan ve F. Şablon ----
export type PlanType = 'Daily' | 'Weekly'
export type PlanStatus = 'Draft' | 'Published' | 'Archived'
export type NutrientTotals = {
  energyKcal: number; proteinG: number; carbsG: number; fatG: number;
  proteinPercent: number; carbsPercent: number; fatPercent: number;
}
export type MealItem = {
  id: string; foodItemId: string; foodName: string; portionName: string; portionCount: number; grams: number;
  isAlternative: boolean; alternativeToItemId: string | null; totals: NutrientTotals;
}
/** Öğün saati Türkiye saatidir, "08:00:00" biçiminde. */
export type MealDto = { id: string; dayNo: number; name: string; time: string; items: MealItem[]; totals: NutrientTotals }
export type DayTotals = { dayNo: number; totals: NutrientTotals }
export type PlanSummary = {
  id: string; clientId: string; title: string; planType: PlanType; status: PlanStatus;
  targetKcal: number | null; energyFormula: string | null; sourceTemplateId: string | null;
  createdAt: string; publishedAt: string | null; archivedAt: string | null;
  meals: MealDto[]; days: DayTotals[]; totals: NutrientTotals; disclaimer: string;
}
export type PlanListItem = Pick<PlanSummary, 'id' | 'clientId' | 'title' | 'planType' | 'status' | 'sourceTemplateId' | 'createdAt' | 'publishedAt' | 'archivedAt'>
export type PlanCreated = { planId: string; status: PlanStatus; sourceTemplateId: string | null; totals: NutrientTotals; warnings: string[] }
export type MealInput = {
  dayNo: number; name: string; time: string;
  items: { foodItemId: string; portionCount: number; alternatives: { foodItemId: string; portionCount: number }[] }[];
}
export type PlanInput = { title: string; planType: PlanType; targetKcal: number | null; energyFormula: string | null; meals: MealInput[] }
export type TemplateListItem = { id: string; name: string; description: string | null; planType: PlanType; updatedAt: string }
export type TemplateSummary = TemplateListItem & { meals: MealDto[]; days: DayTotals[]; totals: NutrientTotals; disclaimer: string }
export type TemplateInput = { name: string; description: string | null; planType: PlanType; meals: MealInput[] }

// ---- G. Öğün günlüğü ----
export type MealLogStatus = 'Eaten' | 'Partial' | 'Skipped'
export type MealLog = { id: string; mealId: string; logDate: string; status: MealLogStatus; note: string | null; hasPhoto: boolean; loggedAt: string }
export type MealComparison = {
  from: string; to: string;
  days: { date: string; meals: { mealId: string; name: string; time: string; log: MealLog | null }[] }[];
}
export type PhotoUrl = { url: string; expiresAt: string }

// ---- I. Randevu ----
export type AppointmentStatus = 'Requested' | 'Confirmed' | 'Cancelled' | 'Completed'
/** Randevu zamanları UTC gelir (sonu Z); ekranda Türkiye saatiyle gösterilir. */
export type Appointment = {
  id: string; clientId: string; clientName: string | null; startsAt: string; endsAt: string;
  status: AppointmentStatus; note: string | null;
}
/** weekday: 1 = Pazartesi … 7 = Pazar; saatler Türkiye saati, "09:00:00". */
export type WorkingHour = { weekday: number; startTime: string; endTime: string }

// ---- J. Bildirim ----
export type Notification = { id: string; type: string; title: string; body: string; isRead: boolean; createdAt: string }

// ---- Ekranda kullanılan Türkçe adlar ----
export const goalNames: Record<Goal, string> = { LoseWeight: 'Kilo verme', Maintain: 'Kilo koruma', GainWeight: 'Kilo alma' }
export const sexNames: Record<Sex, string> = { Female: 'Kadın', Male: 'Erkek' }
export const planStatusNames: Record<PlanStatus, string> = { Draft: 'Taslak', Published: 'Yayımlandı', Archived: 'Arşivde' }
export const planTypeNames: Record<PlanType, string> = { Daily: 'Günlük', Weekly: 'Haftalık' }
export const mealLogNames: Record<MealLogStatus, string> = { Eaten: 'Yedim', Partial: 'Kısmen yedim', Skipped: 'Yemedim' }
export const appointmentLabels: Record<AppointmentStatus, string> = { Requested: 'Talep edildi', Confirmed: 'Onaylandı', Completed: 'Tamamlandı', Cancelled: 'İptal' }
export const formulaNames: Record<EnergyFormula, string> = { MifflinStJeor: 'Mifflin–St Jeor', HarrisBenedict: 'Harris–Benedict' }
export const subscriptionNames: Record<SubscriptionStatus, string> = { Active: 'Aktif', Expired: 'Süresi doldu', Cancelled: 'İptal edildi' }
