import { useState } from 'react'
import type { ReactNode } from 'react'
import { clientsApi } from '../api/clients'
import { foodsApi } from '../api/foods'
import { formulaNames, planStatusNames, planTypeNames } from '../api/models'
import type { ClientSummary, Food, MealDto, MealInput, NutrientTotals, PlanCreated, PlanSummary, PlanType, Restriction, TemplateSummary } from '../api/models'
import { plansApi, templatesApi } from '../api/plans'
import { subscriptionApi } from '../api/subscription'
import { dateLabel, decimal, fullTime, newId, shortTime } from '../format'
import { presetTemplates } from '../presetTemplates'
import { FoodPicker } from './Catalog'
import { Badge, Empty, Field, Message, Modal, Table, Title, Wait, useAction, useLoad, values } from '../ui'
import type { PageProps } from '../ui'

// ---- Düzenleyicinin kendi modeli ----
// Plan → öğün → besin satırı (Composite). Alternatif satır, aynı öğündeki bir ana satıra bağlıdır ve toplama katılmaz.
// Miktar ekranda gram olarak girilir; API'ye porsiyon sayısı gider (porsiyon sayısı = gram ÷ besinin porsiyon gramı).
type Per = { kcal: number; p: number; c: number; f: number }
type Line = { id: string; foodId: string; foodName: string; portionName: string; portionGrams: number; quantity: number; alternativeFor?: string; per: Per }
type EditMeal = { id: string; day: number; name: string; time: string; lines: Line[] }
type Draft = { title: string; description: string; clientId: string; planType: PlanType; targetKcal: string; energyFormula: string; meals: EditMeal[] }

const dayCount = (type: PlanType) => type === 'Weekly' ? 7 : 1
const dayNames = ['Pazartesi', 'Salı', 'Çarşamba', 'Perşembe', 'Cuma', 'Cumartesi', 'Pazar']
const dayName = (type: PlanType, day: number) => type === 'Weekly' ? dayNames[day - 1] : day + '. gün'
const formulaLabel = (value: string | null) => value ? formulaNames[value as keyof typeof formulaNames] ?? value : ''
const grams = (line: Line) => line.quantity * line.portionGrams
/** Gramı porsiyon sayısına çevirir (2 basamak). API'de porsiyon sayısı 0,01–20 arasında olmalı. */
function toCount(value: number, portionGrams: number): number | string {
  const count = Math.round(value / portionGrams * 100) / 100
  if (!(value > 0) || count < 0.01) return 'Miktar çok az.'
  if (count > 20) return 'Bu besinde en fazla ' + decimal(portionGrams * 20, 0) + ' g girilebilir.'
  return count
}

/** API'den gelen öğünleri düzenleyici modeline çevirir (bir porsiyonun değeri = satır toplamı ÷ porsiyon sayısı). */
function fromDto(meals: MealDto[]): EditMeal[] {
  return meals.map(meal => ({
    id: meal.id, day: meal.dayNo, name: meal.name, time: shortTime(meal.time),
    lines: meal.items.map(item => ({
      id: item.id, foodId: item.foodItemId, foodName: item.foodName, portionName: item.portionName,
      portionGrams: item.grams / item.portionCount, quantity: item.portionCount,
      alternativeFor: item.alternativeToItemId ?? undefined,
      per: { kcal: item.totals.energyKcal / item.portionCount, p: item.totals.proteinG / item.portionCount, c: item.totals.carbsG / item.portionCount, f: item.totals.fatG / item.portionCount },
    })),
  }))
}
/** Düzenleyici modelini API gövdesine çevirir: meals[].items[].alternatives[] */
function toInput(meals: EditMeal[]): MealInput[] {
  return meals.map(meal => ({
    dayNo: meal.day, name: meal.name.trim(), time: fullTime(meal.time),
    items: meal.lines.filter(line => !line.alternativeFor).map(line => ({
      foodItemId: line.foodId, portionCount: line.quantity,
      alternatives: meal.lines.filter(alt => alt.alternativeFor === line.id).map(alt => ({ foodItemId: alt.foodId, portionCount: alt.quantity })),
    })),
  }))
}
function perPortion(food: Food): Per {
  const factor = food.portionGrams / 100
  return { kcal: food.energyKcalPer100g * factor, p: food.proteinPer100g * factor, c: food.carbsPer100g * factor, f: food.fatPer100g * factor }
}
function sum(meals: EditMeal[]): Per {
  return meals.flatMap(meal => meal.lines).filter(line => !line.alternativeFor).reduce((total, line) => ({
    kcal: total.kcal + line.per.kcal * line.quantity, p: total.p + line.per.p * line.quantity, c: total.c + line.per.c * line.quantity, f: total.f + line.per.f * line.quantity,
  }), { kcal: 0, p: 0, c: 0, f: 0 })
}
const toTotals = (s: Per): NutrientTotals => {
  const macro = s.p * 4 + s.c * 4 + s.f * 9
  const pct = (value: number) => macro ? value / macro * 100 : 0
  return { energyKcal: s.kcal, proteinG: s.p, carbsG: s.c, fatG: s.f, proteinPercent: pct(s.p * 4), carbsPercent: pct(s.c * 4), fatPercent: pct(s.f * 9) }
}

