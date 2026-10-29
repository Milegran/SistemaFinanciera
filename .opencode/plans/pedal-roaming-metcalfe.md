# Plan: Tabla de Productos con filtros estilo Clientes

## Objetivo
Reemplazar el GridView simple de "Productos registrados" por una tabla con el **mismo diseño y patrón de Clientes**: barra de filtros (búsqueda, Estado, Método, Limpiar), listado con caption, columnas de la imagen, iconos de acción, status pills y paginación. Reutilizar las clases CSS de Clientes (`.client-filters`, `.client-list`, `.status-pill`, `.action-icon`, etc.).

## Columnas (orden exacto de la imagen)

| # | Header | Contenido | Estilo |
|---|--------|-----------|--------|
| 1 | NOMBRE | Avatar cuadrado con ícono + Nombre + Descripción | `.product-name` (patrón `.client-name`) |
| 2 | RANGO MONTO (B/.) | `B/. 500 – B/. 15,000` | texto fuerte |
| 3 | PLAZO | `6 – 60 meses` (PlazoMinimoMeses–PlazoMaximoMeses) | texto |
| 4 | TASA INTERÉS | `12.00 %` verde bold + `Nominal`/`Efectiva` debajo | `.tasa-stack` |
| 5 | MÉTODOS PERMITIDOS | Pills (Francés, Alemán…) | `.method-pill` |
| 6 | COMISIÓN | `Comisión B/. 25.00` (ValorComision, dato real del sistema) | texto |
| 7 | ESTADO | `Activo`/`Inactivo` | `.status-pill` / `.status-pill inactive` |
| 8 | ACCIONES | 👁 ver · ✏ editar · ⏻ toggle estado | `.action-icon` (SVGs de Clientes) |

## Barra de filtros (patrón Clientes, campos de la imagen)

```
[ 🔍 Buscar por nombre o código... ] [Estado: Todos ▾] [Método: Todos ▾] [↺]
```
- **Búsqueda** (`txtBuscarProducto`): Nombre, Código o Descripción (in-memory, case-insensitive)
- **Estado** (`ddlEstadoProducto`): Todos / Activos / Inactivos — `AutoPostBack`
- **Método** (`ddlMetodoFiltro`): Todos + catálogo `METODOS` — `AutoPostBack` — filtra productos que tengan ese método permitido
- **Limpiar** (`btnLimpiarProducto`): resetea todo
- Enter en búsqueda dispara Botcar (JS, igual que Clientes)
- Sin tarjetas KPI (la imagen no las muestra)

## Estructura de la página (reemplaza sección actual)

```
page-header --split [+ Crear producto]     ← ya existe
pnlMensaje alert                           ← ya existe
client-filters (4 controles + hidden)      ← nuevo
cw-card client-list
  list-caption: "LISTADO DE PRODUCTOS" / "Productos financieros disponibles" / btn Imprimir
  table-responsive
    GridView ID="listado" (se conserva el ID)
      AllowPaging=true PageSize=10
      CssClass="table table-hover align-middle"
      8 columnas (arriba)
  grid-count (litConteo: "Mostrando 1–10 de N productos")
modal producto                              ← ya existe
```

## Lógica (.vb) — patrón Clientes adaptado

```vb
Private Sub CargarListado()
    ' 1. todos = Consultar("PRODUCTOS", Actor.Id)  [ya existe]
    ' 2. Diccionario métodos: por cada producto, Consultar("PRODUCTO_METODOS", id)
    '    (catálogo pequeño; N+1 aceptable, cero cambios DAL)
    ' 3. Reconstruir ddlMetodoFiltro desde catálogo METODOS (preservar selección)
    ' 4. Filtrar in-memory: estado, método, término (Nombre/Codigo/Descripcion)
    ' 5. Bind + litConteo (patrón Mostrando X–Y de N)
End Sub

' FiltroCambiado: Handles ddlEstadoProducto, ddlMetodoFiltro → PageIndex=0, CargarListado
' Buscar: Handles btnBuscarProducto
' Limpiar: Handles btnLimpiarProducto → reset controles
' Paginar: Handles listado.PageIndexChanging
' AccionProducto: Handles listado.RowCommand
'   Elegir (✏) → carga campos + abre modal  [handler Elegir existente, se reutiliza]
'   VerProducto (👁) → mismo llenado + abre modal (no hay página de detalle)
'   CambiarEstado (⏻) → flip Activo vía GuardarProducto (carga como Elegir, flip, guarda)
```

**Columna acciones oculta para no-admin**: se conserva `listado.Columns(listado.Columns.Count - 1).Visible = edicion.Visible` (Acciones es la última columna).

## Archivos a tocar

