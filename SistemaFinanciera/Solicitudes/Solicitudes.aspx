<%@ Page Title="Solicitudes de crédito" Language="VB" AutoEventWireup="false" MasterPageFile="~/Site.Master" CodeBehind="Solicitudes.aspx.vb" Inherits="SistemaFinanciera.Solicitudes" %>
<asp:Content ID="Body" ContentPlaceHolderID="MainContent" runat="server">
<div class="finance-page request-page">
<asp:Panel ID="pnlListadoSolicitudes" runat="server" CssClass="request-listing-shell">
    <header class="page-header page-header--split">
        <div><h1>Solicitudes de crédito</h1><p>CapitalWest · Gestión financiera</p></div>
        <asp:Panel ID="headerNueva" runat="server" CssClass="request-header-actions"><a href="<%= ResolveUrl("~/Simulador/Simulador.aspx") %>" class="btn request-simulator-link"><svg viewBox="0 0 24 24" aria-hidden="true" focusable="false"><rect x="5" y="2" width="14" height="20" rx="3"/><path d="M8 6h8M8.5 11h.01M12 11h.01M15.5 11h.01M8.5 15h.01M12 15h.01M15.5 15h.01M8.5 18.5h.01M12 18.5h.01"/></svg><span>Simulador</span></a><button type="button" class="btn btn-primary" data-abrir-modal-solicitud>+ Nueva solicitud</button></asp:Panel>
    </header>
    <div id="pnlMensaje" runat="server" class="alert" role="status" aria-live="polite"><asp:Literal ID="mensaje" runat="server" Mode="Encode" /></div>
    <section class="customer-profile-kpis request-kpis" aria-label="Resumen de solicitudes">
        <asp:LinkButton ID="filtroTotal" runat="server" Text="0" data-label="Total" data-note="Solicitudes registradas" aria-label="Filtrar todas las solicitudes" CssClass="client-kpi customer-metric request-kpi" CommandName="Filtrar" CommandArgument="" CausesValidation="false" />
        <asp:LinkButton ID="filtroEnEvaluacion" runat="server" Text="0" data-label="En evaluación" data-note="En revisión de crédito" aria-label="Filtrar solicitudes en evaluación" CssClass="client-kpi customer-metric request-kpi" CommandName="Filtrar" CommandArgument="EN_EVALUACION" CausesValidation="false" />
        <asp:LinkButton ID="filtroAprobadas" runat="server" Text="0" data-label="Aprobadas" data-note="Listas para formalizar" aria-label="Filtrar solicitudes aprobadas" CssClass="client-kpi customer-metric request-kpi request-kpi--approved" CommandName="Filtrar" CommandArgument="APROBADO" CausesValidation="false" />
        <asp:LinkButton ID="filtroRechazadas" runat="server" Text="0" data-label="Rechazadas" data-note="Con decisión registrada" aria-label="Filtrar solicitudes rechazadas" CssClass="client-kpi customer-metric request-kpi request-kpi--rejected" CommandName="Filtrar" CommandArgument="RECHAZADO" CausesValidation="false" />
    </section>
    <section class="client-filters request-filters" aria-label="Filtros de solicitudes">
        <div><label class="sr-only" for="<%= filtroBusqueda.ClientID %>">Buscar por solicitud, cliente o producto</label><asp:TextBox ID="filtroBusqueda" runat="server" CssClass="form-control" MaxLength="100" placeholder="Buscar solicitud, cliente o producto..." /></div>
        <div><label class="sr-only" for="<%= filtroEstado.ClientID %>">Estado</label><asp:DropDownList ID="filtroEstado" runat="server" CssClass="form-select"><asp:ListItem Text="Estado: Todos" Value="" /><asp:ListItem Text="Solicitud" Value="SOLICITUD" /><asp:ListItem Text="En evaluación" Value="EN_EVALUACION" /><asp:ListItem Text="Aprobada" Value="APROBADO" /><asp:ListItem Text="Rechazada" Value="RECHAZADO" /></asp:DropDownList></div>
        <div><label class="sr-only" for="<%= filtroDesde.ClientID %>">Desde</label><asp:TextBox ID="filtroDesde" runat="server" CssClass="form-control" TextMode="Date" /></div>
        <div><label class="sr-only" for="<%= filtroHasta.ClientID %>">Hasta</label><asp:TextBox ID="filtroHasta" runat="server" CssClass="form-control" TextMode="Date" /></div>
        <div><label class="sr-only" for="<%= filtroCliente.ClientID %>">Cliente</label><asp:DropDownList ID="filtroCliente" runat="server" CssClass="form-select"><asp:ListItem Text="Todos los clientes" Value="" /></asp:DropDownList></div>
        <div><label class="sr-only" for="<%= filtroProducto.ClientID %>">Producto</label><asp:DropDownList ID="filtroProducto" runat="server" CssClass="form-select"><asp:ListItem Text="Todos los productos" Value="" /></asp:DropDownList></div>
        <div class="filter-actions"><asp:Button ID="btnBuscar" runat="server" Text="Buscar" CssClass="btn btn-outline-primary" CausesValidation="false" formnovalidate="formnovalidate" /><asp:Button ID="btnLimpiar" runat="server" Text="↺" ToolTip="Limpiar filtros" CssClass="btn btn-light" CausesValidation="false" formnovalidate="formnovalidate" /></div>
    </section>
    <section class="cw-card client-list request-list">
        <div class="list-caption"><div><strong>Solicitudes registradas</strong><small><asp:Literal ID="litConteo" runat="server" Mode="Encode" /></small></div></div>
        <div class="table-responsive"><asp:GridView ID="listado" runat="server" AutoGenerateColumns="false" DataKeyNames="Id" CssClass="table table-hover align-middle" GridLines="None" EmptyDataText="No hay solicitudes para los filtros seleccionados."><Columns>
            <asp:BoundField DataField="NumeroSolicitud" HeaderText="N.º de solicitud" />
            <asp:BoundField DataField="Cliente" HeaderText="Cliente" />
            <asp:BoundField DataField="Producto" HeaderText="Producto" />
            <asp:BoundField DataField="MontoSolicitado" HeaderText="Monto solicitado" DataFormatString="B/. {0:N2}" />
            <asp:BoundField DataField="FechaSolicitud" HeaderText="Fecha" DataFormatString="{0:dd/MM/yyyy}" />
            <asp:TemplateField HeaderText="Estado"><ItemTemplate><span class='<%# ClaseEstadoSolicitud(Eval("EstadoCodigo")) %>'><%#: TextoEstadoSolicitud(Eval("EstadoCodigo")) %></span></ItemTemplate></asp:TemplateField>
            <asp:TemplateField HeaderText="Acciones"><ItemTemplate><asp:LinkButton ID="btnVerSolicitud" runat="server" CommandName="Elegir" CommandArgument='<%# CType(Container, GridViewRow).RowIndex %>' CssClass="request-row-action" CausesValidation="false" Text="Ver / evaluar" /></ItemTemplate></asp:TemplateField>
        </Columns></asp:GridView></div>
    </section>
