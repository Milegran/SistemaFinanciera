document.addEventListener('DOMContentLoaded', function () {
    'use strict';

    const normalize = value => value.toLowerCase().replace(/\.aspx$/, '').replace(/\/$/, '');
    const path = normalize(window.location.pathname);
    document.querySelectorAll('[data-page]').forEach(function (link) {
        const target = normalize(new URL(link.href).pathname);
        if (path === target || (link.dataset.page === 'Default.aspx' && (path === '' || path.endsWith('/default.aspx')))) {
            link.classList.add('active');
            link.setAttribute('aria-current', 'page');
        }
    });

    const menu = document.getElementById('mobileMenu');
    const rail = document.getElementById('sidebar');
    const backdrop = document.getElementById('menuBackdrop');
    const expand = document.getElementById('sidebarExpand');
    const mobile = window.matchMedia('(max-width: 760px)');

    function setExpanded(expanded, persist) {
        document.body.classList.toggle('sidebar-expanded', expanded);
        if (expand) {
            expand.setAttribute('aria-expanded', String(expanded));
            expand.setAttribute('aria-label', expanded ? 'Contraer menú' : 'Expandir menú');
            expand.textContent = expanded ? '«' : '»';
        }
        if (persist) {
            try { localStorage.setItem('capitalwest-sidebar-expanded', expanded ? 'true' : 'false'); } catch (_) { }
        }
    }

    let savedExpanded = false;
    try { savedExpanded = localStorage.getItem('capitalwest-sidebar-expanded') === 'true'; } catch (_) { }
    setExpanded(savedExpanded, false);
    if (rail) rail.inert = mobile.matches;

    if (expand) expand.addEventListener('click', function () {
        setExpanded(!document.body.classList.contains('sidebar-expanded'), true);
    });

    function closeMobileMenu() {
        if (!rail || !menu || !backdrop) return;
        rail.classList.remove('mobile-open');
        rail.inert = mobile.matches;
        menu.setAttribute('aria-expanded', 'false');
        menu.setAttribute('aria-label', 'Abrir navegación');
        backdrop.hidden = true;
    }

    if (menu && rail && backdrop) {
        menu.addEventListener('click', function () {
            const open = !rail.classList.contains('mobile-open');
            rail.classList.toggle('mobile-open', open);
            rail.inert = !open;
            menu.setAttribute('aria-expanded', String(open));
            menu.setAttribute('aria-label', open ? 'Cerrar navegación' : 'Abrir navegación');
            backdrop.hidden = !open;
        });
        backdrop.addEventListener('click', closeMobileMenu);
        rail.querySelectorAll('a').forEach(function (link) {
            link.addEventListener('click', function () { if (mobile.matches) closeMobileMenu(); });
        });
        document.addEventListener('keydown', function (event) {
            if (event.key === 'Escape') closeMobileMenu();
        });
        window.addEventListener('resize', function () {
            if (mobile.matches) {
                rail.inert = !rail.classList.contains('mobile-open');
            } else {
                closeMobileMenu();
                rail.inert = false;
            }
        });
    }
});