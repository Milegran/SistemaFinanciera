<%@ Page Title="Comprobante de pago" Language="VB" AutoEventWireup="false" MasterPageFile="~/Site.Master" CodeBehind="Factura.aspx.vb" Inherits="SistemaFinanciera.FacturaPago" %>
<asp:Content ID="Body" ContentPlaceHolderID="MainContent" runat="server">
    <div class="finance-page payment-invoice-page">
        <header class="page-header payment-invoice-page-heading">
            <a href="<%= UrlRegreso %>" class="payment-invoice-back" title="Regresar" aria-label="Regresar"><svg viewBox="0 0 24 24" aria-hidden="true"><path d="M19 12H5m7 7-7-7 7-7" /></svg></a>
            <div><span class="eyebrow">CAJA · COMPROBANTE ELECTRÓNICO</span><h1>Comprobante de pago</h1><p>Detalle del pago aplicado al préstamo.</p></div>
        </header>

        <div id="pnlMensaje" runat="server" class="alert" role="status" aria-live="polite">
            <asp:Literal ID="mensaje" runat="server" Mode="Encode" />
        </div>

        <asp:Panel ID="factura" runat="server" CssClass="cw-card payment-flat-receipt" Visible="false">
            <header class="payment-flat-receipt-head">
                <div class="payment-flat-brand"><span class="payment-flat-brand-mark" aria-hidden="true">CW</span><div><strong>CapitalWest</strong><small>Gestión financiera · Panamá</small></div></div>
                <span class="payment-flat-status"><span aria-hidden="true">✓</span> Pago registrado</span>
            </header>

            <div class="payment-flat-title-row">
                <div><span class="eyebrow">COMPROBANTE ELECTRÓNICO</span><h2>Comprobante de pago</h2><p>Detalle de la transacción registrada.</p></div>
                <div class="payment-flat-number"><small>N.º de recibo</small><strong><asp:Literal ID="reciboNumero" runat="server" Mode="Encode" /></strong></div>
            </div>

            <section class="payment-flat-details" aria-label="Datos del pago">
                <div><small>Cliente</small><strong><asp:Literal ID="reciboCliente" runat="server" Mode="Encode" /></strong></div>
                <div><small>Préstamo</small><strong><asp:Literal ID="reciboPrestamo" runat="server" Mode="Encode" /></strong></div>
                <div><small>Fecha y hora</small><strong><asp:Literal ID="reciboFecha" runat="server" Mode="Encode" /></strong></div>
                <div><small>Forma de pago</small><strong><asp:Literal ID="reciboForma" runat="server" Mode="Encode" /></strong></div>
                <div class="payment-flat-reference"><small>Referencia</small><strong><asp:Literal ID="reciboReferencia" runat="server" Mode="Encode" /></strong></div>
            </section>

            <section class="payment-flat-breakdown" aria-label="Aplicación del pago">
                <div class="payment-flat-total"><span>Total recibido</span><strong>B/. <asp:Literal ID="reciboMonto" runat="server" Mode="Encode" /></strong></div>
                <div><span>Aplicado a capital</span><strong>B/. <asp:Literal ID="reciboCapital" runat="server" Mode="Encode" /></strong></div>
                <div><span>Aplicado a intereses</span><strong>B/. <asp:Literal ID="reciboInteres" runat="server" Mode="Encode" /></strong></div>
                <div><span>Mora</span><strong>B/. <asp:Literal ID="reciboMora" runat="server" Mode="Encode" /></strong></div>
                <div><span>Otros cargos</span><strong>B/. <asp:Literal ID="reciboCargos" runat="server" Mode="Encode" /></strong></div>
                <div class="payment-flat-balance"><span>Saldo de capital posterior</span><strong>B/. <asp:Literal ID="reciboSaldoCapital" runat="server" Mode="Encode" /></strong></div>
            </section>

            <p class="payment-flat-note">El pago se distribuyó según el calendario y las condiciones registradas para este préstamo.</p>
            <footer class="payment-flat-actions">
                <button type="button" class="btn btn-primary" data-imprimir-factura-plana>Imprimir comprobante / PDF</button>
            </footer>
        </asp:Panel>
    </div>
    <div id="facturaImpresion" aria-hidden="true"><asp:Literal ID="documentoFactura" runat="server" Mode="PassThrough" /></div>
    <script>
        (() => {
            const button = document.querySelector('[data-imprimir-factura-plana]');
            if (!button) return;
            // El documento formal (#facturaImpresion) es lo único que se imprime.
            button.addEventListener('click', () => window.print());
        })();
    </script>
