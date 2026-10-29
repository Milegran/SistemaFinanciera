# Validaciones de captura de clientes

Las reglas se comprueban en `ClienteServicio.Validar` antes de guardar. El formulario publica las expresiones desde BL y muestra errores junto al campo al escribir, salir y guardar. Impide insertar/pegar caracteres incompatibles en nombres naturales, identificación, teléfono e ingresos. Guardar revisa todas las pestañas y abre la del primer error. Sin JavaScript, el servidor sigue rechazando datos inválidos.

| Campo | Regla |
|---|---|
| Tipo de persona | Natural o jurídica |
| Tipo de identificación | Natural: cédula/pasaporte. Jurídica: RUC |
| Identificación | Obligatoria, máximo 30; mayúsculas y sin espacios al validar |
| Cédula | Tres grupos; prefijo numérico de 1–2 dígitos, con AV/PI opcional, o PE/E/N/AV/PI; dos grupos numéricos de 1–6 dígitos |
| Pasaporte | 5–30 letras ASCII/números |
| RUC | Tres grupos numéricos de 1–10, 1–6 y 1–10 dígitos respectivamente; máximo total 30 |
| Nombres/apellidos naturales | Letras Unicode, marcas de acento, espacios simples entre componentes, apóstrofos rectos o curvos, puntos y guiones; sin números; máximo 120 |
| Razón social | Obligatoria, máximo 120, al menos una letra; admite números y signos, excepto los caracteres prohibidos comunes |
| Nacimiento | Natural: fecha válida entre hoy en Panamá menos 120 años y hoy; jurídica: no aplica |
| Sexo/estado civil | Catálogos cerrados; No aplica solo para jurídica |
| Teléfono | 7–8 dígitos, no solo ceros; permite formato con espacios, guiones, paréntesis y +507 |
| Correo | Obligatorio, máximo 254, sintaxis de correo; el servidor comprueba MailAddress y dominio con punto |
| Provincia/distrito/corregimiento | Selectores dependientes; códigos de 2/4/6 dígitos. DAL comprueba la jerarquía contra el catálogo y obtiene los nombres |
| Dirección | Obligatoria, máximo 300, al menos una letra; admite números y signos |
| Información laboral/actividad | Obligatoria, máximo 200, al menos una letra; admite números y signos |
| Ingresos | Obligatorios en captura; 0–999999999999.99, punto decimal, máximo 2 decimales |
| Referencias | Texto libre obligatorio de hasta 1000 caracteres, con texto de nombre y al menos un teléfono válido de 7–8 dígitos (+507 opcional). Admite saltos de línea, etiquetas, distintos separadores y cualquier orden; relación opcional. No se valida cada referencia como una estructura separada |
| Búsqueda | Texto libre de hasta 254 caracteres en el formulario; SQL parametrizado y búsqueda literal |

Los textos de negocio rechazan `<`, `>` y caracteres de control/formato invisibles (permitiendo tabulación y saltos de línea para referencias). Las longitudes se validan también en el servidor. Las identificaciones siguen siendo únicas incluso para clientes inactivos.

Estas son políticas de captura del proyecto: las expresiones no acreditan existencia de una cédula/RUC, vigencia de un documento ni verificación del DV o consulta a un registro oficial. Si se necesitan otras variantes documentales, actualizar las constantes de BL y sus pruebas. No se impone edad mínima de crédito al registro de clientes; la elegibilidad pertenece al proceso de evaluación.

Las referencias siguen en la columna de texto actual, sin migración de BD. La etiqueta y ejemplo orientan sobre el contenido sin imponer un formato. Un cliente previo con referencias libres o datos que ya no cumplan las reglas deberá corregirlos al editar; consultar y desactivar no revalida esos datos.

Acceso, Productos, Solicitudes, Préstamos y Pagos también cuentan con validación de captura y mensajes junto a los campos. Véase `Validaciones_finanzas.md`.

Verificación: compilación de los cuatro proyectos y 96 comprobaciones BL/DAL con datos revertidos. También se comprobó en navegador el rechazo de números en nombres, errores de identificación/teléfono/correo/dirección y el salto a la pestaña Contacto al intentar guardar datos personales válidos con contacto incompleto.
