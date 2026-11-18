<%@ Page Title="Simulador de préstamos" Language="vb" AutoEventWireup="false" MasterPageFile="~/Site.Master" CodeBehind="Simulador.aspx.vb" Inherits="SistemaFinanciera.Simulador" %>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
<style>
.quote-bar{margin:0 0 18px;padding:14px 18px;border:1px solid #dfe9e3;border-radius:16px;background:#f7faf8}
.quote-bar-head{display:flex;align-items:center;justify-content:space-between;gap:14px;flex-wrap:wrap}
.quote-bar-head h2{margin:2px 0 0;color:#172538;font-size:16px}
.quote-client{margin-top:10px;padding-top:10px;border-top:1px solid #e3ece6}
.quote-client summary{display:inline-flex;align-items:center;gap:6px;color:#0d5c37;font-size:13px;font-weight:700;cursor:pointer;list-style:none}
.quote-client summary::-webkit-details-marker{display:none}
.quote-client summary::before{content:"+";display:inline-grid;place-items:center;width:20px;height:20px;border:1px solid #b9dfca;border-radius:50%;background:#fff;font-size:14px;line-height:1}
.quote-client[open] summary::before{content:"−"}
.quote-client summary span{color:#7a8899;font-weight:500}
.quote-client-help{margin:8px 0 12px;color:#7a8899;font-size:12px}
.quote-fields{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:12px}
.quote-fields .wide{grid-column:span 2}
.quote-fields label{display:block;margin:0 0 5px;color:#53657a;font-size:12px;font-weight:650}
.quote-fields input[readonly]{background:#eef3f0;color:#3b4b5e}
.quote-actions{display:flex;justify-content:flex-end;gap:8px;flex-wrap:wrap}
.quote-btn{display:inline-flex;align-items:center;gap:8px;min-height:40px;padding:0 16px;border:1px solid #c9dccf;border-radius:999px;background:#fff;color:#0d5c37;font-size:13px;font-weight:700;cursor:pointer}
.quote-btn:hover{background:#eaf5ef;border-color:#9ed8bd}
.quote-btn.primary{background:#0d7a48;border-color:#0d7a48;color:#fff}.quote-btn.primary:hover{background:#08663b}
.quote-btn svg{width:17px;height:17px;flex:none}
.quote-error{display:block;margin-top:6px;color:#b94e3a;font-size:12px}
@media(max-width:900px){.quote-fields{grid-template-columns:1fr 1fr}}
@media(max-width:560px){.quote-fields{grid-template-columns:1fr}.quote-fields .wide{grid-column:auto}.quote-actions .quote-btn{flex:1;justify-content:center}}
/* Documento de cotización: solo visible al imprimir */
#cotizacionImpresion{display:none}
@media print{
 @page{size:letter;margin:14mm 12mm}
 body *{visibility:hidden!important}
 #cotizacionImpresion,#cotizacionImpresion *{visibility:visible!important}
 #cotizacionImpresion{display:block!important;position:absolute;left:0;top:0;width:100%;background:#fff}
 .cot-doc{font:10.5pt Arial,Helvetica,sans-serif;color:#1c2a3d}
 .cot-head{display:flex;justify-content:space-between;align-items:center;margin-bottom:18px}
 .cot-head img{height:46px}
 .cot-head h1{margin:0;color:#1c2a3d;font-size:14pt;font-weight:700;text-transform:uppercase;letter-spacing:.08em}
 .cot-parties{display:flex;justify-content:space-between;gap:24px;margin-bottom:6px}
 .cot-parties>div{flex:1}.cot-parties .cot-right{text-align:right}
 .cot-label{display:block;margin-bottom:4px;color:#1c2a3d;font-size:8pt;font-weight:700;text-transform:uppercase;letter-spacing:.08em}
 .cot-parties strong{display:block;margin-bottom:4px;color:#0d7a48;font-size:14pt;font-weight:600}
 .cot-parties p{margin:1px 0;font-size:9.5pt;color:#1c2a3d}
 .cot-section{margin-top:12px;break-inside:avoid}
 .cot-section.cot-section-tabla{break-inside:auto}.cot-section-tabla h2,.cot-section-tabla .cot-note{break-after:avoid}
 .cot-section h2{margin:0 0 6px;padding:5px 8px;background:#0d7a48!important;color:#fff!important;font-size:9.5pt;text-transform:uppercase;letter-spacing:.05em;-webkit-print-color-adjust:exact;print-color-adjust:exact}
 .cot-grid{display:grid;grid-template-columns:1fr 1fr;gap:0 18px;margin:0}
 .cot-grid div{padding:5px 4px;border-bottom:1px solid #e1e9e4}.cot-grid dt{font-size:7.5pt;color:#6b7a8c;text-transform:uppercase;font-weight:700}.cot-grid dd{margin:2px 0 0;font-size:10pt}
 .cot-summary{display:grid;grid-template-columns:repeat(4,1fr);gap:6px}
 .cot-summary div{padding:6px 8px;border:1px solid #d3e0d8;background:#f6faf8!important;-webkit-print-color-adjust:exact;print-color-adjust:exact}
 .cot-summary div.is-key{background:#e6f2eb!important}.cot-summary span{display:block;font-size:7.5pt;color:#6b7a8c;text-transform:uppercase;font-weight:700}.cot-summary strong{font-size:11pt}.cot-summary .is-key strong{color:#0d7a48}
 .cot-note{margin:0 0 6px;font-size:8.5pt;color:#6b7a8c}
 .cot-table{width:100%;border-collapse:collapse;font-size:8.5pt}
 .cot-table thead{display:table-header-group}.cot-table tfoot{display:table-row-group}.cot-table tr{break-inside:avoid}
 .cot-table th{background:#e6f2eb!important;color:#0d5c37;padding:5px;border:1px solid #d3e0d8;-webkit-print-color-adjust:exact;print-color-adjust:exact}
 .cot-table td{padding:4px 5px;border-bottom:1px solid #e8efea;text-align:right}.cot-table td:nth-child(-n+2){text-align:center}
 .cot-table tbody tr:nth-child(even) td{background:#f6faf8!important;-webkit-print-color-adjust:exact;print-color-adjust:exact}
 .cot-table tfoot td{font-weight:700;background:#e6f2eb!important;color:#0d5c37;border-top:1px solid #c5dccd;-webkit-print-color-adjust:exact;print-color-adjust:exact}.cot-table tfoot td:first-child{text-align:left}
 .cot-signs{display:flex;justify-content:space-around;margin-top:46px;break-inside:avoid}.cot-signs div{width:38%;text-align:center;font-size:9pt;color:#6b7a8c}.cot-signs span{display:block;border-top:1px solid #1c2a3d;margin-bottom:4px}
 .cot-foot{margin-top:16px;padding-top:6px;border-top:1px solid #d3e0d8;font-size:7.5pt;color:#6b7a8c}
}
</style>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
<div class="simulator-page">
    <header class="page-header simulator-page-header">
        <div><div class="eyebrow">CapitalWest / Gestión</div><h1>Simulador de préstamos</h1><p>Explora cuotas y costos estimados con las condiciones vigentes del producto.</p></div>
    </header>
    <div id="pnlMensaje" runat="server" class="alert simulator-message" role="status" aria-live="polite"><asp:Literal ID="mensaje" runat="server" Mode="Encode" /></div>
    <section class="cw-card simulator-form-card" aria-labelledby="simulatorFormTitle">
        <div class="simulator-card-heading"><span class="simulator-icon" aria-hidden="true"><svg viewBox="0 0 24 24"><rect x="5" y="2.5" width="14" height="19" rx="3"/><path d="M8 6.5h8M8.5 11h.01M12 11h.01M15.5 11h.01M8.5 15h.01M12 15h.01M15.5 15h.01M8.5 18.5h.01M12 18.5h.01"/></svg></span><div><h2 id="simulatorFormTitle">Configura tu simulación</h2><p>El cálculo usa la tasa vigente, los límites y las opciones del producto.</p></div></div>
        <div class="finance-fields simulator-fields">
            <div class="finance-field"><label for="<%= producto.ClientID %>">Producto financiero</label><asp:DropDownList ID="producto" runat="server" CssClass="form-select" AutoPostBack="true" required="required" /></div>
            <div class="finance-field"><label for="<%= metodo.ClientID %>">Método de cálculo</label><asp:DropDownList ID="metodo" runat="server" CssClass="form-select" required="required" /></div>
            <div class="finance-field"><label for="<%= periodo.ClientID %>">Periodicidad de pago</label><asp:DropDownList ID="periodo" runat="server" CssClass="form-select" Enabled="false" /></div>
            <div class="finance-field"><label for="<%= monto.ClientID %>">Monto a simular (B/.)</label><asp:TextBox ID="monto" runat="server" CssClass="form-control simulator-numeric" TextMode="SingleLine" inputmode="decimal" maxlength="15" pattern="\d{1,12}(\.\d{1,2})?" data-decimal-limit="12,2" required="required" placeholder="Ej. 2500.00" /></div>
            <div class="finance-field"><label for="<%= cuotas.ClientID %>">Plazo solicitado (meses)</label><asp:DropDownList ID="cuotas" runat="server" CssClass="form-select" Enabled="false" required="required" /></div>
            <div class="finance-field"><label for="<%= primerPago.ClientID %>">Fecha estimada del primer pago</label><asp:TextBox ID="primerPago" runat="server" CssClass="form-control" TextMode="Date" required="required" /></div>
        </div>
        <div class="simulator-product-info"><span class="simulator-info-dot" aria-hidden="true"></span><asp:Literal ID="condicionesProducto" runat="server" Mode="Encode" /></div>
        <div class="simulator-form-footer"><label class="simulator-compare-option"><asp:CheckBox ID="comparar" runat="server" /><span><strong>Comparar métodos permitidos</strong><small>Compara intereses y total pagado para este mismo producto.</small></span></label><div class="simulator-form-actions"><button type="button" class="btn btn-light simulator-copy" data-copiar-simulacion disabled aria-disabled="true">Copiar a solicitud</button><asp:Button ID="limpiarSimulador" runat="server" Text="Limpiar" CssClass="btn btn-light simulator-clear" CausesValidation="false" formnovalidate="formnovalidate" /><asp:Button ID="simular" runat="server" Text="Calcular simulación" CssClass="btn btn-primary simulator-submit" /></div></div>
        <p class="simulator-copy-status" data-estado-copia role="status" aria-live="polite">Completa todos los campos obligatorios para copiar.</p>
    </section>
    <asp:Panel ID="resultados" runat="server" Visible="false" CssClass="simulator-results">
        <section class="quote-bar" aria-labelledby="quoteTitle">
            <div class="quote-bar-head">
                <div><span class="simulator-overline">COTIZACIÓN</span><h2 id="quoteTitle">Imprime o descarga esta simulación</h2></div>
                <div class="quote-actions">
                    <asp:LinkButton ID="imprimirCotizacion" runat="server" CssClass="quote-btn" CausesValidation="false"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M7 9V3h10v6"/><rect x="3" y="9" width="18" height="8" rx="2"/><path d="M7 14h10v7H7z"/></svg>Imprimir cotización</asp:LinkButton>
                    <asp:LinkButton ID="pdfCotizacion" runat="server" CssClass="quote-btn" CausesValidation="false"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M14 3H7a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2V8z"/><path d="M14 3v5h5M12 11v6m-3-3 3 3 3-3"/></svg>Descargar cotización PDF</asp:LinkButton>
                    <asp:LinkButton ID="excelCotizacion" runat="server" CssClass="quote-btn primary" CausesValidation="false"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect x="3" y="3" width="18" height="18" rx="2"/><path d="M3 9h18M3 15h18M9 3v18"/></svg>Descargar cotización Excel</asp:LinkButton>
                </div>
            </div>
            <details class="quote-client"<%= If(ClienteCotizacionConDatos(), " open", "") %>>
                <summary>Agregar datos del cliente <span>(opcional)</span></summary>
                <p class="quote-client-help">Si los completas, aparecen en la cotización bajo «Cotización para». Puedes elegir un cliente registrado o escribir los datos de un prospecto.</p>
                <div class="quote-fields">
                    <div class="wide"><label for="<%= clienteCotizacion.ClientID %>">Cliente</label><asp:DropDownList ID="clienteCotizacion" runat="server" CssClass="form-select" AutoPostBack="true" CausesValidation="false" /></div>
                    <div class="wide"><label for="<%= nombreCotizacion.ClientID %>">Nombre completo</label><asp:TextBox ID="nombreCotizacion" runat="server" CssClass="form-control" MaxLength="100" placeholder="Ej. María Pérez González" /></div>
                    <div><label for="<%= identificacionCotizacion.ClientID %>">Identificación</label><asp:TextBox ID="identificacionCotizacion" runat="server" CssClass="form-control" MaxLength="30" placeholder="Ej. 8-123-4567" /></div>
                    <div><label for="<%= telefonoCotizacion.ClientID %>">Teléfono</label><asp:TextBox ID="telefonoCotizacion" runat="server" CssClass="form-control" MaxLength="15" inputmode="tel" placeholder="Ej. 61234567" /></div>
                    <div class="wide"><label for="<%= correoCotizacion.ClientID %>">Correo electrónico</label><asp:TextBox ID="correoCotizacion" runat="server" CssClass="form-control" MaxLength="100" inputmode="email" placeholder="nombre@dominio.com" /></div>
                </div>
            </details>
        </section>
        <section class="simulator-summary" aria-label="Resumen de resultados">
            <article class="simulator-metric"><span>Monto solicitado</span><strong><asp:Literal ID="totalCapital" runat="server" Mode="Encode" /></strong><small>Principal usado en el cálculo</small></article>
            <article class="simulator-metric"><span>Intereses estimados</span><strong class="is-green"><asp:Literal ID="totalIntereses" runat="server" Mode="Encode" /></strong><small>Según la fórmula seleccionada</small></article>
            <article class="simulator-metric simulator-metric-featured"><span>Total programado</span><strong><asp:Literal ID="totalPagar" runat="server" Mode="Encode" /></strong><small>Capital más intereses proyectados</small></article>
            <article class="simulator-metric"><span>Cuota máxima</span><strong><asp:Literal ID="cuotaMaxima" runat="server" Mode="Encode" /></strong><small><asp:Literal ID="metodoResumen" runat="server" Mode="Encode" /></small></article>
        </section>
        <section class="simulator-net-summary" aria-label="Desembolso estimado">
            <div><span>Comisión retenida</span><strong><asp:Literal ID="comisionEstimado" runat="server" Mode="Encode" /></strong></div>
            <div><span>Gastos retenidos</span><strong><asp:Literal ID="gastosEstimados" runat="server" Mode="Encode" /></strong></div>
            <div><span>Neto estimado a recibir</span><strong class="is-green"><asp:Literal ID="netoEstimado" runat="server" Mode="Encode" /></strong></div>
        </section>
        <asp:Panel ID="panelComparacion" runat="server" Visible="false" CssClass="cw-card simulator-comparison">
            <div class="simulator-section-heading"><div><span class="simulator-overline">ANÁLISIS COMPARATIVO</span><h2>Comparación de métodos</h2><p>Solo se incluyen métodos habilitados y compatibles con la tasa vigente.</p></div><span class="simulator-compare-badge">Mismo monto y plazo</span></div>
            <div class="table-responsive"><asp:GridView ID="tablaComparacion" runat="server" AutoGenerateColumns="false" CssClass="table simulator-table" GridLines="None" EmptyDataText="No hay métodos disponibles para comparar."><Columns><asp:BoundField DataField="Metodo" HeaderText="Método" /><asp:BoundField DataField="CuotaMaxima" HeaderText="Cuota máxima" /><asp:BoundField DataField="Intereses" HeaderText="Intereses" /><asp:BoundField DataField="Total" HeaderText="Total programado" /><asp:BoundField DataField="Nota" HeaderText="Observación" /></Columns></asp:GridView></div>
        </asp:Panel>
        <section class="cw-card simulator-schedule" aria-labelledby="scheduleTitle">
            <div class="simulator-section-heading"><div><span class="simulator-overline">CALENDARIO ESTIMADO</span><h2 id="scheduleTitle">Tabla de amortización</h2><p><asp:Literal ID="notaMetodo" runat="server" Mode="Encode" /></p></div><span class="simulator-period-count"><asp:Literal ID="cantidadPeriodos" runat="server" Mode="Encode" /> cuotas</span></div>
            <div class="table-responsive"><asp:GridView ID="tablaPlan" runat="server" AutoGenerateColumns="false" CssClass="table simulator-table" GridLines="None" AllowPaging="true" PageSize="12"><Columns><asp:BoundField DataField="Numero" HeaderText="N.º cuota" /><asp:BoundField DataField="Fecha" HeaderText="Fecha estimada" DataFormatString="{0:dd/MM/yyyy}" /><asp:BoundField DataField="Capital" HeaderText="Capital (B/.)" DataFormatString="{0:N2}" /><asp:BoundField DataField="Interes" HeaderText="Interés (B/.)" DataFormatString="{0:N2}" /><asp:BoundField DataField="Cuota" HeaderText="Pago estimado (B/.)" DataFormatString="{0:N2}" /><asp:BoundField DataField="Saldo" HeaderText="Saldo proyectado (B/.)" DataFormatString="{0:N2}" /></Columns></asp:GridView></div>
            <p class="simulator-disclaimer">Proyección informativa basada en las condiciones actuales. No representa aprobación ni oferta de crédito; el calendario contractual se confirma en la solicitud.</p>
        </section>
    </asp:Panel>
    <div id="cotizacionImpresion" aria-hidden="true"><asp:Literal ID="documentoCotizacion" runat="server" Mode="PassThrough" /></div>
</div>
<script>
(function(){
    var form=document.getElementById('form1');
    var fields=Array.from(document.querySelectorAll('.simulator-fields [required]'));
    var submitButton=document.getElementById('<%= simular.ClientID %>');
    var clearButton=document.getElementById('<%= limpiarSimulador.ClientID %>');
    var copyButton=document.querySelector('[data-copiar-simulacion]');
    var copyStatus=document.querySelector('[data-estado-copia]');
    var transferKey='capitalwest.solicitud.simulacion.v1';
    var requiredToCopy=['<%= producto.ClientID %>','<%= metodo.ClientID %>','<%= periodo.ClientID %>','<%= monto.ClientID %>','<%= cuotas.ClientID %>','<%= primerPago.ClientID %>'].map(function(id){return document.getElementById(id);}).filter(Boolean);
    function completeForCopy(){return requiredToCopy.length===6&&requiredToCopy.every(function(field){return String(field.value||'').trim()!==''&&field.validity.valid;});}
    function refreshCopyButton(){var ready=completeForCopy();if(copyButton){copyButton.disabled=!ready;copyButton.setAttribute('aria-disabled',String(!ready));}if(copyStatus)copyStatus.textContent=ready?'Todos los campos requeridos están listos para copiar.':'Completa todos los campos obligatorios para copiar.';}
    requiredToCopy.forEach(function(field){['input','change','blur'].forEach(function(eventName){field.addEventListener(eventName,refreshCopyButton);});});
    if(copyButton)copyButton.addEventListener('click',function(){
        if(!completeForCopy()){refreshCopyButton();return;}
        var payload={producto:requiredToCopy[0].value,metodo:requiredToCopy[1].value,periodo:requiredToCopy[2].value,monto:requiredToCopy[3].value,cuotas:requiredToCopy[4].value,primerPago:requiredToCopy[5].value};
        var serialized=JSON.stringify(payload),saved=false;
        try{localStorage.setItem(transferKey,serialized);saved=true;}catch(error){}
        try{sessionStorage.setItem(transferKey,serialized);saved=true;}catch(error){}
        if(copyStatus)copyStatus.textContent=saved?'Datos copiados. Abre Nueva solicitud y pulsa “Pegar simulación”; el cliente se selecciona allí.':'El navegador no permitió guardar la simulación para transferirla.';
    });
    refreshCopyButton();
    function validateField(input){
        input.setCustomValidity('');
        var message='';
        if(!input.value.trim()) message='Este campo es obligatorio.';
        else if(input.validity.patternMismatch) message=input.id==='<%= monto.ClientID %>'?'Use hasta 12 dígitos y máximo 2 decimales.':'Indique una cantidad entera de cuotas.';
        else if(!input.validity.valid) message='Introduzca un valor válido.';
        var hint=document.getElementById(input.id+'Error');
        if(!hint){hint=document.createElement('small');hint.id=input.id+'Error';hint.className='field-validation-error';hint.setAttribute('aria-live','polite');input.insertAdjacentElement('afterend',hint);}
        hint.textContent=message;
        hint.hidden=!message;
        input.classList.toggle('is-invalid',Boolean(message));
        input.setAttribute('aria-invalid',String(Boolean(message)));
        if(message){input.setAttribute('aria-describedby',hint.id);}
        else{input.removeAttribute('aria-describedby');}
        return !message;
    }
    if(form){
        form.noValidate=true;
        fields.forEach(function(input){['input','change','blur'].forEach(function(eventName){input.addEventListener(eventName,function(){validateField(input);});});});
        form.addEventListener('submit',function(event){
            if(event.submitter===clearButton||event.submitter!==submitButton)return;
            var firstInvalid=null;
            fields.forEach(function(input){if(!validateField(input)&&!firstInvalid)firstInvalid=input;});
            if(firstInvalid){event.preventDefault();firstInvalid.focus();}
        });
    }
    document.querySelectorAll('.simulator-numeric[data-decimal-limit]').forEach(function(input){
        input.addEventListener('input',function(){
            var limits=input.dataset.decimalLimit.split(','), integerLimit=parseInt(limits[0],10), decimalLimit=parseInt(limits[1],10);
            var value=input.value.replace(/[^0-9.]/g,'');
            var point=value.indexOf('.');
            if(point!==-1){value=value.slice(0,point+1)+value.slice(point+1).replace(/\./g,'');}
            var parts=value.split('.');
            parts[0]=parts[0].slice(0,integerLimit);
            if(parts.length>1){parts[1]=parts[1].slice(0,decimalLimit);value=parts[0]+'.'+parts[1];}else{value=parts[0];}
            input.value=value;
            validateField(input);
            refreshCopyButton();
        });
    });
})();
</script>
</asp:Content>
