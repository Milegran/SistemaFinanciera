document.addEventListener('DOMContentLoaded',function(){
 // Botón de ojo para mostrar/ocultar en todos los campos de contraseña del modal.
 const ojo='<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7S2 12 2 12Z"/><circle cx="12" cy="12" r="3"/></svg>';
 const ojoTachado='<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M10.6 5.1A10.7 10.7 0 0 1 12 5c6.5 0 10 7 10 7a17.6 17.6 0 0 1-2.6 3.5M6.6 6.6C3.8 8.4 2 12 2 12s3.5 7 10 7c1.9 0 3.6-.6 5-1.5"/><path d="M9.9 9.9a3 3 0 0 0 4.2 4.2"/><path d="m3 3 18 18"/></svg>';
 document.querySelectorAll('.admin-dialog input[type="password"], .change-password-card input[type="password"]').forEach(function(field){
  field.dataset.passwordField='1';
  if(field.form)field.form.addEventListener('submit',function(){field.type='password';});
  const wrap=document.createElement('span');wrap.className='pw-wrap';
  field.parentNode.insertBefore(wrap,field);wrap.appendChild(field);
  const btn=document.createElement('button');btn.type='button';btn.className='pw-eye';btn.setAttribute('aria-controls',field.id);
  function pintar(){const visible=field.type==='text';btn.innerHTML=visible?ojoTachado:ojo;btn.setAttribute('aria-label',visible?'Ocultar contraseña':'Mostrar contraseña');btn.setAttribute('aria-pressed',visible?'true':'false');btn.title=visible?'Ocultar contraseña':'Mostrar contraseña';}
  function ajustar(){if(field.offsetHeight)btn.style.height=field.offsetHeight+'px';}
  btn.addEventListener('click',function(){const pos=field.selectionStart;field.type=field.type==='password'?'text':'password';pintar();field.focus();try{field.setSelectionRange(pos,pos);}catch(e){}});
  wrap.appendChild(btn);pintar();ajustar();field.addEventListener('focus',ajustar);window.addEventListener('resize',ajustar);
 });
 document.querySelectorAll('[data-password-policy]').forEach(function(field){
  const min=Number(field.dataset.minimo),max=Number(field.dataset.maximo);
  const confirmField=document.querySelector('[data-igual="'+field.id+'"]');
  const rules=[['Al menos '+min+' caracteres',v=>v.length>=min],['Máximo '+max+' caracteres',v=>v.length<=max]];
  if(field.dataset.mayuscula==='1')rules.push(['Una mayúscula (A–Z)',v=>/[A-Z]/.test(v)]);
  if(field.dataset.numero==='1')rules.push(['Un número (0–9)',v=>/[0-9]/.test(v)]);
  const box=document.createElement('div');box.className='password-guide';box.id=field.id+'Guide';
  const status=document.createElement('div');status.setAttribute('aria-live','polite');box.appendChild(status);
  const meter=document.createElement('meter');meter.min=0;meter.max=4;meter.low=2;meter.high=3;meter.optimum=4;meter.setAttribute('aria-label','Seguridad estimada de la contraseña');box.appendChild(meter);
  const title=document.createElement('div');title.className='password-guide-title';title.textContent='Elige una contraseña segura:';box.appendChild(title);
  const list=document.createElement('ul');box.appendChild(list);
  function addItem(text){const li=document.createElement('li'),icon=document.createElement('span');icon.className='pw-icon';icon.setAttribute('aria-hidden','true');li.appendChild(icon);li.appendChild(document.createTextNode(text));list.appendChild(li);return li;}
  function setItem(li,state){li.className=state;li.firstChild.textContent=state==='ok'?'✓':state==='fail'?'✕':'•';}
  const items=rules.map(r=>addItem(r[0]));
  const matchItem=confirmField?addItem('Coincidencia de contraseñas'):null;
  if(confirmField){const cell=confirmField.closest('.admin-dialog-grid > div, .change-password-fields > div');(cell||confirmField).insertAdjacentElement('afterend',box);confirmField.setAttribute('aria-describedby',box.id);}
  else field.insertAdjacentElement('afterend',box);
  field.setAttribute('aria-describedby',box.id);
  function update(){const value=field.value,missing=[];
   rules.forEach((r,i)=>{const ok=r[1](value);setItem(items[i],!value?'pending':ok?'ok':'fail');if(!ok)missing.push(r[0]);});
   const optional=!value&&field.dataset.obligatoria==='0';
   let score=!value?0:missing.length?1:value.length>=16&&/[a-z]/.test(value)&&/[^a-zA-Z0-9]/.test(value)?4:3;
   meter.value=score;status.textContent=optional?'Vacía: se conserva la contraseña actual.':!value?'Escriba una contraseña.':score===1?'Insegura: faltan requisitos.':score===4?'Segura (estimación).':'Cumple los requisitos; seguridad media.';
   field.setCustomValidity(optional?'':missing.length?'Falta: '+missing.join(', '):'');
   if(confirmField){const conf=confirmField.value,match=conf===value;
    setItem(matchItem,!value&&!conf?'pending':conf&&match?'ok':'fail');
    confirmField.classList.toggle('pw-match',!!conf&&match);
    confirmField.classList.toggle('pw-mismatch',!!conf&&!match);
    confirmField.setCustomValidity(match?'':value&&!conf?'Confirme la contraseña.':'Las contraseñas no coinciden.');}
  }
  field.addEventListener('input',update);field.addEventListener('change',update);
  if(confirmField){confirmField.addEventListener('input',update);confirmField.addEventListener('change',update);}
  update();
 });
});
