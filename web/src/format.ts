// Tarih, saat ve sayı biçimlendirme yardımcıları.
// Türkiye saati UTC+3, yaz saati yok. Randevu zamanları API'de UTC; öğün ve çalışma saatleri yerel "HH:mm:ss".

export const tz = 'Europe/Istanbul'

/** Türkiye saatine göre "yyyy-aa-gg". */
export const dateKey = (value = new Date()) => new Intl.DateTimeFormat('sv-SE', { timeZone: tz }).format(value)

export function moveDate(date: string, days: number) {
  const ref = new Date(date + 'T12:00:00Z')
  ref.setUTCDate(ref.getUTCDate() + days)
  return ref.toISOString().slice(0, 10)
}

/** Türkiye saatindeki gün + saati ("10:30") UTC ISO metnine çevirir: 2026-12-01 10:30 → 2026-12-01T07:30:00.000Z */
export const localTime = (date: string, time: string) => new Date(date + 'T' + time.slice(0, 5) + ':00+03:00').toISOString()

/** Türkiye saatinde o günün başlangıcı (UTC ISO). */
export const dayStartUtc = (date: string) => localTime(date, '00:00')

export const dateLabel = (date: string | null | undefined) => date
  ? new Intl.DateTimeFormat('tr-TR', { day: 'numeric', month: 'short', year: 'numeric', timeZone: tz }).format(new Date(date.length === 10 ? date + 'T12:00:00Z' : date))
  : '—'
export const timeLabel = (date: string) => new Intl.DateTimeFormat('tr-TR', { hour: '2-digit', minute: '2-digit', timeZone: tz }).format(new Date(date))
/** "08:00:00" → "08:00" */
export const shortTime = (time: string) => time.slice(0, 5)
/** "08:00" → "08:00:00" (API'nin beklediği biçim) */
export const fullTime = (time: string) => time.length === 5 ? time + ':00' : time

export const decimal = (value: number, digits = 1) => value.toLocaleString('tr-TR', { maximumFractionDigits: digits })
export const initials = (name: string) => name.split(' ').filter(Boolean).slice(0, 2).map(part => part[0]).join('').toLocaleUpperCase('tr-TR')
export const newId = () => crypto.randomUUID()
