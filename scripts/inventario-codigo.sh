#!/usr/bin/env bash
# Genera un anexo Markdown con los tipos y miembros públicos del código.
# Uso: ./scripts/inventario-codigo.sh > docs/anexo-simbolos.md
set -euo pipefail

raiz="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$raiz"

proyectos=(
  Supermercado.Entidades
  Supermercado.Datos
  Supermercado.Logica
  Supermercado.GUI
  Supermercado.Pruebas
)

echo "# Anexo de símbolos públicos"
echo
echo "Generado por \`scripts/inventario-codigo.sh\`. No editar a mano."
echo

for proyecto in "${proyectos[@]}"; do
  [ -d "$proyecto" ] || continue

  echo "## $proyecto"
  echo

  while IFS= read -r archivo; do
    tipos="$(grep -nE '^(public|internal)[[:space:]]+(sealed[[:space:]]+|abstract[[:space:]]+|static[[:space:]]+|partial[[:space:]]+)*(class|record|interface|enum|struct)[[:space:]]+' "$archivo" || true)"
    [ -n "$tipos" ] || continue

    echo "### \`${archivo#./}\`"
    echo
    echo '```csharp'
    echo "$tipos"

    # Miembros públicos con sangría de cuatro espacios.
    grep -nE '^    public[[:space:]]+' "$archivo" \
      | grep -vE '^[0-9]+:    public[[:space:]]+(sealed[[:space:]]+|abstract[[:space:]]+|static[[:space:]]+|partial[[:space:]]+)*(class|record|interface|enum|struct)[[:space:]]+' \
      || true

    echo '```'
    echo
  done < <(find "$proyecto" -name '*.cs' \
    -not -path '*/obj/*' -not -path '*/bin/*' | sort)
done
