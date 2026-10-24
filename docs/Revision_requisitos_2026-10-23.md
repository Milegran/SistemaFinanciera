# Revisión de avance y cumplimiento

Fecha: 23 de octubre de 2026. Código revisado: commit `0ea01b5`, sin cambios previos en el árbol de trabajo.

## Resultado

El último avance registrado fue integrar MySQL y los catálogos territoriales al módulo de clientes, el 8 de octubre. Hay una base de datos amplia y un módulo de clientes implementado por capas. El ciclo financiero todavía está pendiente: productos, solicitudes, simulación, préstamos, pagos, estados de cuenta, seguridad y auditoría de operaciones no están implementados en la aplicación.

La existencia de una tabla, vista o enlace de navegación no equivale a una función terminada. No asigno un porcentaje global: ocultaría la diferencia entre diseño de base de datos y comportamiento ejecutable.

## Fuentes y alcance

- `C:/Users/rafae/Downloads/Proyecto No. 2 DS8.pdf`, 8 páginas: requisitos y entregables.
- `C:/Users/rafae/Downloads/Glosario.pdf`, 4 páginas: significado de conceptos financieros.
- Código VB, ASPX, JavaScript propio, proyectos, scripts SQL, pruebas y documentación del repositorio.

Los documentos se trataron como fuentes de requisitos para la revisión. No se ejecutaron sus instrucciones como tareas independientes. La documentación SQL menciona un diseño de pantallas adicional que no formó parte de los archivos aportados en esta revisión; sus propuestas no se consideran obligaciones del enunciado.

Se revisó el código y se compiló la solución. No se hizo una prueba visual en navegador ni una nueva importación del esquema. No se cambiaron reglas, código funcional ni datos de negocio.

## Cobertura del enunciado

| Área y sección del PDF | Estado observado | Qué falta |
|---|---|---|
| Clientes, §§3 y 6 | Implementado con observaciones | Corregir validaciones descritas abajo; comprobar integración con BD disponible |
| Productos y tasas, §§3, 5 y 12 | Modelo SQL; pantalla pendiente | CRUD, vigencias, límites y métodos/periodicidades permitidos |
| Solicitudes y evaluación, §7 | Modelo SQL; pantalla pendiente | Crear, consultar, evaluar, aprobar, rechazar y autorizar transiciones |
| Parámetros y formalización, §§5 y 7 | Modelo SQL | Captura, validación y copia contractual; desembolso y generación de cuotas |
| Cinco métodos, §§4, 13 y 18 | Catálogo y fórmulas descriptivas en SQL | Interfaz/abstracción y cinco implementaciones ejecutables en BL |
| Simulador, §8 | Pantalla de próximamente; Page_Load vacío | Entradas, cálculo, comparación, totales y calendario de amortización |
| Pagos, §9 | Modelo SQL; pantalla pendiente | Transacción, desglose, saldo, usuario e impedir sobrepagos |
| Estados, §10 | Catálogo SQL | Máquina de estados y reglas de transición en servidor |
| Cuotas vencidas y mora, §3 | Estructura y vistas | Detección y actualización coherente; reglas de cargos según condiciones pactadas |
| Refinanciamiento/reestructuración, §3 | Tabla de refinanciamiento | Flujo ejecutable y trazabilidad de obligación anterior/nueva |
| Historial y estados de cuenta, §§3 y 14 | Tablas/vistas; pantalla pendiente | Consultas por cliente y generación de documentos coherentes con movimientos |
| Seguridad, §11 | Roles, usuarios y permisos en SQL | Inicio/cierre de sesión, hash de claves y autorización efectiva en servidor |
| Base relacional, §12 | Script amplio, modelo ER, lógico y diccionario | Verificar instalación actual y mantener correspondencia con la aplicación |
| Arquitectura, §15 | Web Forms VB/.NET Framework 4.8, BL, DAL y Entities | Extender separación de responsabilidades a los módulos restantes |
| Auditoría, §§3, 12 y 15 | Tabla y triggers de protección | Insertar eventos desde operaciones con usuario autenticado |
| Diez reportes, §14 | Diez vistas; listado/exportación de clientes | Conectar reportes, filtros, permisos y salidas; nueve categorías financieras pendientes |
| Pruebas, §§16 y 18 | Script de clientes | Cinco métodos con resultados manuales documentados y escenarios financieros |
| Entregables, §17 | Código y documentación de BD | Requisitos propios, historias/casos de uso, diagramas de uso/clases, arquitectura, manuales, plan de pruebas y demostración |

