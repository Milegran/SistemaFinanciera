document.addEventListener('DOMContentLoaded', function () {
    const normalize = value => value.toLowerCase().replace(/\.aspx$/, '').replace(/\/$/, '');
    let path = normalize(window.location.pathname);
    document.querySelectorAll('[data-page]').forEach(function (link) {
        const target = normalize(new URL(link.href).pathname);
        if (path === target || (link.dataset.page === 'Default.aspx' && (path === '' || path === normalize(new URL(link.href).pathname.replace(/Default\.aspx$/i, ''))))) {
            link.classList.add('active'); link.setAttribute('aria-current', 'page');
        }
    });
    const menu = document.getElementById('mobileMenu');
    const rail = document.getElementById('sidebar');
    const backdrop = document.getElementById('menuBackdrop');
    function alignRail() {
        const grid=document.querySelector('.stitch-dashboard .dashboard-grid');
        const shell=document.querySelector('.stitch-shell');
        if(grid&&shell&&window.innerWidth>760) rail.style.top=(grid.getBoundingClientRect().top-shell.getBoundingClientRect().top)+'px';
        else rail.style.removeProperty('top');
    }
    alignRail(); window.addEventListener('resize',alignRail);
    rail.inert = window.innerWidth <= 760;
    function closeMenu() { rail.inert = window.innerWidth <= 760; rail.classList.remove('mobile-open'); menu.setAttribute('aria-expanded', 'false'); backdrop.hidden = true; }
    menu.addEventListener('click', function () {
        const open = rail.classList.toggle('mobile-open');
        rail.inert = !open; menu.setAttribute('aria-expanded', String(open)); backdrop.hidden = !open;
    });
    backdrop.addEventListener('click', closeMenu);
    document.addEventListener('keydown', e => { if (e.key === 'Escape') closeMenu(); });
    window.addEventListener('resize', () => { if (window.innerWidth > 760) closeMenu(); else rail.inert = !rail.classList.contains('mobile-open'); });
});
