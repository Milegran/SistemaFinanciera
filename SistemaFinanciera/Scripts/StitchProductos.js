// Pestañas Productos/Tasas + modales (solo presentación; la lógica de guardado sigue en el servidor).
document.addEventListener('DOMContentLoaded', () => {
    // Confirmación de cambio de estado, con el mismo diálogo accesible que Clientes.
    const confirmOverlay = document.querySelector('[data-product-confirm]');
    if (confirmOverlay) {
        const confirmDialog = confirmOverlay.querySelector('.client-confirm-dialog');
        const confirmTitle = document.getElementById('productConfirmTitle');
        const confirmMessage = document.getElementById('productConfirmMessage');
        const confirmAccept = confirmOverlay.querySelector('[data-product-confirm-accept]');
        const confirmCancel = confirmOverlay.querySelector('[data-product-confirm-cancel]');
        let pendingTrigger = null;
        let approvedTrigger = null;
        let priorOverflow = '';

        function closeConfirmation(restoreFocus) {
            confirmOverlay.hidden = true;
            document.body.style.overflow = priorOverflow;
            if (restoreFocus && pendingTrigger && pendingTrigger.isConnected) pendingTrigger.focus();
            pendingTrigger = null;
        }

        window.solicitarConfirmacionProducto = function (trigger) {
            if (approvedTrigger === trigger) {
                approvedTrigger = null;
                return true;
            }
            pendingTrigger = trigger;
            const isReactivate = trigger.dataset.confirmAction === 'reactivar';
            confirmOverlay.classList.toggle('is-reactivate', isReactivate);
            confirmTitle.textContent = isReactivate ? '¿Reactivar producto?' : '¿Desactivar producto?';
            confirmMessage.textContent = isReactivate
                ? 'El producto volverá a estar disponible para nuevas solicitudes. Los préstamos e historiales existentes se conservarán.'
                : 'El producto dejará de estar disponible para nuevas solicitudes. Los préstamos e historiales existentes se conservarán.';
            confirmAccept.textContent = isReactivate ? 'Reactivar producto' : 'Desactivar producto';
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

    // ---- Pestañas Productos / Tasas ----
    const tabButtons = document.querySelectorAll('.page-tab-switch .page-tab');
    const seccionProductos = document.getElementById('seccionProductos');
    const seccionTasas = document.getElementById('seccionTasas');
    const btnNuevoAccion = document.getElementById('btnNuevoAccion');
    const pageSubtitle = document.getElementById('pageSubtitle');
    let currentTab = 'productos';

    function switchPageTab(tab) {
        currentTab = tab === 'tasas' ? 'tasas' : 'productos';
        tabButtons.forEach(btn => {
            const selected = btn.dataset.tab === currentTab;
            btn.setAttribute('aria-selected', selected ? 'true' : 'false');
        });
        if (seccionProductos) seccionProductos.hidden = currentTab !== 'productos';
        if (seccionTasas) seccionTasas.hidden = currentTab !== 'tasas';
        if (btnNuevoAccion) {
            if (currentTab === 'tasas') {
                btnNuevoAccion.textContent = '+ Nueva tasa';
            } else {
                btnNuevoAccion.textContent = '+ Crear producto';
            }
        }
        if (pageSubtitle) {
            pageSubtitle.textContent = currentTab === 'tasas'
                ? 'Productos / Tasas de interés'
                : 'CapitalWest · Gestión financiera';
        }
        const hfTab = document.querySelector('input[id$="hfTabActiva"]');
        if (hfTab) hfTab.value = currentTab;
    }

    tabButtons.forEach(btn => {
        btn.addEventListener('click', () => switchPageTab(btn.dataset.tab));
        btn.addEventListener('keydown', event => {
            if (event.key === 'ArrowRight' || event.key === 'ArrowLeft') {
                event.preventDefault();
                const next = currentTab === 'productos' ? 'tasas' : 'productos';
                switchPageTab(next);
                const target = document.querySelector('.page-tab-switch .page-tab[data-tab="' + next + '"]');
                if (target) target.focus();
            }
        });
    });

    // Restaurar pestaña activa tras un postback.
    const hfTab = document.querySelector('input[id$="hfTabActiva"]');
    if (hfTab && hfTab.value === 'tasas') switchPageTab('tasas');

    // ---- Listado: búsqueda, fila clicable, imprimir ----
    const search = document.querySelector('input[id$="txtBuscarProducto"]');
    const searchBtn = document.querySelector('input[id$="btnBuscarProducto"]');
    if (search && searchBtn) {
        search.addEventListener('keydown', event => {
            if (event.key === 'Enter') { event.preventDefault(); searchBtn.click(); }
        });
    }
    document.querySelectorAll('tr[data-view-button]').forEach(row => {
        const open = () => { const btn = document.getElementById(row.dataset.viewButton); if (btn) btn.click(); };
        row.addEventListener('click', event => { if (event.target.closest('a,button,input')) return; open(); });
        row.addEventListener('keydown', event => {
            if (event.key === 'Enter' || event.key === ' ') { event.preventDefault(); open(); }
        });
    });
    const print = document.querySelector('[data-print-products]');
    if (print) print.addEventListener('click', () => window.print());
    const printRates = document.querySelector('[data-print-rates]');
    if (printRates) printRates.addEventListener('click', () => window.print());

    // ---- Utilidades de modal reutilizables ----
    function bindModal(overlayId, openAttr, closeAttr) {
        const overlay = document.getElementById(overlayId);
        if (!overlay) return null;
        const dialog = overlay.querySelector('.form-dialog');
        const openers = document.querySelectorAll('[' + openAttr + ']');
        const closers = overlay.querySelectorAll('[' + closeAttr + ']');
        let lastFocus = null;

        function openModal() {
            lastFocus = document.activeElement;
            overlay.removeAttribute('hidden');
            document.body.style.overflow = 'hidden';
            const shell = document.querySelector('.stitch-shell');
            if (shell) shell.querySelectorAll('.cw-topbar,.cw-rail,.cw-footer').forEach(el => { el.inert = true; });
            const first = dialog.querySelector('input:not([type=hidden]),select,textarea,button');
            if (first) first.focus();
        }

        function closeModal() {
            overlay.setAttribute('hidden', '');
            document.body.style.overflow = '';
            const shell = document.querySelector('.stitch-shell');
            if (shell) shell.querySelectorAll('.cw-topbar,.cw-rail,.cw-footer').forEach(el => { el.inert = false; });
            if (lastFocus && typeof lastFocus.focus === 'function') lastFocus.focus();
        }

        openers.forEach(btn => btn.addEventListener('click', openModal));
        closers.forEach(btn => btn.addEventListener('click', closeModal));
        overlay.addEventListener('click', event => { if (event.target === overlay) closeModal(); });
        document.addEventListener('keydown', event => {
            if (event.key !== 'Escape' || overlay.hasAttribute('hidden')) return;
            closeModal();
        });
        if (dialog) {
            dialog.addEventListener('keydown', event => {
                if (event.key !== 'Tab') return;
                const available = Array.from(dialog.querySelectorAll('button,input,select,textarea,a[href]')).filter(el => !el.disabled && el.type !== 'hidden' && el.tabIndex !== -1 && el.getClientRects().length > 0);
                if (!available.length) return;
                const first = available[0], last = available[available.length - 1];
                if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus(); }
                else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus(); }
            });
        }
        return { openModal, closeModal };
    }

    const modalProducto = bindModal('modalProducto', 'data-abrir-modal-producto', 'data-cerrar-modal-producto');
    const modalTasa = bindModal('modalTasa', 'data-abrir-modal-tasa', 'data-cerrar-modal-tasa');
    window.__modalProducto = modalProducto;
    window.__modalTasa = modalTasa;

    // El botón principal abre el modal de la pestaña activa.
    if (btnNuevoAccion) {
        btnNuevoAccion.addEventListener('click', () => {
            const target = currentTab === 'tasas' ? modalTasa : modalProducto;
            if (target) target.openModal();
        });
    }

    if (modalProducto && window.__abrirModalProducto) modalProducto.openModal();
    if (modalTasa && window.__abrirModalTasa) modalTasa.openModal();
    if (modalTasa && window.__cerrarModalTasa) modalTasa.closeModal();
});