| Archivo | Cambio |
|---|---|
| `Productos/Productos.aspx` | Reemplazar sección lista + grid por filtros + nueva tabla (8 columnas TemplateField/BoundField) |
| `Productos/Productos.aspx.vb` | `CargarListado` con filtros, `CoincideFiltroProducto`, handlers Buscar/Limpiar/FiltroCambiado/Paginar/AccionProducto; `Listar()` redirige a `CargarListado` |
| `Productos/Productos.aspx.designer.vb` | +5 controles: `txtBuscarProducto`, `ddlEstadoProducto`, `ddlMetodoFiltro`, `btnBuscarProducto`, `btnLimpiarProducto`, `litConteo` |
| `Content/Stitch.css` | ~20 líneas: `.product-name`, `.product-icon`, `.tasa-stack`, `.method-pill` (reutiliza el resto de clases Clientes tal cual) |
| `Scripts/StitchProductos.js` | +Enter en búsqueda + click fila → ver (patrón row-clickable de Clientes) |

**No se toca**: DAL/BL, modal, `FinanzasPagina.vb`, Clientes.*,其他 páginas.

## Detalle de celdas

**Nombre** (patrón client-name):
```html
<div class="product-name">
  <span class="product-icon">[SVG banco 17px]</span>
  <span><strong><%#: Eval("Nombre") %></strong><small><%#: Eval("Descripcion") %></small></span>
</div>
```

**Tasa**:
```html
<div class="tasa-stack"><strong><%#: CDec(Eval("TasaAnual")).ToString("0.00") %> %</strong>
<small><%#: If(Eval("TipoTasa") = "NOMINAL_ANUAL", "Nominal", "Efectiva") %></small></div>
```

**Métodos** (Dictionary(Of Integer, List(Of String)) poblado en CargarListado):
```html
<%# MetodosPills(CInt(Eval("Id")))) %>
' → <span class="method-pill">Francés</span><span class="method-pill">Alemán</span>
```

**Monto**: `B/. {Min:N0} – B/. {Max:N0}` · **Plazo**: `{MinMeses} – {MaxMeses} meses` · **Comisión**: `Comisión B/. {ValorComision:N2}`

**Acciones** (3 LinkButtons, SVGs copiados de Clientes.aspx líneas 31-33):
- 👁 `CommandName="VerProducto"` · `action-icon action-view`
- ✏ `CommandName="Elegir"` · `action-icon action-edit`
- ⏻ `CommandName="CambiarEstado"` · `action-icon action-toggle` + confirm JS (`confirm('¿Desea cambiar el estado de este producto?')`)

## CSS nuevo (~20 líneas en Stitch.css)

```css
.product-name{display:flex;align-items:center;gap:11px}
.product-name strong{display:block;color:#192234;font-size:13px}
.product-name small{display:block;color:#9aa3b2;font-size:11px;margin-top:3px;max-width:220px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}
.product-icon{display:grid;place-items:center;width:35px;height:35px;flex-shrink:0;border-radius:11px;background:#d0fae5;color:#05734a}
.product-icon svg{width:17px;height:17px;fill:none;stroke:currentColor;stroke-width:1.8;stroke-linecap:round;stroke-linejoin:round}
.tasa-stack strong{display:block;color:#087c49;font-size:13px;font-weight:750}
.tasa-stack small{display:block;color:#9aa3b2;font-size:11px;margin-top:2px}
.method-pill{display:inline-block;padding:3px 9px;border-radius:20px;background:#eef2f7;color:#5b6b80;font-size:10px;font-weight:650;margin:2px 3px 2px 0}
.table tr:nth-child(6n+2) .product-icon{background:#fed7aa;color:#c04c11}
.table tr:nth-child(6n+3) .product-icon{background:#bfdcff;color:#2258c5}
.table tr:nth-child(6n+4) .product-icon{background:#e1e6ef;color:#526278}
.table tr:nth-child(6n+5) .product-icon{background:#fbcfe8;color:#bd277b}
```
Se reutilizan sin duplicar: `.client-filters`, `.client-list`, `.list-caption`, `.status-pill`, `.action-icon`/`.action-svg` (bajo `.client-list`), `.grid-count`, `.table` base, responsive ≤760px.

## JS (+10 líneas en StitchProductos.js)

```js
// Enter en búsqueda → btnBuscarProducto.click()
// tr[data-view-button] click → btnVer.click()   [seteado en RowDataBound como Clientes]
```

## Verificación
1. Filtros: buscar "préstamo", estado Activos, método Francés, Limpiar → resultados correctos
2. Tabla: 8 columnas en orden de la imagen, pills de métodos, tasa verde, status pill
3. ✏ Abrir modal con datos · 👁 abrir modal · ⏻ toggle Activo + refresca lista
4. Paginación a página 2, conteo "Mostrando X–Y de N productos"
5. No-admin: columna Acciones oculta, botón Crear oculto (comportamiento actual)
6. Responsive ≤760px: filtros 2 col, tabla scroll horizontal
7. Enter en búsqueda dispara filtro; dropdowns AutoPostBack filtran
8. Consola sin errores; guardar producto refresca la tabla filtrada
