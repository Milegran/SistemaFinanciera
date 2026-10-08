# Cobertura de las fuentes

Fuentes: Proyecto No. 2 DS8.pdf, secciones 3–19, y Diseño detallado de pantallas del Sistema Financiero, pantallas 3.1–10.8. Los requisitos del PDF son obligatorios; las propuestas de las pantallas se mantienen identificadas.

| Requisito | Fuente | Tablas o vistas |
|---|---|---|
| Autenticación, roles y autorización | PDF 11–12; pantallas 3 y 10.1–10.3 | Usuarios, Roles, UsuarioRoles, Permisos, RolPermisos |
| Bloqueo, recuperación y sesiones | Pantallas 3.1–3.3 | Usuarios, SesionesUsuario, RecuperacionesClave, ParametrosSistema |
| Clientes, identificación panameña y datos personales | PDF 6; pantallas 5.1–5.3 | Clientes, EstadosCiviles |
| Empleador, cargo, antigüedad y referencias repetibles | Pantalla 5.2 | Clientes, ReferenciasCliente |
| Listas territoriales dependientes | Pantallas 5.2 y 10.6 | Provincias, Distritos, Corregimientos |
| Productos, límites, costos y periodicidades | PDF 3, 5, 7; pantallas 6.1–6.2 | ProductosFinancieros, ProductoMetodos, ProductoPeriodicidades |
| Tasas, historial y condiciones pactadas | PDF 5, 12; pantallas 6.3 y 7.2 | Tasas, TiposTasa, Solicitudes, Prestamos |
| Cinco métodos extensibles y documentación | PDF 4, 13, 18–19; pantallas 10.4–10.5 | MetodosCalculo; clases de BL pendientes para ejecución |
| Solicitud, evaluación, aprobación y rechazo | PDF 7; pantallas 7.1–7.3 | Solicitudes, HistorialSolicitudes |
| Formalización, fechas, monto, tasa, plazo, gastos y estado | PDF 5, 7, 10; pantallas 7.4–7.6 | Prestamos, Estados, Periodicidades, HistorialPrestamos |
| Amortización y cambios por abonos anticipados | PDF 4, 8–9; pantallas 7.6–7.8 y 8.1 | Cuotas, VersionesAmortizacion, DetallesAmortizacionHistorica |
| Recibos, desglose, usuario y anulación | PDF 9; pantallas 8.1–8.3 | Pagos, AplicacionesPago, FormasPago |
| Estado de cuenta y movimientos | PDF 3, 14; pantallas 5.3 y 8.4 | MovimientosPrestamo, v_Reporte07EstadoCuenta |
| Cuotas vencidas y mora | PDF 3, 9; pantallas 4.3 y 8.5 | Cuotas, v_SaldosCuota, v_Reporte04PrestamosMora, ParametrosSistema |
| Refinanciamientos y reestructuración | PDF 3; pantallas 7.9–7.10 | Refinanciamientos, Prestamos, HistorialPrestamos |
| Los diez reportes y acceso por rol | PDF 14, 19; pantallas 9.1–9.2 | Reportes, RolReportes, EjecucionesReporte, diez vistas v_Reporte01…10 |
| Operaciones, valores anteriores y nuevos | PDF 3, 12, 15; pantalla 10.8 | Auditoria |
| Notificaciones y preferencias | Pantallas 2.3 y 4.5 | Notificaciones, PreferenciasUsuario |
| Configuración de mora, seguridad y evaluación | Pantalla 10.7 | ParametrosSistema |

## Decisiones y alcance

La base guarda los datos necesarios para las pantallas; no almacena botones, tarjetas o resultados de búsqueda como tablas. Los KPIs se calculan con consultas sobre operaciones reales. Los borradores y la persistencia de simulaciones son opcionales en el documento y no tienen tablas propias: una simulación puede ejecutarse sin guardar un préstamo. El catálogo de métodos contiene fórmulas descriptivas, no código ejecutable.

Se conserva compatibilidad de nombres con el SQL anterior donde ayuda a adaptar la DAL, pero esta base es nueva y no constituye una migración. Las referencias textuales y ubicación textual son campos de transición; sus tablas normalizadas están listas para nuevos formularios. El catálogo territorial se entrega vacío para cargar datos oficiales, evitando una lista incompleta presentada como definitiva.

La base comprueba relaciones y valores de un registro. Edad mínima, referencias obligatorias, catálogo activo, capacidad de pago, tasas superpuestas, cierre de sesiones, orden de aplicación, sobrepagos y transiciones completas dependen de validaciones transaccionales de la aplicación, descritas en LEEME.md.
