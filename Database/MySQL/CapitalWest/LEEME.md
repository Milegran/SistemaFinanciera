# Base de datos CapitalWest

Diseñada a partir de **Proyecto No. 2 DS8.pdf** y **Diseno_de_pantallas_Sistema_Financiero (1).docx**, entregados por el usuario. El enunciado determina los requisitos obligatorios. El documento de pantallas completa los datos y propone algunas reglas; esas propuestas se identifican en ParametrosSistema.EsPropuesta.

## Importación en phpMyAdmin

1. Inicie MySQL desde XAMPP y abra phpMyAdmin.
2. Seleccione **Importar**, el archivo **CapitalWest.sql**, formato SQL y codificación UTF-8.
3. Ejecute la importación. El archivo crea la base **capitalwest** con 40 tablas, 15 vistas y dos triggers que protegen la auditoría.
4. Compruebe que aparecen los cuatro roles, cinco métodos de cálculo, nueve estados y diez reportes.

Importe una sola vez en una base vacía. El archivo no contiene instrucciones para borrar bases ni tablas. No es una migración: no lo importe sobre tablas existentes. Si ya tiene una base capitalwest con datos, use una migración preparada para ese esquema, o cambie CREATE DATABASE y USE a un nombre nuevo. El script anterior SistemaFinanciera.sql se conserva como versión anterior.

Validado mediante importación real en **MariaDB 10.4.32 de XAMPP**. Utiliza InnoDB, claves foráneas, restricciones CHECK, UTF-8 y DECIMAL para importes. No se ha ejecutado en MySQL; para MySQL se requiere como mínimo 8.0.16 por CHECK y funciones de ventana. Cada conexión de la aplicación debe usar SET time_zone='+00:00'. Las fechas de vencimiento son fechas civiles de Panamá; las columnas Utc se guardan en UTC. Las vistas de mora usan la fecha de Panamá con UTC menos cinco horas.

## Archivos

- CapitalWest.sql: creación, catálogos, permisos, vistas y protección de auditoría.
- Diccionario_datos.md: campos, tipos, valores iniciales, obligatoriedad y claves, obtenidos desde la base importada.
- Modelo_logico.md: todas las claves foráneas, incluidos vínculos compuestos.
- Modelo_entidad_relacion.mmd: diagrama Mermaid. Resume relaciones por pareja de tablas; las restricciones UNIQUE y las relaciones compuestas se detallan en el SQL y el modelo lógico.
- Cobertura_requisitos.md: correspondencia con las dos fuentes y decisiones del modelo.
- Validacion.json: resultados de 28 comprobaciones y versión del servidor utilizado.

## Datos incluidos

Catálogos de roles, estados, métodos, periodicidades, tipos de tasa, formas de pago, estados civiles, permisos, reportes y parámetros. No incluye usuarios de acceso, contraseñas, clientes, productos ni préstamos de ejemplo. Cree el primer administrador desde un proceso de alta que calcule un hash seguro y le asigne ADMINISTRADOR en UsuarioRoles.

Los catálogos suministrados ya están cargados en `provincia`, `distrito` y `corregimiento`: 13 provincias, 82 distritos y 668 corregimientos. El formulario utiliza listas dependientes y guarda los códigos de texto originales, incluidos los ceros iniciales. Los nombres de ubicación del cliente se obtienen del catálogo. BRILLANTE (130601) se conserva sin habilitar porque falta su distrito 1306. Para actualizar una instalación existente, consulte `../Ubicacion/LEEME.md`.

ReferenciasCliente permite varias referencias por cliente; Referencias en Clientes conserva el texto del módulo actual. Empleador, Cargo y AntiguedadMeses complementan Ocupacion. RucDv permite separar el dígito verificador. La edad mínima, formatos de cédula/RUC, correo y al menos una referencia se validan en la lógica de negocio al guardar el conjunto completo.

## Condiciones financieras e historial

PlazoMeses es el plazo contractual del diseño; CantidadCuotas es independiente y debe calcularse según la periodicidad. Los límites PlazoMinimo/PlazoMaximo existentes representan cantidad de cuotas; los nuevos límites con sufijo Meses representan el plazo en meses. La BL debe comprobar ambos.

TasaPactada y TipoTasa en Solicitudes guardan las condiciones pedidas. Prestamos guarda una copia contractual completa de tasa, capitalización, costos y plazo. Cambiar el catálogo Tasas no cambia los contratos. EsPredeterminada debe mantenerse de forma transaccional para que solo haya una tasa predeterminada vigente por producto. La BL debe impedir intervalos de vigencia superpuestos cuando corresponda.

Cuotas contiene la amortización actual. VersionesAmortizacion y DetallesAmortizacionHistorica conservan versiones anteriores al recalcular por un abono anticipado. Antes de cambiar cuotas se guarda el plan anterior, conservando las cuotas que ya tienen aplicaciones. InteresCapitalizado es interés incorporado al capital, separado del interés cobrado. Los cinco métodos se implementan en clases de la BL; guardar su fórmula en MetodosCalculo no ejecuta esos algoritmos.

La periodicidad QUINCENAL inicial significa intervalos de 15 días y 24 periodos contractuales por año bajo la convención de 360 días propuesta. No equivale automáticamente a dos fechas fijas por mes. Confirme la convención del contrato, la conversión de tasa y las fechas del plan antes de generar cuotas.

