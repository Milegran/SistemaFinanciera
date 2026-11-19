"""Prueba HTTP con cookies reales contra la copia aislada de SesionPoliticaTests.

Ejecutar --fase antes, reiniciar el IIS de prueba y ejecutar --fase despues.
Requiere Python y lxml. No usar con cuentas o bases de trabajo.
"""
import argparse
from http.cookiejar import LWPCookieJar
from pathlib import Path
from urllib.parse import urlencode, urljoin, urlsplit
from urllib.request import build_opener, HTTPCookieProcessor, Request
from lxml import html

p = argparse.ArgumentParser()
p.add_argument('--base-url', required=True)
p.add_argument('--fase', choices=['antes', 'despues'], required=True)
p.add_argument('--estado', type=Path, required=True)
p.add_argument('--credenciales', type=Path, required=True)
a = p.parse_args()
base = a.base_url.rstrip('/') + '/'
if urlsplit(base).hostname not in ('localhost', '127.0.0.1'):
    raise RuntimeError('Solo localhost de pruebas')
a.estado.mkdir(parents=True, exist_ok=True)
casos = 0

def cliente(archivo=None):
    jar = LWPCookieJar(str(archivo) if archivo else None)
    if archivo and archivo.exists():
        jar.load(ignore_discard=True, ignore_expires=True)
    return build_opener(HTTPCookieProcessor(jar)), jar

def get(c, ruta, datos=None):
    req = Request(urljoin(base, ruta), data=urlencode(datos).encode() if datos is not None else None)
    with c.open(req, timeout=20) as r:
        return r.url, html.fromstring(r.read())

def campo(r, sufijo):
    return next(n for n in r[1].xpath('//*[@name]/@name') if n == sufijo or n.endswith('$'+sufijo))

def enviar(c, r, boton, **valores):
    datos = dict(r[1].xpath('//form')[0].form_values())
    for k, v in valores.items():
        datos[campo(r, k)] = v
    datos[campo(r, boton)] = '1'
    return get(c, r[0], datos)

def login(c, usuario, clave):
    return enviar(c, get(c, 'Acceso.aspx'), 'entrar', usuario=usuario, clave=clave)

def check(valor, texto):
    global casos
    if not valor:
        raise AssertionError(texto)
    casos += 1
    print('OK:', texto, flush=True)

def texto(r):
    return ' '.join(r[1].xpath('//body//text()[not(ancestor::script) and not(ancestor::style)]'))

cookie = a.estado/'sesion-antes.cookies'
if a.fase == 'antes':
    c, jar = cliente()
    check('/Acceso' in get(c, 'Default.aspx')[0], 'Inicio exige login sin cookie')
    usuario, temporal = a.credenciales.read_text(encoding='utf-8-sig').splitlines()
    primera = login(c, usuario, temporal)
    check('/Administracion/CambiarClave' in primera[0], 'Clave temporal dirige al cambio forzado')
    nueva = primera[1].xpath('//*[@data-password-policy]')[0]
    check(nueva.get('minlength') == '8' and nueva.get('maxlength') == '128', 'Longitud HTML coincide con politica de servidor')
    check(nueva.get('data-mayuscula') == '1' and nueva.get('data-numero') == '1', 'Mayuscula y numero publicados para validacion en vivo')
    check('12 caracteres o más' not in texto(primera), 'Eliminado texto fijo de doce caracteres')
    for valor, esperado in [('abc', 'al menos 8'), ('solominusculas123', 'mayúscula'), ('SinNumero', 'un número'), ('A'*129+'1', 'máximo 128')]:
        r = enviar(c, get(c, 'Administracion/CambiarClave.aspx'), 'btnCambiar', txtNueva=valor, txtConfirmar=valor)
        check('/CambiarClave' in r[0] and esperado in texto(r), 'POST sin JavaScript rechaza incumplimiento: '+esperado)
    r = enviar(c, get(c, 'Administracion/CambiarClave.aspx'), 'btnCambiar', txtNueva='NuevaA12', txtConfirmar='DistintaA12')
    check('Las contraseñas no coinciden.' in texto(r), 'Coincidencia comprobada en servidor')
    otro, _ = cliente()
    check('/CambiarClave' in login(otro, usuario, temporal)[0], 'Segunda sesion con clave temporal')
    r = enviar(c, get(c, 'Administracion/CambiarClave.aspx'), 'btnCambiar', txtNueva='NuevaA12', txtConfirmar='NuevaA12')
    check('/Default' in r[0], 'Clave valida guarda y permite continuar en esta sesion')
    check('/Acceso' in get(otro, 'Default.aspx')[0], 'Otra sesion queda revocada al cambiar clave')
    r = login(otro, usuario, 'NuevaA12')
    check('/Default' in r[0], 'Nueva clave permite volver a entrar')
    admin, _ = cliente()
    check('/Default' in login(admin, 'pruebas_admin', 'PruebaSegura123!')[0], 'Administrador ficticio inicia sesion')
    r = enviar(admin, get(admin, 'Administracion/Usuarios.aspx'), 'btnNuevo')
    politica_admin = r[1].xpath('//*[@data-password-policy]')[0]
    check(all(politica_admin.get('data-'+k) == nueva.get('data-'+k) for k in ('minimo','maximo','mayuscula','numero')), 'Crear usuario y cambiar clave publican la misma politica')
    jar.filename = str(cookie)
    jar.save(ignore_discard=True, ignore_expires=True)
    check('/Default' in get(c, 'Default.aspx')[0], 'Cookie funciona antes de detener el servidor')
else:
    c, _ = cliente(cookie)
    check('/Acceso' in get(c, 'Default.aspx')[0], 'Cookie anterior al reinicio no abre Inicio')
    c, _ = cliente(cookie)
    check('/Acceso' in get(c, 'Acceso.aspx')[0], 'Acceso no redirige a Inicio con cookie vieja')
    c, _ = cliente(cookie)
    check('/Acceso' in get(c, 'Clientes/Clientes.aspx')[0], 'Cookie vieja tampoco abre otra pantalla protegida')
    c, _ = cliente()
    check('/Default' in login(c, 'pruebas_admin', 'PruebaSegura123!')[0], 'Login nuevo funciona despues del reinicio')
    check('/Acceso' in get(c, 'Acceso.aspx')[0] and '/Acceso' in get(c, 'Default.aspx')[0], 'Reabrir Acceso exige login aun si IIS sigue activo')
    check('/Default' in login(c, 'pruebas_admin', 'PruebaSegura123!')[0], 'Se puede entrar de nuevo tras reabrir Acceso')
    r = get(c, 'Default.aspx')
    datos = dict(r[1].xpath('//form')[0].form_values())
    datos['__EVENTTARGET'] = 'ctl00$cerrarSesion'
    check('/Acceso' in get(c, r[0], datos)[0] and '/Acceso' in get(c, 'Default.aspx')[0], 'Cerrar sesion sigue bloqueando Inicio')
print('TOTAL HTTP:', casos)