/** Enerji ve makro dağılımı (FR-H2). */
function Nutrition({ totals, compact = false }: { totals: NutrientTotals; compact?: boolean }) {
  const part = (value: number, percent: number) => decimal(value) + ' g (%' + decimal(percent) + ')'
  if (compact) return <span className="nutrition"><strong>{decimal(totals.energyKcal, 0)} kcal</strong><span>P {decimal(totals.proteinG, 0)} · K {decimal(totals.carbsG, 0)} · Y {decimal(totals.fatG, 0)} g</span></span>
  return <span className="nutrition"><strong>{decimal(totals.energyKcal)} kcal</strong><span>Protein {part(totals.proteinG, totals.proteinPercent)}</span><span>Karbonhidrat {part(totals.carbsG, totals.carbsPercent)}</span><span>Yağ {part(totals.fatG, totals.fatPercent)}</span></span>
}

/** Haftalık planda günler yan yana sütun (Pazartesi … Pazar), günlük planda tek sütun. */
function DayColumns({ weekly, children }: { weekly: boolean; children: ReactNode }) {
  return <div className={weekly ? 'week-grid' : 'stack'}>{children}</div>
}

type Row = { id: string; title: string; clientId?: string; planType: PlanType; status?: keyof typeof planStatusNames; extra: string }

