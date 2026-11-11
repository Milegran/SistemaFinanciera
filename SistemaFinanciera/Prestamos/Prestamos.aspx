<%@ Page Title="Préstamos" Language="VB" AutoEventWireup="false" MasterPageFile="~/Site.Master" CodeBehind="Prestamos.aspx.vb" Inherits="SistemaFinanciera.Prestamos" %>
<asp:Content ID="Body" ContentPlaceHolderID="MainContent" runat="server">
<div class="finance-page loan-page">
    <header class="page-header page-header--split">
        <div><h1>Préstamos</h1><p>CapitalWest · Gestión financiera</p></div>
        <a href="<%= ResolveUrl("~/Acceso.aspx") %>" class="btn btn-light">Acceso</a>
    </header>
    <div id="pnlMensaje" runat="server" class="alert" role="status" aria-live="polite"><asp:Literal ID="mensaje" runat="server" Mode="Encode" /></div>

    <asp:Panel ID="formalizacion" runat="server" data-finanzas-form="prestamo" CssClass="cw-card loan-formalization">
        <div class="loan-formalization-heading">
            <span class="loan-formalization-icon" aria-hidden="true"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round"><path d="M7 3.5h7l4 4V20a1.5 1.5 0 0 1-1.5 1.5h-9A1.5 1.5 0 0 1 6 20V5a1.5 1.5 0 0 1 1-1.5Z"/><path d="M14 3.5V8h4M9 12h6m-6 4h6"/></svg></span>
            <div><span class="loan-formalization-eyebrow">CRÉDITO APROBADO</span><h2>Formalizar y desembolsar</h2><p>Selecciona una solicitud aprobada. El contrato, calendario y desembolso se guardarán en una sola transacción. El primer vencimiento debe estar a un período completo desde hoy.</p></div>
        </div>
        <div class="loan-formalization-fields">
            <div class="finance-field"><label for="<%= solicitud.ClientID %>">Solicitud aprobada sin préstamo <span class="required-mark">*</span></label><asp:DropDownList ID="solicitud" runat="server" required="required" CssClass="form-select" AutoPostBack="true" /></div>
            <div class="finance-field"><label for="<%= primerPago.ClientID %>">Primer pago contractual <span class="required-mark">*</span></label><asp:TextBox ID="primerPago" runat="server" CssClass="form-control" TextMode="Date" required="required" /></div>
        </div>
        <div class="loan-conditions" aria-label="Condiciones aprobadas"><asp:Literal ID="condiciones" runat="server" Mode="PassThrough" /></div>
        <div class="finance-actions"><asp:Button ID="desembolsar" runat="server" formnovalidate="formnovalidate" data-validar-finanzas="prestamo" Text="Formalizar y desembolsar" CssClass="btn btn-primary" OnClientClick="return window.solicitarConfirmacionDesembolso ? window.solicitarConfirmacionDesembolso(this) : confirm('¿Confirma el desembolso y las condiciones mostradas?');" /></div>
    </asp:Panel>

    <div class="client-confirm-overlay loan-disbursement-overlay" id="confirmacionDesembolso" hidden>
        <section class="client-confirm-dialog loan-disbursement-dialog" role="alertdialog" aria-modal="true" aria-labelledby="tituloConfirmacionDesembolso" aria-describedby="textoConfirmacionDesembolso" tabindex="-1">
            <span class="client-confirm-icon loan-disbursement-icon" aria-hidden="true"><svg viewBox="0 0 24 24"><path d="M12 3.5 22 20.5H2L12 3.5Z" /><path d="M12 9v5m0 3h.01" /></svg></span>
            <span class="loan-disbursement-eyebrow">CONFIRMACIÓN DE OPERACIÓN</span>
            <h2 id="tituloConfirmacionDesembolso">¿Formalizar y desembolsar este préstamo?</h2>
            <p id="textoConfirmacionDesembolso">Revisa los datos aprobados antes de continuar.</p>
            <div class="loan-confirm-summary">
                <div><small>Solicitud y cliente</small><strong id="confirmacionSolicitud">—</strong></div>
                <div><small>Primer pago contractual</small><strong id="confirmacionPrimerPago">—</strong></div>
                <div class="loan-confirm-terms"><small>Condiciones financieras</small><div id="confirmacionCondiciones">Selecciona una solicitud aprobada para consultar sus condiciones.</div></div>
            </div>
            <div class="loan-confirm-warning" role="note"><strong>Al confirmar:</strong> se creará el préstamo, se generará su calendario de cuotas y el desembolso quedará registrado en la cartera. El monto neto corresponde al aprobado menos la comisión y los gastos retenidos.</div>
            <div class="client-confirm-actions loan-confirm-actions"><button type="button" class="client-confirm-cancel" data-cancelar-confirmacion>Cancelar</button><button type="button" class="client-confirm-accept" data-aceptar-confirmacion>Confirmar desembolso</button></div>
        </section>
    </div>

    <script>
        (function () {
            var overlay = document.getElementById('confirmacionDesembolso');
            var dialog = overlay && overlay.querySelector('.loan-disbursement-dialog');
            var cancel = overlay && overlay.querySelector('[data-cancelar-confirmacion]');
            var accept = overlay && overlay.querySelector('[data-aceptar-confirmacion]');
            var submit = document.getElementById('<%= desembolsar.ClientID %>');
            var application = document.getElementById('<%= solicitud.ClientID %>');
            var firstPayment = document.getElementById('<%= primerPago.ClientID %>');
            var conditions = document.querySelector('.loan-conditions');
            var returnFocus = null;
            var previousOverflow = '';
            function closeDialog(restoreFocus) {
                overlay.hidden = true;
                document.body.style.overflow = previousOverflow;
                if (restoreFocus && returnFocus && returnFocus.isConnected) returnFocus.focus();
            }
            window.solicitarConfirmacionDesembolso = function (button) {
                if (button.dataset.confirmacionDesembolso === 'aceptada') {
                    delete button.dataset.confirmacionDesembolso;
                    return true;
                }
                returnFocus = button;
                document.getElementById('confirmacionSolicitud').textContent = application && application.selectedIndex > 0 ? application.options[application.selectedIndex].text : 'Solicitud seleccionada';
                document.getElementById('confirmacionPrimerPago').textContent = firstPayment && firstPayment.value ? new Date(firstPayment.value + 'T12:00:00').toLocaleDateString('es-PA', { year: 'numeric', month: 'long', day: 'numeric' }) : 'No definido';
                var terms = document.getElementById('confirmacionCondiciones');
                if (conditions && conditions.querySelector('.loan-condition-grid')) terms.innerHTML = conditions.innerHTML;
                else terms.textContent = 'Selecciona una solicitud aprobada para consultar sus condiciones.';
                previousOverflow = document.body.style.overflow;
                document.body.style.overflow = 'hidden';
                overlay.hidden = false;
                cancel.focus();
                return false;
            };
            cancel.addEventListener('click', function () { closeDialog(true); });
            accept.addEventListener('click', function () {
                if (!submit) return;
                submit.dataset.confirmacionDesembolso = 'aceptada';
                closeDialog(false);
                submit.click();
            });
            overlay.addEventListener('click', function (event) { if (event.target === overlay) closeDialog(true); });
            dialog.addEventListener('keydown', function (event) {
                if (event.key === 'Escape') { event.preventDefault(); closeDialog(true); }
                if (event.key === 'Tab') {
                    var focusable = [cancel, accept];
                    if (event.shiftKey && document.activeElement === focusable[0]) { event.preventDefault(); focusable[1].focus(); }
                    else if (!event.shiftKey && document.activeElement === focusable[1]) { event.preventDefault(); focusable[0].focus(); }
                }
            });
        }());
    </script>

    <section class="loan-kpis" aria-label="Resumen de cartera de préstamos">
        <asp:LinkButton ID="filtroTodos" runat="server" Text="0" data-label="Todos los préstamos" data-note="Cartera registrada" aria-label="Mostrar todos los préstamos" CssClass="client-kpi customer-metric request-kpi loan-kpi" CommandArgument="TODOS" OnCommand="FiltrarEstado" CausesValidation="false" />
        <asp:LinkButton ID="filtroVigentes" runat="server" Text="0" data-label="Vigentes" data-note="Con saldo activo" aria-label="Filtrar préstamos vigentes" CssClass="client-kpi customer-metric request-kpi loan-kpi request-kpi--approved" CommandArgument="VIGENTE" OnCommand="FiltrarEstado" CausesValidation="false" />
        <asp:LinkButton ID="filtroMora" runat="server" Text="0" data-label="En mora" data-note="Con cuotas vencidas" aria-label="Filtrar préstamos en mora" CssClass="client-kpi customer-metric request-kpi loan-kpi request-kpi--rejected" CommandArgument="EN_MORA" OnCommand="FiltrarEstado" CausesValidation="false" />
        <asp:LinkButton ID="filtroCancelados" runat="server" Text="0" data-label="Cerrados" data-note="Cancelados o refinanciados" aria-label="Filtrar préstamos cerrados" CssClass="client-kpi customer-metric request-kpi loan-kpi loan-kpi--closed" CommandArgument="CERRADOS" OnCommand="FiltrarEstado" CausesValidation="false" />
    </section>

    <section class="client-filters request-filters loan-filters" aria-label="Buscar y filtrar préstamos">
        <div><label class="sr-only" for="<%= filtroBusqueda.ClientID %>">Buscar préstamo, cliente, producto o método</label><asp:TextBox ID="filtroBusqueda" runat="server" CssClass="form-control" MaxLength="100" placeholder="Buscar por préstamo, cliente, producto o método..." /></div>
        <div><label class="sr-only" for="<%= filtroEstado.ClientID %>">Estado del préstamo</label><asp:DropDownList ID="filtroEstado" runat="server" CssClass="form-select"><asp:ListItem Text="Estado: Todos" Value="" /><asp:ListItem Text="Vigentes" Value="VIGENTE" /><asp:ListItem Text="En mora" Value="EN_MORA" /><asp:ListItem Text="Cerrados" Value="CERRADOS" /></asp:DropDownList></div>
        <div class="filter-actions"><asp:Button ID="btnBuscar" runat="server" Text="Buscar" CssClass="btn btn-outline-primary" CausesValidation="false" formnovalidate="formnovalidate" /><asp:Button ID="btnLimpiar" runat="server" Text="↺" ToolTip="Limpiar filtros" CssClass="btn btn-light" CausesValidation="false" formnovalidate="formnovalidate" /></div>
    </section>

    <section class="cw-card client-list request-list loan-list">
        <div class="list-caption"><div><strong>Cartera de préstamos</strong><small><asp:Literal ID="litConteo" runat="server" Mode="Encode" /></small></div></div>
        <div class="table-responsive">
            <asp:GridView ID="listado" runat="server" AutoGenerateColumns="false" DataKeyNames="Id" AllowPaging="true" PageSize="12" CssClass="table table-hover align-middle" GridLines="None" EmptyDataText="No hay préstamos para los filtros seleccionados." OnRowCommand="Ver" OnPageIndexChanging="PaginarPrestamos">
                <Columns>
                    <asp:BoundField DataField="NumeroPrestamo" HeaderText="Número" />
                    <asp:BoundField DataField="NombreCompleto" HeaderText="Cliente" />
                    <asp:BoundField DataField="Producto" HeaderText="Producto" />
                    <asp:BoundField DataField="Metodo" HeaderText="Método" />
                    <asp:BoundField DataField="MontoAprobado" HeaderText="Monto aprobado" DataFormatString="B/. {0:N2}" />
                    <asp:BoundField DataField="SaldoCapital" HeaderText="Capital pendiente" DataFormatString="B/. {0:N2}" />
                    <asp:TemplateField HeaderText="Estado"><ItemTemplate><span class='<%# ClaseEstadoPrestamo(Eval("EstadoActual")) %>'><%#: TextoEstadoPrestamo(Eval("EstadoActual")) %></span></ItemTemplate></asp:TemplateField>
                    <asp:BoundField DataField="ProximoVencimiento" HeaderText="Próximo pago" DataFormatString="{0:dd/MM/yyyy}" NullDisplayText="—" />
                    <asp:TemplateField HeaderText="Acciones"><ItemTemplate><asp:LinkButton ID="btnVerPrestamo" runat="server" CommandName="Ver" CommandArgument='<%# CType(Container, GridViewRow).RowIndex %>' CssClass="request-row-action" CausesValidation="false" Text="Ver detalle" /></ItemTemplate></asp:TemplateField>
                </Columns>
            </asp:GridView>
        </div>
    </section>
</div>
</asp:Content>
