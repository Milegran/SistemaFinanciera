# Configuración del módulo de clientes

La conexión `SistemaFinanciera` en `SistemaFinanciera/Web.config` usa SQL Server LocalDB con autenticación integrada.
En otro equipo, cree una base `SistemaFinanciera`, ejecute `001_Clientes.sql` dentro de ella y ajuste la conexión según su servidor. El script no elimina datos.

Las capas son: Web Forms → ClienteServicio → ClienteRepositorio → SQL Server. La entidad y la interfaz del repositorio están en Entities.

La identificación se normaliza a mayúsculas y sin espacios; es única incluso para clientes inactivos. La desactivación conserva el registro. Rowversion evita sobrescribir cambios concurrentes y permite reactivar clientes.

Personas naturales: campos obligatorios, fecha no futura y no mayor de 120 años. Personas jurídicas: RUC y razón social; nacimiento, sexo, estado civil y apellidos no aplican. La validación de identificación comprueba formato básico, no verifica su existencia ante una autoridad. Provincia/comarca, distrito y corregimiento se capturan como texto; no se verifica su relación geográfica. Ingresos: no negativos y hasta dos decimales. Teléfono: 7 u 8 dígitos con prefijo +507 opcional.

La autenticación, autorización por roles y auditoría de usuarios del sistema siguen pendientes; no forman parte de este módulo.

Pruebas: `Tests/ProbarClientes.ps1` valida reglas y ejecuta CRUD, duplicados, filtros y conflictos de edición dentro de una transacción que se revierte. Ejecutar después de compilar y crear la tabla: `powershell -ExecutionPolicy Bypass -File Tests/ProbarClientes.ps1`.
