document.addEventListener('DOMContentLoaded', function () {
    const dialog = document.querySelector('.form-overlay');
    if (dialog) {
        const hidden = dialog.querySelector('input[type=hidden]');
        const tabs = Array.from(dialog.querySelectorAll('[data-form-tab]'));
        function selectTab(name) {
            if (!tabs.some(t => t.dataset.formTab === name)) name = 'personal';
            tabs.forEach(t => { const active=t.dataset.formTab===name; t.setAttribute('aria-selected',String(active)); t.tabIndex=active?0:-1; });
            dialog.querySelectorAll('.form-section').forEach(s => s.hidden = s.id !== 'seccion-'+name);
            if (hidden) hidden.value = name;
        }
        tabs.forEach((t,i) => {
            t.addEventListener('click',()=>selectTab(t.dataset.formTab));
            t.addEventListener('keydown',e=>{ if(e.key==='ArrowRight'||e.key==='ArrowLeft'){e.preventDefault();const next=tabs[(i+(e.key==='ArrowRight'?1:tabs.length-1))%tabs.length];selectTab(next.dataset.formTab);next.focus();}});
        });
        selectTab(hidden ? hidden.value : 'personal');
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
    const search=document.querySelector('input[id$="btnBuscar"]');
    document.querySelectorAll('[data-client-filter]').forEach(button=>button.addEventListener('click',()=>{state.value=button.dataset.clientFilter;search.click();}));
    const input=document.querySelector('input[id$="txtBuscar"]');
    if(input)input.addEventListener('keydown',e=>{if(e.key==='Enter'){e.preventDefault();search.click();}});
    const print=document.querySelector('[data-print-clients]');
    if(print)print.addEventListener('click',()=>window.print());
});
