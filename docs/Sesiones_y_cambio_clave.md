# Sesiones y cambio de contraseña

La cookie de autenticación pertenece a la ejecución actual de ASP.NET y a la versión de sesión del usuario. Reiniciar IIS o reciclar la aplicación invalida las cookies anteriores. Volver a abrir Acceso.aspx también cierra la sesión existente: al iniciar desde Visual Studio con esa página se solicitan las credenciales incluso si IIS Express siguió activo.

Cambiar contraseña y Crear usuario comparten PoliticaClave.js y PoliticaClave.css. Ambas pantallas leen la política configurada: longitud mínima, máximo de 128 caracteres, mayúscula, número y coincidencia de confirmación. La validación del servidor sigue siendo obligatoria aunque se omita JavaScript. Se eliminó el texto fijo de 12 caracteres y minúscula que contradecía la política.

Un cambio o recuperación de contraseña incrementa VersionSesion e invalida las demás sesiones. Después de cambiar su clave, el usuario conserva únicamente su sesión actual con un ticket renovado.

No requiere migración: VersionSesion ya existe en Usuarios. Al desplegar esta versión se deberá iniciar sesión nuevamente.

## Validación realizada

- Compilación de la solución y precompilación ASP.NET correctas.
- 12 verificaciones de servicio: recuperación administrativa, política, rechazos y cambios de versión.
- 17 verificaciones HTTP: acceso anónimo, cambio obligatorio, metadatos, validación del servidor, confirmación y revocación de otra sesión.
- 7 verificaciones HTTP después de detener y reiniciar IIS Express: rechazo de cookies previas, nuevo acceso, reapertura de Acceso.aspx y cierre de sesión.
- Suite existente de seguridad administrativa completada correctamente.
- Comprobación visual del formulario obligatorio y la guía compartida.

Las pruebas usaron la base aislada capitalwest_pruebas_sesion, usuarios ficticios e IIS Express en el puerto 61021. La base capitalwest no se modificó.

## Repetir las pruebas

1. Compilar la solución y crear una base desechable capitalwest_pruebas_sesion con el esquema vigente. No usar la base real.
2. Ejecutar Tests/ProbarSesionPolitica.ps1 con Windows PowerShell y -Conexion apuntando exclusivamente a esa base. El script genera credenciales temporales en Tests/obj/SesionPoliticaTests/credenciales-prueba.txt.
3. Servir una copia del sitio con su conexión apuntando a esa base y un nombre de cookie distinto del sitio real.
4. Ejecutar Tests/ProbarSesionWeb.py con --base-url http://localhost:61021/ --fase antes --estado una_carpeta_de_prueba --credenciales Tests/obj/SesionPoliticaTests/credenciales-prueba.txt.
5. Detener y reiniciar ese IIS Express y ejecutar el mismo comando con --fase despues. Conservar la carpeta --estado entre ambas fases.
6. Cerrar el servidor de pruebas y eliminar solo la base desechable.