export default function Plans({ templates }: PageProps & { templates: boolean }) {
  const [display, show] = useState<string | null>(null)
  const [edit, setEdit] = useState<string | 'new' | null>(null)
  const [copy, setCopy] = useState<Row | null>(null)
  const [presets, setPresets] = useState(false)
  const [notice, setNotice] = useState('')
  const [filter, setFilter] = useState('')
  const clients = useLoad(() => clientsApi.list(), 'plan-clients')
  const sub = useLoad(() => subscriptionApi.get(), 'plan-sub')
  const list = useLoad<Row[]>(async () => templates
    ? (await templatesApi.list()).map(item => ({ id: item.id, title: item.name, planType: item.planType, extra: item.description ?? 'Güncellendi: ' + dateLabel(item.updatedAt) }))
    : (await plansApi.list(filter || undefined)).map(item => ({ id: item.id, title: item.title, clientId: item.clientId, planType: item.planType, status: item.status, extra: item.publishedAt ? 'Yayım: ' + dateLabel(item.publishedAt) : 'Oluşturma: ' + dateLabel(item.createdAt) })),
  (templates ? 'templates' : 'plans:') + filter)
  const action = useAction(list.reload)
  const active = sub.data ? sub.data.status === 'Active' : true
  const clientName = (id?: string) => clients.data?.find(row => row.id === id)?.fullName ?? '—'
  function run(task: () => Promise<string>) {
    setNotice('')
    void action.run(async () => setNotice(await task()))
  }
  function saved(result: PlanCreated | null, text: string) {
    setEdit(null)
    setNotice(text + (result?.warnings.length ? ' Uyarılar: ' + result.warnings.join(' ') : ''))
    list.reload()
  }
  return <><Title title={templates ? 'Plan şablonları' : 'Beslenme planları'} subtitle={templates ? 'Yeniden kullanılabilir öğün şablonları oluştur ve danışana bağımsız kopya olarak aktar.' : 'Günlük ve haftalık planları hazırla, yayımla ve arşivle.'} action={<div className="row-actions">{templates && <button className="button secondary" onClick={() => setPresets(true)}>Hazır şablonları ekle</button>}<button className="button primary" disabled={!templates && !active} onClick={() => setEdit('new')}>{templates ? '＋ Şablon oluştur' : '＋ Plan oluştur'}</button></div>} />
    <Message type="info">{!active && (templates ? 'Aboneliğin aktif değilken şablondan yeni plan oluşturulamaz.' : 'Yeni plan oluşturmak için aboneliğini yenile.')}</Message><Message type="success">{notice}</Message><Message>{action.error}</Message>
    <section className="panel">{!templates && <div className="toolbar"><select aria-label="Danışana göre filtrele" value={filter} onChange={event => setFilter(event.target.value)}><option value="">Tüm danışanlar</option>{clients.data?.map(client => <option key={client.id} value={client.id}>{client.fullName}</option>)}</select></div>}
      {!list.data ? <Wait error={list.error} reload={list.reload} /> : <>
      <Table heads={templates ? ['Şablon', 'Süre', 'Açıklama', ''] : ['Plan', 'Danışan', 'Süre', 'Durum', '']}>{list.data.map(plan => <tr key={plan.id}><td><strong>{plan.title}</strong>{!templates && <small className="cell-note">{plan.extra}</small>}</td>{!templates && <td>{clientName(plan.clientId)}</td>}<td>{planTypeNames[plan.planType]}</td>{templates ? <td>{plan.extra}</td> : <td><Badge tone={plan.status === 'Published' ? 'green' : plan.status === 'Draft' ? 'amber' : 'gray'}>{planStatusNames[plan.status!]}</Badge></td>}<td className="row-actions">
        <button className="text-button" onClick={() => show(plan.id)}>Görüntüle</button>{(templates || plan.status === 'Draft') && <><button className="text-button" onClick={() => setEdit(plan.id)}>Düzenle</button><button className="text-button danger-text" disabled={action.busy} onClick={() => { if (confirm(plan.title + ' silinsin mi?')) run(async () => { await (templates ? templatesApi.remove(plan.id) : plansApi.remove(plan.id)); return plan.title + ' silindi.' }) }}>Sil</button></>}
        {templates ? <button className="text-button" disabled={!active} onClick={() => setCopy(plan)}>Danışana kopyala</button>
          : plan.status === 'Draft' ? <button className="text-button" disabled={action.busy} onClick={() => run(async () => { await plansApi.publish(plan.id); return 'Plan yayımlandı. Danışanın varsa önceki yayımlanmış planı arşivlendi ve danışana bildirim gönderildi.' })}>Yayımla</button>
          : plan.status === 'Published' && <button className="text-button" disabled={action.busy} onClick={() => { if (confirm(plan.title + ' arşivlensin mi?')) run(async () => { await plansApi.archive(plan.id); return 'Plan arşivlendi.' }) }}>Arşivle</button>}
      </td></tr>)}</Table>{!list.data.length && <Empty>Kayıt bulunamadı.</Empty>}</>}</section>
    {display && <PlanReviewModal id={display} templates={templates} clientName={clientName} close={() => show(null)} />}
    {edit && <PlanEditor id={edit === 'new' ? undefined : edit} templates={templates} clients={clients.data ?? []} close={() => setEdit(null)} saved={saved} />}
    {presets && <PresetForm existing={list.data?.map(row => row.title) ?? []} close={() => setPresets(false)} added={count => { setPresets(false); setNotice(count + ' hazır şablon eklendi. Danışana kopyalayıp kişiye göre düzenleyebilirsin.'); list.reload() }} />}
    {copy && <CopyForm template={copy} clients={clients.data ?? []} close={() => setCopy(null)} copied={result => { setCopy(null); setNotice('Şablonun bağımsız kopyası taslak plan olarak oluşturuldu; özgün şablon değişmedi.' + (result.warnings.length ? ' Uyarılar: ' + result.warnings.join(' ') : '')) }} />}
  </>
}

