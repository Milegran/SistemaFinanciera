# Importación desde phpMyAdmin

1. Abra phpMyAdmin en su instalación de MySQL/MariaDB.
2. En **Importar**, seleccione `SistemaFinanciera.sql`, formato SQL y codificación UTF-8.
3. Ejecute la importación. El archivo crea y selecciona la base `sistema_financiera` y sus 17 tablas.
4. Si su cuenta no permite crear bases, cree/seleccione una base autorizada y quite las instrucciones `CREATE DATABASE` y `USE`, o cambie su nombre por el permitido.

Requiere MySQL 8.0.16 o posterior (por las restricciones CHECK), o MariaDB 10.6 o posterior. Usa InnoDB, claves foráneas y montos DECIMAL. No desactiva las claves foráneas ni elimina registros. Reimportar el mismo esquema no duplica los catálogos; no sustituye un procedimiento de migración para tablas de otra estructura.

Incluye usuarios, roles, clientes, cinco métodos de cálculo, periodicidades, estados, productos, métodos por producto, tasas, solicitudes, préstamos, cuotas, pagos, aplicaciones de pago, refinanciamientos y auditoría. La relación de aplicaciones permite un pago para varias cuotas y varios pagos para una cuota, siempre del mismo préstamo. El historial del cliente se obtiene desde sus préstamos.

La cuota estimada es Capital + Interes; mora y otros cargos se suman por separado. InteresCapitalizado registra lo que pasa al principal. SaldoProyectado pertenece a la tabla de amortización, no al saldo real posterior a pagos. El estado de una cuota (pendiente, parcial, vencida, pagada) se calcula con su vencimiento y las aplicaciones de pagos no anulados. El plazo se expresa mediante cantidad de cuotas y periodicidad; el monto solicitado y la fecha de solicitud se consultan desde Solicitudes.

Las reglas que involucran varios registros necesitan lógica de negocio y transacciones: confirmar que la solicitud esté aprobada al formalizar, comprobar producto y tasa vigentes, generar amortización, impedir sobrepagos, verificar que el importe del recibo coincida con sus aplicaciones, actualizar saldos/capitalización, anular pagos y registrar auditoría. Para pagos concurrentes, bloquear el préstamo con SELECT ... FOR UPDATE antes de validar y aplicar. Estas tablas no implementan los cinco algoritmos matemáticos.

No se crean usuarios de ejemplo ni contraseñas. La aplicación debe guardar un hash seguro en ContrasenaHash y asignar roles. Fechas operativas: acordar zona horaria en la aplicación; campos terminados en Utc usan UTC. Cada conexión debe configurar su zona horaria.

**La aplicación VB.NET actual sigue conectada a SQL Server.** Importar este archivo no migra los clientes existentes ni conecta automáticamente el programa a MySQL. Para usar esta base se debe adaptar ClienteRepositorio, agregar un proveedor MySQL y cambiar Web.config. La columna Version es un contador: usar UPDATE ... SET Version=Version+1 WHERE Id=@Id AND Version=@Version y comprobar filas afectadas.

Validación: estructura y referencias revisadas; no se ejecutó la importación contra un servidor MySQL/MariaDB en este entorno.

Referencia: https://dev.mysql.com/doc/refman/8.0/en/create-table-check-constraints.html
