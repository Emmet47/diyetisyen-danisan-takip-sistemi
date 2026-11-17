import type { Appointment, Audit, Client, Dietitian, Food, History, MealLog, Measurement, Notice, Package, Plan, Subscription, WorkHour } from './api/models'

export const tz = 'Europe/Istanbul'
export const dateKey = (value = new Date()) => new Intl.DateTimeFormat('sv-SE', { timeZone: tz }).format(value)
export function moveDate(date: string, days: number) {
  const ref = new Date(date + 'T12:00:00Z')
  ref.setUTCDate(ref.getUTCDate() + days)
  return ref.toISOString().slice(0, 10)
}
export function addMonth(date: string) {
  const next = new Date(date + 'T12:00:00Z')
  const day = next.getUTCDate()
  next.setUTCDate(1)
  next.setUTCMonth(next.getUTCMonth() + 1)
  const last = new Date(Date.UTC(next.getUTCFullYear(), next.getUTCMonth() + 1, 0)).getUTCDate()
  next.setUTCDate(Math.min(day, last))
  return next.toISOString().slice(0, 10)
}
export const localTime = (date: string, time: string) => new Date(date + 'T' + time + '+03:00').toISOString()
export const dateLabel = (date: string) => date ? new Intl.DateTimeFormat('tr-TR', { day: 'numeric', month: 'short', year: 'numeric', timeZone: tz }).format(new Date(date)) : '—'
export const timeLabel = (date: string) => new Intl.DateTimeFormat('tr-TR', { hour: '2-digit', minute: '2-digit', timeZone: tz }).format(new Date(date))
export const initials = (name: string) => name.split(' ').filter(Boolean).slice(0, 2).map(part => part[0]).join('').toLocaleUpperCase('tr-TR')
export const newId = () => crypto.randomUUID()

