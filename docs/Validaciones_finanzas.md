# Captura de los módulos financieros

Acceso, Productos, Solicitudes (creación y evaluación), Préstamos y Pagos muestran errores junto al campo al escribir, cambiar, salir y enviar. Las acciones validan su propia sección; seleccionar registros, cambiar el producto, limpiar y cerrar sesión no exigen completar otro formulario. Enter usa la acción del formulario actual. Los errores se anuncian con aria-live y aria-describedby, y se enfoca el primero al enviar.

| Captura | Reglas |
|---|---|
| Nombre del administrador | Obligatorio, 160 caracteres, letras Unicode, acentos, espacios, apóstrofos, puntos y guiones. Admite iniciales como José A. Pérez; rechaza números. Comparte patrón con clientes. |
| Correo | Obligatorio, máximo 254, sintaxis válida y dominio con punto. |
| Usuario inicial | 3–20 caracteres; empieza con letra ASCII y continúa con letras, números, puntos, guiones o guion bajo. El inicio de sesión conserva compatibilidad con usuarios existentes. |
| Contraseña inicial | 8–20 caracteres y confirmación idéntica. No se filtran los caracteres de las contraseñas. El ingreso conserva su comprobación de credenciales. |
| Código de producto | 2–20 caracteres, empieza con letra ASCII; permite números y guiones/guion bajo entre componentes, sin separadores dobles ni finales (ej.: PRE-01). Se normaliza a mayúsculas en servidor. |
| Nombre de producto | 3–80 caracteres, empieza con letra Unicode; admite letras, acentos, números posteriores y espacios, apóstrofos, puntos, comas, guiones, &, /, +, %, paréntesis. Ej.: Crédito personal 2026. |
| Descripción | Obligatoria, máximo 500; incluye al menos una letra, con texto libre y saltos de línea. El límite se activa al cargar. |
| Montos y costos | Números con punto y hasta dos decimales; límites de almacenamiento. Mínimo ≤ máximo; comisión + gastos ≤ monto mínimo. |
| Cuotas y plazos | Cuotas enteras de 1 a 1200; meses positivos hasta 1200 con dos decimales; mínimos ≤ máximos. |
| Tasa | 0–100 %, hasta seis decimales. Tipo y capitalización del catálogo. |
| Métodos y periodicidades | Al menos una opción en cada grupo. El servidor comprueba las asociaciones históricas. |
| Solicitud | Selectores obligatorios; monto y cuotas limitados al producto seleccionado. Primer pago futuro dentro de dos años. Observaciones opcionales, hasta 1000. El servidor comprueba además plazo equivalente y condiciones vigentes. |
| Evaluación | Aprobar exige monto entre el mínimo/costos del producto y el menor entre solicitado y máximo del producto; rechazar exige motivo. No se validan los campos de una nueva solicitud al evaluar otra. Motivo de hasta 1000 caracteres. Transiciones y límites de aprobación se comprueban en servidor. |
| Formalización | Solicitud aprobada elegida y fecha contractual exactamente un período desde hoy; el selector limita a esa fecha al cargar las condiciones. |
| Pago | Préstamo y forma obligatorios, monto desde 0.01 hasta deuda exigible, dos decimales. Fecha operativa de solo lectura. Referencia obligatoria salvo efectivo, máximo 100. |

Nombres personales, códigos y números impiden caracteres incompatibles al escribir cuando el navegador entrega beforeinput. Pegar, autocompletar o modificar el DOM también dispara comprobaciones al editar y al enviar. Los textos de negocio rechazan marcado y caracteres invisibles; observaciones conservan saltos de línea y puntuación. No se imponen formatos rígidos a descripciones, motivos o referencias de pago.

`FinanzasFormularios.js` controla la captura y la presentación. BL vuelve a validar textos, importes y reglas de negocio, y DAL verifica permisos, catálogos, estados y relaciones antes de guardar. Desactivar JavaScript no permite omitir la validación del servidor.

Verificación: compilación de la solución y precompilación ASPX; 51 comprobaciones de captura sin base (`powershell.exe -File Tests/ProbarFinanzas.ps1 -SoloCaptura`), 85 financieras y 96 de clientes sobre una base aislada. Navegador: nombres/correos inválidos y corrección en Acceso; nombre, rango y tasa en Productos; campos obligatorios y motivo condicional en Solicitudes; formulario de Préstamos sin solicitud elegida; importe mínimo y referencia condicional en Pagos. No se crearon usuarios ni operaciones de prueba en capitalwest.

Revisión de captura: antes de insertar o pegar se revisa el valor completo, incluida la selección reemplazada. Los números se presentan como texto con teclado numérico para aplicar longitud y precisión de forma consistente; el servidor mantiene el análisis decimal estricto. Se bloquean exponentes, signos, segundos puntos y decimales en cuotas, y se valida el rango. Descripciones, observaciones no vacías y motivos deben contener texto; las referencias de pago pueden ser numéricas. Verificado en navegador el bloqueo de códigos/nombres con inicio numérico, pegado de fracciones/exponentes y los límites de 20/80/500 desde la carga. Las pruebas usan la base aislada, sin cambios de datos en capitalwest.
