// Validación de captura para Acceso y los módulos financieros.
// El servidor vuelve a comprobar todas las reglas antes de persistir datos.
document.addEventListener('DOMContentLoaded', () => {
    const scopes = Array.from(document.querySelectorAll('[data-finanzas-form]'));
    const forbidden = /[<>\x00-\x08\x0B\x0C\x0E-\x1F\x7F\p{Cf}]/u;
    const field = (scope, name) => scope.querySelector('[id$="_' + name + '"]');
    const numeric = el => el.type === 'number' || el.dataset.numero === 'true';
    function numericSyntax(el, value) {
        const decimals = el.step === '1' ? 0 : (el.step.split('.')[1] || '').length;
        const digits = (el.max || '999999999999.99').split('.')[0].length;
        return new RegExp('^\\d{0,' + digits + '}' + (decimals ? '(?:\\.\\d{0,' + decimals + '})?' : '') + '$').test(value);
    }
    const controls = scope => Array.from(scope.querySelectorAll('input:not([type=hidden]):not([type=submit]):not([type=checkbox]),textarea,select')).filter(el => !el.disabled && !el.readOnly);
    function message(el, text) {
        const id = el.id + '_error';
        let output = document.getElementById(id);
        if (!output) {
            output = document.createElement('span');
            output.id = id;
            output.className = 'finance-field-error';
            output.setAttribute('aria-live', 'polite');
            el.insertAdjacentElement('afterend', output);
            el.setAttribute('aria-describedby', [el.getAttribute('aria-describedby'), id].filter(Boolean).join(' '));
        }
        output.textContent = text;
        el.classList.toggle('finance-invalid', Boolean(text));
        el.setAttribute('aria-invalid', String(Boolean(text)));
    }
    function error(el, action) {
        const scope = el.closest('[data-finanzas-form]');
        const key = el.id.split('_').pop();
        const raw = el.value;
        const value = raw.trim().normalize('NFC');
        let required = el.required;
        if (scope.dataset.finanzasForm === 'evaluacion') {
            if (key === 'aprobado' && action !== 'aprobar') return '';
            required = (key === 'aprobado' && action === 'aprobar') || (key === 'motivo' && action === 'rechazar');
        }
        if (key === 'referencia') required = field(scope, 'forma').value !== 'EFECTIVO';
        if (el.validity.badInput) return 'Introduzca un número válido.';
        if (!value) return required ? 'Este campo es obligatorio.' : '';
        if (el.type !== 'password' && forbidden.test(value)) return 'No se admiten estos símbolos o caracteres invisibles.';
        if (el.maxLength > 0 && raw.length > el.maxLength) return 'Máximo ' + el.maxLength + ' caracteres.';
        if (el.minLength > 0 && raw.length < el.minLength) return 'Mínimo ' + el.minLength + ' caracteres.';
        if (el.dataset.regla === 'nombre-persona' && !new RegExp(el.dataset.patronNombre, 'u').test(value)) return 'Use letras, acentos, espacios, apóstrofos, puntos o guiones; no números.';
        if (el.dataset.patron && !new RegExp(el.dataset.patron, 'u').test(el.dataset.regla === 'codigo' ? value.toUpperCase() : value)) return el.dataset.ayuda || 'Revise el formato de este campo.';
        if (el.dataset.regla === 'texto-descriptivo' && !/\p{L}/u.test(value)) return 'Incluya texto, no solo números o signos.';
        if (el.type === 'email' && (el.validity.typeMismatch || !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value))) return 'Escriba un correo válido. Ej.: nombre@dominio.com.';
        if (el.pattern && !new RegExp('^(?:' + el.pattern + ')$', 'u').test(value)) return el.dataset.ayuda || 'Revise el formato de este campo.';
        if (el.dataset.igual && raw !== document.getElementById(el.dataset.igual).value) return 'Las contraseñas no coinciden.';
        if (numeric(el)) {
            const decimals = el.step === '1' ? 0 : (el.step.split('.')[1] || '').length;
            if (!new RegExp('^\\d+' + (decimals ? '(?:\\.\\d{1,' + decimals + '})?' : '') + '$').test(raw)) return decimals ? 'Use un número positivo con hasta ' + decimals + ' decimales, separados con punto.' : 'Use un número entero positivo.';
            if (el.min && Number(raw) < Number(el.min)) return 'El mínimo permitido es ' + el.min + '.';
            if (el.max && Number(raw) > Number(el.max)) return 'El máximo permitido es ' + el.max + '.';
        }
        if (el.type === 'date') {
            if (el.validity.typeMismatch || !/^\d{4}-\d{2}-\d{2}$/.test(raw)) return 'Seleccione una fecha válida.';
            if (el.min && raw < el.min) return 'La fecha mínima es ' + el.min + '.';
            if (el.max && raw > el.max) return 'La fecha máxima es ' + el.max + '.';
        }
        const paired = {maximo:'minimo', cuotasMax:'cuotasMin', mesesMax:'mesesMin'}[key];
        if (paired && field(scope, paired).value && Number(raw) < Number(field(scope, paired).value)) return 'El máximo debe ser mayor o igual al mínimo.';
        if ((key === 'comision' || key === 'gastos') && Number(field(scope,'comision').value) + Number(field(scope,'gastos').value) > Number(field(scope,'minimo').value)) return 'La suma de comisión y gastos no puede superar el monto mínimo.';
        return '';
    }
    function validate(scope, action, focus) {
        let first;
        controls(scope).forEach(el => {
            const text = error(el, action);
            message(el, text);
            if (text && !first) first = el;
        });
        scope.querySelectorAll('[data-al-menos-uno]').forEach(group => {
            const text = group.querySelector('input:checked') ? '' : 'Seleccione al menos una opción.';
            message(group, text);
            if (text && !first) first = group.querySelector('input');
        });
        if (first && focus) first.focus();
        return !first;
    }
    scopes.forEach(scope => {
        controls(scope).forEach(el => {
            if (el.dataset.maxlength) el.maxLength = Number(el.dataset.maxlength);
            // type=number no ofrece selección ni aplica maxlength. Captura decimal
            // como texto con teclado numérico; el servidor conserva su validación.
            if (el.type === 'number') {
                el.dataset.numero = 'true';
                el.type = 'text';
                el.inputMode = el.step === '1' ? 'numeric' : 'decimal';
                const decimals = el.step === '1' ? 0 : (el.step.split('.')[1] || '').length;
                el.maxLength = (el.max || '999999999999.99').split('.')[0].length + (decimals ? decimals + 1 : 0);
                el.autocomplete = 'off';
            }
            let lastAccepted = el.value;
            if (el.required) {
                const label = document.querySelector('label[for="' + el.id + '"]');
                if (label) label.classList.add('finance-required');
            }
            function insertionProblem(text) {
                const start = el.selectionStart, end = el.selectionEnd;
                const proposed = start === null ? text : el.value.slice(0, start) + text + el.value.slice(end);
                if (el.maxLength > 0 && proposed.length > el.maxLength) return 'Máximo ' + el.maxLength + ' caracteres.';
                if (el.type !== 'password' && forbidden.test(proposed)) return 'No se admiten estos símbolos o caracteres invisibles.';
                if (el.dataset.regla === 'nombre-persona' && !/^[\p{L}\p{M} '\u2019.\-]*$/u.test(proposed)) return 'Use letras, acentos, espacios, apóstrofos, puntos o guiones; no números.';
                if (el.dataset.regla === 'codigo' && proposed && !/^[A-Za-z][A-Za-z0-9_-]*$/.test(proposed)) return el.dataset.ayuda;
                if (el.dataset.regla === 'usuario-nuevo' && proposed && !/^[A-Za-z][A-Za-z0-9._-]*$/.test(proposed)) return el.dataset.ayuda;
                if (el.dataset.regla === 'nombre-producto' && proposed && !/^[\p{L}][\p{L}\p{M}0-9 '\u2019.,&()/+%\-]*$/u.test(proposed)) return el.dataset.ayuda;
                if (numeric(el) && !numericSyntax(el, proposed)) return el.step === '1' ? 'Use solo enteros dentro de la longitud permitida.' : 'Use solo números y un punto, con hasta ' + (el.step.split('.')[1] || '').length + ' decimales.';
                if (numeric(el) && proposed && el.max && Number(proposed) > Number(el.max)) return 'El máximo permitido es ' + el.max + '.';
                return '';
            }
            el.addEventListener('beforeinput', event => {
                if (!event.data || event.isComposing || event.inputType.startsWith('delete')) return;
                const text = insertionProblem(event.data);
                if (text) { event.preventDefault(); message(el, text); }
            });
            el.addEventListener('paste', event => {
                if (!event.clipboardData) return;
                const text = insertionProblem(event.clipboardData.getData('text'));
                if (text) { event.preventDefault(); message(el, text); }
            });
            ['input','change','blur'].forEach(type => el.addEventListener(type, () => {
                if (type === 'input' && !el.isComposing && numeric(el) && !numericSyntax(el, el.value)) {
                    el.value = lastAccepted;
                    message(el, 'Valor rechazado: revise longitud, punto decimal y cantidad de decimales.');
                    return;
                }
                lastAccepted = el.value;
                el.dataset.tocado = 'true';
                message(el, error(el, scope.dataset.accion));
                controls(scope).filter(other => other !== el && other.dataset.tocado).forEach(other => message(other, error(other, scope.dataset.accion)));
            }));
            el.addEventListener('invalid', event => { event.preventDefault(); message(el, error(el, scope.dataset.accion)); });
            el.addEventListener('keydown', event => {
                if (event.key !== 'Enter' || el.tagName !== 'INPUT') return;
                const button = scope.querySelector('[data-validar-finanzas]:not(:disabled)');
                if (button) { event.preventDefault(); button.click(); }
            });
        });
        scope.querySelectorAll('[data-al-menos-uno]').forEach(group => group.addEventListener('change', () => message(group, group.querySelector('input:checked') ? '' : 'Seleccione al menos una opción.')));
    });
    document.addEventListener('click', event => {
        const button = event.target.closest('[data-validar-finanzas]');
        if (!button) return;
        const scope = button.closest('[data-finanzas-form]');
        scope.dataset.accion = button.dataset.validarFinanzas;
        if (!validate(scope, scope.dataset.accion, true)) { event.preventDefault(); event.stopImmediatePropagation(); }
    }, true);
    document.addEventListener('submit', event => {
        const button = event.submitter;
        if (button && button.hasAttribute('data-validar-finanzas')) {
            const scope = button.closest('[data-finanzas-form]');
            if (!validate(scope, button.dataset.validarFinanzas, true)) { event.preventDefault(); event.stopImmediatePropagation(); }
        }
    }, true);
});

// Botón cerrar de las alertas del sistema (.alert-success / .alert-danger).
document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('.alert').forEach(box => {
        if (!box.textContent.trim()) {
            box.setAttribute('hidden', '');
            return;
        }
        if (box.querySelector('.alert-close')) return;
        const btn = document.createElement('button');
        btn.type = 'button';
        btn.className = 'alert-close';
        btn.setAttribute('aria-label', 'Cerrar aviso');
        btn.innerHTML = '&times;';
        box.appendChild(btn);
    });
    document.addEventListener('click', event => {
        const btn = event.target.closest('.alert-close');
        if (!btn) return;
        const box = btn.closest('.alert');
        if (box) box.setAttribute('hidden', '');
    });
});