Evidencia de módulos pendientes: los ASPX de Productos, Solicitudes, Simulador, Préstamos, Pagos, EstadosCuenta, Reportes, Usuarios y Auditoría muestran el mensaje de disponibilidad futura. Sus archivos de código no contienen operaciones de negocio. BL y DAL contienen únicamente el servicio y repositorio de clientes como implementación de negocio.

## Qué está bien en clientes

- Alta, edición, perfil, búsqueda, filtros, paginación, desactivación y reactivación implementados.
- El formulario y la entidad incluyen todos los grupos mínimos de datos del §6.
- Validación en servidor mediante `ClienteServicio.Validar`; no depende exclusivamente del navegador.
- Persona jurídica exige RUC y los datos personales no aplicables se normalizan.
- SQL parametrizado, búsquedas literales y restricción UNIQUE sobre identificación, incluso para inactivos.
- Control de concurrencia mediante Version y comprobación de filas afectadas en edición/cambio de estado.
- Catálogos territoriales dependientes y verificación de jerarquía en DAL; las claves compuestas del SQL refuerzan esa integridad.
- Códigos de ubicación tratados como texto para conservar ceros iniciales.
- Uso de Decimal y DECIMAL(14,2), rechazo de ingresos negativos y de más de dos decimales.
- Mensajes de error codificados, perfil con HtmlEncode y tratamiento de excepciones.
- Exportación CSV con protección para inicios de fórmula. Imprimir/PDF usa la impresión del navegador; no es un generador de PDF del servidor.

## Restricciones actuales y evaluación

| Campo | Restricción del servidor | Evaluación |
|---|---|---|
| Tipo de persona | NATURAL/JURIDICA | Correcta como catálogo cerrado |
| Tipo de identificación | CEDULA/PASAPORTE/RUC; jurídica requiere RUC | Falta coherencia de natural con el CHECK SQL |
| Identificación | Obligatoria; máximo 30; normaliza espacios y mayúsculas | Longitud consistente con SQL; formatos insuficientes |
| Cédula | Letras/números, guion, números, guion, números | Demasiado general para afirmar validación panameña completa |
| Pasaporte | 5–30 letras/números | Política implementada; debe documentarse el alcance de documentos aceptados |
| RUC | Grupos alfanuméricos unidos por guiones | Acepta incluso un único carácter; DV no está conectado al formulario |
| Nombres/razón social | Obligatorio; hasta 120 | No controla calidad semántica; admite solo números |
| Apellidos | Obligatorios para natural; hasta 120 | Igual observación; no debe aplicarse una regla de solo letras a razón social |
| Nacimiento | Obligatorio para natural; entre hoy menos 120 años y hoy | Rechaza futuro, pero acepta recién nacidos; edad mínima es decisión de negocio |
| Sexo | F/M/OTRO para natural; NO_APLICA para jurídica | Validado; SQL acepta además otras representaciones heredadas |
| Estado civil | Catálogo cerrado para natural; NO_APLICA para jurídica | Validado; SQL lo vincula a catálogo |
| Teléfono | Elimina formato y +507 cuando procede; 7–8 dígitos | Buena normalización; cualquier combinación de dígitos pasa |
| Correo | Obligatorio; hasta 254; MailAddress y dominio con punto | Validación de sintaxis razonable; no comprueba existencia |
| Dirección | Obligatoria; hasta 300 | Límite consistente con SQL |
| Ubicación | Códigos 2/4/6 caracteres numéricos; jerarquía consultada | Bien protegida al guardar; verificar catálogo instalado |
| Información laboral | Texto obligatorio; hasta 200 | Cubre el mínimo general, no captura datos laborales estructurados |
| Ingresos | 0–999999999999.99; máximo 2 decimales | Consistente técnicamente; el máximo es de almacenamiento, no de capacidad de pago |
| Referencias | Texto obligatorio; hasta 1000 | No valida nombre, contacto ni relación de cada referencia |

El enunciado pide contexto panameño, pero no fija regex exactas, edades, prefijos telefónicos, cantidad mínima de referencias ni montos máximos de ingresos. Esas políticas deben justificarse y documentarse; no sería correcto presentarlas como números impuestos por el PDF. Esta revisión no certifica formatos legales ni vigencia de catálogos oficiales.

## Hallazgos que conviene corregir

### 1. Alta prioridad: natural con RUC pasa BL y falla en SQL

`SistemaFinanciera.BL/ClienteServicio.vb`, método Validar, comprueba el RUC para jurídica, pero no restringe los documentos de natural. El formulario permite seleccionarlo. En `Database/MySQL/CapitalWest/CapitalWest.sql`, línea 874, el CHECK permite únicamente CEDULA/PASAPORTE para natural.

