import { useState } from 'react'
import type { Client, Food, Meal, Plan } from '../api/models'
import { dateKey, newId, subscriptionFor } from '../demo'
import type { DemoProps } from './Clients'
import { Badge, Empty, Field, Message, Modal, Table, Title, useAction, values } from '../ui'

const states = { Draft: 'Taslak', Published: 'Yayımlandı', Archived: 'Arşivde' }
const number = (value: number) => value.toLocaleString('tr-TR', { maximumFractionDigits: 1 })
function totals(meals: Meal[], foods: Food[]) {
  return meals.flatMap(meal => meal.lines).reduce((sum, line) => {
    const food = foods.find(row => row.id === line.foodId)
    if (!food || line.alternativeFor) return sum
    const factor = food.portionGrams * line.quantity / 100
    return { kcal: sum.kcal + food.energyKcalPer100g * factor, p: sum.p + food.proteinPer100g * factor, c: sum.c + food.carbsPer100g * factor, f: sum.f + food.fatPer100g * factor }
  }, { kcal: 0, p: 0, c: 0, f: 0 })
}
function Nutrition({ meals, foods }: { meals: Meal[]; foods: Food[] }) {
  const sum = totals(meals, foods)
  const macroKcal = sum.p * 4 + sum.c * 4 + sum.f * 9
  const part = (value: number, factor: number) => number(value) + ' g' + (macroKcal ? ' (%' + number(value * factor / macroKcal * 100) + ')' : '')
  return <span className="nutrition"><strong>{number(sum.kcal)} kcal</strong><span>Protein {part(sum.p, 4)}</span><span>Karbonhidrat {part(sum.c, 4)}</span><span>Yağ {part(sum.f, 9)}</span></span>
}
function restrictionNotes(meals: Meal[], client: Client | undefined, foods: Food[]) {
  const foodIds = new Set(meals.flatMap(meal => meal.lines.map(line => line.foodId)))
  return (client?.restrictions ?? []).filter(item => foodIds.has(item.foodItemId)).map(item => ({ ...item, foodName: foods.find(food => food.id === item.foodItemId)?.name ?? item.foodName }))
}

function copyMeals(meals: Meal[]): Meal[] {
  return meals.map(meal => {
    const lineIds = new Map(meal.lines.map(line => [line.id, newId()]))
    return {
      ...meal,
      id: newId(),
      lines: meal.lines.map(line => ({
        ...line,
        id: lineIds.get(line.id)!,
        alternativeFor: line.alternativeFor ? lineIds.get(line.alternativeFor) : undefined,
      })),
    }
  })
}

