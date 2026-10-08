-- Ejecutar en la base configurada en Web.config. No elimina tablas ni datos.
IF OBJECT_ID(N'dbo.Clientes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Clientes (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Clientes PRIMARY KEY,
        TipoPersona nvarchar(10) NOT NULL,
        TipoIdentificacion nvarchar(10) NOT NULL,
        Identificacion nvarchar(30) NOT NULL,
        Nombres nvarchar(120) NOT NULL,
        Apellidos nvarchar(120) NOT NULL,
        FechaNacimiento date NULL,
        Sexo nvarchar(10) NOT NULL,
        EstadoCivil nvarchar(15) NOT NULL,
        Telefono nvarchar(8) NOT NULL,
        Correo nvarchar(254) NOT NULL,
        Direccion nvarchar(300) NOT NULL,
        Provincia nvarchar(80) NOT NULL,
        Distrito nvarchar(80) NOT NULL,
        Corregimiento nvarchar(80) NOT NULL,
        Ocupacion nvarchar(200) NOT NULL,
        Ingresos decimal(14,2) NOT NULL,
        Referencias nvarchar(1000) NOT NULL,
        Activo bit NOT NULL CONSTRAINT DF_Clientes_Activo DEFAULT 1,
        Version rowversion NOT NULL,
        CreadoUtc datetime2 NOT NULL CONSTRAINT DF_Clientes_Creado DEFAULT SYSUTCDATETIME(),
        ModificadoUtc datetime2 NOT NULL CONSTRAINT DF_Clientes_Modificado DEFAULT SYSUTCDATETIME(),
        CONSTRAINT UQ_Clientes_Identificacion UNIQUE (Identificacion),
        CONSTRAINT CK_Clientes_Ingresos CHECK (Ingresos >= 0),
        CONSTRAINT CK_Clientes_Persona CHECK (TipoPersona IN ('NATURAL','JURIDICA')),
        CONSTRAINT CK_Clientes_Identificacion CHECK (TipoIdentificacion IN ('CEDULA','PASAPORTE','RUC'))
    );
END;
