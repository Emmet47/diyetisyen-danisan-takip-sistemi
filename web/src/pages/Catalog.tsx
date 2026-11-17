import { useState } from 'react'
import type { Food } from '../api/models'
import { dateLabel, dateKey, newId } from '../demo'
import type { DemoProps } from './Clients'
import { Badge, Empty, Field, Message, Modal, Table, Title, useAction, values } from '../ui'

const externalFood: Food = { id: 'usda-blueberry', name: 'Yaban mersini', portionName: '1 kase', portionGrams: 148, energyKcalPer100g: 57, proteinPer100g: .74, carbsPer100g: 14.5, fatPer100g: .33, sourceName: 'USDA FoodData Central', sourceUpdatedAt: dateKey(), editable: false }
export default function Catalog({ demo, update }: DemoProps) {
  const [query, setQuery] = useState('')
  const [source, setSource] = useState('all')
  const [external, setExternal] = useState(false)
  const [edit, setEdit] = useState<Food | 'new' | null>(null)
  const action = useAction()
  const rows = [...demo.foods, ...(external && !demo.foods.some(item => item.id === externalFood.id) ? [externalFood] : [])]
    .filter(food => food.name.toLocaleLowerCase('tr').includes(query.trim().toLocaleLowerCase('tr')) && (source === 'all' || food.sourceName.startsWith(source)))
  return <><Title title="Besin kataloğu" subtitle="Besinleri ara ve porsiyon ile 100 g besin değerlerini incele." action={<button className="button primary" onClick={() => setEdit('new')}>＋ Besin ekle</button>} />
    <Message>{action.error}</Message>
    <section className="panel"><div className="toolbar"><input className="search" placeholder="Besin adı ara…" type="search" aria-label="Besin ara" value={query} onChange={event => setQuery(event.target.value)} /><select aria-label="Besin kaynağı" value={source} onChange={event => setSource(event.target.value)}><option value="all">Tüm kaynaklar</option><option value="Yerel">Yerel</option><option value="USDA">USDA</option></select><label className="check"><input type="checkbox" checked={external} onChange={event => setExternal(event.target.checked)} /> Örnek USDA sonucunu göster</label></div>
    <Table heads={['Besin', 'Porsiyon', 'Enerji / 100 g', 'P / K / Y', 'Kaynak', '']}>{rows.map(row => <tr key={row.id}><td><strong>{row.name}</strong></td><td>{row.portionName} · {row.portionGrams} g</td><td>{row.energyKcalPer100g} kcal</td><td>{row.proteinPer100g} / {row.carbsPer100g} / {row.fatPer100g} g</td><td><Badge tone="gray">{row.sourceName}</Badge><small className="cell-note">{dateLabel(row.sourceUpdatedAt)}</small></td><td className="right">
      {row.editable && <><button className="text-button" onClick={() => setEdit(row)}>Düzenle</button>{' '}<button className="text-button danger-text" onClick={() => {
        if (!confirm(row.name + ' silinsin mi?')) return
        void action.run(() => {
          if ([...demo.plans, ...demo.templates].some(plan => plan.meals.some(meal => meal.lines.some(line => line.foodId === row.id))) || demo.clients.some(client => client.restrictions?.some(item => item.foodItemId === row.id))) throw new Error('Bu besin bir plan, şablon veya danışan kısıtında kullanılıyor.')
          update(data => ({ ...data, foods: data.foods.filter(food => food.id !== row.id) }))
        })
      }}>Sil</button></>}
      {row.id === externalFood.id && !demo.foods.some(food => food.id === row.id) && <button className="text-button" onClick={() => setEdit(row)}>Aktar</button>}
    </td></tr>)}</Table>{!rows.length && <Empty>Aramaya uygun besin bulunamadı.</Empty>}</section>
    {edit && <FoodForm food={edit === 'new' ? undefined : edit} close={() => setEdit(null)} save={item => {
      update(data => ({ ...data, foods: data.foods.some(food => food.id === item.id) ? data.foods.map(food => food.id === item.id ? item : food) : [...data.foods, item] }))
      setEdit(null)
    }} />}
  </>
}

function FoodForm({ food, close, save }: { food?: Food; close: () => void; save: (food: Food) => void }) {
  const action = useAction()
  return <Modal title={!food ? 'Yeni besin' : food.editable ? 'Besini düzenle' : 'USDA sonucunu aktar'} close={close}><form className="form" onSubmit={event => {
    const data = values(event)
    void action.run(() => {
      if (!data.name.trim() || !data.portionName.trim()) throw new Error('Besin ve porsiyon adını gir.')
      save({
        id: food?.id ?? newId(), name: data.name.trim(), portionName: data.portionName.trim(), portionGrams: Number(data.portionGrams),
        energyKcalPer100g: Number(data.energyKcalPer100g), proteinPer100g: Number(data.proteinPer100g), carbsPer100g: Number(data.carbsPer100g), fatPer100g: Number(data.fatPer100g),
        sourceName: food?.sourceName ?? 'Yerel', sourceUpdatedAt: dateKey(), editable: true,
      })
    })
  }}><Field label="Besin adı"><input name="name" defaultValue={food?.name} maxLength={120} required /></Field><Field label="Porsiyon adı"><input name="portionName" defaultValue={food?.portionName} maxLength={60} required /></Field><div className="form-grid">{([['portionGrams', 'Porsiyon (g)', '0.01'], ['energyKcalPer100g', '100 g enerji (kcal)', '0'], ['proteinPer100g', '100 g protein (g)', '0'], ['carbsPer100g', '100 g karbonhidrat (g)', '0'], ['fatPer100g', '100 g yağ (g)', '0']] as const).map(([name, label, min]) => <Field label={label} key={name}><input name={name} step=".01" min={min} type="number" defaultValue={food?.[name]} required /></Field>)}</div><Message>{action.error}</Message><div className="form-actions"><button className="button primary" disabled={action.busy}>Kaydet</button></div></form></Modal>
}