</asp:Panel>
    <asp:Panel ID="pnlDetalleSolicitud" runat="server" Visible="false" CssClass="request-detail-page">
        <div class="request-detail-heading">
            <asp:LinkButton ID="btnVolverSolicitudes" runat="server" ToolTip="Volver a solicitudes" aria-label="Volver a solicitudes" CssClass="customer-back request-detail-back" CausesValidation="false" OnClick="VolverSolicitudes"><svg class="request-detail-back-icon" viewBox="0 0 24 24" aria-hidden="true" focusable="false"><path d="M19 12H5m7 7-7-7 7-7" /></svg></asp:LinkButton>
            <div class="request-detail-heading-content">
                <span class="request-detail-eyebrow">CAPITALWEST · EVALUACIÓN DE CRÉDITO</span>
                <h1 class="request-detail-title">Detalle de solicitud</h1>
                <p><asp:Literal ID="litDetalleNumero" runat="server" Mode="Encode" /> · <asp:Literal ID="litDetalleEstado" runat="server" Mode="Encode" /></p>
            </div>
        </div>
        <section class="request-detail-applicant request-applicant-profile cw-card" aria-labelledby="requestApplicantTitle">
            <div class="request-detail-card-head request-applicant-card-head">
                <div class="request-applicant-person"><span class="request-applicant-icon" aria-hidden="true"><asp:Literal ID="litInicialesCliente" runat="server" Mode="Encode" /></span><div><h2 id="requestApplicantTitle"><asp:Literal ID="litDetalleNombre" runat="server" Mode="Encode" /></h2><small class="request-applicant-person-type"><asp:Literal ID="litTipoPersonaCabecera" runat="server" Mode="Encode" /></small></div></div>
                <span class="request-detail-identification"><asp:Literal ID="litDetalleIdentificacion" runat="server" Mode="Encode" /></span>
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
        <section class="request-detail-summary" aria-label="Resumen financiero de la solicitud">
            <article class="simulator-metric"><span>Monto solicitado</span><strong>B/. <asp:Literal ID="litMontoSolicitado" runat="server" Mode="Encode" /></strong><small>Principal solicitado</small></article>
            <article class="simulator-metric"><span>Intereses estimados</span><strong class="is-green">B/. <asp:Literal ID="litIntereses" runat="server" Mode="Encode" /></strong><small>Según las condiciones registradas</small></article>
            <article class="simulator-metric simulator-metric-featured"><span>Total programado</span><strong>B/. <asp:Literal ID="litTotalProgramado" runat="server" Mode="Encode" /></strong><small>Capital más intereses estimados</small></article>
            <article class="simulator-metric"><span>Cuota máxima</span><strong>B/. <asp:Literal ID="litCuotaMaxima" runat="server" Mode="Encode" /></strong><small><asp:Literal ID="litMetodoResumen" runat="server" Mode="Encode" /></small></article>
        </section>
        <section class="request-detail-applicant request-detail-application cw-card" aria-label="Información de registro y evaluación">
            <div class="request-detail-card-head"><div><span class="request-detail-eyebrow">REGISTRO Y SEGUIMIENTO</span><h2>Datos de la solicitud</h2></div></div>
            <div class="request-applicant-grid">
                <div><span>Fecha de solicitud</span><strong><asp:Literal ID="litFechaSolicitud" runat="server" Mode="Encode" /></strong></div>
                <div><span>Monto aprobado</span><strong><asp:Literal ID="litMontoAprobado" runat="server" Mode="Encode" /></strong></div>
                <div><span>Ingresos evaluados</span><strong>B/. <asp:Literal ID="litIngresosEvaluados" runat="server" Mode="Encode" /></strong></div>
                <div><span>Fecha de evaluación</span><strong><asp:Literal ID="litFechaEvaluacion" runat="server" Mode="Encode" /></strong></div>
                <div class="request-applicant-address"><span>Observaciones del solicitante</span><strong><asp:Literal ID="litObservacionesSolicitud" runat="server" Mode="Encode" /></strong></div>
                <div class="request-applicant-address"><span>Motivo de rechazo</span><strong><asp:Literal ID="litMotivoRechazo" runat="server" Mode="Encode" /></strong></div>
            </div>
        </section>
        <section class="simulator-net-summary request-detail-net" aria-label="Costos y monto neto">
            <div><span>Comisión retenida</span><strong>B/. <asp:Literal ID="litComision" runat="server" Mode="Encode" /></strong></div>
            <div><span>Gastos retenidos</span><strong>B/. <asp:Literal ID="litGastos" runat="server" Mode="Encode" /></strong></div>
            <div><span>Neto estimado a recibir</span><strong class="is-green">B/. <asp:Literal ID="litNeto" runat="server" Mode="Encode" /></strong></div>
        </section>
        <section class="cw-card simulator-schedule request-detail-schedule" aria-labelledby="requestScheduleTitle">
            <div class="simulator-section-heading"><div><span class="simulator-eyebrow">CALENDARIO ESTIMADO</span><h2 id="requestScheduleTitle">Tabla de amortización</h2><p><asp:Literal ID="litDetalleCondiciones" runat="server" Mode="Encode" /></p></div><span class="simulator-period-count"><asp:Literal ID="litCantidadCuotas" runat="server" Mode="Encode" /> cuotas</span></div>
            <div class="table-responsive"><asp:GridView ID="tablaDetallePlan" runat="server" AutoGenerateColumns="false" AllowPaging="true" PageSize="12" CssClass="table table-hover align-middle simulator-table" GridLines="None" EmptyDataText="No se pudo generar el calendario estimado para esta solicitud."><Columns><asp:BoundField DataField="Numero" HeaderText="N.º cuota" /><asp:BoundField DataField="Fecha" HeaderText="Fecha estimada" DataFormatString="{0:dd/MM/yyyy}" /><asp:BoundField DataField="Capital" HeaderText="Capital (B/.)" DataFormatString="{0:N2}" /><asp:BoundField DataField="Interes" HeaderText="Interés (B/.)" DataFormatString="{0:N2}" /><asp:BoundField DataField="Cuota" HeaderText="Pago estimado (B/.)" DataFormatString="{0:N2}" /><asp:BoundField DataField="Saldo" HeaderText="Saldo proyectado (B/.)" DataFormatString="{0:N2}" /></Columns></asp:GridView></div>
        </section>
        <asp:Panel ID="evaluacion" runat="server" data-finanzas-form="evaluacion" CssClass="cw-card request-evaluation" Visible="false"><div class="list-caption"><div><strong>Evaluación de solicitud</strong><small><asp:Literal ID="seleccionada" runat="server" Mode="Encode" /></small></div></div><div id="pnlMensajeEvaluacion" runat="server" visible="false" class="alert" role="status" aria-live="polite" tabindex="-1"><asp:Literal ID="mensajeEvaluacion" runat="server" Mode="Encode" EnableViewState="false" /></div>
        <div class="finance-fields"><div class="finance-field"><label for="<%= aprobado.ClientID %>">Monto aprobado (B/.)</label><asp:TextBox ID="aprobado" runat="server" max="999999999999.99" CssClass="form-control" TextMode="Number" min="0.01" step="0.01" /></div><div class="finance-field"><label for="<%= motivo.ClientID %>">Motivo / observación de evaluación</label><asp:TextBox ID="motivo" runat="server" data-regla="texto-descriptivo" CssClass="form-control" TextMode="MultiLine" MaxLength="1000" data-maxlength="1000" Rows="2" /></div></div><div class="finance-actions request-decision-actions"><asp:Button ID="evaluar" runat="server" formnovalidate="formnovalidate" data-validar-finanzas="evaluar" Text="Iniciar evaluación" CssClass="btn btn-primary request-start-evaluation" /><asp:Button ID="aprobar" runat="server" formnovalidate="formnovalidate" data-validar-finanzas="aprobar" Text="Aprobar" CssClass="btn btn-primary" /><asp:Button ID="rechazar" runat="server" formnovalidate="formnovalidate" data-validar-finanzas="rechazar" Text="Rechazar" CssClass="btn btn-light request-reject" /></div></asp:Panel>
    </asp:Panel>
    <asp:HiddenField ID="modalAbierto" runat="server" Value="0" />
    <div class="form-overlay form-overlay--solicitud" id="modalNuevaSolicitud" runat="server" hidden role="dialog" aria-modal="true" aria-labelledby="tituloSolicitudModal">
    <asp:Panel ID="creacion" runat="server" data-finanzas-form="solicitud" CssClass="form-dialog request-create" DefaultButton="crear">
        <div class="form-dialog-head"><div><h2 id="tituloSolicitudModal">Nueva solicitud</h2><p class="form-dialog-lead">La tasa, capitalización y costos se toman del producto vigente. El calendario definitivo se confirma al desembolsar.</p><button type="button" class="request-paste-button" data-pegar-simulacion><svg viewBox="0 0 24 24" aria-hidden="true"><path d="M8 5H5a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h11a2 2 0 0 0 2-2v-2M8 5a2 2 0 0 0 2 2h3a2 2 0 0 0 2-2M8 5a2 2 0 0 1 2-2h3a2 2 0 0 1 2 2m-3 8h7m-3-3 3 3-3 3m-6-2H7"/></svg>Pegar simulación</button><small class="request-paste-status" data-estado-pegado role="status" aria-live="polite"></small></div><button type="button" class="form-dialog-close" aria-label="Cerrar ventana" data-cerrar-modal-solicitud>&times;</button></div>
        <div id="pnlMensajeCreacion" runat="server" visible="false" class="alert" role="alert"><asp:Literal ID="mensajeCreacion" runat="server" Mode="Encode" EnableViewState="false" /></div>
        <div class="finance-fields"><div class="finance-field"><label for="<%= cliente.ClientID %>">Cliente activo</label><asp:DropDownList ID="cliente" runat="server" required="required" CssClass="form-select" /></div><div class="finance-field"><label for="<%= producto.ClientID %>">Producto activo</label><asp:DropDownList ID="producto" runat="server" required="required" CssClass="form-select" AutoPostBack="true" /></div><div class="finance-field"><label for="<%= metodo.ClientID %>">Método de cálculo</label><asp:DropDownList ID="metodo" runat="server" required="required" CssClass="form-select" /></div><div class="finance-field"><label for="<%= periodo.ClientID %>">Periodicidad de pago</label><asp:DropDownList ID="periodo" runat="server" Enabled="false" CssClass="form-select" /></div><div class="finance-field"><label for="<%= monto.ClientID %>">Monto solicitado (B/.)</label><asp:TextBox ID="monto" runat="server" CssClass="form-control" TextMode="Number" min="0.01" max="999999999999.99" step="0.01" required="required" /></div><div class="finance-field"><label for="<%= cuotas.ClientID %>">Plazo solicitado (meses)</label><asp:DropDownList ID="cuotas" runat="server" CssClass="form-select" Enabled="false" required="required" /><small><asp:Literal ID="ayudaPlazo" runat="server" Mode="Encode" /></small></div><div class="finance-field"><label for="<%= primerPago.ClientID %>">Primer pago previsto</label><asp:TextBox ID="primerPago" runat="server" CssClass="form-control" TextMode="Date" required="required" /></div><div class="finance-field"><label for="<%= observaciones.ClientID %>">Observaciones (opcional)</label><asp:TextBox ID="observaciones" runat="server" data-regla="texto-descriptivo" CssClass="form-control" TextMode="MultiLine" MaxLength="1000" data-maxlength="1000" Rows="2" /></div></div>
        <div class="finance-actions request-modal-actions"><button type="button" class="btn btn-light" data-cerrar-modal-solicitud>Cancelar</button><asp:Button ID="crear" runat="server" formnovalidate="formnovalidate" data-validar-finanzas="solicitud" Text="Registrar solicitud" CssClass="btn btn-primary" /></div>
    </asp:Panel>
    </div>
    <asp:Panel ID="simulacion" runat="server" Visible="false" CssClass="cw-card request-simulation"><div class="list-caption"><div><strong>Simulación</strong><small><asp:Literal ID="totales" runat="server" Mode="Encode" /></small></div></div><div class="table-responsive"><asp:GridView ID="tablaPlan" runat="server" AutoGenerateColumns="false" CssClass="table table-hover align-middle" GridLines="None"><Columns><asp:BoundField DataField="Numero" HeaderText="Cuota" /><asp:BoundField DataField="Fecha" HeaderText="Vencimiento" DataFormatString="{0:dd/MM/yyyy}" /><asp:BoundField DataField="Capital" HeaderText="Capital" DataFormatString="B/. {0:N2}" /><asp:BoundField DataField="Interes" HeaderText="Interés" DataFormatString="B/. {0:N2}" /><asp:BoundField DataField="Cuota" HeaderText="Importe" DataFormatString="B/. {0:N2}" /><asp:BoundField DataField="Saldo" HeaderText="Saldo proyectado" DataFormatString="B/. {0:N2}" /></Columns></asp:GridView></div></asp:Panel>