function CopyForm({ template, clients, close, copied }: { template: Row; clients: ClientSummary[]; close: () => void; copied: (result: PlanCreated) => void }) {
  const action = useAction()
  return <Modal title="Şablonu danışana kopyala" close={close}><form className="form" onSubmit={event => {
    const data = values(event)
    void action.run(async () => copied(await templatesApi.copyToClient(template.id, data.clientId, data.title.trim() || null)))
  }}><p className="muted">Şablonun bağımsız kopyası taslak plan olur (Prototype). Kopyada yaptığın değişiklik şablonu etkilemez.</p>
    <Field label="Danışan"><select name="clientId" required defaultValue=""><option value="">Danışan seç</option>{clients.filter(row => row.isActive).map(row => <option key={row.id} value={row.id}>{row.fullName}</option>)}</select></Field>
    <Field label="Plan adı"><input name="title" defaultValue={template.title} maxLength={100} /></Field>
    <Message>{action.error}</Message><div className="form-actions"><button className="button primary" disabled={action.busy}>Taslak oluştur</button></div></form></Modal>
}

/**
 * Hazır şablonlar (src/presetTemplates.ts): seçilenler mevcut API ile (POST /api/templates) diyetisyenin hesabına eklenir.
 * Besinler ortak besin listesinde adıyla bulunur; gram → porsiyon sayısına çevrilir.
 */
function PresetForm({ existing, close, added }: { existing: string[]; close: () => void; added: (count: number) => void }) {
  const [selected, setSelected] = useState(() => new Set(presetTemplates.filter(item => !existing.includes(item.name)).map(item => item.name)))
  const [progress, setProgress] = useState('')
  const action = useAction()
  function toggle(name: string) {
    const next = new Set(selected)
    if (next.has(name)) next.delete(name); else next.add(name)
    setSelected(next)
  }
  function install() {
    void action.run(async () => {
      const chosen = presetTemplates.filter(item => selected.has(item.name))
      const names = [...new Set(chosen.flatMap(item => item.days.flat().flatMap(meal => meal.items.map(([food]) => food))))]
      setProgress('Besinler bulunuyor…')
      const foods = new Map<string, Food>()
      for (const name of names) {
        const found = (await foodsApi.search(name, 'Yerel')).items.find(item => item.id && item.name === name)
        if (!found) throw new Error('"' + name + '" ortak besinlerde bulunamadı. Ortak besinler API ilk açılışta yüklenir.')
        foods.set(name, found)
      }
      let count = 0
      for (const item of chosen) {
        setProgress(item.name + ' ekleniyor… (' + (count + 1) + '/' + chosen.length + ')')
        const meals: MealInput[] = item.days.flatMap((day, index) => day.map(meal => ({
          dayNo: index + 1, name: meal.name, time: meal.time,
          items: meal.items.map(([name, amount]) => {
            const food = foods.get(name)!
            const portions = toCount(amount, food.portionGrams)
            if (typeof portions === 'string') throw new Error(item.name + ' · ' + name + ': ' + portions)
            return { foodItemId: food.id!, portionCount: portions, alternatives: [] }
          }),
        })))
        await templatesApi.create({ name: item.name, description: item.description + ' Örnek şablondur; danışana kopyalayıp kişiye göre düzenleyin.', planType: item.planType, meals })
        count++
      }
      added(count)
    })
  }
  const daily = presetTemplates.filter(item => item.planType === 'Daily')
  const weekly = presetTemplates.filter(item => item.planType === 'Weekly')
  const row = (item: (typeof presetTemplates)[number]) => {
    const exists = existing.includes(item.name)
    return <label className="check" key={item.name}><input type="checkbox" disabled={exists || action.busy} checked={!exists && selected.has(item.name)} onChange={() => toggle(item.name)} /> {item.name}{exists ? ' (zaten var)' : ''}</label>
  }
  return <Modal title="Hazır şablonları ekle" close={() => { if (!action.busy) close() }}><div className="stack">
    <p className="muted">Günümüzde sık kullanılan diyet tiplerine göre hazırlanmış örnek şablonlar. Besinler ortak besin listesinden seçildi, miktarlar gram. Danışana kopyaladıktan sonra kişiye göre düzenlemelisin; hesaplamalar profesyonel değerlendirme yerine geçmez.</p>
    <div className="form-grid"><div className="stack"><strong>Günlük ({daily.length})</strong>{daily.map(row)}</div><div className="stack"><strong>Haftalık ({weekly.length})</strong>{weekly.map(row)}</div></div>
    <Message type="info">{action.busy && progress}</Message><Message>{action.error}</Message>
    <div className="form-actions"><button className="button secondary" disabled={action.busy} onClick={close}>Vazgeç</button><button className="button primary" disabled={action.busy || !selected.size} onClick={install}>{action.busy ? 'Ekleniyor…' : selected.size + ' şablonu ekle'}</button></div>
  </div></Modal>
}

