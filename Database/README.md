# Configuración de CapitalWest con XAMPP

La aplicación Web Forms utiliza ahora MariaDB/MySQL mediante MySqlConnector 2.6.2. Se ejecuta en IIS Express desde Visual Studio; XAMPP proporciona la base de datos y phpMyAdmin.

## Preparar cada equipo

1. En XAMPP, inicie MySQL. Inicie Apache para abrir phpMyAdmin.
2. En phpMyAdmin seleccione Importar y el archivo `Database/MySQL/CapitalWest/CapitalWest.sql`, formato SQL, UTF-8. El script crea `capitalwest`.
3. Importe una sola vez en una base vacía. Si ya importó este mismo archivo y existen sus 40 tablas, no necesita volver a importarlo. El esquema anterior `sistema_financiera` no es el que utiliza esta conexión.
4. Abra `SistemaFinanciera.slnx` en Visual Studio y restaure los paquetes NuGet de la solución. La DAL usa PackageReference para MySqlConnector; el proyecto web conserva sus packages.config existentes. Compile la solución.
5. Revise la conexión `SistemaFinanciera` en `SistemaFinanciera/Web.config`. Para XAMPP local usa: `Server=127.0.0.1;Port=3306;Database=capitalwest;User ID=root;Password=;Connection Timeout=15;Character Set=utf8mb4`. Cambie puerto, usuario y contraseña si su instalación es diferente. No suba contraseñas reales al repositorio.
6. Ejecute con IIS Express. El dashboard debe mostrar cero clientes en una base nueva, sin aviso rojo. Registre un cliente para verificar el recorrido completo.

Git actualiza el código y los scripts SQL, pero no importa bases ni transfiere los datos de un equipo a otro. Cada integrante puede usar una base local independiente. Para compartir datos, la empresa/equipo debe configurar un servidor común y sus credenciales.

## Módulo de clientes

Web Forms → ClienteServicio → ClienteRepositorio → MariaDB. La DAL utiliza consultas parametrizadas, LAST_INSERT_ID a través de MySqlCommand.LastInsertedId, búsquedas literales y DECIMAL(14,2). Cada conexión configura UTC.

La identificación es única incluso para clientes inactivos. La desactivación conserva el registro. La columna Version es BIGINT UNSIGNED y se incrementa con cada modificación. La UI conserva un token opaco de ocho bytes que la DAL convierte al contador numérico; no usa rowversion de SQL Server. Si dos usuarios editan la misma versión, la segunda operación se rechaza.

Los catálogos Sexo/EstadoCivil del formulario coinciden con CapitalWest.sql, incluida la persona jurídica con NO_APLICA. La ubicación se selecciona desde las tablas suministradas `provincia`, `distrito` y `corregimiento`, y se guardan sus códigos originales y nombres. Las referencias continúan como texto. Préstamos/pagos, autenticación y auditoría de usuarios desde la aplicación requieren implementación adicional.

No se migran automáticamente clientes de LocalDB a MariaDB. `001_Clientes.sql` se conserva como script histórico de SQL Server y no debe ejecutarse en phpMyAdmin. Use únicamente CapitalWest.sql para una instalación nueva.

## Pruebas

Después de compilar e importar, ejecute en PowerShell:

```powershell
powershell -ExecutionPolicy Bypass -File Tests/ProbarClientes.ps1
```

Si usa otro puerto o usuario, pase `-Conexion` con su cadena. El script compila un ensamblado de pruebas en `Tests/obj` y lo ejecuta en un AppDomain con las mismas redirecciones de ensamblados de Web.config; obj está excluido de Git. Las operaciones usan TransactionScope y se revierten; no se guardan clientes de ejemplo. Las pruebas verifican reglas, CRUD, duplicados, filtros, persona jurídica, concurrencia y tipos de datos de MariaDB.

Si se ve el diseño anterior, recargue con Ctrl + Shift + R. Site.Master incluye versión en las URLs de los estilos para actualizar la caché tras este cambio.


Catálogos geográficos obligatorios: consulte [Ubicación]( MySQL/Ubicacion/LEEME.md ). Para una instalación existente importe `ActualizarUbicacion.sql`; el esquema completo se reserva para bases nuevas.