export default function Plans({ demo, update, templates }: DemoProps & { templates: boolean }) {
  const [display, show] = useState<Plan | null>(null)
  const [edit, setEdit] = useState<Plan | 'new' | null>(null)
  const [copy, setCopy] = useState<Plan | null>(null)
  const [notice, setNotice] = useState('')
  const [filter, setFilter] = useState('')
  const action = useAction()
  const items = templates ? demo.templates : demo.plans
  const list = items.filter(item => !filter || item.clientId === filter)
  const sub = subscriptionFor(demo)
  function save(plan: Plan) {
    update(data => {
      const key = templates ? 'templates' : 'plans'
      const rows = data[key]
      return { ...data, [key]: rows.some(row => row.id === plan.id) ? rows.map(row => row.id === plan.id ? plan : row) : [...rows, plan] }
    })
    setEdit(null)
  }
  function publish(plan: Plan) {
    void action.run(() => {
      const client = demo.clients.find(row => row.id === plan.clientId)
      if (!client?.isActive) throw new Error('Plan yayımlamak için danışan aktif olmalı.')
      const allergies = restrictionNotes(plan.meals, client, demo.foods).filter(item => item.type === 'Allergy')
      if (allergies.length) throw new Error('Plan alerjen içeriyor: ' + allergies.map(item => item.foodName).join(', '))
      const date = dateKey()
      update(data => ({ ...data, plans: data.plans.map(row => row.id === plan.id ? { ...row, state: 'Published', publishedDate: date } : row.clientId === plan.clientId && row.state === 'Published' ? { ...row, state: 'Archived', archivedDate: date } : row) }))
      setNotice('Plan yayımlandı.')
    })
  }
  return <><Title title={templates ? 'Plan şablonları' : 'Beslenme planları'} subtitle={templates ? 'Yeniden kullanılabilir öğün şablonları oluştur ve danışana kopyala.' : 'Günlük ve haftalık planları hazırla, yayımla ve arşivle.'} action={<button className="button primary" disabled={!templates && sub.status !== 'Active'} onClick={() => setEdit('new')}>{templates ? '＋ Şablon oluştur' : '＋ Plan oluştur'}</button>} />
    <Message type="info">{!templates && sub.status !== 'Active' && 'Yeni plan oluşturmak için aboneliğini yenile.'}</Message><Message type="success">{notice}</Message><Message>{action.error}</Message>
    <section className="panel">{!templates && <div className="toolbar"><select aria-label="Danışana göre filtrele" value={filter} onChange={event => setFilter(event.target.value)}><option value="">Tüm danışanlar</option>{demo.clients.map(client => <option key={client.id} value={client.id}>{client.fullName}</option>)}</select></div>}
      <Table heads={templates ? ['Şablon', 'Süre', 'Öğün', ''] : ['Plan', 'Danışan', 'Süre', 'Durum', '']}>{list.map(plan => <tr key={plan.id}><td><strong>{plan.title}</strong></td>{!templates && <td>{demo.clients.find(row => row.id === plan.clientId)?.fullName ?? '—'}</td>}<td>{plan.days} gün</td>{templates ? <td>{plan.meals.length}</td> : <td><Badge tone={plan.state === 'Published' ? 'green' : plan.state === 'Draft' ? 'amber' : 'gray'}>{states[plan.state]}</Badge></td>}<td className="row-actions">
        <button className="text-button" onClick={() => show(plan)}>Görüntüle</button>{(templates || plan.state === 'Draft') && <><button className="text-button" onClick={() => setEdit(plan)}>Düzenle</button><button className="text-button danger-text" onClick={() => { if (confirm(plan.title + ' silinsin mi?')) update(data => ({ ...data, [templates ? 'templates' : 'plans']: items.filter(row => row.id !== plan.id) })) }}>Sil</button></>}
        {templates ? <button className="text-button" disabled={sub.status !== 'Active'} onClick={() => setCopy(plan)}>Danışana kopyala</button> : plan.state === 'Draft' ? <button className="text-button" disabled={action.busy} onClick={() => publish(plan)}>Yayımla</button> : plan.state === 'Published' && <button className="text-button" onClick={() => { if (confirm(plan.title + ' arşivlensin mi?')) save({ ...plan, state: 'Archived', archivedDate: dateKey() }) }}>Arşivle</button>}
      </td></tr>)}</Table>{!list.length && <Empty>Kayıt bulunamadı.</Empty>}</section>
    {display && <Modal title={display.title} close={() => show(null)}><PlanReview plan={display} foods={demo.foods} /></Modal>}
    {edit && <PlanEditor original={edit === 'new' ? undefined : edit} templates={templates} foods={demo.foods} clients={demo.clients} close={() => setEdit(null)} save={save} />}
    {copy && <Modal title="Şablonu danışana kopyala" close={() => setCopy(null)}><form className="form" onSubmit={event => {
      const data = values(event)
      const client = demo.clients.find(row => row.id === data.clientId)
      if (!client?.isActive || !data.title.trim()) return
      const { days, meals, targetKcal, energyFormula } = copy
      const draft: Plan = { id: newId(), clientId: client.id, title: data.title.trim(), days, meals: copyMeals(meals), targetKcal, energyFormula, state: 'Draft' }
      update(state => ({ ...state, plans: [...state.plans, draft] }))
      const warnings = restrictionNotes(draft.meals, client, demo.foods)
      setNotice('Taslak plan oluşturuldu.' + (warnings.length ? ' Kısıtlı besinler: ' + warnings.map(row => row.foodName).join(', ') : ''))
      setCopy(null)
    }}><Field label="Danışan"><select name="clientId" required defaultValue=""><option value="">Danışan seç</option>{demo.clients.filter(row => row.isActive).map(row => <option key={row.id} value={row.id}>{row.fullName}</option>)}</select></Field><Field label="Plan adı"><input name="title" defaultValue={copy.title} maxLength={100} required /></Field><div className="form-actions"><button className="button primary">Taslak oluştur</button></div></form></Modal>}
  </>
}

