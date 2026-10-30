# Reportes financieros

La página Reportes incluye diez informes, filtros por fechas/cliente/producto, paginación de 12 filas y exportación de todo el resultado en CSV o una vista imprimible para guardar en PDF desde el navegador.

Estado de cuenta y amortización requieren seleccionar un préstamo. El saldo acumulado del estado de cuenta conserva los movimientos anteriores al filtro de fechas. Los pagos anulados no forman parte del reporte de cobros. La mora muestra una fila por cuota vencida; su importe ya está incluido en el total vencido. Los saldos son actuales, no un cierre contable histórico. El comparativo muestra métodos de préstamos reales con condiciones que pueden ser diferentes.

Cada reporte indica qué fecha se filtra. Hasta incluye todo el último día. Exportar e imprimir usan los filtros del resultado generado, aunque luego se hayan editado los campos; pulse Generar reporte para aplicar cambios. Las consultas se ejecutan nuevamente al paginar o exportar, por lo que reflejan modificaciones concurrentes de los datos.

## Verificación

Compile la solución y ejecute en Windows PowerShell:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tests\ProbarReportes.ps1
```

Las pruebas usan la conexión de Web.config y un administrador activo. Son de solo lectura: verifican las diez consultas y sus filtros, fechas inválidas, la selección obligatoria del préstamo, rechazo de usuario inexistente y escape del CSV. Se verificaron 31 casos y la precompilación ASP.NET. No crean pagos ni modifican clientes.

No requiere migración: utiliza las vistas existentes del esquema CapitalWest. La impresión abre una página independiente y ofrece el botón Imprimir / Guardar PDF. Se verificó el código y las consultas; la revisión visual automática quedó pendiente por una restricción de la herramienta del navegador.
