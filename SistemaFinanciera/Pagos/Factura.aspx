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
    <script>
        (() => {
            const button = document.querySelector('[data-imprimir-factura-plana]');
            if (!button) return;
            button.addEventListener('click', () => window.print());
            window.addEventListener('beforeprint', () => document.body.classList.add('imprimir-factura-plana'));
            window.addEventListener('afterprint', () => document.body.classList.remove('imprimir-factura-plana'));
        })();
    </script>
</asp:Content>