function PlanReview({ plan, foods }: { plan: Plan; foods: Food[] }) {
  return <div className="stack">{plan.targetKcal && <p>Günlük hedef {plan.targetKcal} kcal{plan.energyFormula ? ' · ' + plan.energyFormula : ''}</p>}<Nutrition meals={plan.meals} foods={foods} />{Array.from({ length: plan.days }, (_, index) => index + 1).map(day => {
    const meals = plan.meals.filter(item => item.day === day)
    return <section className="record" key={day}><h3>{day}. gün</h3><Nutrition meals={meals} foods={foods} />{meals.map(meal => <div className="meal-preview" key={meal.id}><strong>{meal.time} · {meal.name}</strong><Nutrition meals={[meal]} foods={foods} /><ul>{meal.lines.map(line => <li key={line.id}>{foods.find(food => food.id === line.foodId)?.name} · {line.quantity} porsiyon{line.alternativeFor ? ' (alternatif)' : ''}</li>)}</ul></div>)}</section>
  })}</div>
}
function PlanEditor({ original, templates, clients, foods, close, save }: { original?: Plan; templates: boolean; clients: Client[]; foods: Food[]; close: () => void; save: (plan: Plan) => void }) {
  const [step, setStep] = useState(1)
  const [plan, setPlan] = useState<Plan>(() => original ? structuredClone(original) : { id: newId(), title: '', days: 1, state: 'Draft', meals: [] })
  const [error, setError] = useState('')
  const warnings = restrictionNotes(plan.meals, clients.find(row => row.id === plan.clientId), foods)
  function patch(meal: Meal) { setPlan(data => ({ ...data, meals: data.meals.map(row => row.id === meal.id ? meal : row) })) }
  return <Modal title={templates ? 'Şablon düzenleyici' : 'Beslenme planı düzenleyici'} close={close}>
    <div className="steps" aria-label="Plan adımları">{['Plan bilgileri', 'Öğünler', 'Özet'].map((label, index) => <span className={step === index + 1 ? 'active' : ''} key={label}>{label}</span>)}</div>
    {step === 1 && <form className="form" onSubmit={event => {
      const data = values(event)
      if (!data.title.trim()) return
      if (plan.meals.some(meal => meal.day > Number(data.days)) && !confirm('Kapsam dışındaki günlerin öğünleri silinsin mi?')) return
      setPlan({ ...plan, title: data.title.trim(), clientId: data.clientId || undefined, days: Number(data.days), targetKcal: data.targetKcal ? Number(data.targetKcal) : undefined, energyFormula: data.energyFormula || undefined, meals: plan.meals.filter(meal => meal.day <= Number(data.days)) })
      setStep(2)
    }}><Field label={templates ? 'Şablon adı' : 'Plan adı'}><input name="title" defaultValue={plan.title} required maxLength={100} /></Field>
      {!templates && <Field label="Danışan"><select name="clientId" defaultValue={plan.clientId ?? ''} required><option value="">Danışan seç</option>{clients.filter(row => row.isActive || row.id === plan.clientId).map(row => <option key={row.id} value={row.id}>{row.fullName}</option>)}</select></Field>}
      <Field label="Süre"><select name="days" defaultValue={plan.days}><option value="1">Günlük</option><option value="7">Haftalık</option></select></Field>
      {!templates && <div className="form-grid"><Field label="Günlük enerji hedefi (kcal)"><input name="targetKcal" type="number" min="1" defaultValue={plan.targetKcal} /></Field><Field label="Enerji formülü"><select name="energyFormula" defaultValue={plan.energyFormula ?? ''}><option value="">Seçim yok</option><option value="Mifflin–St Jeor">Mifflin–St Jeor</option><option value="Harris–Benedict">Harris–Benedict</option></select></Field></div>}
      <button className="button primary">Öğünlere geç</button></form>}
    {step === 2 && <div className="stack">
      {plan.meals.map(meal => <section className="record stack" key={meal.id}>
        <div className="toolbar flush"><Field label="Gün"><select value={meal.day} onChange={event => patch({ ...meal, day: Number(event.target.value) })}>{Array.from({ length: plan.days }, (_, i) => <option value={i + 1} key={i}>{i + 1}. gün</option>)}</select></Field><Field label="Öğün adı"><input value={meal.name} maxLength={60} onChange={event => patch({ ...meal, name: event.target.value })} /></Field><Field label="Saat"><input type="time" value={meal.time} onChange={event => patch({ ...meal, time: event.target.value })} /></Field><button className="text-button danger-text" onClick={() => setPlan({ ...plan, meals: plan.meals.filter(row => row.id !== meal.id) })}>Öğünü sil</button></div>
        {meal.lines.map(line => <div className="list-line" key={line.id}><span>{foods.find(food => food.id === line.foodId)?.name} · {line.quantity} porsiyon{line.alternativeFor ? ' (alternatif)' : ''}</span><button className="text-button" onClick={() => patch({ ...meal, lines: meal.lines.filter(row => row.id !== line.id && row.alternativeFor !== line.id) })}>Kaldır</button></div>)}
        <form className="toolbar flush" onSubmit={event => {
          const data = values(event)
          patch({ ...meal, lines: [...meal.lines, { id: newId(), foodId: data.food, quantity: Number(data.quantity), alternativeFor: data.alternative || undefined }] })
          event.currentTarget.reset()
        }}><Field label="Besin"><select name="food" defaultValue="" required><option value="">Besin seç</option>{foods.map(food => <option key={food.id} value={food.id}>{food.name}</option>)}</select></Field><Field label="Porsiyon"><input name="quantity" type="number" min=".01" step=".01" defaultValue="1" required /></Field><Field label="Ana besin"><select name="alternative"><option value="">Alternatif değil</option>{meal.lines.filter(line => !line.alternativeFor).map(line => <option value={line.id} key={line.id}>{foods.find(food => food.id === line.foodId)?.name}</option>)}</select></Field><button className="button secondary">Besin ekle</button></form>
        <Nutrition meals={[meal]} foods={foods} />
      </section>)}
      <button className="button secondary" onClick={() => setPlan({ ...plan, meals: [...plan.meals, { id: newId(), day: 1, name: '', time: '08:30', lines: [] }] })}>＋ Öğün ekle</button><Message>{error}</Message>
      <div className="form-actions"><button className="button secondary" onClick={() => setStep(1)}>Geri</button><button className="button primary" onClick={() => {
        if (!plan.meals.length || Array.from({ length: plan.days }, (_, i) => i + 1).some(day => !plan.meals.some(meal => meal.day === day)) || plan.meals.some(meal => !meal.name.trim() || !meal.time || !meal.lines.some(line => !line.alternativeFor))) { setError('Her gün için öğün ekle. Her öğüne bir ad, saat ve en az bir ana besin gir.'); return }
        setError(''); setStep(3)
      }}>Özeti incele</button></div>
    </div>}
    {step === 3 && <div className="stack"><PlanReview plan={plan} foods={foods} />
      {warnings.map(row => <Message key={row.foodItemId} type={row.type === 'Allergy' ? 'error' : 'info'}>{row.foodName} · {row.type === 'Allergy' ? 'Alerji. Kaydetmeden önce çıkar.' : 'Danışan bu besini sevmiyor.'}</Message>)}
      <div className="form-actions"><button className="button secondary" onClick={() => setStep(2)}>Geri</button><button className="button primary" disabled={warnings.some(row => row.type === 'Allergy')} onClick={() => save(plan)}>Kaydet</button></div>
    </div>}
  </Modal>
}