</div>
<script>
(function(){
    var modal=document.getElementById('<%= modalNuevaSolicitud.ClientID %>');
    var state=document.getElementById('<%= modalAbierto.ClientID %>');
    var opener=document.querySelector('[data-abrir-modal-solicitud]');
    var pasteButton=modal&&modal.querySelector('[data-pegar-simulacion]');
    var pasteStatus=modal&&modal.querySelector('[data-estado-pegado]');
    var closers=modal?modal.querySelectorAll('[data-cerrar-modal-solicitud]'):[];
    var lastFocus=null;
    var transferKey='capitalwest.solicitud.simulacion.v1';
    var pendingKey='capitalwest.solicitud.simulacion.pending.v1';
    function openModal(){if(!modal)return;lastFocus=document.activeElement;modal.hidden=false;if(state)state.value='1';document.body.classList.add('request-modal-open');var first=modal.querySelector('select:not(:disabled),input:not(:disabled),textarea:not(:disabled),button:not(:disabled)');if(first)first.focus();}
    function closeModal(){if(!modal)return;modal.hidden=true;if(state)state.value='0';document.body.classList.remove('request-modal-open');if(lastFocus&&lastFocus.focus)lastFocus.focus();}
    function setValue(id,value){var field=document.getElementById(id);if(!field||value===undefined||value===null)return false;if(field.tagName==='SELECT'&&!Array.prototype.some.call(field.options,function(option){return option.value===String(value);})){return false;}field.value=String(value);field.dispatchEvent(new Event('input',{bubbles:true}));field.dispatchEvent(new Event('change',{bubbles:true}));return true;}
    function readStored(key){try{var value=localStorage.getItem(key);if(value)return value;}catch(error){}try{return sessionStorage.getItem(key);}catch(ignore){return null;}}
    function writeStored(key,value){try{localStorage.setItem(key,value);}catch(error){}try{sessionStorage.setItem(key,value);}catch(error){}}
    function removeStored(key){try{localStorage.removeItem(key);}catch(error){}try{sessionStorage.removeItem(key);}catch(error){}}
    function applyTransfer(data){var pasted=0,skipped=[];[[ '<%= monto.ClientID %>',data.monto,'monto'],['<%= cuotas.ClientID %>',data.cuotas,'cuotas'],['<%= primerPago.ClientID %>',data.primerPago,'fecha'],['<%= metodo.ClientID %>',data.metodo,'método'],['<%= periodo.ClientID %>',data.periodo,'periodicidad']].forEach(function(item){if(setValue(item[0],item[1]))pasted++;else skipped.push(item[2]);});if(pasteStatus)pasteStatus.textContent=skipped.length?'Se pegaron '+pasted+' campos. Revisa: '+skipped.join(', ')+'. Selecciona el cliente.':'Datos pegados correctamente. Selecciona el cliente para completar la solicitud.';removeStored(transferKey);removeStored(pendingKey);}
    function pasteTransfer(){try{var raw=readStored(pendingKey)||readStored(transferKey);if(!raw){if(pasteStatus)pasteStatus.textContent='No hay datos copiados. Ve al simulador, completa los campos y pulsa “Copiar a solicitud”.';return;}var data=JSON.parse(raw);var productField=document.getElementById('<%= producto.ClientID %>');if(!productField||!data.producto){if(pasteStatus)pasteStatus.textContent='La simulación copiada no contiene un producto.';return;}var hasProduct=Array.prototype.some.call(productField.options,function(option){return option.value===String(data.producto);});if(!hasProduct){if(pasteStatus)pasteStatus.textContent='El producto copiado ya no está disponible. Selecciónalo manualmente.';return;}if(productField.value!==String(data.producto)){writeStored(pendingKey,JSON.stringify(data));productField.value=String(data.producto);if(state)state.value='1';var postback=window.__doPostBack;if(typeof postback==='function'){postback(productField.name,'');return;}productField.dispatchEvent(new Event('change',{bubbles:true}));return;}applyTransfer(data);}catch(error){if(pasteStatus)pasteStatus.textContent='No se pudieron pegar los datos de la simulación.';}}
    if(opener)opener.addEventListener('click',openModal);
    if(pasteButton)pasteButton.addEventListener('click',pasteTransfer);
    closers.forEach(function(closer){closer.addEventListener('click',closeModal);});
    if(modal){if(!modal.hidden)document.body.classList.add('request-modal-open');modal.addEventListener('click',function(event){if(event.target===modal)closeModal();});document.addEventListener('keydown',function(event){if(event.key==='Escape'&&!modal.hidden)closeModal();});var pending=readStored(pendingKey);if(pending){try{var transfer=JSON.parse(pending);var selectedProduct=document.getElementById('<%= producto.ClientID %>');if(selectedProduct&&selectedProduct.value===String(transfer.producto))applyTransfer(transfer);}catch(error){removeStored(pendingKey);}}}
})();
</script>
</asp:Content>