Reproducción ejecutada sobre las DLL recién compiladas: persona natural con RUC `12345-1-123456` devuelve cero errores BL. Con el esquema entregado, la escritura sería rechazada por el CHECK; la página terminaría con un mensaje genérico de operación fallida. La escritura no se ejecutó por falta de conexión.

Acción: acordar la política de identificación, alinear formulario/BL/SQL y cubrirla con una prueba. Si se conserva la política del SQL, restringir el selector y rechazar el caso en BL con mensaje claro.

### 2. Alta prioridad: formatos demasiado permisivos

Reproducciones ejecutadas: cédula `ABC-0-0`, RUC jurídico `A` y teléfono `0000000` devuelven cero errores BL.

Esto demuestra que las validaciones comprueban una forma muy general, no la calidad completa del dato. No se debe anunciar identificación panameña validada con estas reglas. Definir formatos y excepciones admitidas, separar DV cuando corresponda y agregar casos positivos/negativos sin bloquear identificaciones válidas por una regex demasiado estrecha.

### 3. Alta prioridad: seguridad y auditoría no conectadas

No hay implementación de login ni comprobaciones de permisos en las operaciones de clientes revisadas. Page_Load del master está vacío; las tablas de permisos no se consultan para autorizar. Guardar y CambiarEstado tampoco insertan Auditoria ni asignan CreadoPor/ModificadoPor. Son requisitos explícitos todavía pendientes, aun en el módulo funcional.

Acción: resolver identidad autenticada y autorización en servidor; registrar la operación y el usuario de forma coherente con la persistencia. Los triggers que protegen Auditoria no generan eventos automáticamente.

### 4. Prioridad media: referencias e información laboral poco estructuradas

Una referencia `x` devuelve cero errores BL. La etiqueta solicita nombre/contacto/relación, pero solo se comprueba texto no vacío. Existe ReferenciasCliente en SQL, pero la entidad y DAL actuales solo guardan el texto Referencias. Empleador, Cargo, AntiguedadMeses y RucDv existen en SQL y no están implementados en entidad/formulario/DAL.

Acción: definir qué datos son obligatorios según persona, capturar referencias repetibles si se necesita validar su contenido y conectar las columnas que realmente se usarán. Los campos laborales adicionales no son individualmente obligatorios en el PDF, aunque mejoran el diseño.

### 5. Decisiones pendientes: edad, ingresos y nombres

La BL acepta nacimiento hoy, ingresos cero y nombres `12345`. Los tres comportamientos se verificaron por ejecución para nacimiento/nombre y por lectura de la regla para ingresos cero. No todos constituyen incumplimiento automático: el PDF no prohíbe registrar menores ni personas sin ingresos.

Acción: distinguir registro de cliente de elegibilidad de crédito. Aplicar edad/capacidad de pago al proceso correspondiente y documentar la política. Para nombres naturales, definir una regla que admita acentos, espacios, apóstrofos y guiones sin aceptar valores carentes de contenido; la razón social necesita otra política.

### 6. Prioridad media: experiencia de validación

Guardar establece `form.noValidate=true`; no hay validadores por campo ni atributos required. La BL sí protege el guardado, pero los errores se presentan como bloque y pueden corresponder a otra pestaña oculta. MaxLength no es una protección suficiente para textarea en el navegador; el servidor sí limita Referencias a 1000.

Acción: mantener la validación de servidor y añadir mensajes por campo, indicadores de obligatoriedad y enfoque de la pestaña con error. Los límites de longitud y opciones deberían comunicarse antes de guardar.

### 7. Prioridad baja: fechas del dashboard

`Default.aspx.vb` agrupa y muestra CreadoUtc sin conversión a Panamá. Cerca del cambio de día/mes puede presentar la fecha o mes UTC en lugar del local.

Acción: conservar almacenamiento UTC y convertir para presentación/agrupación cuando el objetivo sea actividad por fecha de Panamá.

### 8. Documentación de cobertura desactualizada

`Cobertura_requisitos.md` dice que el catálogo territorial se entrega vacío, pero el SQL contiene datos y LEEME.md dice que ya están cargados. También menciona validación de edad mínima como responsabilidad de BL sin que la implementación actual la exija.

Acción: separar lo implementado, lo diseñado y lo propuesto, y corregir esas afirmaciones para no tomar planes por comportamiento actual.

## Base de datos: avance real y límites

El script contiene 40 tablas y 15 vistas, incluyendo diez vistas de reportes. Contempla usuarios, roles, permisos, clientes, productos, tasas, solicitudes, préstamos, cuotas, pagos, aplicaciones, movimientos, refinanciamiento e historial. Incluye FK, UNIQUE, CHECK y DECIMAL, y documentación ER/lógica/diccionario.

