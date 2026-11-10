#!/bin/bash
# Diyagramları kaynak dosyalardan yeniden çizer.
# Gerekenler: Node.js (npx) ve Graphviz (dot komutu).
# Kullanım: bu klasörden  ./ciz.sh   (çıktılar docs/uml/png ve docs/uml/svg klasörlerine yazılır)
set -e
cd "$(dirname "$0")/.."
python3 araclar/gen_cd3.py            # CD-3 (enum ve değer nesneleri) Graphviz kaynağını üretir
python3 araclar/gen_p6.py             # P-6 (Repository) Graphviz kaynağını üretir
for f in *.mmd; do
  b=$(basename "$f" .mmd)
  npx -y -p @mermaid-js/mermaid-cli mmdc -c araclar/mermaid.json -i "$f" -o "png/$b.png" -s 2 -b white
  npx -y -p @mermaid-js/mermaid-cli mmdc -c araclar/mermaid.json -i "$f" -o "svg/$b.svg" -b white
done
for f in *.dot; do
  b=$(basename "$f" .dot)
  dot -Tpng -Gdpi=150 "$f" -o "png/$b.png"
  dot -Tsvg "$f" -o "svg/$b.svg"
done
echo "Bitti."
