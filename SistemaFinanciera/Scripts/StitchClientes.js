document.addEventListener('DOMContentLoaded', function () {
    const confirmOverlay = document.querySelector('[data-client-confirm]');
    if (confirmOverlay) {
        const confirmDialog = confirmOverlay.querySelector('.client-confirm-dialog');
        const confirmTitle = document.getElementById('clientConfirmTitle');
        const confirmMessage = document.getElementById('clientConfirmMessage');
        const confirmAccept = confirmOverlay.querySelector('[data-confirm-accept]');
        const confirmCancel = confirmOverlay.querySelector('[data-confirm-cancel]');
        let pendingTrigger = null;
        let approvedTrigger = null;
        let priorOverflow = '';
        function closeConfirmation(restoreFocus) {
            confirmOverlay.hidden = true;
            document.body.style.overflow = priorOverflow;
            if (restoreFocus && pendingTrigger && pendingTrigger.isConnected) pendingTrigger.focus();
            pendingTrigger = null;
        }
        window.solicitarConfirmacionEstado = function (trigger) {
            if (approvedTrigger === trigger) {
                approvedTrigger = null;
                return true;
            }
            pendingTrigger = trigger;
            const action = trigger.dataset.confirmAction || ((trigger.value || trigger.textContent || '').toLowerCase().includes('reactivar') ? 'reactivar' : 'desactivar');
            const isReactivate = action === 'reactivar';
            confirmOverlay.classList.toggle('is-reactivate', isReactivate);
            confirmTitle.textContent = isReactivate ? '¿Reactivar cliente?' : '¿Desactivar cliente?';
            confirmMessage.textContent = isReactivate
                ? 'El cliente volverá a estar disponible para las operaciones permitidas. Su historial y sus registros se conservarán.'
                : 'El cliente no podrá iniciar nuevas solicitudes mientras esté inactivo. Su historial y sus registros se conservarán.';
            confirmAccept.textContent = isReactivate ? 'Reactivar cliente' : 'Desactivar cliente';
            priorOverflow = document.body.style.overflow;
            document.body.style.overflow = 'hidden';
            confirmOverlay.hidden = false;
            confirmCancel.focus();
            return false;
        };
        confirmCancel.addEventListener('click', () => closeConfirmation(true));
        confirmAccept.addEventListener('click', () => {
            if (!pendingTrigger) return;
            const trigger = pendingTrigger;
            approvedTrigger = trigger;
            closeConfirmation(false);
            trigger.click();
        });
        confirmOverlay.addEventListener('click', event => {
            if (event.target === confirmOverlay) closeConfirmation(true);
        });
        confirmDialog.addEventListener('keydown', event => {
            if (event.key === 'Escape') {
                event.preventDefault();
                closeConfirmation(true);
            } else if (event.key === 'Tab') {
                const focusable = [confirmCancel, confirmAccept];
                if (event.shiftKey && document.activeElement === focusable[0]) {
                    event.preventDefault();
                    focusable[1].focus();
                } else if (!event.shiftKey && document.activeElement === focusable[1]) {
                    event.preventDefault();
                    focusable[0].focus();
                }
            }
        });
    }
    const dialog = document.querySelector('.form-overlay');
    if (dialog) {
        const hidden = dialog.querySelector('input[type=hidden]');
        const tabs = Array.from(dialog.querySelectorAll('[data-form-tab]'));
        const submitButton = dialog.querySelector('input[id$="btnGuardarCliente"]');
        function selectTab(name) {
            if (!tabs.some(t => t.dataset.formTab === name)) name = 'personal';
            tabs.forEach(t => { const active=t.dataset.formTab===name; t.setAttribute('aria-selected',String(active)); t.tabIndex=active?0:-1; });
            dialog.querySelectorAll('.form-section').forEach(s => s.hidden = s.id !== 'seccion-'+name);
            if (hidden) hidden.value = name;
            if (submitButton) submitButton.value = name === 'referencias' ? 'Guardar cliente' : 'Siguiente';
        }
        tabs.forEach((t,i) => {
            t.addEventListener('click',()=>selectTab(t.dataset.formTab));
            t.addEventListener('keydown',e=>{ if(e.key==='ArrowRight'||e.key==='ArrowLeft'){e.preventDefault();const next=tabs[(i+(e.key==='ArrowRight'?1:tabs.length-1))%tabs.length];selectTab(next.dataset.formTab);next.focus();}});
        });
        selectTab(hidden ? hidden.value : 'personal');
        // Las expresiones se publican desde ClienteServicio para mantener las mismas reglas.
        const rules = JSON.parse(document.getElementById('reglas-cliente').textContent);
        const fields = Array.from(dialog.querySelectorAll('.form-control,.form-select'));
        const field = name => fields.find(el => el.id.endsWith('_'+name));
        const kind = field('ddlTipoPersona');
        const documentType = field('ddlTipoIdentificacion');
        const namePattern = new RegExp(rules.nombre, 'u');
        const phonePattern = new RegExp(rules.telefono, 'u');
        const letters = /\p{L}/u;
        const forbidden = /[\x00-\x08\x0B\x0C\x0E-\x1F\x7F\p{Cf}<>]/u;
        function phone(value) {
            let result = value.replace(/[\s()+-]/g, '');
            if (result.startsWith('507') && [10,11].includes(result.length)) result = result.slice(3);
            return result;
        }
        function natural() { return kind.value === 'NATURAL'; }
        function configure() {
            fields.forEach(el => { el.required = !el.disabled; });
            const id = field('txtIdentificacion');
            const labels = {
                CEDULA: ['8-123-456 o E-123-456', rules.cedula],
                PASAPORTE: ['5 a 30 letras o números', rules.pasaporte],
                RUC: ['12345-1-123456', rules.ruc]
            };
            const selected = labels[documentType.value];
            id.placeholder = selected ? selected[0] : 'Seleccione el tipo de identificación';
            id.inputMode = 'text'; // El teclado debe permitir los guiones del documento.
            field('txtNombres').placeholder = natural() ? 'Ej. María José' : 'Ej. Comercial 123, S.A.';
            field('txtApellidos').placeholder = 'Ej. Pérez-Gómez';
            field('txtTelefono').inputMode = 'tel';
            field('txtTelefono').autocomplete = 'tel';
            field('txtCorreo').autocomplete = 'email';
            field('txtIngresos').inputMode = 'decimal';
            field('txtReferencias').maxLength = 1000;
            field('txtFechaNacimiento').min = rules.fechaMinima;
            field('txtFechaNacimiento').max = rules.fechaMaxima;
        }
        function errorFor(el) {
            if (el.disabled) return '';
            const value = el.value.trim().normalize('NFC');
            const key = el.id.split('_').pop();
            if (el.validity.badInput) return 'Introduzca un valor válido para este campo.';
            if (!value) return 'Este campo es obligatorio.';
            if (el.maxLength > 0 && value.length > el.maxLength) return 'Máximo '+el.maxLength+' caracteres.';
            if (forbidden.test(value)) return 'No se admiten estos símbolos o caracteres de control.';
            switch (key) {
                case 'ddlTipoPersona': return ['NATURAL','JURIDICA'].includes(value) ? '' : 'Seleccione un tipo de persona válido.';
                case 'ddlTipoIdentificacion': return (natural() ? ['CEDULA','PASAPORTE'] : ['RUC']).includes(value) ? '' : 'Seleccione una identificación compatible con el tipo de persona.';
                case 'txtIdentificacion': {
                    const pattern = {CEDULA:rules.cedula,PASAPORTE:rules.pasaporte,RUC:rules.ruc}[documentType.value];
                    return pattern && new RegExp(pattern,'u').test(value.replace(/\s/g,'').toUpperCase()) ? '' : 'Formato inválido. Ejemplo: '+el.placeholder+'.';
                }
                case 'txtNombres':
                    return (natural() ? namePattern.test(value) : letters.test(value)) ? '' : (natural() ? 'Solo letras, acentos, espacios, apóstrofos, puntos y guiones entre nombres.' : 'La razón social debe contener al menos una letra.');
                case 'txtApellidos': return namePattern.test(value) ? '' : 'Solo letras, acentos, espacios, apóstrofos, puntos y guiones entre apellidos.';
                case 'txtFechaNacimiento': {
                    const date = new Date(value+'T12:00:00Z');
                    return /^\d{4}-\d{2}-\d{2}$/.test(value) && !Number.isNaN(date.getTime()) && date.toISOString().slice(0,10) === value && value >= rules.fechaMinima && value <= rules.fechaMaxima ? '' : 'Seleccione una fecha válida, no futura y dentro de los últimos 120 años.';
                }
                case 'ddlSexo': return ['F','M','OTRO'].includes(value) || !natural() ? '' : 'Seleccione el sexo; No aplica es solo para personas jurídicas.';
                case 'ddlEstadoCivil': return ['SOLTERO','CASADO','UNIDO','DIVORCIADO','VIUDO'].includes(value) || !natural() ? '' : 'Seleccione un estado civil válido.';
                case 'txtTelefono': return phonePattern.test(phone(value)) ? '' : 'Indique 7 u 8 dígitos, distintos de solo ceros. Puede incluir +507.';
                case 'txtCorreo': return /^[^\s@<>]+@[^\s@<>]+\.[^\s@<>]+$/.test(value) && !el.validity.typeMismatch ? '' : 'Escriba un correo válido, por ejemplo nombre@dominio.com.';
                case 'txtDireccion': return letters.test(value) ? '' : 'Describa la dirección con letras; puede incluir números y signos.';
                case 'txtOcupacion': return letters.test(value) ? '' : 'Describa la ocupación o actividad económica con letras.';
                case 'txtIngresos': return /^\d+(?:\.\d{1,2})?$/.test(value) && Number(value) <= 999999999999.99 ? '' : 'Indique un monto entre 0 y 999999999999.99, con máximo dos decimales.';
                case 'txtReferencias':
                    return /[\p{L}\p{M}]{2,}/u.test(value) && (value.match(/\+?[0-9](?:[0-9 \t()+-]*[0-9])?/g) || []).some(candidate => phonePattern.test(phone(candidate)))
                        ? '' : 'Incluya un nombre y un teléfono válido de 7 u 8 dígitos. Puede usar texto libre y saltos de línea.';
                default:
                    return el.tagName === 'SELECT' && !Array.from(el.options).some(option => option.value === value && !option.disabled) ? 'Seleccione una opción válida del catálogo.' : '';
            }
        }
        function showError(el, message) {
            let hint = document.getElementById(el.id+'Error');
            if (!hint) {
                hint = document.createElement('small');
                hint.id = el.id+'Error';
                hint.className = 'field-validation-error';
                hint.setAttribute('aria-live','polite');
                el.insertAdjacentElement('afterend',hint);
                el.setAttribute('aria-describedby', [el.getAttribute('aria-describedby'),hint.id].filter(Boolean).join(' '));
            }
            hint.textContent = message;
            hint.hidden = !message;
            el.classList.toggle('is-invalid', Boolean(message));
            el.setAttribute('aria-invalid', String(Boolean(message)));
            el.setCustomValidity(message);
            return !message;
        }
        function validate(el) { return showError(el,errorFor(el)); }
        function allowedCharacters(el, text) {
            const key = el.id.split('_').pop();
            if ((key === 'txtNombres' && natural()) || key === 'txtApellidos') return /^[\p{L}\p{M}\s'\u2019.\-]*$/u.test(text);
            if (key === 'txtIdentificacion') return (documentType.value === 'RUC' ? /^[0-9\s-]*$/ : /^[a-zA-Z0-9\s-]*$/).test(text);
            if (key === 'txtTelefono') return /^[0-9\s()+-]*$/.test(text);
            if (key === 'txtIngresos') return /^[0-9.]*$/.test(text);
            return !forbidden.test(text);
        }
        configure();
        dialog.closest('form').noValidate = true; // Validamos antes de enfocar cualquier pestaña oculta.
        fields.forEach(el => {
            el.addEventListener('beforeinput', e => {
                if (!e.isComposing && e.data && !allowedCharacters(el,e.data)) {
                    e.preventDefault();
                    showError(el,'Ese carácter no corresponde a este campo.');
                }
            });
            el.addEventListener('paste', e => {
                if (!allowedCharacters(el,e.clipboardData.getData('text'))) {
                    e.preventDefault();
                    showError(el,'El texto pegado contiene caracteres que no corresponden a este campo.');
                }
            });
            el.addEventListener('input', () => validate(el));
            el.addEventListener('blur', () => {
                if (el === field('txtIdentificacion')) el.value = el.value.replace(/\s/g,'').toUpperCase();
                validate(el);
            });
            el.addEventListener('change', () => { configure(); validate(el); });
        });
        documentType.addEventListener('change', () => { configure(); if (field('txtIdentificacion').value) validate(field('txtIdentificacion')); });
        window.validarFormularioCliente = function () {
            const invalid = fields.filter(el => !validate(el));
            if (!invalid.length) return true;
            const section = invalid[0].closest('.form-section');
            if (section) selectTab(section.id.replace('seccion-',''));
            invalid[0].focus();
            return false;
        };
        window.avanzarFormularioCliente = function () {
            const current = hidden ? hidden.value : 'personal';
            if (current === 'referencias') return window.validarFormularioCliente();
            const section = dialog.querySelector('#seccion-'+current);
            if (!section) return false;
            const invalid = fields.filter(el => section.contains(el) && !validate(el));
            if (invalid.length) {
                invalid[0].focus();
                return false;
            }
            const index = tabs.findIndex(tab => tab.dataset.formTab === current);
            const next = tabs[index + 1];
            if (!next) return false;
            selectTab(next.dataset.formTab);
            next.focus();
            return false;
        };
        document.body.style.overflow='hidden';
        // Mantener la navegación por teclado dentro del diálogo; el formulario es validado por el servidor.
        const shell=document.querySelector('.stitch-shell');
        shell.querySelectorAll('.cw-topbar,.cw-rail,.cw-footer').forEach(el=>el.inert=true);
        dialog.addEventListener('keydown',e=>{
            if(e.key!=='Tab')return;
            const available=Array.from(dialog.querySelectorAll('button,input,select,textarea,a[href]')).filter(el=>!el.disabled&&el.type!=='hidden'&&el.tabIndex!==-1&&el.getClientRects().length>0);
            if(!available.length)return;
            const first=available[0],last=available[available.length-1];
            if(e.shiftKey&&document.activeElement===first){e.preventDefault();last.focus();}
            else if(!e.shiftKey&&document.activeElement===last){e.preventDefault();first.focus();}
        });
        if(dialog.querySelector('[role=alert]')) dialog.querySelector('[role=alert]').setAttribute('tabindex','-1');
        const focusTarget=dialog.querySelector('[role=alert]')||tabs.find(t=>t.getAttribute('aria-selected')==='true');
        if(focusTarget)focusTarget.focus();
    }
    const state=document.querySelector('select[id$="ddlEstado"]');
    const province=document.querySelector('select[id$="ddlProvinciaFiltro"]');
    const person=document.querySelector('select[id$="ddlPersonaFiltro"]');
    const loanFilter=document.querySelector('input[id$="hidFiltroPrestamo"]');
    const search=document.querySelector('input[id$="btnBuscar"]');
    document.querySelectorAll('[data-client-filter]').forEach(button=>button.addEventListener('click',()=>{
        if(!state||!search)return;
        const filter=button.dataset.clientFilter;
        state.value=filter==='1'||filter==='0'?filter:'';
        if(province)province.value='';
        if(person)person.value='';
        const input=document.querySelector('input[id$="txtBuscar"]');
        if(input)input.value='';
        if(loanFilter)loanFilter.value=filter==='prestamo'?'1':'0';
        search.click();
    }));
    [state,province,person].filter(Boolean).forEach(control=>control.addEventListener('change',()=>{if(loanFilter)loanFilter.value='0';}));
    const input=document.querySelector('input[id$="txtBuscar"]');
    if(input)input.addEventListener('keydown',e=>{if(e.key==='Enter'){e.preventDefault();search.click();}});
    const print=document.querySelector('[data-print-clients]');
    if(print)print.addEventListener('click',()=>window.print());
    document.querySelectorAll('tr[data-view-button]').forEach(row=>{
        const openProfile=()=>{const view=document.getElementById(row.dataset.viewButton);if(view)view.click();};
        row.addEventListener('click',event=>{if(event.target.closest('a,button,input'))return;openProfile();});
        row.addEventListener('keydown',event=>{if(event.key==='Enter'||event.key===' '){event.preventDefault();openProfile();}});
    });
    const profileTabs=document.querySelectorAll('[data-profile-tab]');
    profileTabs.forEach(tab=>tab.addEventListener('click',()=>{
        const selected=tab.dataset.profileTab;
        profileTabs.forEach(item=>{const active=item===tab;item.classList.toggle('is-active',active);item.setAttribute('aria-selected',String(active));});
        document.querySelectorAll('[data-profile-panel]').forEach(panel=>{panel.hidden=panel.dataset.profilePanel!==selected;});
    }));
});
