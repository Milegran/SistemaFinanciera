<%@ Page Title="Productos financieros" Language="VB" AutoEventWireup="false" MasterPageFile="~/Site.Master" CodeBehind="Productos.aspx.vb" Inherits="SistemaFinanciera.Productos" %>
<asp:Content ID="Body" ContentPlaceHolderID="MainContent" runat="server"><div class="finance-page"><asp:Panel ID="pnlHeaderProductos" runat="server"><div class="page-header page-header--split"><div><h1>Productos financieros</h1><p id="pageSubtitle">CapitalWest · Gestión financiera</p><asp:HiddenField ID="hfTabActiva" runat="server" Value="productos" /><div class="page-tab-switch" role="tablist" aria-label="Secciones de productos"><button type="button" class="page-tab" id="tabProductos" role="tab" aria-selected="true" aria-controls="seccionProductos" data-tab="productos">Productos</button><button type="button" class="page-tab" id="tabTasas" role="tab" aria-selected="false" aria-controls="seccionTasas" data-tab="tasas">Tasas</button></div></div><div class="page-header-actions"><asp:Panel ID="pnlAccionCrear" runat="server"><button type="button" class="btn btn-primary" id="btnNuevoAccion">+ Crear producto</button></asp:Panel></div></div></asp:Panel><div id="pnlMensaje" runat="server" class="alert" role="status" aria-live="polite"><asp:Literal ID="mensaje" runat="server" Mode="Encode" /></div><asp:Panel ID="pnlCatalogosProductos" runat="server"><section id="seccionProductos" role="tabpanel" aria-labelledby="tabProductos"><div class="client-filters client-filters--producto"><div><label class="sr-only" for="<%= txtBuscarProducto.ClientID %>">Buscar productos</label><asp:TextBox ID="txtBuscarProducto" runat="server" CssClass="form-control" MaxLength="100" placeholder="Buscar por nombre o código..." /></div><div><label class="sr-only" for="<%= ddlEstadoProducto.ClientID %>">Estado</label><asp:DropDownList ID="ddlEstadoProducto" runat="server" CssClass="form-select" AutoPostBack="true"><asp:ListItem Text="Estado: Todos" Value="" /><asp:ListItem Text="Activos" Value="1" /><asp:ListItem Text="Inactivos" Value="0" /></asp:DropDownList></div><div><label class="sr-only" for="<%= ddlMetodoFiltro.ClientID %>">Método</label><asp:DropDownList ID="ddlMetodoFiltro" runat="server" CssClass="form-select" AutoPostBack="true" /></div><div class="filter-actions"><asp:Button ID="btnBuscarProducto" runat="server" Text="Buscar" CssClass="btn btn-outline-primary" CausesValidation="false" /><asp:Button ID="btnLimpiarProducto" runat="server" Text="↺" ToolTip="Limpiar filtros" CssClass="btn btn-light" CausesValidation="false" /></div></div><section class="cw-card client-list"><div class="list-caption"><div><strong>Listado de productos</strong><small>Productos financieros disponibles</small></div><div><button type="button" class="btn btn-sm btn-outline-secondary" data-print-products>Imprimir / PDF</button></div></div><div class="table-responsive"><asp:GridView ID="listado" runat="server" AutoGenerateColumns="false" DataKeyNames="Id,Activo" CssClass="table table-hover align-middle" GridLines="None" EmptyDataText="No hay productos para los filtros seleccionados." AllowPaging="true" PageSize="10" OnRowDataBound="ProductoRowDataBound"><Columns><asp:TemplateField HeaderText="Nombre"><ItemTemplate><div class="product-name"><span class="product-icon"><svg viewBox="0 0 24 24" aria-hidden="true" focusable="false"><path d="M3 21h18M5 21V7l7-4 7 4v14M9 21v-6h6v6" /></svg></span><span><strong><%#: Eval("Nombre") %></strong><small><%#: Eval("Descripcion") %></small></span></div></ItemTemplate></asp:TemplateField><asp:TemplateField HeaderText="Rango monto (B/.)"><ItemTemplate>B/. <%#: Convert.ToDecimal(Eval("MontoMinimo")).ToString("N0") %> – B/. <%#: Convert.ToDecimal(Eval("MontoMaximo")).ToString("N0") %></ItemTemplate></asp:TemplateField><asp:TemplateField HeaderText="Plazo"><ItemTemplate><%#: Convert.ToDecimal(Eval("PlazoMinimoMeses")).ToString("0.##") %> – <%#: Convert.ToDecimal(Eval("PlazoMaximoMeses")).ToString("0.##") %> meses</ItemTemplate></asp:TemplateField><asp:TemplateField HeaderText="Tasa interés"><ItemTemplate><div class="tasa-stack"><strong><%#: Convert.ToDecimal(Eval("TasaAnual")).ToString("0.00") %> %</strong><small><%#: If(CStr(Eval("TipoTasa")) = "NOMINAL_ANUAL", "Nominal", "Efectiva") %></small></div></ItemTemplate></asp:TemplateField><asp:TemplateField HeaderText="Métodos permitidos"><ItemTemplate><%# MetodosPills(CInt(Eval("Id"))) %></ItemTemplate></asp:TemplateField><asp:TemplateField HeaderText="Comisión"><ItemTemplate>Comisión B/. <%#: Convert.ToDecimal(Eval("ValorComision")).ToString("N2") %></ItemTemplate></asp:TemplateField><asp:TemplateField HeaderText="Estado"><ItemTemplate><span class='<%# If(CBool(Eval("Activo")), "status-pill", "status-pill inactive") %>'><%#: If(CBool(Eval("Activo")), "Activo", "Inactivo") %></span></ItemTemplate></asp:TemplateField><asp:TemplateField HeaderText="Acciones"><ItemTemplate><asp:LinkButton ID="btnVerProducto" runat="server" ToolTip="Consultar producto" aria-label="Consultar producto" CommandName="VerProducto" CommandArgument='<%# CType(Container, GridViewRow).RowIndex %>' CssClass="action-icon action-view" CausesValidation="false"><svg class="action-svg" viewBox="0 0 24 24" aria-hidden="true" focusable="false"><path d="M2.5 12s3.4-6 9.5-6 9.5 6 9.5 6-3.4 6-9.5 6-9.5-6-9.5-6Z" /><circle cx="12" cy="12" r="2.8" /></svg></asp:LinkButton><asp:LinkButton ID="btnEditarProducto" runat="server" ToolTip="Editar producto" aria-label="Editar producto" CommandName="Elegir" CommandArgument='<%# CType(Container, GridViewRow).RowIndex %>' CssClass="action-icon action-edit" CausesValidation="false"><svg class="action-svg" viewBox="0 0 24 24" aria-hidden="true" focusable="false"><path d="m14.5 6.5 3 3M4 20l4.4-.9L19 8.5a2.1 2.1 0 0 0-3-3L5.4 16.1 4 20Z" /></svg></asp:LinkButton><asp:LinkButton ID="btnEstadoProducto" runat="server" ToolTip='<%# If(CBool(Eval("Activo")), "Desactivar producto", "Reactivar producto") %>' aria-label='<%# If(CBool(Eval("Activo")), "Desactivar producto", "Reactivar producto") %>' data-confirm-action='<%# If(CBool(Eval("Activo")), "desactivar", "reactivar") %>' CommandName="CambiarEstado" CommandArgument='<%# CType(Container, GridViewRow).RowIndex %>' CssClass="action-icon action-toggle" CausesValidation="false" OnClientClick="return window.solicitarConfirmacionProducto ? window.solicitarConfirmacionProducto(this) : confirm('¿Desea cambiar el estado de este producto?');"><svg class="action-svg" viewBox="0 0 24 24" aria-hidden="true" focusable="false"><circle cx="12" cy="12" r="9" /><path d="m5.6 5.6 12.8 12.8" /></svg></asp:LinkButton></ItemTemplate></asp:TemplateField></Columns></asp:GridView></div><div class="grid-count"><asp:Literal ID="litConteo" runat="server" Mode="Encode" /></div></section></section><section id="seccionTasas" role="tabpanel" aria-labelledby="tabTasas" hidden>
<div class="client-filters client-filters--tasas">
<div>
<label class="sr-only" for="<%= ddlProductoTasa.ClientID %>">Producto</label>
<asp:DropDownList ID="ddlProductoTasa" runat="server" CssClass="form-select" AutoPostBack="true" />
</div>
<div>
<label class="sr-only" for="<%= ddlEstadoTasa.ClientID %>">Estado de la tasa</label>
<asp:DropDownList ID="ddlEstadoTasa" runat="server" CssClass="form-select" AutoPostBack="true">
<asp:ListItem Text="Estado: Todas" Value="" />
<asp:ListItem Text="Vigentes" Value="vigente" />
<asp:ListItem Text="Cerradas" Value="cerrada" />
</asp:DropDownList>
</div>
<div class="filter-actions">
<asp:Button ID="btnLimpiarTasas" runat="server" Text="↺" ToolTip="Limpiar filtros de tasas" CssClass="btn btn-light" CausesValidation="false" />
</div>
</div>
<section class="cw-card client-list">
<div class="list-caption">
<div>
<strong>Parámetros de interés</strong>
<small>Catálogo de tasas activas e históricas. No se sobrescribe nada: cada cambio cierra la tasa anterior y abre una nueva.</small>
</div>
<div>
<button type="button" class="btn btn-sm btn-outline-secondary" data-print-rates>Imprimir / PDF</button>
</div>
</div>
<div class="table-responsive">
<asp:GridView ID="listadoTasas" runat="server" AutoGenerateColumns="false" DataKeyNames="Id,ProductoId" CssClass="table table-hover align-middle" GridLines="None" EmptyDataText="No hay tasas para los filtros seleccionados." AllowPaging="true" PageSize="10">
<Columns>
<asp:TemplateField HeaderText="Producto">
<ItemTemplate>
<strong class="rate-product"><%#: Eval("Producto") %></strong>
</ItemTemplate>
</asp:TemplateField>
<asp:TemplateField HeaderText="Tipo de tasa">
<ItemTemplate><%#: TipoTasaTexto(Eval("TipoTasa")) %></ItemTemplate>
</asp:TemplateField>
<asp:TemplateField HeaderText="Valor (%)">
<ItemTemplate>
<div class="tasa-stack">
<strong class="rate-valor"><%#: Convert.ToDecimal(Eval("Porcentaje")).ToString("0.00") %> %</strong>
<small><%#: TipoTasaTexto(Eval("TipoTasa")) %></small>
</div>
</ItemTemplate>
</asp:TemplateField>
<asp:TemplateField HeaderText="Vigente desde">
<ItemTemplate><%#: FechaCorta(Eval("VigenteDesde")) %></ItemTemplate>
</asp:TemplateField>
<asp:TemplateField HeaderText="Vigente hasta">
<ItemTemplate><%#: FechaCorta(Eval("VigenteHasta")) %></ItemTemplate>
</asp:TemplateField>
<asp:TemplateField HeaderText="Estado">
<ItemTemplate>
<span class='<%# If(CBool(Eval("Activo")) AndAlso CBool(Eval("EsPredeterminada")), "status-pill", "status-pill inactive") %>'><%#: If(CBool(Eval("Activo")) AndAlso CBool(Eval("EsPredeterminada")), "Vigente", "Cerrada") %></span>
</ItemTemplate>
</asp:TemplateField>
</Columns>
</asp:GridView>
</div>
<div class="grid-count">
<asp:Literal ID="litConteoTasas" runat="server" Mode="Encode" />
</div>
</section>
</section>
</asp:Panel>
<asp:Panel ID="pnlDetalleProducto" runat="server" Visible="false" CssClass="product-detail-view">
<div class="customer-detail-header product-detail-header">
    <asp:Button ID="btnVolverDetalle" runat="server" Text="←" ToolTip="Volver al listado" CssClass="customer-back" CausesValidation="false" formnovalidate="formnovalidate" />
    <asp:Literal ID="litProductoDetalleCabecera" runat="server" Mode="PassThrough" />
    <div class="customer-detail-actions">
        <asp:Button ID="btnEditarDetalle" runat="server" Text="Editar producto" CssClass="btn btn-light customer-action" CausesValidation="false" formnovalidate="formnovalidate" />
        <asp:Button ID="btnNuevaTasaDetalle" runat="server" Text="+ Nueva tasa" CssClass="btn btn-light customer-action" CausesValidation="false" formnovalidate="formnovalidate" />
        <asp:Button ID="btnEstadoDetalle" runat="server" Text="Desactivar producto" CssClass="btn btn-light customer-action customer-danger-action" CausesValidation="false" formnovalidate="formnovalidate" OnClientClick="return window.solicitarConfirmacionProducto ? window.solicitarConfirmacionProducto(this) : confirm('¿Desea cambiar el estado de este producto?');" />
    </div>