function PlanReviewModal({ id, templates, clientName, close }: { id: string; templates: boolean; clientName: (id?: string) => string; close: () => void }) {
  const data = useLoad<PlanSummary | TemplateSummary>(() => templates ? templatesApi.get(id) : plansApi.get(id), 'review:' + id)
  const plan = data.data
  const title = plan ? 'name' in plan ? plan.name : plan.title : 'Plan'
  const weekly = plan?.planType === 'Weekly'
  return <Modal title={title} wide={weekly} close={close}>{!plan ? <Wait error={data.error} reload={data.reload} /> : <div className="stack">
    {'status' in plan && <p>{clientName(plan.clientId)} · {planStatusNames[plan.status]} · {planTypeNames[plan.planType]}{plan.targetKcal ? ' · Günlük hedef ' + plan.targetKcal + ' kcal' : ''}{plan.energyFormula ? ' · ' + formulaLabel(plan.energyFormula) : ''}</p>}
    {'description' in plan && plan.description && <p className="muted">{plan.description}</p>}
    <p className="muted">{weekly ? 'Haftalık toplam' : 'Günlük toplam'}</p><Nutrition totals={plan.totals} />
    <DayColumns weekly={weekly}>{plan.days.map(day => { const meals = plan.meals.filter(meal => meal.dayNo === day.dayNo)
      return <section className={weekly ? 'week-day' : 'record'} key={day.dayNo}><h3>{dayName(plan.planType, day.dayNo)}</h3><Nutrition totals={day.totals} compact={weekly} />{meals.map(meal => <div className={weekly ? 'record meal-preview' : 'meal-preview'} key={meal.id}><strong>{shortTime(meal.time)} · {meal.name}</strong><Nutrition totals={meal.totals} compact={weekly} /><ul>{meal.items.map(item => <li key={item.id}>{item.foodName} · {decimal(item.grams)} g{weekly ? '' : ' (' + decimal(item.portionCount, 2) + ' × ' + item.portionName + ')'}{item.isAlternative ? ' (alternatif, toplama katılmaz)' : ''}</li>)}</ul></div>)}</section>
    })}</DayColumns>
    <p className="muted">{plan.disclaimer}</p>
  </div>}</Modal>
}

function PlanEditor({ id, templates, clients, close, saved }: { id?: string; templates: boolean; clients: ClientSummary[]; close: () => void; saved: (result: PlanCreated | null, text: string) => void }) {
  const original = useLoad<PlanSummary | TemplateSummary | null>(() => !id ? Promise.resolve(null) : templates ? templatesApi.get(id) : plansApi.get(id), 'editor:' + id)
  if (id && !original.data) return <Modal title="Düzenleyici" close={close}><Wait error={original.error} reload={original.reload} /></Modal>
  const source = original.data
  const draft: Draft = !source
    ? { title: '', description: '', clientId: '', planType: 'Daily', targetKcal: '', energyFormula: '', meals: [] }
    : 'name' in source
      ? { title: source.name, description: source.description ?? '', clientId: '', planType: source.planType, targetKcal: '', energyFormula: '', meals: fromDto(source.meals) }
      : { title: source.title, description: '', clientId: source.clientId, planType: source.planType, targetKcal: source.targetKcal ? String(source.targetKcal) : '', energyFormula: source.energyFormula ?? '', meals: fromDto(source.meals) }
  return <EditorSteps id={id} initial={draft} templates={templates} clients={clients} close={close} saved={saved} />
}

