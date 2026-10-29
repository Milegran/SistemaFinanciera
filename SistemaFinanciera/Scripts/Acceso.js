(function () {
    'use strict';

    var form = document.getElementById('form1');
    var wrapper = form && form.closest('.auth-form');
    var usuario = document.getElementById(wrapper && wrapper.dataset.userId);
    var clave = document.getElementById(wrapper && wrapper.dataset.passwordId);
    var recordar = document.getElementById('rememberUser');
    var toggle = document.getElementById('togglePassword');
    var submit = document.getElementById(wrapper && wrapper.dataset.submitId);
    var loading = document.getElementById('loginLoading');
    var dialog = document.getElementById('recoveryDialog');
    var openRecovery = document.getElementById('openRecovery');
    var closeRecovery = document.getElementById('closeRecovery');
    var confirmRecovery = document.getElementById('confirmRecovery');
    var storageKey = 'capitalwest:remembered-user';
    var lastFocus;

    if (!form || !usuario || !clave) return;

    try {
        var rememberedUser = window.localStorage.getItem(storageKey) || '';
        if (rememberedUser) {
            usuario.value = rememberedUser;
            recordar.checked = true;
        }
    } catch (ignore) { }

    if (usuario.value) clave.focus();
    else usuario.focus();

    function validateRequired(control, errorId, message) {
        var error = document.getElementById(errorId);
        function update() {
            var empty = control.value.trim() === '';
            control.setCustomValidity(empty ? message : '');
            if (!empty) {
                error.hidden = true;
                control.removeAttribute('aria-invalid');
            }
        }
        control.addEventListener('invalid', function () {
            error.hidden = false;
            control.setAttribute('aria-invalid', 'true');
        });
        control.addEventListener('input', update);
        update();
    }

    validateRequired(usuario, 'usernameError', 'Ingresa tu usuario o correo.');
    validateRequired(clave, 'passwordError', 'Ingresa tu contraseña.');

    if (toggle) {
        toggle.addEventListener('click', function () {
            var mostrar = clave.type === 'password';
            clave.type = mostrar ? 'text' : 'password';
            toggle.textContent = mostrar ? 'Ocultar' : 'Mostrar';
            toggle.setAttribute('aria-label', mostrar ? 'Ocultar contraseña' : 'Mostrar contraseña');
            toggle.setAttribute('aria-pressed', mostrar ? 'true' : 'false');
            clave.focus();
        });
    }

    form.addEventListener('submit', function (event) {
        if (form.dataset.submitting === 'true') {
            event.preventDefault();
            return;
        }

        usuario.value = usuario.value.trim();
        try {
            if (recordar.checked) window.localStorage.setItem(storageKey, usuario.value);
            else window.localStorage.removeItem(storageKey);
        } catch (ignore) { }

        form.dataset.submitting = 'true';
        form.setAttribute('aria-busy', 'true');
        if (submit) submit.classList.add('is-loading');
        if (loading) loading.hidden = false;
    });

    function showRecovery() {
        lastFocus = document.activeElement;
        dialog.hidden = false;
        confirmRecovery.focus();
    }

    function hideRecovery() {
        dialog.hidden = true;
        if (lastFocus && lastFocus.focus) lastFocus.focus();
    }

    if (openRecovery) openRecovery.addEventListener('click', showRecovery);
    if (closeRecovery) closeRecovery.addEventListener('click', hideRecovery);
    if (confirmRecovery) confirmRecovery.addEventListener('click', hideRecovery);
    if (dialog) {
        dialog.addEventListener('click', function (event) {
            if (event.target === dialog) hideRecovery();
        });
        document.addEventListener('keydown', function (event) {
            if (event.key === 'Escape' && !dialog.hidden) hideRecovery();
        });
    }
}());
