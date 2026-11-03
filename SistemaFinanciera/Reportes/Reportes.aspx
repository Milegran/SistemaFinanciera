<%@ Page Title="Reportes financieros" Language="vb" AutoEventWireup="false" MasterPageFile="~/Site.Master" CodeBehind="Reportes.aspx.vb" Inherits="SistemaFinanciera.Reportes" %>
<asp:Content ID="Head" ContentPlaceHolderID="HeadContent" runat="server">
<style>
.reports-intro{display:flex;justify-content:space-between;align-items:center;gap:20px;margin-bottom:24px}
.reports-intro h1{margin:0 0 8px;color:#162235;font-family:inherit;font-size:38px;font-weight:750;line-height:1.15;letter-spacing:-1.2px}
.reports-intro p{margin:0;color:#8993a4;font-size:13px}
.reports-badge{padding:9px 16px;border-radius:30px;background:#e4f5ed;color:#087c49;font-weight:700;white-space:nowrap}
.reports-filters{display:grid;grid-template-columns:2fr 1fr 1fr;gap:18px}
.reports-filters label{display:block;margin-bottom:7px;font-size:12px;font-weight:600}
.reports-filters select,.reports-filters input{width:100%}
.reports-actions{display:flex;align-items:center;gap:10px;flex-wrap:wrap;margin-top:20px}
.reports-description{margin:12px 0 0;color:#718096;font-size:13px}
.reports-summary{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:16px;margin:20px 0}
.reports-metric{padding:22px;border:1px solid #e5eaf0;border-radius:20px;background:#fff}
.reports-metric small{display:block;color:#7a889a;font-size:11px;text-transform:uppercase;letter-spacing:.05em}
.reports-metric strong{display:block;margin-top:8px;color:#0b6240;font-size:24px}
.reports-results{margin-top:20px}
.reports-result-header{display:flex;align-items:center;justify-content:space-between;gap:15px;flex-wrap:wrap;margin-bottom:20px}
.reports-result-header h2{margin:0;font-size:19px}
.reports-result-header p{margin:7px 0 0;color:#7a889a;font-size:12px}
.reports-chart-card{margin-top:20px;padding:23px;border:1px solid #e7edf0;border-radius:24px;background:linear-gradient(135deg,#fff 0%,#fbfdfc 100%)}
.reports-chart-head{display:flex;justify-content:space-between;align-items:flex-start;gap:14px;margin-bottom:18px}
.reports-chart-head h3{margin:0;color:#1a2738;font-size:17px;font-weight:700}
.reports-chart-head p{margin:5px 0 0;color:#8995a5;font-size:11px}
.reports-chart-mark{padding:6px 10px;border-radius:999px;background:#e8f5ee;color:#087c49;font-size:10px;font-weight:700;white-space:nowrap}
.report-bars{display:grid;gap:13px}
.report-bar-row{display:grid;grid-template-columns:minmax(125px,220px) minmax(80px,1fr) minmax(90px,130px);align-items:center;gap:13px}
.report-bar-label{overflow:hidden;color:#526174;font-size:11px;text-overflow:ellipsis;white-space:nowrap}
.report-bar-track{height:11px;overflow:hidden;border-radius:99px;background:#edf2f4}
.report-bar-fill{height:100%;border-radius:inherit;background:linear-gradient(90deg,#13a266,#087849);box-shadow:0 2px 7px #0d7a4820}
.report-bar-value{color:#087c49;font-size:11px;font-weight:700;text-align:right;white-space:nowrap}
.report-line-chart{display:block;width:100%;height:auto;min-height:220px;overflow:visible}
.report-line-chart .chart-grid{stroke:#edf1f4;stroke-width:1}
.report-line-chart .chart-area{fill:url(#reportChartFill)}
.report-line-chart .chart-line{fill:none;stroke:#0d7a48;stroke-width:3;stroke-linecap:round;stroke-linejoin:round}
.report-line-chart .chart-point{fill:#fff;stroke:#0d7a48;stroke-width:2}
.report-line-chart text{fill:#8995a5;font-family:inherit;font-size:10px}
.report-chart-combo{display:grid;grid-template-columns:minmax(0,1fr) minmax(0,1.05fr);align-items:stretch;gap:20px}
.report-pie-panel,.report-bars-panel{min-width:0;padding:17px;border:1px solid #edf1f3;border-radius:18px;background:#fff}
.report-pie-panel h4,.report-bars-panel h4{margin:0 0 15px;color:#344255;font-size:12px;font-weight:700}
.report-pie-layout{display:grid;grid-template-columns:minmax(145px,180px) minmax(0,1fr);align-items:center;gap:14px}
.report-pie{display:block;width:100%;max-width:180px;margin:auto}
.report-pie-total{fill:#182638;font-family:inherit;font-size:12px;font-weight:800}
.report-pie-caption{fill:#8995a5;font-family:inherit;font-size:8px}
.report-pie-legend{display:grid;gap:10px;min-width:0}
.report-pie-item{display:grid;grid-template-columns:9px minmax(0,1fr) auto;align-items:center;gap:7px 9px;font-size:10px}
.report-pie-dot{width:9px;height:9px;border-radius:50%}
.report-pie-name{overflow:hidden;color:#657386;text-overflow:ellipsis;white-space:nowrap}
.report-pie-item strong{color:#233247;font-size:10px;white-space:nowrap}
.report-pie-item small{grid-column:2/4;color:#929cab;font-size:9px}
.report-chart-empty{padding:32px 12px;color:#8793a3;font-size:12px;text-align:center}
.reports-table{min-width:850px}
.reports-table th{white-space:nowrap}
.reports-caption{margin-top:18px;color:#738399;font-size:12px}
@media(max-width:900px){.report-chart-combo{grid-template-columns:1fr}}
@media(max-width:760px){.reports-filters{grid-template-columns:1fr}.reports-summary{grid-template-columns:1fr}.reports-intro{align-items:flex-start}.reports-intro h1{font-size:30px;letter-spacing:-.8px}.reports-metric strong{font-size:20px}.reports-badge{display:none}.report-bar-row{grid-template-columns:minmax(85px,120px) minmax(55px,1fr) minmax(75px,95px);gap:8px}.reports-chart-card{padding:17px;border-radius:20px}.reports-chart-head h3{font-size:15px}.report-pie-layout{grid-template-columns:minmax(130px,160px) minmax(0,1fr);gap:10px}.report-pie-item{grid-template-columns:8px minmax(0,1fr) auto;gap:6px}.report-pie-panel,.report-bars-panel{padding:13px}}
</style>
</asp:Content>
<asp:Content ID="Body" ContentPlaceHolderID="MainContent" runat="server">
<div class="finance-page reports-page">
 <div class="reports-intro"><div><div class="eyebrow">CapitalWest / Análisis</div><h1>Reportes financieros</h1><p>Consulta la cartera, revisa movimientos y exporta la información que necesitas.</p></div><span class="reports-badge">10 reportes disponibles</span></div>
 <div id="pnlMensaje" runat="server" class="alert" role="status" aria-live="polite"><asp:Literal ID="mensaje" runat="server" Mode="Encode" /></div>
 <section class="cw-card" aria-label="Filtros del reporte"><div class="reports-filters">
  <div><label for="<%= reporte.ClientID %>">Reporte</label><asp:DropDownList ID="reporte" runat="server" CssClass="form-select" AutoPostBack="true" /></div>
  <div><label for="<%= desde.ClientID %>">Desde</label><asp:TextBox ID="desde" runat="server" CssClass="form-control" TextMode="Date" min="1900-01-01" max="2099-12-31" /></div>
  <div><label for="<%= hasta.ClientID %>">Hasta</label><asp:TextBox ID="hasta" runat="server" CssClass="form-control" TextMode="Date" min="1900-01-01" max="2099-12-31" /></div>
  <div><label for="<%= cliente.ClientID %>">Cliente</label><asp:DropDownList ID="cliente" runat="server" CssClass="form-select" AutoPostBack="true" /></div>
  <div><label for="<%= producto.ClientID %>">Producto</label><asp:DropDownList ID="producto" runat="server" CssClass="form-select" AutoPostBack="true" /></div>
  <div><label for="<%= prestamo.ClientID %>">Préstamo</label><asp:DropDownList ID="prestamo" runat="server" CssClass="form-select" /></div>
 </div><p class="reports-description"><asp:Literal ID="descripcion" runat="server" Mode="Encode" /></p>
 <div class="reports-actions"><asp:Button ID="generar" runat="server" Text="Generar reporte" CssClass="btn btn-primary" /><asp:Button ID="limpiar" runat="server" Text="Limpiar filtros" CssClass="btn btn-light" CausesValidation="false" /></div></section>
 <asp:Panel ID="resultados" runat="server" Visible="false">
  <div class="reports-summary"><div class="reports-metric"><small>Registros del resultado</small><strong><asp:Literal ID="cantidad" runat="server" Mode="Encode" /></strong></div><div class="reports-metric"><small><asp:Literal ID="etiquetaTotal" runat="server" Mode="Encode" /></small><strong><asp:Literal ID="total" runat="server" Mode="Encode" /></strong></div><div class="reports-metric"><small>Generado en Panamá</small><strong style="font-size:17px"><asp:Literal ID="fechaGenerado" runat="server" Mode="Encode" /></strong></div></div>
  <section class="reports-chart-card" aria-label="Gráfica del reporte"><div class="reports-chart-head"><div><h3><asp:Literal ID="tituloGrafica" runat="server" Mode="Encode" /></h3><p><asp:Literal ID="subtituloGrafica" runat="server" Mode="Encode" /></p></div><span class="reports-chart-mark">Visualización del reporte</span></div><asp:Literal ID="graficaReporte" runat="server" Mode="PassThrough" /></section>
  <section class="cw-card reports-results"><div class="reports-result-header"><div><h2><asp:Literal ID="titulo" runat="server" Mode="Encode" /></h2><p><asp:Literal ID="filtrosAplicados" runat="server" Mode="Encode" /></p></div><div class="reports-actions" style="margin-top:0"><asp:Button ID="exportar" runat="server" Text="CSV / Excel" CssClass="btn btn-outline-primary" /><asp:Button ID="imprimir" runat="server" Text="Imprimir / PDF" CssClass="btn btn-light" formtarget="_blank" /></div></div>
   <div class="table-responsive"><asp:GridView ID="tabla" runat="server" AutoGenerateColumns="false" AllowPaging="true" PageSize="12" EnableViewState="false" CssClass="table table-hover reports-table simulator-table" GridLines="None" EmptyDataText="No hay registros para los filtros seleccionados." /></div>
   <p class="reports-caption"><asp:Literal ID="conteo" runat="server" Mode="Encode" /></p><p class="reports-caption">Los importes se expresan en balboas (B/.) y la tabla, descarga e impresión conservan el resultado completo.</p>
  </section>
 </asp:Panel>
</div>
</asp:Content>
