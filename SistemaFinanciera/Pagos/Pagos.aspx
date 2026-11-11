<%@ Page Title="Pagos" Language="VB" AutoEventWireup="false" MasterPageFile="~/Site.Master" CodeBehind="Pagos.aspx.vb" Inherits="SistemaFinanciera.Pagos" %>
<asp:Content ID="Body" ContentPlaceHolderID="MainContent" runat="server">
    <div class="finance-page payment-page">
        <div class="page-header">
            <div><h1>Pagos</h1><p>CapitalWest · Gestión financiera</p></div>
        </div>

        <div id="pnlMensaje" runat="server" class="alert" role="status" aria-live="polite">
            <asp:Literal ID="mensaje" runat="server" Mode="Encode" />
        </div>

        <asp:Panel ID="registro" runat="server" data-finanzas-form="pago" CssClass="cw-card payment-entry">
            <div class="payment-entry-heading">
                <div><span class="eyebrow">CAJA · REGISTRO SEGURO</span><h2>Registrar pago</h2></div>
                <p>Elige el préstamo para calcular lo vencido y la cuota programada de este mes.</p>
            </div>

            <div class="finance-fields payment-fields">
                <div class="finance-field payment-loan-field">
                    <label for="<%= prestamo.ClientID %>">Préstamo</label>
                    <asp:DropDownList ID="prestamo" runat="server" required="required" CssClass="form-select" AutoPostBack="true" />
                </div>
                <div class="finance-field">
                    <label for="<%= fechaPago.ClientID %>">Fecha operativa de Panamá</label>
                    <asp:TextBox ID="fechaPago" runat="server" CssClass="form-control" TextMode="Date" ReadOnly="true" />
                    <small class="payment-field-hint">Se registra con la fecha de hoy; no se permiten fechas anteriores ni futuras.</small>
                </div>
                <div class="finance-field">
                    <label for="<%= forma.ClientID %>">Forma de pago</label>
                    <asp:DropDownList ID="forma" runat="server" required="required" CssClass="form-select" />
                </div>
                <div class="finance-field payment-amount-field">
                    <label for="<%= monto.ClientID %>">Monto recibido (B/.)</label>
                    <div class="payment-amount-control">
                        <asp:TextBox ID="monto" runat="server" CssClass="form-control" TextMode="Number" min="0.01" max="999999999999.99" step="0.01" required="required" inputmode="decimal" autocomplete="off" aria-describedby="montoAyuda montoErrorServidor" />
                        <div class="payment-suggestions" role="group" aria-label="Montos sugeridos para el pago">
                            <button id="pagarVencido" runat="server" type="button" class="payment-suggestion" data-pago-sugerencia="vencido" disabled="disabled" title="Autocompletar solo las cuotas vencidas">
                                <span>Vencido</span><strong>B/. <asp:Literal ID="textoMontoVencido" runat="server" Mode="Encode" Text="0.00" /></strong>
                            </button>
                            <button id="pagarTodo" runat="server" type="button" class="payment-suggestion payment-suggestion--primary" data-pago-sugerencia="total" disabled="disabled" title="Autocompletar cuotas vencidas y la cuota programada de este mes">
                                <span>Total del mes</span><strong>B/. <asp:Literal ID="textoMontoTotal" runat="server" Mode="Encode" Text="0.00" /></strong>
                            </button>
                        </div>
                    </div>
                    <small id="montoAyuda" class="payment-field-hint">Escribe el importe o pulsa una sugerencia para autocompletar.</small>
                    <asp:Label ID="montoErrorServidor" runat="server" CssClass="finance-field-error payment-server-error" Visible="false" />
                </div>
                <div class="finance-field">
                    <label for="<%= referencia.ClientID %>">Referencia <span class="payment-optional">(obligatoria salvo efectivo)</span></label>
                    <asp:TextBox ID="referencia" runat="server" CssClass="form-control" MaxLength="100" autocomplete="off" />
                </div>
            </div>

            <asp:HiddenField ID="montoVencidoSugerido" runat="server" Value="0.00" />
            <asp:HiddenField ID="montoTotalSugerido" runat="server" Value="0.00" />
            <asp:Literal ID="saldo" runat="server" Mode="Encode" />
            <asp:Panel ID="avisoPagoNoDisponible" runat="server" CssClass="payment-blocked-note" Visible="false" role="status" aria-live="polite">
                <span class="payment-blocked-icon" aria-hidden="true">!</span><asp:Literal ID="razonPagoNoDisponible" runat="server" Mode="Encode" />
            </asp:Panel>
            <div class="finance-actions payment-actions">
                <asp:Button ID="guardar" runat="server" formnovalidate="formnovalidate" data-validar-finanzas="pago" Text="Registrar pago" CssClass="btn btn-primary" OnClientClick="return confirmarRegistroPago(this);" />
            </div>
        </asp:Panel>

        <dialog id="dialogConfirmarPago" class="payment-confirm-dialog" aria-labelledby="tituloConfirmarPago" aria-describedby="textoConfirmarPago">
            <div class="payment-confirm-icon" aria-hidden="true">✓</div>
            <span class="eyebrow">CONFIRMACIÓN DE CAJA</span>
            <h2 id="tituloConfirmarPago">¿Registrar este pago?</h2>
            <p id="textoConfirmarPago">Revisa los datos antes de aplicar el pago al préstamo.</p>
            <dl class="payment-confirm-summary">
                <div><dt>Préstamo</dt><dd id="confirmarPagoPrestamo">—</dd></div>
                <div><dt>Monto recibido</dt><dd id="confirmarPagoMonto">B/. 0.00</dd></div>
            </dl>
            <div class="payment-confirm-actions">
                <button type="button" class="btn btn-light" data-cancelar-confirmacion>Cancelar</button>
                <button type="button" class="btn btn-primary" data-confirmar-pago>Confirmar pago</button>
            </div>
        </dialog>

        <div id="cargandoPago" class="payment-loading" role="status" aria-live="polite" aria-hidden="true" hidden>
            <div class="payment-loading-card">
                <span class="payment-loading-spinner" aria-hidden="true"></span>
                <h2>Registrando pago…</h2>
                <p>Aplicando el pago y preparando el comprobante. No cierres esta pantalla.</p>
            </div>
        </div>

        <dialog id="dialogComprobantePago" class="payment-invoice-dialog" aria-modal="true" aria-labelledby="tituloComprobantePago">
            <div class="payment-invoice-shell">
                <div class="receipt-printer-slot" aria-hidden="true"></div>
                <div class="receipt-paper-window">
                    <asp:Panel ID="comprobante" runat="server" CssClass="cw-card payment-receipt" Visible="false" data-comprobante-pago="true">
                        <header class="receipt-topline">
                            <div class="receipt-brand"><strong>CapitalWest</strong><small>Gestión financiera · Panamá</small></div>
                            <div class="receipt-number"><span id="tituloComprobantePago">COMPROBANTE DE PAGO</span><strong><asp:Literal ID="reciboNumero" runat="server" Mode="Encode" /></strong><small><asp:Literal ID="reciboFecha" runat="server" Mode="Encode" /></small></div>
                        </header>
                        <section class="receipt-section">
                            <h2>Datos del cliente</h2>
                            <div class="receipt-details">
                                <div><span>Cliente</span><strong><asp:Literal ID="reciboCliente" runat="server" Mode="Encode" /></strong></div>
                                <div><span>Préstamo</span><strong><asp:Literal ID="reciboPrestamo" runat="server" Mode="Encode" /></strong></div>
                                <div><span>Forma de pago</span><strong><asp:Literal ID="reciboForma" runat="server" Mode="Encode" /></strong></div>
                            </div>
                        </section>
                        <section class="receipt-section">
                            <h2>Aplicación del pago</h2>
                            <div class="receipt-breakdown">
                                <div><span>Mora</span><strong>B/. <asp:Literal ID="reciboMora" runat="server" Mode="Encode" /></strong></div>
                                <div><span>Cargos</span><strong>B/. <asp:Literal ID="reciboCargos" runat="server" Mode="Encode" /></strong></div>
                                <div><span>Interés</span><strong>B/. <asp:Literal ID="reciboInteres" runat="server" Mode="Encode" /></strong></div>
                                <div><span>Capital</span><strong>B/. <asp:Literal ID="reciboCapital" runat="server" Mode="Encode" /></strong></div>
                                <div class="receipt-total"><span>Total recibido</span><strong>B/. <asp:Literal ID="reciboMonto" runat="server" Mode="Encode" /></strong></div>
                                <div class="receipt-balance"><span>Capital pendiente</span><strong>B/. <asp:Literal ID="reciboSaldoCapital" runat="server" Mode="Encode" /></strong></div>
                            </div>
                        </section>
                        <footer class="receipt-note">Comprobante generado electrónicamente por CapitalWest.<br />Gracias por su pago.</footer>
                    </asp:Panel>
                </div>
                <div class="payment-invoice-actions">
                    <button type="button" class="btn btn-primary" data-imprimir-comprobante>Imprimir / PDF</button>
                    <button type="button" class="btn btn-light" data-cerrar-comprobante>Registrar otro pago</button>
                </div>
            </div>
        </dialog>

        <asp:Panel ID="panelCuotasCliente" runat="server" CssClass="cw-card payment-client-dues" Visible="false">
            <div class="payment-invoices-heading"><div><h2>Cuotas pendientes del cliente</h2><p>Próximos pagos y saldos pendientes de sus préstamos.</p></div><span><asp:Literal ID="conteoCuotasCliente" runat="server" Mode="Encode" /></span></div>
            <div class="table-responsive"><asp:GridView ID="cuotasCliente" runat="server" AutoGenerateColumns="false" AllowPaging="true" PageSize="20" OnPageIndexChanging="PaginarCuotasCliente" CssClass="table" GridLines="None" EmptyDataText="Este cliente no tiene cuotas pendientes."><Columns>
                <asp:BoundField DataField="NumeroPrestamo" HeaderText="Préstamo" />
                <asp:BoundField DataField="NumeroCuota" HeaderText="Cuota" />
                <asp:BoundField DataField="FechaVencimiento" HeaderText="Vencimiento" DataFormatString="{0:dd/MM/yyyy}" />
                <asp:BoundField DataField="CapitalPendiente" HeaderText="Capital pendiente" DataFormatString="{0:N2}" />
                <asp:BoundField DataField="InteresPendiente" HeaderText="Interés pendiente" DataFormatString="{0:N2}" />
                <asp:BoundField DataField="MoraPendiente" HeaderText="Mora" DataFormatString="{0:N2}" />
                <asp:BoundField DataField="CargosPendientes" HeaderText="Cargos" DataFormatString="{0:N2}" />
                <asp:BoundField DataField="TotalPendiente" HeaderText="Total por pagar" DataFormatString="{0:N2}" />
                <asp:BoundField DataField="EstadoCuota" HeaderText="Estado" />
            </Columns></asp:GridView></div>
        </asp:Panel>

        <section class="cw-card">
            <div class="payment-invoices-heading"><div><h2>Facturas de pago</h2><p>Busca comprobantes emitidos y vuelve a consultarlos cuando los necesites.</p></div><span><asp:Literal ID="conteoFacturas" runat="server" Mode="Encode" /></span></div>
            <asp:Panel ID="panelFiltroFacturas" runat="server" CssClass="payment-invoice-filters" DefaultButton="filtrarFacturas" role="search" aria-label="Filtros de facturas">
                <div class="payment-invoice-search"><label for="<%= buscarFactura.ClientID %>">Buscar factura</label><asp:TextBox ID="buscarFactura" runat="server" CssClass="form-control" MaxLength="100" placeholder="N.º de factura, cliente o préstamo" /></div>
                <div><label for="<%= clienteFactura.ClientID %>">Cliente</label><asp:DropDownList ID="clienteFactura" runat="server" CssClass="form-select" AutoPostBack="true" OnSelectedIndexChanged="CambiarClienteFiltroClick" /></div>
                <div><label for="<%= formaFactura.ClientID %>">Forma de pago</label><asp:DropDownList ID="formaFactura" runat="server" CssClass="form-select"><asp:ListItem Text="Todas las formas" Value="" /></asp:DropDownList></div>
                <div><label for="<%= estadoFactura.ClientID %>">Estado</label><asp:DropDownList ID="estadoFactura" runat="server" CssClass="form-select"><asp:ListItem Text="Todos los estados" Value="" /><asp:ListItem Text="Aplicado" Value="APLICADO" /><asp:ListItem Text="Anulado" Value="ANULADO" /></asp:DropDownList></div>
                <div><label for="<%= fechaDesdeFactura.ClientID %>">Desde</label><asp:TextBox ID="fechaDesdeFactura" runat="server" CssClass="form-control" TextMode="Date" /></div>
                <div><label for="<%= fechaHastaFactura.ClientID %>">Hasta</label><asp:TextBox ID="fechaHastaFactura" runat="server" CssClass="form-control" TextMode="Date" /></div>
                <asp:Button ID="filtrarFacturas" runat="server" Text="Buscar" CssClass="btn btn-outline-primary" CausesValidation="false" OnClick="FiltrarFacturasClick" />
                <asp:Button ID="limpiarFacturas" runat="server" Text="Limpiar" CssClass="btn btn-light" CausesValidation="false" OnClick="LimpiarFacturasClick" />
            </asp:Panel>
            <div class="table-responsive"><asp:GridView ID="listado" runat="server" AutoGenerateColumns="false" DataKeyNames="Id,PrestamoId" AllowPaging="true" PageSize="20" OnPageIndexChanging="PaginarFacturas" CssClass="table" GridLines="None" EmptyDataText="No hay pagos registrados para los filtros seleccionados."><Columns>
                <asp:BoundField DataField="NumeroRecibo" HeaderText="Recibo" />
                <asp:BoundField DataField="NumeroPrestamo" HeaderText="Préstamo" />
                <asp:BoundField DataField="Cliente" HeaderText="Cliente" />
                <asp:BoundField DataField="FechaPago" HeaderText="Fecha" DataFormatString="{0:dd/MM/yyyy}" />
                <asp:BoundField DataField="FormaPagoCodigo" HeaderText="Forma de pago" />
                <asp:BoundField DataField="MontoPagado" HeaderText="Importe" DataFormatString="{0:N2}" />
                <asp:BoundField DataField="Capital" HeaderText="Capital" DataFormatString="{0:N2}" />
                <asp:BoundField DataField="Interes" HeaderText="Interés" DataFormatString="{0:N2}" />
                <asp:BoundField DataField="Mora" HeaderText="Mora" DataFormatString="{0:N2}" />
                <asp:BoundField DataField="OtrosCargos" HeaderText="Cargos" DataFormatString="{0:N2}" />
                <asp:BoundField DataField="SaldoCapitalPosterior" HeaderText="Saldo capital" DataFormatString="{0:N2}" />
                <asp:BoundField DataField="Estado" HeaderText="Estado" />
                <asp:BoundField DataField="Usuario" HeaderText="Registrado por" />
                <asp:TemplateField HeaderText="Factura"><ItemTemplate><asp:LinkButton ID="verComprobante" runat="server" CommandName="VerComprobante" CommandArgument='<%# Container.DisplayIndex %>' CausesValidation="false" CssClass="receipt-view-link" ToolTip="Ver la factura de este pago">Ver factura</asp:LinkButton></ItemTemplate></asp:TemplateField>
            </Columns></asp:GridView></div>
        </section>
    </div>
    <script>
        (() => {
            const field = document.querySelector('[id$="_monto"]');
            if (!field) return;
            const dialog = document.getElementById('dialogConfirmarPago');
            const invoiceDialog = document.getElementById('dialogComprobantePago');
            const loading = document.getElementById('cargandoPago');
            const loan = document.querySelector('[id$="_prestamo"]');
            const save = document.querySelector('[id$="_guardar"]');
            const formatAmount = value => 'B/. ' + (Number(value) || 0).toLocaleString('es-PA', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
            window.confirmarRegistroPago = button => {
                if (button.dataset.confirmed === 'true') {
                    delete button.dataset.confirmed;
                    return true;
                }
                if (!dialog || typeof dialog.showModal !== 'function') {
                    const accepted = window.confirm('¿Confirma el pago y el préstamo seleccionado?');
                    if (accepted && loading) { loading.hidden = false; loading.setAttribute('aria-hidden', 'false'); }
                    return accepted;
                }
                const option = loan && loan.selectedIndex >= 0 ? loan.options[loan.selectedIndex] : null;
                document.getElementById('confirmarPagoPrestamo').textContent = option ? option.text : '—';
                document.getElementById('confirmarPagoMonto').textContent = formatAmount(field.value);
                dialog.showModal();
                return false;
            };
            dialog.addEventListener('click', event => {
                if (event.target === dialog) dialog.close();
            });
            invoiceDialog.addEventListener('click', event => {
                if (event.target === invoiceDialog) invoiceDialog.close();
            });
            invoiceDialog.querySelector('[data-cerrar-comprobante]').addEventListener('click', () => invoiceDialog.close());
            dialog.querySelector('[data-cancelar-confirmacion]').addEventListener('click', () => dialog.close());
            dialog.querySelector('[data-confirmar-pago]').addEventListener('click', () => {
                if (!save || save.dataset.pending === 'true') return;
                dialog.close();
                save.dataset.pending = 'true';
                save.dataset.confirmed = 'true';
                loading.hidden = false;
                loading.setAttribute('aria-hidden', 'false');
                save.click();
            });
            document.addEventListener('click', event => {
                const button = event.target.closest('[data-pago-sugerencia]');
                if (!button || button.disabled) return;
                const suffix = button.dataset.pagoSugerencia === 'vencido' ? '_montoVencidoSugerido' : '_montoTotalSugerido';
                const source = document.querySelector('[id$="' + suffix + '"]');
                if (!source) return;
                field.value = source.value;
                field.dispatchEvent(new Event('input', { bubbles: true }));
                field.dispatchEvent(new Event('change', { bubbles: true }));
                field.focus();
                field.select();
            });
            field.addEventListener('input', () => {
                const serverError = document.querySelector('[id$="_montoErrorServidor"]');
                if (serverError) serverError.hidden = true;
                field.classList.remove('finance-invalid');
            });
            document.addEventListener('click', event => {
                if (!event.target.closest('[data-imprimir-comprobante]')) return;
                document.body.classList.add('imprimir-comprobante');
                window.print();
            });
            window.addEventListener('afterprint', () => document.body.classList.remove('imprimir-comprobante'));
        })();
    </script>
</asp:Content>
