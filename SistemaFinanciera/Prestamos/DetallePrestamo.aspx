<%@ Page Title="Detalle del préstamo" Language="VB" AutoEventWireup="false" MasterPageFile="~/Site.Master" CodeBehind="DetallePrestamo.aspx.vb" Inherits="SistemaFinanciera.DetallePrestamo" %>
<asp:Content ID="Body" ContentPlaceHolderID="MainContent" runat="server">
<div class="finance-page request-detail-page loan-detail-page">
    <div id="pnlMensaje" runat="server" class="alert" role="status" aria-live="polite"><asp:Literal ID="mensaje" runat="server" Mode="Encode" /></div>
    <header class="request-detail-heading">
        <asp:LinkButton ID="btnVolverPrestamos" runat="server" ToolTip="Volver a préstamos" aria-label="Volver a préstamos" CssClass="customer-back request-detail-back" CausesValidation="false" OnClick="VolverPrestamos"><svg class="request-detail-back-icon" viewBox="0 0 24 24" aria-hidden="true" focusable="false"><path d="M19 12H5m7 7-7-7 7-7" /></svg></asp:LinkButton>
        <div class="request-detail-heading-content">
            <span class="request-detail-eyebrow">CAPITALWEST · CONTRATO DE CRÉDITO</span>
            <h1 class="request-detail-title">Detalle del préstamo</h1>
            <p><asp:Literal ID="litNumeroPrestamo" runat="server" Mode="Encode" /> · <asp:Literal ID="litEstadoPrestamo" runat="server" Mode="Encode" /></p>
        </div>
    </header>

    <section class="request-detail-applicant request-applicant-profile cw-card" aria-labelledby="loanApplicantTitle">
        <div class="request-detail-card-head request-applicant-card-head">
            <div class="request-applicant-person"><span class="request-applicant-icon" aria-hidden="true"><asp:Literal ID="litInicialesCliente" runat="server" Mode="Encode" /></span><div><h2 id="loanApplicantTitle"><asp:Literal ID="litNombreCliente" runat="server" Mode="Encode" /></h2><small class="request-applicant-person-type"><asp:Literal ID="litTipoPersona" runat="server" Mode="Encode" /></small></div></div>
            <span class="request-detail-identification"><asp:Literal ID="litIdentificacion" runat="server" Mode="Encode" /></span>
        </div>
        <div class="request-applicant-grid">
            <div><span>Fecha de nacimiento</span><strong><asp:Literal ID="litNacimiento" runat="server" Mode="Encode" /></strong></div>
            <div><span>Teléfono</span><strong><asp:Literal ID="litTelefono" runat="server" Mode="Encode" /></strong></div>
            <div><span>Correo electrónico</span><strong><asp:Literal ID="litCorreo" runat="server" Mode="Encode" /></strong></div>
            <div><span>Sexo</span><strong><asp:Literal ID="litSexo" runat="server" Mode="Encode" /></strong></div>
            <div><span>Estado civil</span><strong><asp:Literal ID="litEstadoCivil" runat="server" Mode="Encode" /></strong></div>
            <div><span>Ocupación / situación laboral</span><strong><asp:Literal ID="litOcupacion" runat="server" Mode="Encode" /></strong></div>
            <div><span>Ingresos mensuales</span><strong class="is-green">B/. <asp:Literal ID="litIngresos" runat="server" Mode="Encode" /></strong></div>
            <div class="request-applicant-address"><span>Dirección</span><strong><asp:Literal ID="litDireccion" runat="server" Mode="Encode" /></strong></div>
            <div class="request-applicant-address"><span>Referencias personales</span><strong><asp:Literal ID="litReferencias" runat="server" Mode="Encode" /></strong></div>
        </div>
    </section>

    <section class="request-detail-summary" aria-label="Resumen financiero del préstamo">
        <article class="simulator-metric"><span>Capital aprobado</span><strong>B/. <asp:Literal ID="litCapitalAprobado" runat="server" Mode="Encode" /></strong><small>Monto contractual</small></article>
        <article class="simulator-metric simulator-metric-featured"><span>Capital pendiente</span><strong>B/. <asp:Literal ID="litCapitalPendiente" runat="server" Mode="Encode" /></strong><small>Saldo de capital actual</small></article>
        <article class="simulator-metric"><span>Intereses pendientes</span><strong class="is-green">B/. <asp:Literal ID="litInteresesPendientes" runat="server" Mode="Encode" /></strong><small>Interés programado</small></article>
        <article class="simulator-metric"><span>Mora y cargos pendientes</span><strong><asp:Literal ID="litMoraCargos" runat="server" Mode="Encode" /></strong><small>Importes registrados</small></article>
    </section>

    <section class="request-detail-applicant request-detail-application cw-card" aria-label="Condiciones del préstamo">
        <div class="request-detail-card-head"><div><span class="request-detail-eyebrow">INFORMACIÓN CONTRACTUAL</span><h2>Datos del préstamo</h2></div></div>
        <div class="request-applicant-grid">
            <div><span>Producto</span><strong><asp:Literal ID="litProducto" runat="server" Mode="Encode" /></strong></div>
            <div><span>Método de cálculo</span><strong><asp:Literal ID="litMetodo" runat="server" Mode="Encode" /></strong></div>
            <div><span>Periodicidad</span><strong><asp:Literal ID="litPeriodicidad" runat="server" Mode="Encode" /></strong></div>
            <div><span>Tasa anual pactada</span><strong><asp:Literal ID="litTasa" runat="server" Mode="Encode" /></strong></div>
            <div><span>Capitalización</span><strong><asp:Literal ID="litCapitalizacion" runat="server" Mode="Encode" /></strong></div>
            <div><span>Cantidad de cuotas</span><strong><asp:Literal ID="litCantidadCuotas" runat="server" Mode="Encode" /></strong></div>
            <div><span>Plazo</span><strong><asp:Literal ID="litPlazo" runat="server" Mode="Encode" /></strong></div>
            <div><span>Fecha de aprobación</span><strong><asp:Literal ID="litFechaAprobacion" runat="server" Mode="Encode" /></strong></div>
            <div><span>Fecha de desembolso</span><strong><asp:Literal ID="litFechaDesembolso" runat="server" Mode="Encode" /></strong></div>
            <div><span>Primer vencimiento</span><strong><asp:Literal ID="litPrimerPago" runat="server" Mode="Encode" /></strong></div>
            <div><span>Comisión retenida</span><strong>B/. <asp:Literal ID="litComision" runat="server" Mode="Encode" /></strong></div>
            <div><span>Gastos asociados</span><strong>B/. <asp:Literal ID="litGastos" runat="server" Mode="Encode" /></strong></div>
        </div>
        <div class="finance-actions loan-detail-actions"><asp:LinkButton ID="btnVolverAbajo" runat="server" CssClass="btn btn-light" CausesValidation="false" OnClick="VolverPrestamos">Volver a préstamos</asp:LinkButton><a class="btn btn-primary" href="<%= ResolveUrl("~/Pagos/Pagos.aspx") %>">Registrar pago</a></div>
    </section>

    <section class="cw-card simulator-schedule request-detail-schedule" aria-labelledby="loanScheduleTitle">
        <div class="simulator-section-heading"><div><span class="simulator-eyebrow">CALENDARIO REAL DEL CONTRATO</span><h2 id="loanScheduleTitle">Tabla de amortización y saldos</h2><p>Consulta el estado de cada cuota y sus importes pendientes.</p></div><span class="simulator-period-count"><asp:Literal ID="litBadgeCuotas" runat="server" Mode="Encode" /> cuotas</span></div>
        <div class="table-responsive"><asp:GridView ID="cuotas" runat="server" AutoGenerateColumns="false" AllowPaging="true" PageSize="12" OnPageIndexChanging="CambiarPaginaCuotas" CssClass="table table-hover align-middle simulator-table" GridLines="None" EmptyDataText="Este préstamo no tiene cuotas registradas."><Columns>
            <asp:BoundField DataField="NumeroCuota" HeaderText="N.º cuota" />
            <asp:BoundField DataField="FechaVencimiento" HeaderText="Vencimiento" DataFormatString="{0:dd/MM/yyyy}" />
            <asp:BoundField DataField="Capital" HeaderText="Capital (B/.)" DataFormatString="{0:N2}" />
            <asp:BoundField DataField="Interes" HeaderText="Interés (B/.)" DataFormatString="{0:N2}" />
            <asp:BoundField DataField="CapitalPendiente" HeaderText="Capital pendiente (B/.)" DataFormatString="{0:N2}" />
            <asp:BoundField DataField="TotalPendiente" HeaderText="Total pendiente (B/.)" DataFormatString="{0:N2}" />
            <asp:BoundField DataField="EstadoCuota" HeaderText="Estado" />
        </Columns></asp:GridView></div>
    </section>

    <section class="cw-card simulator-schedule request-detail-schedule" aria-labelledby="loanMovementsTitle">
        <div class="simulator-section-heading"><div><span class="simulator-eyebrow">HISTORIAL CONTABLE</span><h2 id="loanMovementsTitle">Movimientos del préstamo</h2><p>Intereses y pagos contabilizados en el contrato.</p></div></div>
        <div class="table-responsive"><asp:GridView ID="movimientos" runat="server" AutoGenerateColumns="false" AllowPaging="true" PageSize="12" OnPageIndexChanging="CambiarPaginaMovimientos" CssClass="table table-hover align-middle simulator-table" GridLines="None" EmptyDataText="Este préstamo aún no tiene movimientos."><Columns>
            <asp:BoundField DataField="Fecha" HeaderText="Fecha" DataFormatString="{0:dd/MM/yyyy HH:mm}" />
            <asp:BoundField DataField="Tipo" HeaderText="Operación" />
            <asp:BoundField DataField="Concepto" HeaderText="Concepto" />
            <asp:BoundField DataField="Debito" HeaderText="Débito (B/.)" DataFormatString="{0:N2}" />
            <asp:BoundField DataField="Credito" HeaderText="Crédito (B/.)" DataFormatString="{0:N2}" />
            <asp:BoundField DataField="SaldoAcumulado" HeaderText="Saldo (B/.)" DataFormatString="{0:N2}" />
        </Columns></asp:GridView></div>
    </section>
</div>
</asp:Content>