</div>
<section class="customer-profile-kpis product-detail-kpis">
    <article class="customer-metric"><span>RANGO DE MONTO</span><strong><asp:Literal ID="litProductoMontoKpi" runat="server" Mode="Encode" /></strong><small>Monto permitido</small></article>
    <article class="customer-metric"><span>PLAZO</span><strong><asp:Literal ID="litProductoPlazoKpi" runat="server" Mode="Encode" /></strong><small>Rango de cuotas</small></article>
    <article class="customer-metric"><span>TASA ANUAL</span><strong class="is-green"><asp:Literal ID="litProductoTasaKpi" runat="server" Mode="Encode" /></strong><small><asp:Literal ID="litProductoTipoTasaKpi" runat="server" Mode="Encode" /></small></article>
    <article class="customer-metric"><span>COSTOS FIJOS</span><strong><asp:Literal ID="litProductoCostosKpi" runat="server" Mode="Encode" /></strong><small>Retenidos del desembolso</small></article>
</section>
<div class="product-detail-content">
    <section class="customer-profile-panel product-detail-card">
        <div class="product-detail-section-heading"><div><span class="product-detail-overline">CONDICIONES</span><h2>Información del producto</h2></div></div>
        <dl class="customer-detail-grid product-detail-grid">
            <div class="customer-detail-item"><dt>Código</dt><dd><asp:Literal ID="litProductoCodigo" runat="server" Mode="Encode" /></dd></div>
            <div class="customer-detail-item"><dt>Descripción</dt><dd><asp:Literal ID="litProductoDescripcion" runat="server" Mode="Encode" /></dd></div>
            <div class="customer-detail-item"><dt>Capitalización</dt><dd><asp:Literal ID="litProductoCapitalizacion" runat="server" Mode="Encode" /></dd></div>
            <div class="customer-detail-item"><dt>Monto mínimo</dt><dd><asp:Literal ID="litProductoMontoMinimo" runat="server" Mode="Encode" /></dd></div>
            <div class="customer-detail-item"><dt>Monto máximo</dt><dd><asp:Literal ID="litProductoMontoMaximo" runat="server" Mode="Encode" /></dd></div>
            <div class="customer-detail-item"><dt>Plazo en cuotas</dt><dd><asp:Literal ID="litProductoCuotas" runat="server" Mode="Encode" /></dd></div>
            <div class="customer-detail-item"><dt>Plazo en meses</dt><dd><asp:Literal ID="litProductoMeses" runat="server" Mode="Encode" /></dd></div>
            <div class="customer-detail-item"><dt>Comisión fija</dt><dd><asp:Literal ID="litProductoComision" runat="server" Mode="Encode" /></dd></div>
            <div class="customer-detail-item"><dt>Gastos fijos</dt><dd><asp:Literal ID="litProductoGastos" runat="server" Mode="Encode" /></dd></div>
            <div class="customer-detail-item"><dt>Métodos permitidos</dt><dd><asp:Literal ID="litProductoMetodos" runat="server" Mode="Encode" /></dd></div>
            <div class="customer-detail-item"><dt>Periodicidades de pago</dt><dd><asp:Literal ID="litProductoPeriodicidades" runat="server" Mode="Encode" /></dd></div>
        </dl>
    </section>
    <section class="customer-profile-panel product-detail-card product-detail-history">
        <div class="product-detail-section-heading"><div><span class="product-detail-overline">TRAZABILIDAD</span><h2>Historial de tasas</h2><p>Versiones asociadas a este producto.</p></div></div>
        <div class="table-responsive"><asp:GridView ID="historialTasasDetalle" runat="server" AutoGenerateColumns="false" CssClass="table customer-detail-table" GridLines="None" EmptyDataText="No hay historial de tasas para este producto."><Columns><asp:TemplateField HeaderText="Tipo"><ItemTemplate><%#: TipoTasaTexto(Eval("TipoTasa")) %></ItemTemplate></asp:TemplateField><asp:TemplateField HeaderText="Tasa anual"><ItemTemplate><%#: Convert.ToDecimal(Eval("Porcentaje")).ToString("0.######") %> %</ItemTemplate></asp:TemplateField><asp:TemplateField HeaderText="Vigente desde"><ItemTemplate><%#: FechaCorta(Eval("VigenteDesde")) %></ItemTemplate></asp:TemplateField><asp:TemplateField HeaderText="Vigente hasta"><ItemTemplate><%#: FechaCorta(Eval("VigenteHasta")) %></ItemTemplate></asp:TemplateField><asp:TemplateField HeaderText="Estado"><ItemTemplate><span class='<%# If(CBool(Eval("Activo")) AndAlso CBool(Eval("EsPredeterminada")), "status-pill", "status-pill inactive") %>'><%#: If(CBool(Eval("Activo")) AndAlso CBool(Eval("EsPredeterminada")), "Vigente", "Cerrada") %></span></ItemTemplate></asp:TemplateField></Columns></asp:GridView></div>
    </section>