/** Besin satırının gramı: kutudan çıkınca porsiyon sayısına çevrilip kaydedilir. */
function GramInput({ line, change }: { line: Line; change: (quantity: number) => void }) {
  const [error, setError] = useState('')
  return <span className="gram-input"><input type="number" min="1" step="1" aria-label={line.foodName + ' gram'} key={line.quantity} defaultValue={Math.round(grams(line) * 10) / 10} onBlur={event => {
    const count = toCount(Number(event.target.value), line.portionGrams)
    if (typeof count === 'string') { setError(count); event.target.value = String(Math.round(grams(line) * 10) / 10); return }
    setError(''); if (count !== line.quantity) change(count)
  }} /> g{error && <small className="danger-text"> {error}</small>}</span>
}

/** Plan adım adım kurulur ve kaydetmeden önce özetlenir (Builder, sd-2). */
function EditorSteps({ id, initial, templates, clients, close, saved }: { id?: string; initial: Draft; templates: boolean; clients: ClientSummary[]; close: () => void; saved: (result: PlanCreated | null, text: string) => void }) {
  const [step, setStep] = useState(1)
  const [plan, setPlan] = useState<Draft>(initial)
  const [error, setError] = useState('')
  const [adding, setAdding] = useState<{ mealId: string; grams: string; alternative: string } | null>(null)
  const action = useAction()
  const client = useLoad(() => !templates && plan.clientId ? clientsApi.get(plan.clientId) : Promise.resolve(null), 'editor-client:' + plan.clientId)
  const restrictions: Restriction[] = client.data?.restrictions ?? []
  const used = new Set(plan.meals.flatMap(meal => meal.lines.map(line => line.foodId)))
  const warnings = restrictions.filter(item => used.has(item.foodItemId))
  const days = Array.from({ length: dayCount(plan.planType) }, (_, i) => i + 1)
  const weekly = plan.planType === 'Weekly'
  const mealsOf = (day: number) => plan.meals.filter(meal => meal.day === day).sort((a, b) => a.time.localeCompare(b.time))
  function patch(meal: EditMeal) { setPlan(data => ({ ...data, meals: data.meals.map(row => row.id === meal.id ? meal : row) })) }
  function addMeal(day: number) {
    const meal = { id: newId(), day, name: '', time: ['08:00', '12:30', '16:00', '19:30'][mealsOf(day).length] ?? '21:00', lines: [] }
    setPlan(data => ({ ...data, meals: [...data.meals, meal] }))
  }
  /** Önceki günün öğünlerini bu güne kopyalar (haftalık planı hızlı doldurmak için). */
  function copyDay(day: number) {
    const copies = mealsOf(day - 1).map(meal => {
      const ids = new Map(meal.lines.map(line => [line.id, newId()]))
      return { ...meal, id: newId(), day, lines: meal.lines.map(line => ({ ...line, id: ids.get(line.id)!, alternativeFor: line.alternativeFor ? ids.get(line.alternativeFor) : undefined })) }
    })
    setPlan(data => ({ ...data, meals: [...data.meals, ...copies] }))
  }
  function addFood(meal: EditMeal, food: Food) {
    const count = toCount(Number((adding?.grams ?? '').replace(',', '.') || food.portionGrams), food.portionGrams)
    if (typeof count === 'string') { setError(count); return }
    setError('')
    patch({ ...meal, lines: [...meal.lines, { id: newId(), foodId: food.id!, foodName: food.name, portionName: food.portionName, portionGrams: food.portionGrams, quantity: count, alternativeFor: adding?.alternative || undefined, per: perPortion(food) }] })
    setAdding({ mealId: meal.id, grams: '', alternative: '' })
  }
  function save() {
    void action.run(async () => {
      const meals = toInput(plan.meals)
      if (templates) {
        const input = { name: plan.title, description: plan.description.trim() || null, planType: plan.planType, meals }
        if (id) await templatesApi.update(id, input)
        else await templatesApi.create(input)
        saved(null, 'Şablon kaydedildi.')
      } else {
        const input = { title: plan.title, planType: plan.planType, targetKcal: plan.targetKcal ? Number(plan.targetKcal) : null, energyFormula: plan.energyFormula || null, meals }
        const result = id ? await plansApi.update(id, input) : await plansApi.create({ ...input, clientId: plan.clientId })
        saved(result, 'Taslak plan kaydedildi.')
      }
    })
  }
  function mealCard(meal: EditMeal) {
    const open = adding?.mealId === meal.id
    return <section className="record stack" key={meal.id}>
      <div className="toolbar flush"><Field label="Öğün adı"><input value={meal.name} maxLength={50} placeholder="Kahvaltı" onChange={event => patch({ ...meal, name: event.target.value })} /></Field><Field label="Saat"><input type="time" value={meal.time} onChange={event => patch({ ...meal, time: event.target.value })} /></Field></div>
      {meal.lines.map(line => <div className="list-line" key={line.id}><span>{line.foodName}{line.alternativeFor ? ' (alternatif: ' + (meal.lines.find(row => row.id === line.alternativeFor)?.foodName ?? '') + ')' : ''}</span><span className="row-actions"><GramInput line={line} change={quantity => patch({ ...meal, lines: meal.lines.map(row => row.id === line.id ? { ...row, quantity } : row) })} /><button className="text-button" onClick={() => patch({ ...meal, lines: meal.lines.filter(row => row.id !== line.id && row.alternativeFor !== line.id) })}>Kaldır</button></span></div>)}
      {open ? <div className="stack">
        <div className="toolbar flush"><Field label="Miktar (g, boşsa 1 porsiyon)"><input type="number" min="1" step="1" value={adding.grams} onChange={event => setAdding({ ...adding, grams: event.target.value })} /></Field><Field label="Ana besin"><select value={adding.alternative} onChange={event => setAdding({ ...adding, alternative: event.target.value })}><option value="">Alternatif değil</option>{meal.lines.filter(line => !line.alternativeFor).map(line => <option value={line.id} key={line.id}>{line.foodName} yerine</option>)}</select></Field></div>
        <FoodPicker label="Besin ekle" pick={food => addFood(meal, food)} />
        <button className="text-button" onClick={() => setAdding(null)}>Besin eklemeyi kapat</button>
      </div> : <button className="text-button" onClick={() => setAdding({ mealId: meal.id, grams: '', alternative: '' })}>＋ Besin ekle</button>}
      <div className="toolbar flush"><Nutrition totals={toTotals(sum([meal]))} compact={weekly} /><button className="text-button danger-text" onClick={() => setPlan({ ...plan, meals: plan.meals.filter(row => row.id !== meal.id) })}>Öğünü sil</button></div>
    </section>
  }
  return <Modal title={templates ? 'Şablon düzenleyici' : 'Beslenme planı düzenleyici'} wide={weekly && step > 1} close={close}>
    <div className="steps" aria-label="Plan adımları">{['Plan bilgileri', 'Öğünler', 'Özet'].map((label, index) => <span className={step === index + 1 ? 'active' : ''} key={label}>{label}</span>)}</div>
    {step === 1 && <form className="form" onSubmit={event => {
      const data = values(event)
      const planType = data.planType as PlanType
      if (!data.title.trim()) return
      if (plan.meals.some(meal => meal.day > dayCount(planType)) && !confirm('Kapsam dışındaki günlerin öğünleri silinsin mi?')) return
      setPlan({ ...plan, title: data.title.trim(), description: data.description ?? '', clientId: id ? plan.clientId : data.clientId ?? '', planType, targetKcal: data.targetKcal ?? '', energyFormula: data.energyFormula ?? '', meals: plan.meals.filter(meal => meal.day <= dayCount(planType)) })
      setStep(2)
    }}><Field label={templates ? 'Şablon adı' : 'Plan adı'}><input name="title" defaultValue={plan.title} required maxLength={100} /></Field>
      {templates && <Field label="Açıklama"><textarea name="description" defaultValue={plan.description} maxLength={500} /></Field>}
      {!templates && <Field label="Danışan"><select name="clientId" defaultValue={plan.clientId} required disabled={!!id}><option value="">Danışan seç</option>{clients.filter(row => row.isActive || row.id === plan.clientId).map(row => <option key={row.id} value={row.id}>{row.fullName}</option>)}</select></Field>}
      <Field label="Süre"><select name="planType" defaultValue={plan.planType}><option value="Daily">Günlük (1 gün)</option><option value="Weekly">Haftalık (7 gün)</option></select></Field>
      {!templates && <div className="form-grid"><Field label="Günlük enerji hedefi (kcal, 800–6000)"><input name="targetKcal" type="number" min="800" max="6000" defaultValue={plan.targetKcal} /></Field><Field label="Enerji formülü"><select name="energyFormula" defaultValue={plan.energyFormula}><option value="">Seçim yok</option>{Object.entries(formulaNames).map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select></Field></div>}
      {!templates && <p className="muted">Hedef enerjiyi “Enerji hesabı” sayfasında danışanın son ölçümüyle hesaplayabilirsin.</p>}
      <button className="button primary">Öğünlere geç</button></form>}
    {step === 2 && <div className="stack">
      <DayColumns weekly={weekly}>{days.map(day => <div className="week-day" key={day}>
        {weekly && <h3>{dayName(plan.planType, day)} · {decimal(sum(mealsOf(day)).kcal, 0)} kcal</h3>}
        {mealsOf(day).map(mealCard)}
        <button className="button secondary" onClick={() => addMeal(day)}>＋ Öğün ekle</button>
        {weekly && day > 1 && !mealsOf(day).length && mealsOf(day - 1).length > 0 && <button className="text-button" onClick={() => copyDay(day)}>{dayNames[day - 2]} gününü kopyala</button>}
      </div>)}</DayColumns>
      <Message>{error}</Message>
      <div className="form-actions"><button className="button secondary" onClick={() => setStep(1)}>Geri</button><button className="button primary" onClick={() => {
        if (!plan.meals.length || days.some(day => !mealsOf(day).length) || plan.meals.some(meal => !meal.name.trim() || !meal.time || !meal.lines.some(line => !line.alternativeFor))) { setError('Her gün için öğün ekle. Her öğüne bir ad, saat ve en az bir ana besin gir.'); return }
        setError(''); setStep(3)
      }}>Özeti incele</button></div>
    </div>}
    {step === 3 && <div className="stack">
      <p>{plan.title}{!templates && plan.clientId ? ' · ' + (clients.find(row => row.id === plan.clientId)?.fullName ?? '') : ''} · {planTypeNames[plan.planType]}{plan.targetKcal ? ' · Hedef ' + plan.targetKcal + ' kcal' : ''}{plan.energyFormula ? ' · ' + formulaLabel(plan.energyFormula) : ''}</p>
      <p className="muted">{weekly ? 'Haftalık toplam' : 'Günlük toplam'}</p><Nutrition totals={toTotals(sum(plan.meals))} />
      <DayColumns weekly={weekly}>{days.map(day => <section className={weekly ? 'week-day' : 'record'} key={day}><h3>{dayName(plan.planType, day)}</h3><Nutrition totals={toTotals(sum(mealsOf(day)))} compact={weekly} />{mealsOf(day).map(meal => <div className={weekly ? 'record meal-preview' : 'meal-preview'} key={meal.id}><strong>{meal.time} · {meal.name}</strong><ul>{meal.lines.map(line => <li key={line.id}>{line.foodName} · {decimal(grams(line))} g{line.alternativeFor ? ' (alternatif)' : ''}</li>)}</ul></div>)}</section>)}</DayColumns>
      {warnings.map(row => <Message key={row.foodItemId} type={row.type === 'Allergy' ? 'error' : 'info'}>{row.foodName} · {row.type === 'Allergy' ? 'Danışanın alerji listesinde. Kaydetmeden önce çıkar.' : 'Danışan bu besini sevmiyor.'}</Message>)}
      <p className="muted">Toplamlar tahminidir; kayıttan sonra sunucunun hesapladığı değerler geçerlidir. Hesaplamalar profesyonel değerlendirme yerine geçmez.</p>
      <Message>{action.error}</Message>
      <div className="form-actions"><button className="button secondary" onClick={() => setStep(2)}>Geri</button><button className="button primary" disabled={action.busy || warnings.some(row => row.type === 'Allergy')} onClick={save}>{action.busy ? 'Kaydediliyor…' : 'Kaydet'}</button></div>
    </div>}
  </Modal>
}
