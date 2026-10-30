<%@ Page Title="Simulador de préstamos" Language="vb" AutoEventWireup="false" MasterPageFile="~/Site.Master" CodeBehind="Simulador.aspx.vb" Inherits="SistemaFinanciera.Simulador" %>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server" />
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