</div>
</asp:Panel><div class="form-overlay form-overlay--producto" id="modalProducto" hidden role="dialog" aria-modal="true" aria-labelledby="tituloProductoModal"><asp:Panel ID="edicion" runat="server" data-finanzas-form="producto" CssClass="form-dialog form-dialog--producto"><div class="form-dialog-head"><h5 id="tituloProductoModal">Crear o editar producto</h5><button type="button" class="form-dialog-close" aria-label="Cerrar" data-cerrar-modal-producto>&times;</button></div><p class="form-dialog-lead">Las condiciones nuevas no modifican los contratos ya pactados. Los costos se retienen del desembolso. Guarde al menos un método y una periodicidad.</p><div class="finance-fields"><div class="finance-field"><label for="<%= codigo.ClientID %>">Código</label><asp:TextBox ID="codigo" runat="server" data-regla="codigo" data-ayuda="Use 2–20 caracteres, empiece con una letra. Ej.: PRE-01." CssClass="form-control"  MaxLength="20" minlength="2" required="required" /></div><div class="finance-field"><label for="<%= nombre.ClientID %>">Nombre</label><asp:TextBox ID="nombre" runat="server" data-regla="nombre-producto" data-ayuda="Use 3–80 caracteres y empiece con una letra. Ej.: Crédito personal 2026." CssClass="form-control"  MaxLength="80" minlength="3" required="required" /></div><div class="finance-field"><label for="<%= descripcion.ClientID %>">Descripción</label><asp:TextBox ID="descripcion" runat="server" data-regla="texto-descriptivo" CssClass="form-control" TextMode="MultiLine" MaxLength="500" data-maxlength="500" Rows="2" required="required" /></div><div class="finance-field"><label for="<%= minimo.ClientID %>">Monto mínimo (B/.)</label><asp:TextBox ID="minimo" runat="server" CssClass="form-control" TextMode="Number" min="0.01" max="999999999999.99" step="0.01" required="required" /></div><div class="finance-field"><label for="<%= maximo.ClientID %>">Monto máximo (B/.)</label><asp:TextBox ID="maximo" runat="server" CssClass="form-control" TextMode="Number" min="0.01" max="999999999999.99" step="0.01" required="required" /></div><div class="finance-field"><label for="<%= cuotasMin.ClientID %>">Cuotas mínimas</label><asp:TextBox ID="cuotasMin" runat="server" CssClass="form-control" TextMode="Number" min="1" max="1200" step="1" required="required" /></div><div class="finance-field"><label for="<%= cuotasMax.ClientID %>">Cuotas máximas</label><asp:TextBox ID="cuotasMax" runat="server" CssClass="form-control" TextMode="Number" min="1" max="1200" step="1" required="required" /></div><div class="finance-field"><label for="<%= mesesMin.ClientID %>">Plazo mínimo en meses</label><asp:TextBox ID="mesesMin" runat="server" CssClass="form-control" TextMode="Number" min="0.01" max="1200" step="0.01" required="required" /></div><div class="finance-field"><label for="<%= mesesMax.ClientID %>">Plazo máximo en meses</label><asp:TextBox ID="mesesMax" runat="server" CssClass="form-control" TextMode="Number" min="0.01" max="1200" step="0.01" required="required" /></div><div class="finance-field"><label for="<%= tasa.ClientID %>">Tasa anual (%)</label><asp:TextBox ID="tasa" runat="server" CssClass="form-control" TextMode="Number" min="0" max="100" step="0.000001" required="required" /></div><div class="finance-field"><label for="<%= tipoTasa.ClientID %>">Tipo de tasa</label><asp:DropDownList ID="tipoTasa" runat="server" required="required" CssClass="form-select"  /></div><div class="finance-field"><label for="<%= capitalizacion.ClientID %>">Capitalización</label><asp:DropDownList ID="capitalizacion" runat="server" required="required" CssClass="form-select"  /></div><div class="finance-field"><label for="<%= comision.ClientID %>">Comisión fija retenida (B/.)</label><asp:TextBox ID="comision" runat="server" CssClass="form-control" TextMode="Number" min="0" max="99999999.99" step="0.01" required="required" /></div><div class="finance-field"><label for="<%= gastos.ClientID %>">Gastos fijos retenidos (B/.)</label><asp:TextBox ID="gastos" runat="server" CssClass="form-control" TextMode="Number" min="0" max="99999999.99" step="0.01" required="required" /></div><div class="finance-field"><label for="<%= activo.ClientID %>">Estado</label><asp:CheckBox ID="activo" runat="server" Text="Activo" /></div><div class="finance-field"><label for="<%= metodos.ClientID %>">Métodos permitidos</label><asp:CheckBoxList ID="metodos" runat="server" data-al-menos-uno="true" RepeatDirection="Vertical" CssClass="finance-checks" /></div><div class="finance-field"><label for="<%= periodos.ClientID %>">Periodicidades de pago</label><asp:CheckBoxList ID="periodos" runat="server" data-al-menos-uno="true" RepeatDirection="Vertical" CssClass="finance-checks" /></div></div><div class="modal-footer-cw"><asp:Button ID="nuevo" runat="server" Text="Limpiar / nuevo" CssClass="btn btn-light border" CausesValidation="false" formnovalidate="formnovalidate" /><asp:Button ID="guardar" runat="server" formnovalidate="formnovalidate" data-validar-finanzas="producto" Text="Guardar producto" CssClass="btn btn-primary" /></div></asp:Panel></div></div>
<div class="form-overlay form-overlay--producto" id="modalTasa" hidden role="dialog" aria-modal="true" aria-labelledby="tituloTasaModal">
<asp:Panel ID="edicionTasa" runat="server" CssClass="form-dialog form-dialog--tasa">
<div class="form-dialog-head">
<h5 id="tituloTasaModal">Nueva tasa de interés</h5>
<button type="button" class="form-dialog-close" aria-label="Cerrar" data-cerrar-modal-tasa>&times;</button>
</div>
<p class="form-dialog-lead form-dialog-lead--warn">Al registrar una nueva tasa vigente, la tasa activa anterior pasará automáticamente a estado «Cerrada». Los préstamos ya formalizados conservan su tasa pactada.</p>
<div class="finance-fields finance-fields--tasa">
<div class="finance-field">
<label for="<%= ddlProductoTasaForm.ClientID %>">Producto financiero</label>
<asp:DropDownList ID="ddlProductoTasaForm" runat="server" CssClass="form-select" required="required" />
</div>
<div class="finance-field">
<label for="<%= tasaValor.ClientID %>">Tasa (% anual)</label>
<asp:TextBox ID="tasaValor" runat="server" CssClass="form-control" TextMode="Number" min="0" max="100" step="0.01" placeholder="11.50" required="required" />
</div>
<div class="finance-field">
<label for="<%= tipoTasaForm.ClientID %>">Tipo de tasa</label>
<asp:DropDownList ID="tipoTasaForm" runat="server" CssClass="form-select" required="required">
<asp:ListItem Text="Nominal anual" Value="NOMINAL_ANUAL" />
<asp:ListItem Text="Efectiva anual" Value="EFECTIVA_ANUAL" />
</asp:DropDownList>
</div>
<div class="finance-field">
<label for="<%= fechaDesdeTasa.ClientID %>">Fecha de inicio de vigencia</label>
<asp:TextBox ID="fechaDesdeTasa" runat="server" CssClass="form-control" TextMode="Date" required="required" />
</div>
</div>
<div class="modal-footer-cw">
<button type="button" class="btn btn-light border" data-cerrar-modal-tasa>Cancelar</button>
<asp:Button ID="btnGuardarTasa" runat="server" Text="Publicar tasa" CssClass="btn btn-primary" />
</div>
</asp:Panel>
</div>
<div class="client-confirm-overlay" data-product-confirm hidden>
    <section class="client-confirm-dialog" role="alertdialog" aria-modal="true" aria-labelledby="productConfirmTitle" aria-describedby="productConfirmMessage" tabindex="-1">
        <span class="client-confirm-icon" aria-hidden="true"><svg viewBox="0 0 24 24"><path d="M12 3.5 22 20.5H2L12 3.5Z" /><path d="M12 9v5m0 3h.01" /></svg></span>
        <h2 id="productConfirmTitle">¿Desactivar producto?</h2>
        <p id="productConfirmMessage">El producto dejará de estar disponible para nuevas solicitudes. Los préstamos e historiales existentes se conservarán.</p>
        <div class="client-confirm-actions"><button type="button" class="client-confirm-cancel" data-product-confirm-cancel>Cancelar</button><button type="button" class="client-confirm-accept" data-product-confirm-accept>Desactivar producto</button></div>
    </section>
</div>
<script src="<%= ResolveUrl("~/Scripts/StitchProductos.js?v=productos-tabs-1") %>"></script></asp:Content>