export type DemoData = {
  clients: Client[]; foods: Food[]; measurements: Measurement[]; plans: Plan[]; templates: Plan[];
  appointments: Appointment[]; notices: Notice[]; mealLogs: MealLog[]; packages: Package[];
  dietitians: Dietitian[]; audit: Audit[]; subscription: Subscription; history: Record<string, History[]>; hours: WorkHour[];
}
function seed(): DemoData {
  const today = dateKey()
  const clients: Client[] = [
    { id: 'c1', fullName: 'Zeynep Kaya', goalType: 'LoseWeight', restrictions: [{ foodItemId: 'f4', foodName: 'Badem', type: 'Allergy' }], targetWeightKg: 68 },
    { id: 'c2', fullName: 'Mert Demir', goalType: 'GainWeight', targetWeightKg: 78 },
    { id: 'c3', fullName: 'Elif Yıldız', goalType: 'Maintain' },
    { id: 'c4', fullName: 'Can Arslan', goalType: 'LoseWeight' },
    { id: 'c5', fullName: 'Selin Aydın', goalType: 'Maintain' },
    { id: 'c6', fullName: 'Deniz Aksoy', goalType: 'LoseWeight' },
    { id: 'c7', fullName: 'Ece Koç', goalType: 'GainWeight' },
    { id: 'c8', fullName: 'Arda Yılmaz', goalType: 'Maintain' },
  ].map((client, index) => ({ ...client, email: 'danisan' + (index + 1) + '@ornek.test', birthDate: '1996-05-14', sex: index % 2 ? 'Male' : 'Female', isActive: true } as Client))
  const foods: Food[] = [
    ['Yulaf ezmesi', '1 porsiyon', 40, 379, 13.2, 67.7, 6.5, 'USDA FoodData Central'],
    ['Süzme yoğurt', '1 kase', 150, 97, 9, 4, 5, 'Yerel'],
    ['Izgara tavuk göğsü', '1 porsiyon', 120, 165, 31, 0, 3.6, 'Yerel'],
    ['Badem', '1 avuç', 25, 579, 21.2, 21.6, 49.9, 'USDA FoodData Central'],
    ['Bulgur pilavı', '1 porsiyon', 150, 83, 3.1, 18.6, 0.2, 'Yerel'],
    ['Elma', '1 orta boy', 150, 52, 0.3, 13.8, 0.2, 'Yerel'],
  ].map(([name, portionName, portionGrams, energy, protein, carbs, fat, source], i) => ({
    id: 'f' + (i + 1), name: String(name), portionName: String(portionName), portionGrams: Number(portionGrams),
    energyKcalPer100g: Number(energy), proteinPer100g: Number(protein), carbsPer100g: Number(carbs),
    fatPer100g: Number(fat), sourceName: String(source), sourceUpdatedAt: moveDate(today, -12), editable: false,
  }))
  const meals = [
    { id: 'm1', day: 1, name: 'Kahvaltı', time: '08:30', lines: [{ id: 'l1', foodId: 'f1', quantity: 2 }, { id: 'l2', foodId: 'f2', quantity: 1 }] },
    { id: 'm2', day: 1, name: 'Öğle', time: '13:00', lines: [{ id: 'l3', foodId: 'f3', quantity: 1 }, { id: 'l4', foodId: 'f5', quantity: 2 }] },
  ]
  const packages = [10, 30, 100].map((maxClients, i) => ({ id: 'pkg' + i, name: ['Başlangıç', 'Standart', 'Profesyonel'][i], maxClients, monthlyPrice: [199, 399, 699][i], isActive: true }))
  return {
    clients, foods, packages, history: {},
    subscription: { planId: 'pkg0', planName: 'Başlangıç', monthlyPrice: 199, endDate: moveDate(today, 22), status: 'Active', usage: { activeClients: 8, maxClients: 10, nearLimit: true } },
    measurements: [73.2, 72.5, 71.8, 71.4, 70.6, 70.1].map((weight, i) => ({ id: 'b' + i, clientId: 'c1', date: moveDate(today, (i - 5) * 7), weight, height: 169, waist: 88 - i * 1.1 })),
    plans: [
      { id: 'p1', clientId: 'c1', title: 'Dengeli beslenme', days: 1, state: 'Published', meals, publishedDate: moveDate(today, -7) },
      { id: 'p2', clientId: 'c2', title: 'Kilo kazanımı', days: 1, state: 'Draft', meals },
      { id: 'p3', clientId: 'c3', title: 'Kilo koruma', days: 1, state: 'Published', meals, publishedDate: moveDate(today, -7) },
    ],
    templates: [{ id: 't1', title: 'Dengeli başlangıç', days: 1, state: 'Draft', meals }],
    appointments: [
      { id: 'a1', clientId: 'c1', startsAt: localTime(today, '10:00'), status: 'Confirmed' },
      { id: 'a2', clientId: 'c2', startsAt: localTime(today, '11:30'), status: 'Requested' },
      { id: 'a3', clientId: 'c3', startsAt: localTime(today, '14:00'), status: 'Confirmed' },
    ],
    notices: [
      { id: 'n1', title: 'Yeni randevu talebi', detail: 'Mert Demir bir görüşme talep etti.', at: localTime(today, '08:30'), read: false },
      { id: 'n2', title: 'Ölçüm hatırlatması', detail: 'Elif Yıldız’ın ölçüm zamanını kontrol edin.', at: localTime(moveDate(today, -1), '09:00'), read: false },
    ],
    mealLogs: [
      { id: 'g1', clientId: 'c1', date: today, mealId: 'm1', meal: 'Kahvaltı', status: 'Ate', note: 'Porsiyonu tamamladı.' },
      { id: 'g2', clientId: 'c1', date: today, mealId: 'm2', meal: 'Öğle', status: 'Partial', note: 'Pilavın yarısı tüketildi.' },
    ],
    dietitians: [
      { id: 'd1', fullName: 'Ayşe Örnek', email: 'ayse@ornek.test', isActive: true, planName: 'Başlangıç', subscriptionStatus: 'Active', usage: { activeClients: 8, maxClients: 10, nearLimit: true } },
      { id: 'd2', fullName: 'Burak Örnek', email: 'burak@ornek.test', isActive: true, planName: 'Standart', subscriptionStatus: 'Active', usage: { activeClients: 14, maxClients: 30, nearLimit: false } },
      { id: 'd3', fullName: 'Aylin Örnek', email: 'aylin@ornek.test', isActive: false, planName: 'Profesyonel', subscriptionStatus: 'Expired', usage: { activeClients: 0, maxClients: 100, nearLimit: false } },
    ],
    audit: [{ id: 'k1', at: localTime(today, '08:20'), action: 'DietitianRegistered', entityType: 'Dietitian', userId: 'd1' }],
    hours: Array.from({ length: 6 }, (_, day) => ({ day: day + 1, start: '09:00', end: '17:00' })),
  }
}
const storeKey = 'ddts.web.demo.v2'
export function readDemo(): DemoData {
  try {
    const data = sessionStorage.getItem(storeKey)
    return data ? JSON.parse(data) as DemoData : seed()
  } catch {
    return seed()
  }
}
export function saveDemo(data: DemoData) {
  sessionStorage.setItem(storeKey, JSON.stringify(data))
}
export function subscriptionFor(data: DemoData): Subscription {
  const activeClients = data.clients.filter(client => client.isActive).length
  const maxClients = data.subscription.usage.maxClients
  return {
    ...data.subscription,
    status: data.subscription.status === 'Active' && data.subscription.endDate < dateKey() ? 'Expired' : data.subscription.status,
    usage: { activeClients, maxClients, nearLimit: activeClients >= maxClients * 0.8 },
  }
}

export function plannedMeals(plans: Plan[], clientId: string, from: string, to: string) {
  return plans.filter(plan => plan.clientId === clientId && plan.publishedDate && plan.state !== 'Draft')
    .flatMap(plan => {
      const start = plan.publishedDate!
      const first = from > start ? from : start
      const last = plan.archivedDate && plan.archivedDate <= to ? moveDate(plan.archivedDate, -1) : to
      const rows: { key: string; date: string; meal: Plan['meals'][number] }[] = []
      for (let day = first; day <= last; day = moveDate(day, 1)) {
        const elapsed = Math.round((Date.parse(day) - Date.parse(start)) / 86400000)
        for (const meal of plan.meals.filter(item => item.day === elapsed % plan.days + 1)) {
          rows.push({ key: plan.id + ':' + day + ':' + meal.id, date: day, meal })
        }
      }
      return rows
    }).sort((a, b) => a.date.localeCompare(b.date) || a.meal.time.localeCompare(b.meal.time))
}