SaldoCapital es saldo contable real; SaldoProyectado es saldo del plan. InteresesProgramadosPendientes incluye intereses futuros del calendario: no es interés devengado ni el importe de cancelación anticipada. No use esa columna como liquidación a la fecha.

AplicacionesPago registra varias cuotas por recibo y varios abonos por cuota. Las claves compuestas impiden aplicar pagos a cuotas de otro préstamo. Las vistas excluyen pagos anulados del saldo por cuota. v_PagosDesglosados muestra DiferenciaAplicacion para comprobar que el recibo coincide con el desglose.

MovimientosPrestamo alimenta el estado de cuenta mediante débitos y créditos y saldo acumulado. El saldo depende de que la BL contabilice todos los desembolsos, intereses devengados, mora, cargos, pagos y reversas, una sola vez. No se genera automáticamente al insertar un recibo. Las reversas conservan el movimiento original y ReversaDeId impide revertirlo dos veces.

## Operaciones que debe ejecutar la aplicación

Las restricciones de esta base no sustituyen las transacciones de negocio. Formalizar, pagar, cancelar y refinanciar deben usar BEGIN/COMMIT y SELECT ... FOR UPDATE sobre el préstamo o solicitud antes de validar:

- Formalizar solo una solicitud aprobada, con cliente/producto activos, condiciones válidas y permisos. SolicitudId único impide formalizar dos veces.
- Verificar monto contra límites, métodos/periodicidades permitidos, tasas vigentes y datos evaluados. La aprobación y el rechazo requieren sus datos mínimos.
- Al registrar un pago, bloquear el préstamo, calcular la deuda exigible, impedir sobrepagos, comprobar el desglose y la prioridad mora → intereses → capital. Definir explícitamente el tratamiento de otros cargos. Insertar recibo y aplicaciones, actualizar SaldoCapital, movimientos, estado, historial y auditoría en la misma transacción. ClaveIdempotencia única permite evitar repetir una solicitud de pago; la aplicación la genera.
- Calcular mora con tasa y gracia configuradas; no hay una tasa obligatoria en el enunciado. Cambiar el estado al detectar o saldar cuotas vencidas.
- Liquidar anticipadamente con el interés devengado según el método, registrar el pago, dejar saldo cero y fecha de cancelación. No cobrar automáticamente todo el interés futuro.
- Refinanciar solo préstamos vigentes o en mora; verificar mismo cliente, marcar el original, crear el contrato nuevo y transferir el desglose de deuda sin duplicar ingresos o desembolsos. La suma del desglose se comprueba en la tabla.
- Anular pagos solo con permiso, motivo y fecha; revertir saldos y movimientos en la misma transacción. No borrar el recibo ni la auditoría.
- Registrar auditoría y los historiales con usuario autenticado; los triggers impiden editar/borrar Auditoria, pero no insertan eventos automáticamente. No almacenar contraseñas ni tokens sin hash en detalles.

Los permisos de rol son datos para la autorización en servidor. No basta ocultar botones. CONSULTA tiene permisos de lectura/generación; sus reportes específicos no se habilitan por defecto hasta que un administrador los autorice en RolReportes. EVALUAR/APROBAR/RECHAZAR se asigna inicialmente al Administrador como propone el documento, y puede delegarse mediante RolPermisos. Proteja los roles y estados EsSistema y no permita que el último administrador se desactive.

Los tokens de recuperación/sesión se almacenan como hash SHA-256 del token aleatorio; las contraseñas usan un algoritmo especializado para contraseñas con sal y parámetros. Al cambiar la clave, incremente VersionSesion y revoque sesiones. La comprobación de vencimiento y uso único corresponde a la BL.

## Conexión del proyecto actual

El módulo de clientes y el dashboard utilizan MySqlConnector y la base `capitalwest` de MariaDB/MySQL. La conexión predeterminada usa 127.0.0.1:3306 y usuario root para XAMPP local. Consulte Database/README.md para importar la base, restaurar NuGet y configurar cada equipo.

Version es un contador numérico de concurrencia que la DAL incrementa al actualizar, comprobando las filas afectadas. La interfaz conserva su token de ocho bytes. Los datos de LocalDB no se transfieren automáticamente; 001_Clientes.sql es histórico.

La migración del módulo existente no implementa todavía las operaciones financieras de los otros módulos.

## Verificación

La importación y las 28 pruebas SQL se ejecutaron en una instancia aislada, distinta del servicio MySQL habitual. La base entregada solo contiene catálogos. Además pasaron 42 comprobaciones de clientes mediante BL/DAL, incluida la conexión MariaDB y la jerarquía territorial; los datos de prueba fueron revertidos. Se comprobó la migración repetida y la conservación de clientes anteriores. Las pruebas SQL cubren estructura, permisos, referencias, valores inválidos, pagos completos/parciales, anulación, estado de cuenta y auditoría inmutable; no prueban los cinco algoritmos financieros pendientes de implementación.


Catálogos geográficos obligatorios: consulte [Ubicación]( ../Ubicacion/LEEME.md ). Para una instalación existente importe `ActualizarUbicacion.sql`; el esquema completo se reserva para bases nuevas.
