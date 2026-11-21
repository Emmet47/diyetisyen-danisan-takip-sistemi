// Hazır plan şablonları (10 günlük + 10 haftalık). "Plan şablonları → Hazır şablonları ekle" ile
// diyetisyenin hesabına mevcut API (POST /api/templates) üzerinden eklenir; backend'e ayrı veri yüklenmez.
// Besinler 30 ortak (hazır yüklenen) besinden seçildi, miktarlar gram. Enerji değerleri ortak besinlerin
// 100 g değerleriyle hesaplandı ve hedefe ±%2 yakın. Örnek şablondur: danışana kopyalayıp kişiye göre düzenlenmeli.
// Hesaplamalar profesyonel değerlendirme yerine geçmez. (Üretim betiği: elle tasarlanan gün menüleri hedef
// enerjiye oranlanarak ölçeklendi; haftalık şablonlarda 2–3 gün menüsü dönüşümlü kullanılıyor.)

export type PresetMeal = { name: string; time: string; items: [food: string, grams: number][] }
export type PresetTemplate = { name: string; description: string; planType: 'Daily' | 'Weekly'; days: PresetMeal[][] }

export const presetTemplates: PresetTemplate[] = [
  {
    name: "Dengeli beslenme 1500 kcal", planType: "Daily",
    description: "Kilo vermek için dengeli makro dağılımlı günlük örnek plan.",
    days: [
      [
        { name: "Kahvaltı", time: "08:00:00", items: [["Yumurta", 100], ["Beyaz peynir", 30], ["Tam buğday ekmeği", 50], ["Domates", 120], ["Salatalık", 100], ["Siyah zeytin", 15]] },
        { name: "Öğle", time: "12:30:00", items: [["Tavuk göğsü (ızgara)", 115], ["Bulgur pilavı", 145], ["Yoğurt (tam yağlı)", 145], ["Salatalık", 100]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Elma", 175], ["Ceviz içi", 12]] },
        { name: "Akşam", time: "19:30:00", items: [["Kırmızı mercimek (pişmiş)", 195], ["Tam buğday ekmeği", 50], ["Ispanak (pişmiş)", 175], ["Zeytinyağı", 5], ["Yoğurt (tam yağlı)", 145]] },
      ],
    ],
  },
  {
    name: "Dengeli beslenme 1800 kcal", planType: "Daily",
    description: "Kilo korumaya yakın, dengeli günlük örnek plan.",
    days: [
      [
        { name: "Kahvaltı", time: "08:00:00", items: [["Yulaf ezmesi", 56], ["Süt (yarım yağlı)", 225], ["Muz", 135], ["Ceviz içi", 13], ["Bal", 8]] },
        { name: "Öğle", time: "12:30:00", items: [["Kuru fasulye (haşlanmış)", 200], ["Pirinç pilavı", 170], ["Salatalık", 150], ["Domates", 120]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Yoğurt (tam yağlı)", 170], ["Portakal", 170]] },
        { name: "Akşam", time: "19:30:00", items: [["Somon (fırında)", 135], ["Haşlanmış patates", 170], ["Brokoli (haşlanmış)", 170], ["Zeytinyağı", 6]] },
      ],
    ],
  },
  {
    name: "Dengeli beslenme 2000 kcal", planType: "Daily",
    description: "Kilo koruma için dengeli günlük örnek plan.",
    days: [
      [
        { name: "Kahvaltı", time: "08:00:00", items: [["Yumurta", 125], ["Kaşar peyniri", 25], ["Tam buğday ekmeği", 65], ["Domates", 120], ["Siyah zeytin", 20]] },
        { name: "Öğle", time: "12:30:00", items: [["Nohut (haşlanmış)", 200], ["Bulgur pilavı", 190], ["Yoğurt (tam yağlı)", 190]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Muz", 150], ["Badem", 15]] },
        { name: "Akşam", time: "19:30:00", items: [["Dana kıyma (pişmiş)", 125], ["Haşlanmış makarna", 190], ["Ispanak (pişmiş)", 225], ["Salatalık", 150]] },
      ],
    ],
  },
  {
    name: "Dengeli beslenme 2500 kcal", planType: "Daily",
    description: "Aktif yetişkin ya da kilo alma için yüksek enerjili günlük örnek plan.",
    days: [
      [
        { name: "Kahvaltı", time: "08:00:00", items: [["Yumurta", 165], ["Beyaz peynir", 50], ["Tam buğday ekmeği", 80], ["Domates", 120], ["Salatalık", 100], ["Siyah zeytin", 25]] },
        { name: "Öğle", time: "12:30:00", items: [["Tavuk göğsü (ızgara)", 195], ["Bulgur pilavı", 245], ["Yoğurt (tam yağlı)", 245], ["Salatalık", 100]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Elma", 295], ["Ceviz içi", 20]] },
        { name: "Akşam", time: "19:30:00", items: [["Kırmızı mercimek (pişmiş)", 325], ["Tam buğday ekmeği", 80], ["Ispanak (pişmiş)", 295], ["Zeytinyağı", 8], ["Yoğurt (tam yağlı)", 245]] },
      ],
    ],
  },
  {
    name: "Akdeniz tipi 2000 kcal", planType: "Daily",
    description: "Zeytinyağı, baklagil, sebze ve balık ağırlıklı Akdeniz tipi günlük örnek plan.",
    days: [
      [
        { name: "Kahvaltı", time: "08:00:00", items: [["Beyaz peynir", 35], ["Siyah zeytin", 35], ["Domates", 120], ["Salatalık", 150], ["Tam buğday ekmeği", 60], ["Zeytinyağı", 6]] },
        { name: "Öğle", time: "12:30:00", items: [["Nohut (haşlanmış)", 200], ["Bulgur pilavı", 185], ["Ispanak (pişmiş)", 225], ["Zeytinyağı", 12]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Yoğurt (tam yağlı)", 185], ["Ceviz içi", 15], ["Portakal", 185]] },
        { name: "Akşam", time: "19:30:00", items: [["Somon (fırında)", 150], ["Brokoli (haşlanmış)", 185], ["Haşlanmış patates", 185], ["Zeytinyağı", 12]] },
      ],
    ],
  },
  {
    name: "Ketojenik 2000 kcal", planType: "Daily",
    description: "Çok düşük karbonhidratlı, yağdan zengin ketojenik günlük örnek plan.",
    days: [
      [
        { name: "Kahvaltı", time: "08:00:00", items: [["Yumurta", 165], ["Beyaz peynir", 65], ["Siyah zeytin", 35], ["Salatalık", 150], ["Zeytinyağı", 11]] },
        { name: "Öğle", time: "12:30:00", items: [["Somon (fırında)", 165], ["Ispanak (pişmiş)", 195], ["Zeytinyağı", 16]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Ceviz içi", 26], ["Badem", 13]] },
        { name: "Akşam", time: "19:30:00", items: [["Dana kıyma (pişmiş)", 165], ["Brokoli (haşlanmış)", 165], ["Kaşar peyniri", 20], ["Zeytinyağı", 11]] },
      ],
    ],
  },
  {
    name: "Düşük karbonhidratlı 1600 kcal", planType: "Daily",
    description: "Karbonhidratı azaltılmış, proteinden zengin günlük örnek plan.",
    days: [
      [
        { name: "Kahvaltı", time: "08:00:00", items: [["Yumurta", 115], ["Beyaz peynir", 35], ["Domates", 120], ["Salatalık", 150], ["Siyah zeytin", 15]] },
        { name: "Öğle", time: "12:30:00", items: [["Tavuk göğsü (ızgara)", 170], ["Brokoli (haşlanmış)", 170], ["Yoğurt (tam yağlı)", 170], ["Zeytinyağı", 11]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Badem", 27], ["Elma", 105]] },
        { name: "Akşam", time: "19:30:00", items: [["Somon (fırında)", 135], ["Ispanak (pişmiş)", 205], ["Kırmızı mercimek (pişmiş)", 115], ["Zeytinyağı", 6]] },
      ],
    ],
  },
  {
    name: "Yüksek proteinli 2200 kcal", planType: "Daily",
    description: "Kas kazanımı için proteinden zengin günlük örnek plan.",
    days: [
      [
        { name: "Kahvaltı", time: "08:00:00", items: [["Yumurta", 160], ["Beyaz peynir", 65], ["Tam buğday ekmeği", 55], ["Domates", 120]] },
        { name: "Öğle", time: "12:30:00", items: [["Tavuk göğsü (ızgara)", 195], ["Bulgur pilavı", 160], ["Yoğurt (tam yağlı)", 160]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Süt (yarım yağlı)", 215], ["Badem", 26], ["Muz", 130]] },
        { name: "Akşam", time: "19:30:00", items: [["Dana kıyma (pişmiş)", 160], ["Kırmızı mercimek (pişmiş)", 215], ["Brokoli (haşlanmış)", 160]] },
      ],
    ],
  },
  {
    name: "Vejetaryen 1800 kcal", planType: "Daily",
    description: "Et içermeyen; yumurta, süt ürünü ve baklagil ağırlıklı günlük örnek plan.",
    days: [
      [
        { name: "Kahvaltı", time: "08:00:00", items: [["Yumurta", 115], ["Beyaz peynir", 35], ["Tam buğday ekmeği", 60], ["Domates", 120], ["Siyah zeytin", 15]] },
        { name: "Öğle", time: "12:30:00", items: [["Kırmızı mercimek (pişmiş)", 230], ["Bulgur pilavı", 175], ["Yoğurt (tam yağlı)", 175]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Muz", 140], ["Ceviz içi", 14]] },
        { name: "Akşam", time: "19:30:00", items: [["Nohut (haşlanmış)", 185], ["Ispanak (pişmiş)", 205], ["Pirinç pilavı", 175], ["Zeytinyağı", 6]] },
      ],
    ],
  },
  {
    name: "Aralıklı oruç 16:8 1800 kcal", planType: "Daily",
    description: "12.00–20.00 arasında üç öğünlük aralıklı oruç (16:8) günlük örnek plan.",
    days: [
      [
        { name: "İlk öğün", time: "12:00:00", items: [["Yumurta", 160], ["Beyaz peynir", 45], ["Tam buğday ekmeği", 80], ["Domates", 120], ["Salatalık", 150], ["Siyah zeytin", 25]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Yoğurt (tam yağlı)", 235], ["Ceviz içi", 19], ["Elma", 285]] },
        { name: "Son öğün", time: "19:30:00", items: [["Tavuk göğsü (ızgara)", 235], ["Bulgur pilavı", 235], ["Brokoli (haşlanmış)", 235], ["Zeytinyağı", 8]] },
      ],
    ],
  },
  {
    name: "Dengeli beslenme 1500 kcal (haftalık)", planType: "Weekly",
    description: "Kilo vermek için dengeli makro dağılımlı haftalık örnek plan.",
    days: [
      [
        { name: "Kahvaltı", time: "08:00:00", items: [["Yumurta", 100], ["Beyaz peynir", 30], ["Tam buğday ekmeği", 50], ["Domates", 120], ["Salatalık", 100], ["Siyah zeytin", 15]] },
        { name: "Öğle", time: "12:30:00", items: [["Tavuk göğsü (ızgara)", 115], ["Bulgur pilavı", 145], ["Yoğurt (tam yağlı)", 145], ["Salatalık", 100]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Elma", 175], ["Ceviz içi", 12]] },
        { name: "Akşam", time: "19:30:00", items: [["Kırmızı mercimek (pişmiş)", 195], ["Tam buğday ekmeği", 50], ["Ispanak (pişmiş)", 175], ["Zeytinyağı", 5], ["Yoğurt (tam yağlı)", 145]] },
      ],
      [
        { name: "Kahvaltı", time: "08:00:00", items: [["Yulaf ezmesi", 47], ["Süt (yarım yağlı)", 185], ["Muz", 110], ["Ceviz içi", 11], ["Bal", 7]] },
        { name: "Öğle", time: "12:30:00", items: [["Kuru fasulye (haşlanmış)", 170], ["Pirinç pilavı", 140], ["Salatalık", 150], ["Domates", 120]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Yoğurt (tam yağlı)", 140], ["Portakal", 140]] },
        { name: "Akşam", time: "19:30:00", items: [["Somon (fırında)", 110], ["Haşlanmış patates", 140], ["Brokoli (haşlanmış)", 140], ["Zeytinyağı", 5]] },
      ],
      [
        { name: "Kahvaltı", time: "08:00:00", items: [["Yumurta", 95], ["Kaşar peyniri", 20], ["Tam buğday ekmeği", 45], ["Domates", 120], ["Siyah zeytin", 15]] },
        { name: "Öğle", time: "12:30:00", items: [["Nohut (haşlanmış)", 150], ["Bulgur pilavı", 140], ["Yoğurt (tam yağlı)", 140]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Muz", 115], ["Badem", 11]] },
        { name: "Akşam", time: "19:30:00", items: [["Dana kıyma (pişmiş)", 95], ["Haşlanmış makarna", 140], ["Ispanak (pişmiş)", 170], ["Salatalık", 150]] },
      ],
      [
        { name: "Kahvaltı", time: "08:00:00", items: [["Yumurta", 100], ["Beyaz peynir", 30], ["Tam buğday ekmeği", 50], ["Domates", 120], ["Salatalık", 100], ["Siyah zeytin", 15]] },
        { name: "Öğle", time: "12:30:00", items: [["Tavuk göğsü (ızgara)", 115], ["Bulgur pilavı", 145], ["Yoğurt (tam yağlı)", 145], ["Salatalık", 100]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Elma", 175], ["Ceviz içi", 12]] },
        { name: "Akşam", time: "19:30:00", items: [["Kırmızı mercimek (pişmiş)", 195], ["Tam buğday ekmeği", 50], ["Ispanak (pişmiş)", 175], ["Zeytinyağı", 5], ["Yoğurt (tam yağlı)", 145]] },
      ],
      [
        { name: "Kahvaltı", time: "08:00:00", items: [["Yulaf ezmesi", 47], ["Süt (yarım yağlı)", 185], ["Muz", 110], ["Ceviz içi", 11], ["Bal", 7]] },
        { name: "Öğle", time: "12:30:00", items: [["Kuru fasulye (haşlanmış)", 170], ["Pirinç pilavı", 140], ["Salatalık", 150], ["Domates", 120]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Yoğurt (tam yağlı)", 140], ["Portakal", 140]] },
        { name: "Akşam", time: "19:30:00", items: [["Somon (fırında)", 110], ["Haşlanmış patates", 140], ["Brokoli (haşlanmış)", 140], ["Zeytinyağı", 5]] },
      ],
      [
        { name: "Kahvaltı", time: "08:00:00", items: [["Yumurta", 95], ["Kaşar peyniri", 20], ["Tam buğday ekmeği", 45], ["Domates", 120], ["Siyah zeytin", 15]] },
        { name: "Öğle", time: "12:30:00", items: [["Nohut (haşlanmış)", 150], ["Bulgur pilavı", 140], ["Yoğurt (tam yağlı)", 140]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Muz", 115], ["Badem", 11]] },
        { name: "Akşam", time: "19:30:00", items: [["Dana kıyma (pişmiş)", 95], ["Haşlanmış makarna", 140], ["Ispanak (pişmiş)", 170], ["Salatalık", 150]] },
      ],
      [
        { name: "Kahvaltı", time: "08:00:00", items: [["Yumurta", 100], ["Beyaz peynir", 30], ["Tam buğday ekmeği", 50], ["Domates", 120], ["Salatalık", 100], ["Siyah zeytin", 15]] },
        { name: "Öğle", time: "12:30:00", items: [["Tavuk göğsü (ızgara)", 115], ["Bulgur pilavı", 145], ["Yoğurt (tam yağlı)", 145], ["Salatalık", 100]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Elma", 175], ["Ceviz içi", 12]] },
        { name: "Akşam", time: "19:30:00", items: [["Kırmızı mercimek (pişmiş)", 195], ["Tam buğday ekmeği", 50], ["Ispanak (pişmiş)", 175], ["Zeytinyağı", 5], ["Yoğurt (tam yağlı)", 145]] },
      ],
    ],
  },
  {
    name: "Dengeli beslenme 1800 kcal (haftalık)", planType: "Weekly",
    description: "Kilo korumaya yakın, dengeli haftalık örnek plan.",
    days: [
      [
        { name: "Kahvaltı", time: "08:00:00", items: [["Yumurta", 115], ["Beyaz peynir", 35], ["Tam buğday ekmeği", 60], ["Domates", 120], ["Salatalık", 100], ["Siyah zeytin", 20]] },
        { name: "Öğle", time: "12:30:00", items: [["Tavuk göğsü (ızgara)", 140], ["Bulgur pilavı", 175], ["Yoğurt (tam yağlı)", 175], ["Salatalık", 100]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Elma", 210], ["Ceviz içi", 14]] },
        { name: "Akşam", time: "19:30:00", items: [["Kırmızı mercimek (pişmiş)", 235], ["Tam buğday ekmeği", 60], ["Ispanak (pişmiş)", 210], ["Zeytinyağı", 6], ["Yoğurt (tam yağlı)", 175]] },
      ],
      [
        { name: "Kahvaltı", time: "08:00:00", items: [["Yulaf ezmesi", 56], ["Süt (yarım yağlı)", 225], ["Muz", 135], ["Ceviz içi", 13], ["Bal", 8]] },
        { name: "Öğle", time: "12:30:00", items: [["Kuru fasulye (haşlanmış)", 200], ["Pirinç pilavı", 170], ["Salatalık", 150], ["Domates", 120]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Yoğurt (tam yağlı)", 170], ["Portakal", 170]] },
        { name: "Akşam", time: "19:30:00", items: [["Somon (fırında)", 135], ["Haşlanmış patates", 170], ["Brokoli (haşlanmış)", 170], ["Zeytinyağı", 6]] },
      ],
      [
        { name: "Kahvaltı", time: "08:00:00", items: [["Yumurta", 115], ["Kaşar peyniri", 25], ["Tam buğday ekmeği", 55], ["Domates", 120], ["Siyah zeytin", 15]] },
        { name: "Öğle", time: "12:30:00", items: [["Nohut (haşlanmış)", 180], ["Bulgur pilavı", 170], ["Yoğurt (tam yağlı)", 170]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Muz", 135], ["Badem", 14]] },
        { name: "Akşam", time: "19:30:00", items: [["Dana kıyma (pişmiş)", 115], ["Haşlanmış makarna", 170], ["Ispanak (pişmiş)", 205], ["Salatalık", 150]] },
      ],
      [
        { name: "Kahvaltı", time: "08:00:00", items: [["Yumurta", 115], ["Beyaz peynir", 35], ["Tam buğday ekmeği", 60], ["Domates", 120], ["Salatalık", 100], ["Siyah zeytin", 20]] },
        { name: "Öğle", time: "12:30:00", items: [["Tavuk göğsü (ızgara)", 140], ["Bulgur pilavı", 175], ["Yoğurt (tam yağlı)", 175], ["Salatalık", 100]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Elma", 210], ["Ceviz içi", 14]] },
        { name: "Akşam", time: "19:30:00", items: [["Kırmızı mercimek (pişmiş)", 235], ["Tam buğday ekmeği", 60], ["Ispanak (pişmiş)", 210], ["Zeytinyağı", 6], ["Yoğurt (tam yağlı)", 175]] },
      ],
      [
        { name: "Kahvaltı", time: "08:00:00", items: [["Yulaf ezmesi", 56], ["Süt (yarım yağlı)", 225], ["Muz", 135], ["Ceviz içi", 13], ["Bal", 8]] },
        { name: "Öğle", time: "12:30:00", items: [["Kuru fasulye (haşlanmış)", 200], ["Pirinç pilavı", 170], ["Salatalık", 150], ["Domates", 120]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Yoğurt (tam yağlı)", 170], ["Portakal", 170]] },
        { name: "Akşam", time: "19:30:00", items: [["Somon (fırında)", 135], ["Haşlanmış patates", 170], ["Brokoli (haşlanmış)", 170], ["Zeytinyağı", 6]] },
      ],
      [
        { name: "Kahvaltı", time: "08:00:00", items: [["Yumurta", 115], ["Kaşar peyniri", 25], ["Tam buğday ekmeği", 55], ["Domates", 120], ["Siyah zeytin", 15]] },
        { name: "Öğle", time: "12:30:00", items: [["Nohut (haşlanmış)", 180], ["Bulgur pilavı", 170], ["Yoğurt (tam yağlı)", 170]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Muz", 135], ["Badem", 14]] },
        { name: "Akşam", time: "19:30:00", items: [["Dana kıyma (pişmiş)", 115], ["Haşlanmış makarna", 170], ["Ispanak (pişmiş)", 205], ["Salatalık", 150]] },
      ],
      [
        { name: "Kahvaltı", time: "08:00:00", items: [["Yumurta", 115], ["Beyaz peynir", 35], ["Tam buğday ekmeği", 60], ["Domates", 120], ["Salatalık", 100], ["Siyah zeytin", 20]] },
        { name: "Öğle", time: "12:30:00", items: [["Tavuk göğsü (ızgara)", 140], ["Bulgur pilavı", 175], ["Yoğurt (tam yağlı)", 175], ["Salatalık", 100]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Elma", 210], ["Ceviz içi", 14]] },
        { name: "Akşam", time: "19:30:00", items: [["Kırmızı mercimek (pişmiş)", 235], ["Tam buğday ekmeği", 60], ["Ispanak (pişmiş)", 210], ["Zeytinyağı", 6], ["Yoğurt (tam yağlı)", 175]] },
      ],
    ],
  },
  {
    name: "Dengeli beslenme 2000 kcal (haftalık)", planType: "Weekly",
    description: "Kilo koruma için dengeli haftalık örnek plan.",
    days: [
      [
        { name: "Kahvaltı", time: "08:00:00", items: [["Yumurta", 130], ["Beyaz peynir", 40], ["Tam buğday ekmeği", 65], ["Domates", 120], ["Salatalık", 100], ["Siyah zeytin", 20]] },
        { name: "Öğle", time: "12:30:00", items: [["Tavuk göğsü (ızgara)", 155], ["Bulgur pilavı", 195], ["Yoğurt (tam yağlı)", 195], ["Salatalık", 100]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Elma", 235], ["Ceviz içi", 16]] },
        { name: "Akşam", time: "19:30:00", items: [["Kırmızı mercimek (pişmiş)", 260], ["Tam buğday ekmeği", 65], ["Ispanak (pişmiş)", 235], ["Zeytinyağı", 7], ["Yoğurt (tam yağlı)", 195]] },
      ],
      [
        { name: "Kahvaltı", time: "08:00:00", items: [["Yulaf ezmesi", 62], ["Süt (yarım yağlı)", 250], ["Muz", 150], ["Ceviz içi", 15], ["Bal", 9]] },
        { name: "Öğle", time: "12:30:00", items: [["Kuru fasulye (haşlanmış)", 225], ["Pirinç pilavı", 185], ["Salatalık", 150], ["Domates", 120]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Yoğurt (tam yağlı)", 185], ["Portakal", 185]] },
        { name: "Akşam", time: "19:30:00", items: [["Somon (fırında)", 150], ["Haşlanmış patates", 185], ["Brokoli (haşlanmış)", 185], ["Zeytinyağı", 6]] },
      ],
      [
        { name: "Kahvaltı", time: "08:00:00", items: [["Yumurta", 125], ["Kaşar peyniri", 25], ["Tam buğday ekmeği", 65], ["Domates", 120], ["Siyah zeytin", 20]] },
        { name: "Öğle", time: "12:30:00", items: [["Nohut (haşlanmış)", 200], ["Bulgur pilavı", 190], ["Yoğurt (tam yağlı)", 190]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Muz", 150], ["Badem", 15]] },
        { name: "Akşam", time: "19:30:00", items: [["Dana kıyma (pişmiş)", 125], ["Haşlanmış makarna", 190], ["Ispanak (pişmiş)", 225], ["Salatalık", 150]] },
      ],
      [
        { name: "Kahvaltı", time: "08:00:00", items: [["Yumurta", 130], ["Beyaz peynir", 40], ["Tam buğday ekmeği", 65], ["Domates", 120], ["Salatalık", 100], ["Siyah zeytin", 20]] },
        { name: "Öğle", time: "12:30:00", items: [["Tavuk göğsü (ızgara)", 155], ["Bulgur pilavı", 195], ["Yoğurt (tam yağlı)", 195], ["Salatalık", 100]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Elma", 235], ["Ceviz içi", 16]] },
        { name: "Akşam", time: "19:30:00", items: [["Kırmızı mercimek (pişmiş)", 260], ["Tam buğday ekmeği", 65], ["Ispanak (pişmiş)", 235], ["Zeytinyağı", 7], ["Yoğurt (tam yağlı)", 195]] },
      ],
      [
        { name: "Kahvaltı", time: "08:00:00", items: [["Yulaf ezmesi", 62], ["Süt (yarım yağlı)", 250], ["Muz", 150], ["Ceviz içi", 15], ["Bal", 9]] },
        { name: "Öğle", time: "12:30:00", items: [["Kuru fasulye (haşlanmış)", 225], ["Pirinç pilavı", 185], ["Salatalık", 150], ["Domates", 120]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Yoğurt (tam yağlı)", 185], ["Portakal", 185]] },
        { name: "Akşam", time: "19:30:00", items: [["Somon (fırında)", 150], ["Haşlanmış patates", 185], ["Brokoli (haşlanmış)", 185], ["Zeytinyağı", 6]] },
      ],
      [
        { name: "Kahvaltı", time: "08:00:00", items: [["Yumurta", 125], ["Kaşar peyniri", 25], ["Tam buğday ekmeği", 65], ["Domates", 120], ["Siyah zeytin", 20]] },
        { name: "Öğle", time: "12:30:00", items: [["Nohut (haşlanmış)", 200], ["Bulgur pilavı", 190], ["Yoğurt (tam yağlı)", 190]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Muz", 150], ["Badem", 15]] },
        { name: "Akşam", time: "19:30:00", items: [["Dana kıyma (pişmiş)", 125], ["Haşlanmış makarna", 190], ["Ispanak (pişmiş)", 225], ["Salatalık", 150]] },
      ],
      [
        { name: "Kahvaltı", time: "08:00:00", items: [["Yumurta", 130], ["Beyaz peynir", 40], ["Tam buğday ekmeği", 65], ["Domates", 120], ["Salatalık", 100], ["Siyah zeytin", 20]] },
        { name: "Öğle", time: "12:30:00", items: [["Tavuk göğsü (ızgara)", 155], ["Bulgur pilavı", 195], ["Yoğurt (tam yağlı)", 195], ["Salatalık", 100]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Elma", 235], ["Ceviz içi", 16]] },
        { name: "Akşam", time: "19:30:00", items: [["Kırmızı mercimek (pişmiş)", 260], ["Tam buğday ekmeği", 65], ["Ispanak (pişmiş)", 235], ["Zeytinyağı", 7], ["Yoğurt (tam yağlı)", 195]] },
      ],
    ],
  },
  {
    name: "Dengeli beslenme 2500 kcal (haftalık)", planType: "Weekly",
    description: "Aktif yetişkin ya da kilo alma için yüksek enerjili haftalık örnek plan.",
    days: [
      [
        { name: "Kahvaltı", time: "08:00:00", items: [["Yumurta", 165], ["Beyaz peynir", 50], ["Tam buğday ekmeği", 80], ["Domates", 120], ["Salatalık", 100], ["Siyah zeytin", 25]] },
        { name: "Öğle", time: "12:30:00", items: [["Tavuk göğsü (ızgara)", 195], ["Bulgur pilavı", 245], ["Yoğurt (tam yağlı)", 245], ["Salatalık", 100]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Elma", 295], ["Ceviz içi", 20]] },
        { name: "Akşam", time: "19:30:00", items: [["Kırmızı mercimek (pişmiş)", 325], ["Tam buğday ekmeği", 80], ["Ispanak (pişmiş)", 295], ["Zeytinyağı", 8], ["Yoğurt (tam yağlı)", 245]] },
      ],
      [
        { name: "Kahvaltı", time: "08:00:00", items: [["Yulaf ezmesi", 78], ["Süt (yarım yağlı)", 310], ["Muz", 185], ["Ceviz içi", 19], ["Bal", 11]] },
        { name: "Öğle", time: "12:30:00", items: [["Kuru fasulye (haşlanmış)", 280], ["Pirinç pilavı", 235], ["Salatalık", 150], ["Domates", 120]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Yoğurt (tam yağlı)", 235], ["Portakal", 235]] },
        { name: "Akşam", time: "19:30:00", items: [["Somon (fırında)", 185], ["Haşlanmış patates", 235], ["Brokoli (haşlanmış)", 235], ["Zeytinyağı", 8]] },
      ],
      [
        { name: "Kahvaltı", time: "08:00:00", items: [["Yumurta", 155], ["Kaşar peyniri", 30], ["Tam buğday ekmeği", 80], ["Domates", 120], ["Siyah zeytin", 25]] },
        { name: "Öğle", time: "12:30:00", items: [["Nohut (haşlanmış)", 250], ["Bulgur pilavı", 235], ["Yoğurt (tam yağlı)", 235]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Muz", 190], ["Badem", 19]] },
        { name: "Akşam", time: "19:30:00", items: [["Dana kıyma (pişmiş)", 155], ["Haşlanmış makarna", 235], ["Ispanak (pişmiş)", 280], ["Salatalık", 150]] },
      ],
      [
        { name: "Kahvaltı", time: "08:00:00", items: [["Yumurta", 165], ["Beyaz peynir", 50], ["Tam buğday ekmeği", 80], ["Domates", 120], ["Salatalık", 100], ["Siyah zeytin", 25]] },
        { name: "Öğle", time: "12:30:00", items: [["Tavuk göğsü (ızgara)", 195], ["Bulgur pilavı", 245], ["Yoğurt (tam yağlı)", 245], ["Salatalık", 100]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Elma", 295], ["Ceviz içi", 20]] },
        { name: "Akşam", time: "19:30:00", items: [["Kırmızı mercimek (pişmiş)", 325], ["Tam buğday ekmeği", 80], ["Ispanak (pişmiş)", 295], ["Zeytinyağı", 8], ["Yoğurt (tam yağlı)", 245]] },
      ],
      [
        { name: "Kahvaltı", time: "08:00:00", items: [["Yulaf ezmesi", 78], ["Süt (yarım yağlı)", 310], ["Muz", 185], ["Ceviz içi", 19], ["Bal", 11]] },
        { name: "Öğle", time: "12:30:00", items: [["Kuru fasulye (haşlanmış)", 280], ["Pirinç pilavı", 235], ["Salatalık", 150], ["Domates", 120]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Yoğurt (tam yağlı)", 235], ["Portakal", 235]] },
        { name: "Akşam", time: "19:30:00", items: [["Somon (fırında)", 185], ["Haşlanmış patates", 235], ["Brokoli (haşlanmış)", 235], ["Zeytinyağı", 8]] },
      ],
      [
        { name: "Kahvaltı", time: "08:00:00", items: [["Yumurta", 155], ["Kaşar peyniri", 30], ["Tam buğday ekmeği", 80], ["Domates", 120], ["Siyah zeytin", 25]] },
        { name: "Öğle", time: "12:30:00", items: [["Nohut (haşlanmış)", 250], ["Bulgur pilavı", 235], ["Yoğurt (tam yağlı)", 235]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Muz", 190], ["Badem", 19]] },
        { name: "Akşam", time: "19:30:00", items: [["Dana kıyma (pişmiş)", 155], ["Haşlanmış makarna", 235], ["Ispanak (pişmiş)", 280], ["Salatalık", 150]] },
      ],
      [
        { name: "Kahvaltı", time: "08:00:00", items: [["Yumurta", 165], ["Beyaz peynir", 50], ["Tam buğday ekmeği", 80], ["Domates", 120], ["Salatalık", 100], ["Siyah zeytin", 25]] },
        { name: "Öğle", time: "12:30:00", items: [["Tavuk göğsü (ızgara)", 195], ["Bulgur pilavı", 245], ["Yoğurt (tam yağlı)", 245], ["Salatalık", 100]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Elma", 295], ["Ceviz içi", 20]] },
        { name: "Akşam", time: "19:30:00", items: [["Kırmızı mercimek (pişmiş)", 325], ["Tam buğday ekmeği", 80], ["Ispanak (pişmiş)", 295], ["Zeytinyağı", 8], ["Yoğurt (tam yağlı)", 245]] },
      ],
    ],
  },
  {
    name: "Akdeniz tipi 2000 kcal (haftalık)", planType: "Weekly",
    description: "Zeytinyağı, baklagil, sebze ve balık ağırlıklı Akdeniz tipi haftalık örnek plan.",
    days: [
      [
        { name: "Kahvaltı", time: "08:00:00", items: [["Beyaz peynir", 35], ["Siyah zeytin", 35], ["Domates", 120], ["Salatalık", 150], ["Tam buğday ekmeği", 60], ["Zeytinyağı", 6]] },
        { name: "Öğle", time: "12:30:00", items: [["Nohut (haşlanmış)", 200], ["Bulgur pilavı", 185], ["Ispanak (pişmiş)", 225], ["Zeytinyağı", 12]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Yoğurt (tam yağlı)", 185], ["Ceviz içi", 15], ["Portakal", 185]] },
        { name: "Akşam", time: "19:30:00", items: [["Somon (fırında)", 150], ["Brokoli (haşlanmış)", 185], ["Haşlanmış patates", 185], ["Zeytinyağı", 12]] },
      ],
      [
        { name: "Kahvaltı", time: "08:00:00", items: [["Yumurta", 125], ["Siyah zeytin", 20], ["Domates", 120], ["Tam buğday ekmeği", 60], ["Zeytinyağı", 6]] },
        { name: "Öğle", time: "12:30:00", items: [["Kuru fasulye (haşlanmış)", 225], ["Bulgur pilavı", 185], ["Salatalık", 150], ["Zeytinyağı", 12]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Elma", 225], ["Badem", 30]] },
        { name: "Akşam", time: "19:30:00", items: [["Kırmızı mercimek (pişmiş)", 250], ["Tavuk göğsü (ızgara)", 150], ["Ispanak (pişmiş)", 225], ["Zeytinyağı", 12]] },
      ],
      [
        { name: "Kahvaltı", time: "08:00:00", items: [["Beyaz peynir", 35], ["Siyah zeytin", 35], ["Domates", 120], ["Salatalık", 150], ["Tam buğday ekmeği", 60], ["Zeytinyağı", 6]] },
        { name: "Öğle", time: "12:30:00", items: [["Nohut (haşlanmış)", 200], ["Bulgur pilavı", 185], ["Ispanak (pişmiş)", 225], ["Zeytinyağı", 12]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Yoğurt (tam yağlı)", 185], ["Ceviz içi", 15], ["Portakal", 185]] },
        { name: "Akşam", time: "19:30:00", items: [["Somon (fırında)", 150], ["Brokoli (haşlanmış)", 185], ["Haşlanmış patates", 185], ["Zeytinyağı", 12]] },
      ],
      [
        { name: "Kahvaltı", time: "08:00:00", items: [["Yumurta", 125], ["Siyah zeytin", 20], ["Domates", 120], ["Tam buğday ekmeği", 60], ["Zeytinyağı", 6]] },
        { name: "Öğle", time: "12:30:00", items: [["Kuru fasulye (haşlanmış)", 225], ["Bulgur pilavı", 185], ["Salatalık", 150], ["Zeytinyağı", 12]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Elma", 225], ["Badem", 30]] },
        { name: "Akşam", time: "19:30:00", items: [["Kırmızı mercimek (pişmiş)", 250], ["Tavuk göğsü (ızgara)", 150], ["Ispanak (pişmiş)", 225], ["Zeytinyağı", 12]] },
      ],
      [
        { name: "Kahvaltı", time: "08:00:00", items: [["Beyaz peynir", 35], ["Siyah zeytin", 35], ["Domates", 120], ["Salatalık", 150], ["Tam buğday ekmeği", 60], ["Zeytinyağı", 6]] },
        { name: "Öğle", time: "12:30:00", items: [["Nohut (haşlanmış)", 200], ["Bulgur pilavı", 185], ["Ispanak (pişmiş)", 225], ["Zeytinyağı", 12]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Yoğurt (tam yağlı)", 185], ["Ceviz içi", 15], ["Portakal", 185]] },
        { name: "Akşam", time: "19:30:00", items: [["Somon (fırında)", 150], ["Brokoli (haşlanmış)", 185], ["Haşlanmış patates", 185], ["Zeytinyağı", 12]] },
      ],
      [
        { name: "Kahvaltı", time: "08:00:00", items: [["Yumurta", 125], ["Siyah zeytin", 20], ["Domates", 120], ["Tam buğday ekmeği", 60], ["Zeytinyağı", 6]] },
        { name: "Öğle", time: "12:30:00", items: [["Kuru fasulye (haşlanmış)", 225], ["Bulgur pilavı", 185], ["Salatalık", 150], ["Zeytinyağı", 12]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Elma", 225], ["Badem", 30]] },
        { name: "Akşam", time: "19:30:00", items: [["Kırmızı mercimek (pişmiş)", 250], ["Tavuk göğsü (ızgara)", 150], ["Ispanak (pişmiş)", 225], ["Zeytinyağı", 12]] },
      ],
      [
        { name: "Kahvaltı", time: "08:00:00", items: [["Beyaz peynir", 35], ["Siyah zeytin", 35], ["Domates", 120], ["Salatalık", 150], ["Tam buğday ekmeği", 60], ["Zeytinyağı", 6]] },
        { name: "Öğle", time: "12:30:00", items: [["Nohut (haşlanmış)", 200], ["Bulgur pilavı", 185], ["Ispanak (pişmiş)", 225], ["Zeytinyağı", 12]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Yoğurt (tam yağlı)", 185], ["Ceviz içi", 15], ["Portakal", 185]] },
        { name: "Akşam", time: "19:30:00", items: [["Somon (fırında)", 150], ["Brokoli (haşlanmış)", 185], ["Haşlanmış patates", 185], ["Zeytinyağı", 12]] },
      ],
    ],
  },
  {
    name: "Ketojenik 1800 kcal (haftalık)", planType: "Weekly",
    description: "Çok düşük karbonhidratlı, yağdan zengin ketojenik haftalık örnek plan.",
    days: [
      [
        { name: "Kahvaltı", time: "08:00:00", items: [["Yumurta", 150], ["Beyaz peynir", 60], ["Siyah zeytin", 30], ["Salatalık", 150], ["Zeytinyağı", 10]] },
        { name: "Öğle", time: "12:30:00", items: [["Somon (fırında)", 150], ["Ispanak (pişmiş)", 180], ["Zeytinyağı", 15]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Ceviz içi", 24], ["Badem", 12]] },
        { name: "Akşam", time: "19:30:00", items: [["Dana kıyma (pişmiş)", 150], ["Brokoli (haşlanmış)", 150], ["Kaşar peyniri", 20], ["Zeytinyağı", 10]] },
      ],
      [
        { name: "Kahvaltı", time: "08:00:00", items: [["Yumurta", 165], ["Kaşar peyniri", 45], ["Domates", 60], ["Zeytinyağı", 11]] },
        { name: "Öğle", time: "12:30:00", items: [["Tavuk göğsü (ızgara)", 165], ["Salatalık", 150], ["Siyah zeytin", 35], ["Zeytinyağı", 22]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Badem", 26], ["Beyaz peynir", 35]] },
        { name: "Akşam", time: "19:30:00", items: [["Somon (fırında)", 165], ["Ispanak (pişmiş)", 195], ["Zeytinyağı", 16]] },
      ],
      [
        { name: "Kahvaltı", time: "08:00:00", items: [["Yumurta", 150], ["Beyaz peynir", 60], ["Siyah zeytin", 30], ["Salatalık", 150], ["Zeytinyağı", 10]] },
        { name: "Öğle", time: "12:30:00", items: [["Somon (fırında)", 150], ["Ispanak (pişmiş)", 180], ["Zeytinyağı", 15]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Ceviz içi", 24], ["Badem", 12]] },
        { name: "Akşam", time: "19:30:00", items: [["Dana kıyma (pişmiş)", 150], ["Brokoli (haşlanmış)", 150], ["Kaşar peyniri", 20], ["Zeytinyağı", 10]] },
      ],
      [
        { name: "Kahvaltı", time: "08:00:00", items: [["Yumurta", 165], ["Kaşar peyniri", 45], ["Domates", 60], ["Zeytinyağı", 11]] },
        { name: "Öğle", time: "12:30:00", items: [["Tavuk göğsü (ızgara)", 165], ["Salatalık", 150], ["Siyah zeytin", 35], ["Zeytinyağı", 22]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Badem", 26], ["Beyaz peynir", 35]] },
        { name: "Akşam", time: "19:30:00", items: [["Somon (fırında)", 165], ["Ispanak (pişmiş)", 195], ["Zeytinyağı", 16]] },
      ],
      [
        { name: "Kahvaltı", time: "08:00:00", items: [["Yumurta", 150], ["Beyaz peynir", 60], ["Siyah zeytin", 30], ["Salatalık", 150], ["Zeytinyağı", 10]] },
        { name: "Öğle", time: "12:30:00", items: [["Somon (fırında)", 150], ["Ispanak (pişmiş)", 180], ["Zeytinyağı", 15]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Ceviz içi", 24], ["Badem", 12]] },
        { name: "Akşam", time: "19:30:00", items: [["Dana kıyma (pişmiş)", 150], ["Brokoli (haşlanmış)", 150], ["Kaşar peyniri", 20], ["Zeytinyağı", 10]] },
      ],
      [
        { name: "Kahvaltı", time: "08:00:00", items: [["Yumurta", 165], ["Kaşar peyniri", 45], ["Domates", 60], ["Zeytinyağı", 11]] },
        { name: "Öğle", time: "12:30:00", items: [["Tavuk göğsü (ızgara)", 165], ["Salatalık", 150], ["Siyah zeytin", 35], ["Zeytinyağı", 22]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Badem", 26], ["Beyaz peynir", 35]] },
        { name: "Akşam", time: "19:30:00", items: [["Somon (fırında)", 165], ["Ispanak (pişmiş)", 195], ["Zeytinyağı", 16]] },
      ],
      [
        { name: "Kahvaltı", time: "08:00:00", items: [["Yumurta", 150], ["Beyaz peynir", 60], ["Siyah zeytin", 30], ["Salatalık", 150], ["Zeytinyağı", 10]] },
        { name: "Öğle", time: "12:30:00", items: [["Somon (fırında)", 150], ["Ispanak (pişmiş)", 180], ["Zeytinyağı", 15]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Ceviz içi", 24], ["Badem", 12]] },
        { name: "Akşam", time: "19:30:00", items: [["Dana kıyma (pişmiş)", 150], ["Brokoli (haşlanmış)", 150], ["Kaşar peyniri", 20], ["Zeytinyağı", 10]] },
      ],
    ],
  },
  {
    name: "Düşük karbonhidratlı 1600 kcal (haftalık)", planType: "Weekly",
    description: "Karbonhidratı azaltılmış, proteinden zengin haftalık örnek plan.",
    days: [
      [
        { name: "Kahvaltı", time: "08:00:00", items: [["Yumurta", 115], ["Beyaz peynir", 35], ["Domates", 120], ["Salatalık", 150], ["Siyah zeytin", 15]] },
        { name: "Öğle", time: "12:30:00", items: [["Tavuk göğsü (ızgara)", 170], ["Brokoli (haşlanmış)", 170], ["Yoğurt (tam yağlı)", 170], ["Zeytinyağı", 11]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Badem", 27], ["Elma", 105]] },
        { name: "Akşam", time: "19:30:00", items: [["Somon (fırında)", 135], ["Ispanak (pişmiş)", 205], ["Kırmızı mercimek (pişmiş)", 115], ["Zeytinyağı", 6]] },
      ],
      [
        { name: "Kahvaltı", time: "08:00:00", items: [["Yumurta", 135], ["Kaşar peyniri", 25], ["Domates", 120], ["Tam buğday ekmeği", 35]] },
        { name: "Öğle", time: "12:30:00", items: [["Dana kıyma (pişmiş)", 135], ["Ispanak (pişmiş)", 240], ["Yoğurt (tam yağlı)", 200]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Ceviz içi", 16], ["Portakal", 200]] },
        { name: "Akşam", time: "19:30:00", items: [["Tavuk göğsü (ızgara)", 160], ["Brokoli (haşlanmış)", 200], ["Salatalık", 150], ["Zeytinyağı", 13]] },
      ],
      [
        { name: "Kahvaltı", time: "08:00:00", items: [["Yumurta", 115], ["Beyaz peynir", 35], ["Domates", 120], ["Salatalık", 150], ["Siyah zeytin", 15]] },
        { name: "Öğle", time: "12:30:00", items: [["Tavuk göğsü (ızgara)", 170], ["Brokoli (haşlanmış)", 170], ["Yoğurt (tam yağlı)", 170], ["Zeytinyağı", 11]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Badem", 27], ["Elma", 105]] },
        { name: "Akşam", time: "19:30:00", items: [["Somon (fırında)", 135], ["Ispanak (pişmiş)", 205], ["Kırmızı mercimek (pişmiş)", 115], ["Zeytinyağı", 6]] },
      ],
      [
        { name: "Kahvaltı", time: "08:00:00", items: [["Yumurta", 135], ["Kaşar peyniri", 25], ["Domates", 120], ["Tam buğday ekmeği", 35]] },
        { name: "Öğle", time: "12:30:00", items: [["Dana kıyma (pişmiş)", 135], ["Ispanak (pişmiş)", 240], ["Yoğurt (tam yağlı)", 200]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Ceviz içi", 16], ["Portakal", 200]] },
        { name: "Akşam", time: "19:30:00", items: [["Tavuk göğsü (ızgara)", 160], ["Brokoli (haşlanmış)", 200], ["Salatalık", 150], ["Zeytinyağı", 13]] },
      ],
      [
        { name: "Kahvaltı", time: "08:00:00", items: [["Yumurta", 115], ["Beyaz peynir", 35], ["Domates", 120], ["Salatalık", 150], ["Siyah zeytin", 15]] },
        { name: "Öğle", time: "12:30:00", items: [["Tavuk göğsü (ızgara)", 170], ["Brokoli (haşlanmış)", 170], ["Yoğurt (tam yağlı)", 170], ["Zeytinyağı", 11]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Badem", 27], ["Elma", 105]] },
        { name: "Akşam", time: "19:30:00", items: [["Somon (fırında)", 135], ["Ispanak (pişmiş)", 205], ["Kırmızı mercimek (pişmiş)", 115], ["Zeytinyağı", 6]] },
      ],
      [
        { name: "Kahvaltı", time: "08:00:00", items: [["Yumurta", 135], ["Kaşar peyniri", 25], ["Domates", 120], ["Tam buğday ekmeği", 35]] },
        { name: "Öğle", time: "12:30:00", items: [["Dana kıyma (pişmiş)", 135], ["Ispanak (pişmiş)", 240], ["Yoğurt (tam yağlı)", 200]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Ceviz içi", 16], ["Portakal", 200]] },
        { name: "Akşam", time: "19:30:00", items: [["Tavuk göğsü (ızgara)", 160], ["Brokoli (haşlanmış)", 200], ["Salatalık", 150], ["Zeytinyağı", 13]] },
      ],
      [
        { name: "Kahvaltı", time: "08:00:00", items: [["Yumurta", 115], ["Beyaz peynir", 35], ["Domates", 120], ["Salatalık", 150], ["Siyah zeytin", 15]] },
        { name: "Öğle", time: "12:30:00", items: [["Tavuk göğsü (ızgara)", 170], ["Brokoli (haşlanmış)", 170], ["Yoğurt (tam yağlı)", 170], ["Zeytinyağı", 11]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Badem", 27], ["Elma", 105]] },
        { name: "Akşam", time: "19:30:00", items: [["Somon (fırında)", 135], ["Ispanak (pişmiş)", 205], ["Kırmızı mercimek (pişmiş)", 115], ["Zeytinyağı", 6]] },
      ],
    ],
  },
  {
    name: "Yüksek proteinli 2400 kcal (haftalık)", planType: "Weekly",
    description: "Kas kazanımı için proteinden zengin haftalık örnek plan.",
    days: [
      [
        { name: "Kahvaltı", time: "08:00:00", items: [["Yumurta", 175], ["Beyaz peynir", 70], ["Tam buğday ekmeği", 60], ["Domates", 120]] },
        { name: "Öğle", time: "12:30:00", items: [["Tavuk göğsü (ızgara)", 210], ["Bulgur pilavı", 175], ["Yoğurt (tam yağlı)", 175]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Süt (yarım yağlı)", 235], ["Badem", 28], ["Muz", 140]] },
        { name: "Akşam", time: "19:30:00", items: [["Dana kıyma (pişmiş)", 175], ["Kırmızı mercimek (pişmiş)", 235], ["Brokoli (haşlanmış)", 175]] },
      ],
      [
        { name: "Kahvaltı", time: "08:00:00", items: [["Yulaf ezmesi", 76], ["Süt (yarım yağlı)", 255], ["Yumurta", 125], ["Ceviz içi", 15]] },
        { name: "Öğle", time: "12:30:00", items: [["Somon (fırında)", 190], ["Pirinç pilavı", 190], ["Ispanak (pişmiş)", 230]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Yoğurt (tam yağlı)", 255], ["Elma", 230]] },
        { name: "Akşam", time: "19:30:00", items: [["Tavuk göğsü (ızgara)", 230], ["Nohut (haşlanmış)", 205], ["Salatalık", 150]] },
      ],
      [
        { name: "Kahvaltı", time: "08:00:00", items: [["Yumurta", 175], ["Beyaz peynir", 70], ["Tam buğday ekmeği", 60], ["Domates", 120]] },
        { name: "Öğle", time: "12:30:00", items: [["Tavuk göğsü (ızgara)", 210], ["Bulgur pilavı", 175], ["Yoğurt (tam yağlı)", 175]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Süt (yarım yağlı)", 235], ["Badem", 28], ["Muz", 140]] },
        { name: "Akşam", time: "19:30:00", items: [["Dana kıyma (pişmiş)", 175], ["Kırmızı mercimek (pişmiş)", 235], ["Brokoli (haşlanmış)", 175]] },
      ],
      [
        { name: "Kahvaltı", time: "08:00:00", items: [["Yulaf ezmesi", 76], ["Süt (yarım yağlı)", 255], ["Yumurta", 125], ["Ceviz içi", 15]] },
        { name: "Öğle", time: "12:30:00", items: [["Somon (fırında)", 190], ["Pirinç pilavı", 190], ["Ispanak (pişmiş)", 230]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Yoğurt (tam yağlı)", 255], ["Elma", 230]] },
        { name: "Akşam", time: "19:30:00", items: [["Tavuk göğsü (ızgara)", 230], ["Nohut (haşlanmış)", 205], ["Salatalık", 150]] },
      ],
      [
        { name: "Kahvaltı", time: "08:00:00", items: [["Yumurta", 175], ["Beyaz peynir", 70], ["Tam buğday ekmeği", 60], ["Domates", 120]] },
        { name: "Öğle", time: "12:30:00", items: [["Tavuk göğsü (ızgara)", 210], ["Bulgur pilavı", 175], ["Yoğurt (tam yağlı)", 175]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Süt (yarım yağlı)", 235], ["Badem", 28], ["Muz", 140]] },
        { name: "Akşam", time: "19:30:00", items: [["Dana kıyma (pişmiş)", 175], ["Kırmızı mercimek (pişmiş)", 235], ["Brokoli (haşlanmış)", 175]] },
      ],
      [
        { name: "Kahvaltı", time: "08:00:00", items: [["Yulaf ezmesi", 76], ["Süt (yarım yağlı)", 255], ["Yumurta", 125], ["Ceviz içi", 15]] },
        { name: "Öğle", time: "12:30:00", items: [["Somon (fırında)", 190], ["Pirinç pilavı", 190], ["Ispanak (pişmiş)", 230]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Yoğurt (tam yağlı)", 255], ["Elma", 230]] },
        { name: "Akşam", time: "19:30:00", items: [["Tavuk göğsü (ızgara)", 230], ["Nohut (haşlanmış)", 205], ["Salatalık", 150]] },
      ],
      [
        { name: "Kahvaltı", time: "08:00:00", items: [["Yumurta", 175], ["Beyaz peynir", 70], ["Tam buğday ekmeği", 60], ["Domates", 120]] },
        { name: "Öğle", time: "12:30:00", items: [["Tavuk göğsü (ızgara)", 210], ["Bulgur pilavı", 175], ["Yoğurt (tam yağlı)", 175]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Süt (yarım yağlı)", 235], ["Badem", 28], ["Muz", 140]] },
        { name: "Akşam", time: "19:30:00", items: [["Dana kıyma (pişmiş)", 175], ["Kırmızı mercimek (pişmiş)", 235], ["Brokoli (haşlanmış)", 175]] },
      ],
    ],
  },
  {
    name: "Vejetaryen 2000 kcal (haftalık)", planType: "Weekly",
    description: "Et içermeyen; yumurta, süt ürünü ve baklagil ağırlıklı haftalık örnek plan.",
    days: [
      [
        { name: "Kahvaltı", time: "08:00:00", items: [["Yumurta", 130], ["Beyaz peynir", 40], ["Tam buğday ekmeği", 65], ["Domates", 120], ["Siyah zeytin", 20]] },
        { name: "Öğle", time: "12:30:00", items: [["Kırmızı mercimek (pişmiş)", 255], ["Bulgur pilavı", 190], ["Yoğurt (tam yağlı)", 190]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Muz", 155], ["Ceviz içi", 15]] },
        { name: "Akşam", time: "19:30:00", items: [["Nohut (haşlanmış)", 205], ["Ispanak (pişmiş)", 230], ["Pirinç pilavı", 190], ["Zeytinyağı", 6]] },
      ],
      [
        { name: "Kahvaltı", time: "08:00:00", items: [["Yulaf ezmesi", 70], ["Süt (yarım yağlı)", 280], ["Elma", 250], ["Badem", 17]] },
        { name: "Öğle", time: "12:30:00", items: [["Kuru fasulye (haşlanmış)", 250], ["Bulgur pilavı", 210], ["Salatalık", 150]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Yoğurt (tam yağlı)", 210], ["Bal", 10], ["Ceviz içi", 17]] },
        { name: "Akşam", time: "19:30:00", items: [["Yumurta", 140], ["Brokoli (haşlanmış)", 210], ["Haşlanmış patates", 210], ["Kaşar peyniri", 30]] },
      ],
      [
        { name: "Kahvaltı", time: "08:00:00", items: [["Yumurta", 130], ["Beyaz peynir", 40], ["Tam buğday ekmeği", 65], ["Domates", 120], ["Siyah zeytin", 20]] },
        { name: "Öğle", time: "12:30:00", items: [["Kırmızı mercimek (pişmiş)", 255], ["Bulgur pilavı", 190], ["Yoğurt (tam yağlı)", 190]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Muz", 155], ["Ceviz içi", 15]] },
        { name: "Akşam", time: "19:30:00", items: [["Nohut (haşlanmış)", 205], ["Ispanak (pişmiş)", 230], ["Pirinç pilavı", 190], ["Zeytinyağı", 6]] },
      ],
      [
        { name: "Kahvaltı", time: "08:00:00", items: [["Yulaf ezmesi", 70], ["Süt (yarım yağlı)", 280], ["Elma", 250], ["Badem", 17]] },
        { name: "Öğle", time: "12:30:00", items: [["Kuru fasulye (haşlanmış)", 250], ["Bulgur pilavı", 210], ["Salatalık", 150]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Yoğurt (tam yağlı)", 210], ["Bal", 10], ["Ceviz içi", 17]] },
        { name: "Akşam", time: "19:30:00", items: [["Yumurta", 140], ["Brokoli (haşlanmış)", 210], ["Haşlanmış patates", 210], ["Kaşar peyniri", 30]] },
      ],
      [
        { name: "Kahvaltı", time: "08:00:00", items: [["Yumurta", 130], ["Beyaz peynir", 40], ["Tam buğday ekmeği", 65], ["Domates", 120], ["Siyah zeytin", 20]] },
        { name: "Öğle", time: "12:30:00", items: [["Kırmızı mercimek (pişmiş)", 255], ["Bulgur pilavı", 190], ["Yoğurt (tam yağlı)", 190]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Muz", 155], ["Ceviz içi", 15]] },
        { name: "Akşam", time: "19:30:00", items: [["Nohut (haşlanmış)", 205], ["Ispanak (pişmiş)", 230], ["Pirinç pilavı", 190], ["Zeytinyağı", 6]] },
      ],
      [
        { name: "Kahvaltı", time: "08:00:00", items: [["Yulaf ezmesi", 70], ["Süt (yarım yağlı)", 280], ["Elma", 250], ["Badem", 17]] },
        { name: "Öğle", time: "12:30:00", items: [["Kuru fasulye (haşlanmış)", 250], ["Bulgur pilavı", 210], ["Salatalık", 150]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Yoğurt (tam yağlı)", 210], ["Bal", 10], ["Ceviz içi", 17]] },
        { name: "Akşam", time: "19:30:00", items: [["Yumurta", 140], ["Brokoli (haşlanmış)", 210], ["Haşlanmış patates", 210], ["Kaşar peyniri", 30]] },
      ],
      [
        { name: "Kahvaltı", time: "08:00:00", items: [["Yumurta", 130], ["Beyaz peynir", 40], ["Tam buğday ekmeği", 65], ["Domates", 120], ["Siyah zeytin", 20]] },
        { name: "Öğle", time: "12:30:00", items: [["Kırmızı mercimek (pişmiş)", 255], ["Bulgur pilavı", 190], ["Yoğurt (tam yağlı)", 190]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Muz", 155], ["Ceviz içi", 15]] },
        { name: "Akşam", time: "19:30:00", items: [["Nohut (haşlanmış)", 205], ["Ispanak (pişmiş)", 230], ["Pirinç pilavı", 190], ["Zeytinyağı", 6]] },
      ],
    ],
  },
  {
    name: "Aralıklı oruç 16:8 2000 kcal (haftalık)", planType: "Weekly",
    description: "12.00–20.00 arasında üç öğünlük aralıklı oruç (16:8) haftalık örnek plan.",
    days: [
      [
        { name: "İlk öğün", time: "12:00:00", items: [["Yumurta", 175], ["Beyaz peynir", 55], ["Tam buğday ekmeği", 90], ["Domates", 120], ["Salatalık", 150], ["Siyah zeytin", 25]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Yoğurt (tam yağlı)", 265], ["Ceviz içi", 21], ["Elma", 315]] },
        { name: "Son öğün", time: "19:30:00", items: [["Tavuk göğsü (ızgara)", 265], ["Bulgur pilavı", 265], ["Brokoli (haşlanmış)", 265], ["Zeytinyağı", 9]] },
      ],
      [
        { name: "İlk öğün", time: "12:00:00", items: [["Yulaf ezmesi", 92], ["Süt (yarım yağlı)", 365], ["Muz", 220], ["Badem", 22]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Beyaz peynir", 55], ["Tam buğday ekmeği", 45], ["Domates", 120]] },
        { name: "Son öğün", time: "19:30:00", items: [["Somon (fırında)", 220], ["Haşlanmış patates", 275], ["Ispanak (pişmiş)", 330], ["Zeytinyağı", 9]] },
      ],
      [
        { name: "İlk öğün", time: "12:00:00", items: [["Yumurta", 175], ["Beyaz peynir", 55], ["Tam buğday ekmeği", 90], ["Domates", 120], ["Salatalık", 150], ["Siyah zeytin", 25]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Yoğurt (tam yağlı)", 265], ["Ceviz içi", 21], ["Elma", 315]] },
        { name: "Son öğün", time: "19:30:00", items: [["Tavuk göğsü (ızgara)", 265], ["Bulgur pilavı", 265], ["Brokoli (haşlanmış)", 265], ["Zeytinyağı", 9]] },
      ],
      [
        { name: "İlk öğün", time: "12:00:00", items: [["Yulaf ezmesi", 92], ["Süt (yarım yağlı)", 365], ["Muz", 220], ["Badem", 22]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Beyaz peynir", 55], ["Tam buğday ekmeği", 45], ["Domates", 120]] },
        { name: "Son öğün", time: "19:30:00", items: [["Somon (fırında)", 220], ["Haşlanmış patates", 275], ["Ispanak (pişmiş)", 330], ["Zeytinyağı", 9]] },
      ],
      [
        { name: "İlk öğün", time: "12:00:00", items: [["Yumurta", 175], ["Beyaz peynir", 55], ["Tam buğday ekmeği", 90], ["Domates", 120], ["Salatalık", 150], ["Siyah zeytin", 25]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Yoğurt (tam yağlı)", 265], ["Ceviz içi", 21], ["Elma", 315]] },
        { name: "Son öğün", time: "19:30:00", items: [["Tavuk göğsü (ızgara)", 265], ["Bulgur pilavı", 265], ["Brokoli (haşlanmış)", 265], ["Zeytinyağı", 9]] },
      ],
      [
        { name: "İlk öğün", time: "12:00:00", items: [["Yulaf ezmesi", 92], ["Süt (yarım yağlı)", 365], ["Muz", 220], ["Badem", 22]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Beyaz peynir", 55], ["Tam buğday ekmeği", 45], ["Domates", 120]] },
        { name: "Son öğün", time: "19:30:00", items: [["Somon (fırında)", 220], ["Haşlanmış patates", 275], ["Ispanak (pişmiş)", 330], ["Zeytinyağı", 9]] },
      ],
      [
        { name: "İlk öğün", time: "12:00:00", items: [["Yumurta", 175], ["Beyaz peynir", 55], ["Tam buğday ekmeği", 90], ["Domates", 120], ["Salatalık", 150], ["Siyah zeytin", 25]] },
        { name: "Ara öğün", time: "16:00:00", items: [["Yoğurt (tam yağlı)", 265], ["Ceviz içi", 21], ["Elma", 315]] },
        { name: "Son öğün", time: "19:30:00", items: [["Tavuk göğsü (ızgara)", 265], ["Bulgur pilavı", 265], ["Brokoli (haşlanmış)", 265], ["Zeytinyağı", 9]] },
      ],
    ],
  },
]