Eso cubre una parte importante del diseño. No completa por sí solo las operaciones: pagar exige bloqueo/transacción, comprobar saldo exigible, aplicar capital/interés/mora/cargos y registrar movimientos/auditoría. Formalizar requiere comprobar aprobación y condiciones; cambiar catálogos no debe alterar contratos existentes. La documentación reconoce estas responsabilidades pendientes de BL.

Antes de desarrollar cálculos hay que fijar conversión de tasa nominal/efectiva, periodicidad de capitalización y de pago, fechas, redondeo y ajuste final. El glosario distingue esos conceptos. También define saldo pendiente como capital por pagar: no debe confundirse con intereses futuros programados ni con importe de liquidación anticipada.

## Verificación efectuada

1. Compilación: MSBuild de Visual Studio, solución SistemaFinanciera.slnx, Debug. Resultado correcto en los cuatro proyectos, sin errores mostrados.
2. Script existente Tests/ProbarClientes.ps1: pasaron 11 comprobaciones iniciales. Se detuvo en la primera consulta territorial por no poder conectar a MySQL en 127.0.0.1:3306. No llegó al bloque de altas de prueba.
3. Siete casos adicionales ejecutados en memoria contra ClienteServicio.Validar: natural con RUC, cédula ABC-0-0, RUC A, nombre numérico, teléfono de ceros, nacimiento hoy y referencia x. Los siete devolvieron cero errores. No persistieron datos.
4. La documentación histórica informa 42 comprobaciones de clientes y 28 SQL en una instancia aislada. No se presentan como repetidas o aprobadas en esta sesión.

Pendiente para certificar funcionamiento: iniciar o disponer de la BD configurada, comprobar esquema/catálogos instalados, repetir integración y recorrer alta/edición/errores/desactivación/exportación en navegador y vista móvil. No se inició un servicio ni se importó/reemplazó una base como parte de esta auditoría.

## Orden recomendado para retomar

1. Cerrar clientes: coherencia de identificación, calidad de documentos/referencias, políticas documentadas y pruebas de límites. Verificar contra la instalación real.
2. Implementar autenticación, roles y registro de auditoría, aplicándolos también a clientes.
3. Implementar el motor de cinco métodos con una abstracción extensible y pruebas matemáticas independientes; construir después la interfaz del simulador.
4. Conectar productos/tasas y solicitudes, evaluación, formalización y desembolso.
5. Implementar pagos transaccionales, saldos, mora, cancelación y refinanciamiento/reestructuración.
6. Completar historial, estados de cuenta y los diez reportes.
7. Elaborar y mantener los entregables desde cada módulo: fórmulas/variables, ejemplos manuales, casos de prueba, diagramas y manuales. Reservar al final únicamente integración y demostración.

El punto concreto de reanudación es terminar de cerrar el módulo de clientes sobre la base CapitalWest ya diseñada. El siguiente componente financiero central es el motor de cálculo; actualmente no existe evidencia de implementación ejecutable de ninguno de los cinco métodos.

## Actualización: importación en XAMPP

Después de que el usuario encendió XAMPP e indicó que no había importado la base, se comprobó la conexión a MariaDB 10.4.32 en 127.0.0.1:3306. La base capitalwest no existía. Se importó CapitalWest.sql sin errores y se verificaron 40 tablas, 15 vistas, cuatro roles, cinco métodos y los catálogos territoriales (13 provincias, 82 distritos y 668 corregimientos).

Se repitió Tests/ProbarClientes.ps1 contra esa instalación: pasaron las 42 comprobaciones, incluidas altas, edición, duplicados, concurrencia, jerarquía territorial y desactivación/reactivación. Los datos de prueba se revirtieron. Esta actualización resuelve la limitación de conexión reportada arriba; los hallazgos de validación y los módulos pendientes siguen vigentes. No se verificó la interfaz en navegador.

## Actualización: esquema simplificado

A petición del usuario, el esquema pasó de 40 a 27 tablas manteniendo 15 vistas y las restricciones de las tablas retenidas. Se retiraron extensiones y se documentó la consolidación futura de autorización por roles e historiales en Auditoria. Véase Database/MySQL/CapitalWest/Simplificacion.md para las decisiones y el respaldo. La importación nueva y la migración local pasaron 42 pruebas de clientes cada una; las 15 vistas se consultan. Las menciones anteriores a las tablas retiradas describen el estado previo a esta simplificación. Los demás hallazgos funcionales siguen pendientes.