</asp:Content>
<asp:Content ID="Head" ContentPlaceHolderID="HeadContent" runat="server">
<style>
#facturaImpresion{display:none}
@media print{
 /* Margen 0 quita la fecha y el título que agrega el navegador; el documento trae su propio margen. */
 @page{size:letter portrait;margin:0}
 body *{visibility:hidden!important}
 #facturaImpresion,#facturaImpresion *{visibility:visible!important}
 #facturaImpresion{display:block!important;position:absolute;left:0;top:0;width:100%;background:#fff}
 .fac-doc{box-sizing:border-box;width:100%;padding:16mm 18mm;font:10.5pt Arial,Helvetica,sans-serif;color:#1c2a3d}
 .fac-head{display:flex;justify-content:space-between;align-items:center;margin-bottom:18px}
 .fac-head img{height:48px}
 .fac-head h1{margin:0;color:#1c2a3d;font-size:15pt;font-weight:700;text-transform:uppercase;letter-spacing:.08em}
 .fac-head p{margin:3px 0 0;text-align:right;color:#0d7a48;font-size:9pt;font-weight:700;letter-spacing:.06em;text-transform:uppercase}
 .fac-parties{display:flex;justify-content:space-between;gap:24px;padding-bottom:12px;border-bottom:1px solid #d3e0d8}
 .fac-parties>div{flex:1}.fac-parties .fac-right{text-align:right}
 .fac-label{display:block;margin-bottom:4px;font-size:8pt;font-weight:700;text-transform:uppercase;letter-spacing:.08em}
 .fac-parties strong{display:block;margin-bottom:4px;color:#0d7a48;font-size:14pt;font-weight:600}
 .fac-parties p{margin:1px 0;font-size:9.5pt}
 .fac-section{margin-top:16px;break-inside:avoid}
 .fac-section h2{margin:0 0 6px;padding:5px 8px;background:#0d7a48!important;color:#fff!important;font-size:9.5pt;text-transform:uppercase;letter-spacing:.05em;-webkit-print-color-adjust:exact;print-color-adjust:exact}
 .fac-table{width:100%;border-collapse:collapse;font-size:10pt}
 .fac-table th{padding:6px 8px;background:#e6f2eb!important;color:#0d5c37;border:1px solid #d3e0d8;text-align:left;-webkit-print-color-adjust:exact;print-color-adjust:exact}
 .fac-table td{padding:7px 8px;border-bottom:1px solid #e8efea}
 .fac-table .num{text-align:right;white-space:nowrap}
 .fac-table tfoot td{font-weight:700;font-size:11pt;color:#0d5c37;background:#e6f2eb!important;border-top:1px solid #c5dccd;-webkit-print-color-adjust:exact;print-color-adjust:exact}
 .fac-summary{display:grid;grid-template-columns:1fr 1fr;gap:8px}
 .fac-summary div{padding:8px 10px;border:1px solid #d3e0d8;background:#f6faf8!important;-webkit-print-color-adjust:exact;print-color-adjust:exact}
 .fac-summary div.is-key{background:#e6f2eb!important}
 .fac-summary span{display:block;font-size:7.5pt;color:#6b7a8c;text-transform:uppercase;font-weight:700}
 .fac-summary strong{font-size:13pt}.fac-summary .is-key strong{color:#0d7a48}
 .fac-signs{display:flex;justify-content:space-around;margin-top:56px;break-inside:avoid}
 .fac-signs div{width:38%;text-align:center;font-size:9pt;color:#6b7a8c}.fac-signs span{display:block;margin-bottom:4px;border-top:1px solid #1c2a3d}
 .fac-foot{margin-top:22px;padding-top:6px;border-top:1px solid #d3e0d8;font-size:8pt;color:#6b7a8c}
}
</style>
</asp:Content>
