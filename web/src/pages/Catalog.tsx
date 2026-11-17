import { useEffect, useState } from 'react'
import { foodsApi } from '../api/foods'
import type { FoodSource } from '../api/foods'
import type { Food, FoodInput, FoodSearchResult } from '../api/models'
import { dateLabel, decimal } from '../format'
import { Badge, Empty, Field, Message, Modal, Table, Title, Wait, useAction, useLoad, values } from '../ui'
import type { PageProps } from '../ui'

/** Yazmayı bitirdikten kısa süre sonra aramayı başlatır (her tuşta istek atılmasın). */
function useDebounced(value: string, delay = 300) {
  const [debounced, setDebounced] = useState(value)
  useEffect(() => {
    const timer = window.setTimeout(() => setDebounced(value), delay)
    return () => window.clearTimeout(timer)
  }, [value, delay])
  return debounced
}

/**
 * Plan, şablon ve kısıt ekranlarında yerel besin seçmek için arama kutusu.
 * Yazdıkça arar; kutu boşken bütün yerel besinler listelenir.
 */
export function FoodPicker({ label = 'Besin', pick }: { label?: string; pick: (food: Food) => void }) {
  const [text, setText] = useState('')
  const query = useDebounced(text.trim())
  const result = useLoad(() => foodsApi.search(query), 'picker:' + query)
  const foods = result.data?.items.filter(item => item.id)
  return <div className="stack">
    <Field label={label}><input type="search" placeholder="Besin adı ara… (boşsa hepsi)" value={text} onChange={event => setText(event.target.value)} onKeyDown={event => { if (event.key === 'Enter') event.preventDefault() }} /></Field>
    {!foods ? <Wait error={result.error} reload={result.reload} /> : foods.length ? <div className="stack picker-list">{foods.map(food => <div className="list-line" key={food.id}><span>{food.name} · {food.portionName} ({decimal(food.portionGrams)} g) · {decimal(food.energyKcalPer100g, 0)} kcal/100 g</span><button type="button" className="text-button" onClick={() => pick(food)}>Seç</button></div>)}</div>
      : <Empty>Yerel kayıtlarda bulunamadı. Besin kataloğundan ekleyebilir ya da USDA'dan aktarabilirsin.</Empty>}
  </div>
}

export default function Catalog(_props: PageProps) {
  const [text, setText] = useState('')
  const [source, setSource] = useState<FoodSource>('')
  const [external, setExternal] = useState(false)
  const [edit, setEdit] = useState<Food | 'new' | null>(null)
  const [notice, setNotice] = useState('')
  // Yazdıkça arar; kutu silinince bütün liste geri gelir.
  const q = useDebounced(text.trim(), external ? 600 : 300)
  const search = useLoad<FoodSearchResult>(() => foodsApi.search(q, source, external), JSON.stringify([q, source, external]))
  const action = useAction(search.reload)
  const rows = search.data?.items
  return <><Title title="Besin kataloğu" subtitle="Besinleri ara, kaynağa göre süz; USDA sonuçlarını kontrol edip yerel kayıtlara aktar." action={<button className="button primary" onClick={() => setEdit('new')}>＋ Besin ekle</button>} />
    <Message>{action.error}</Message><Message type="success">{notice}</Message>
    <form className="panel toolbar" onSubmit={event => { event.preventDefault(); search.reload() }}>
      <input className="search" placeholder="Besin adı ara… (Türkçe yazabilirsin)" type="search" aria-label="Besin ara" value={text} onChange={event => { setText(event.target.value); setNotice('') }} />
      <select aria-label="Besin kaynağı" value={source} onChange={event => setSource(event.target.value as FoodSource)}><option value="">Tüm kaynaklar</option><option value="Yerel">Yerel</option><option value="USDA">USDA</option></select>
      <label className="check"><input type="checkbox" checked={external} onChange={event => setExternal(event.target.checked)} /> USDA FoodData Central'da da ara</label>
      <button className="button secondary">Ara</button>
    </form>
    {search.data?.externalUnavailable && <Message type="info">{search.data.message ?? 'Dış kaynağa şu an ulaşılamıyor, yerel kayıtlar gösteriliyor.'}</Message>}
    {!rows ? <Wait error={search.error} reload={search.reload} /> : <section className="panel">
      <Table heads={['Besin', 'Porsiyon', 'Enerji / 100 g', 'P / K / Y (100 g)', 'Kaynak ve güncellik', '']}>{rows.map((row, index) => <tr key={row.id ?? 'dis-' + row.externalId + index}>
        <td><strong>{row.name}</strong></td><td>{row.portionName} · {decimal(row.portionGrams)} g</td><td>{decimal(row.energyKcalPer100g)} kcal</td>
        <td>{decimal(row.proteinPer100g)} / {decimal(row.carbsPer100g)} / {decimal(row.fatPer100g)} g</td>
        <td><Badge tone={row.id ? 'gray' : 'amber'}>{row.sourceName}</Badge><small className="cell-note">{row.retrievedAt ? 'Alındı: ' + dateLabel(row.retrievedAt) : 'Yerel kayıt'}{row.externalId ? ' · #' + row.externalId : ''}</small></td>
        <td className="row-actions">{row.id ? <><button className="text-button" onClick={() => setEdit(row)}>Düzenle</button><button className="text-button danger-text" disabled={action.busy} onClick={() => {
          if (!confirm(row.name + ' silinsin mi?')) return
          void action.run(async () => { await foodsApi.remove(row.id!); setNotice(row.name + ' silindi.') })
        }}>Sil</button></> : <button className="text-button" onClick={() => setEdit(row)}>Kontrol et ve aktar</button>}</td>
      </tr>)}</Table>{!rows.length && <Empty>Aramaya uygun besin bulunamadı.</Empty>}
      <p className="muted padded">Ortak (hazır yüklenen) besinleri herkes kullanır ama düzenleyemez; planda kullanılan besin silinemez.</p>
    </section>}
    {edit && <FoodForm food={edit === 'new' ? undefined : edit} close={() => setEdit(null)} save={async (input, food) => {
      if (food && !food.id) {
        await foodsApi.import({ externalId: food.externalId!, name: input.name, portionName: input.portionName, portionGrams: input.portionGrams })
        setNotice(input.name + ' yerel kayıtlara aktarıldı.')
      } else if (food?.id) {
        await foodsApi.update(food.id, input)
        setNotice(input.name + ' güncellendi.')
      } else {
        await foodsApi.create(input)
        setNotice(input.name + ' eklendi.')
      }
      setEdit(null)
      search.reload()
    }} />}
  </>
}

function FoodForm({ food, close, save }: { food?: Food; close: () => void; save: (input: FoodInput, food?: Food) => Promise<void> }) {
  const action = useAction()
  const importing = !!food && !food.id
  return <Modal title={!food ? 'Yeni besin' : importing ? 'USDA sonucunu kontrol et ve aktar' : 'Besini düzenle'} close={close}><form className="form" onSubmit={event => {
    const data = values(event)
    void action.run(() => save({
      name: data.name.trim(), portionName: data.portionName.trim(), portionGrams: Number(data.portionGrams),
      energyKcalPer100g: Number(data.energyKcalPer100g ?? food?.energyKcalPer100g), proteinPer100g: Number(data.proteinPer100g ?? food?.proteinPer100g),
      carbsPer100g: Number(data.carbsPer100g ?? food?.carbsPer100g), fatPer100g: Number(data.fatPer100g ?? food?.fatPer100g),
    }, food))
  }}>
    {importing && <Message type="info">Besin değerleri USDA'dan geldiği gibi aktarılır. Türkçe ad ve porsiyon bilgisini düzenleyebilirsin.</Message>}
    <Field label="Besin adı"><input name="name" defaultValue={food?.name} maxLength={150} required /></Field>
    <div className="form-grid"><Field label="Porsiyon adı"><input name="portionName" defaultValue={food?.portionName} maxLength={50} required /></Field><Field label="Porsiyon (g)"><input name="portionGrams" type="number" step=".1" min="0.1" defaultValue={food?.portionGrams} required /></Field></div>
    {importing ? <p className="muted">100 g: {decimal(food.energyKcalPer100g)} kcal · Protein {decimal(food.proteinPer100g)} g · Karbonhidrat {decimal(food.carbsPer100g)} g · Yağ {decimal(food.fatPer100g)} g</p>
      : <div className="form-grid">{([['energyKcalPer100g', '100 g enerji (kcal)', 900], ['proteinPer100g', '100 g protein (g)', 100], ['carbsPer100g', '100 g karbonhidrat (g)', 100], ['fatPer100g', '100 g yağ (g)', 100]] as const).map(([name, label, max]) => <Field label={label} key={name}><input name={name} step=".01" min="0" max={max} type="number" defaultValue={food?.[name]} required /></Field>)}</div>}
    <Message>{action.error}</Message><div className="form-actions"><button type="button" className="button secondary" onClick={close}>Vazgeç</button><button className="button primary" disabled={action.busy}>{importing ? 'Aktar' : 'Kaydet'}</button></div>
  </form></Modal>
}
