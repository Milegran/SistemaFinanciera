Option Strict On
Imports System.Data.SqlClient
Imports SistemaFinanciera.Entities

' Acceso parametrizado; la restricción UNIQUE también protege contra altas simultáneas.
Public Class ClienteRepositorio
    Implements IClienteRepositorio
    Private ReadOnly conexion As String
    Public Sub New(cadenaConexion As String)
        If String.IsNullOrWhiteSpace(cadenaConexion) Then Throw New ArgumentException("Configure la conexión SistemaFinanciera.")
        conexion = cadenaConexion
    End Sub
    Public Function Listar(busqueda As String, activo As Nullable(Of Boolean)) As List(Of Cliente) Implements IClienteRepositorio.Listar
        Dim resultado As New List(Of Cliente)
        Using cn As New SqlConnection(conexion), cmd As New SqlCommand("SELECT * FROM dbo.Clientes WHERE (@Estado IS NULL OR Activo=@Estado) AND (@Busqueda=N'' OR CHARINDEX(@Busqueda,Identificacion)>0 OR CHARINDEX(@Busqueda,Nombres+N' '+Apellidos)>0 OR CHARINDEX(@Busqueda,Correo)>0 OR CHARINDEX(@Busqueda,Telefono)>0) ORDER BY Nombres,Apellidos,Id", cn)
            cmd.Parameters.Add("@Estado", SqlDbType.Bit).Value = If(activo.HasValue, CType(activo.GetValueOrDefault(), Object), DBNull.Value)
            cmd.Parameters.Add("@Busqueda", SqlDbType.NVarChar, 254).Value = busqueda
            cn.Open()
            Using lector = cmd.ExecuteReader()
                While lector.Read()
                    resultado.Add(Leer(lector))
                End While
            End Using
        End Using
        Return resultado
    End Function
    Public Function Obtener(id As Integer) As Cliente Implements IClienteRepositorio.Obtener
        Using cn As New SqlConnection(conexion), cmd As New SqlCommand("SELECT * FROM dbo.Clientes WHERE Id=@Id", cn)
            cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id
            cn.Open()
            Using lector = cmd.ExecuteReader()
                If lector.Read() Then Return Leer(lector)
            End Using
        End Using
        Throw New InvalidOperationException("El cliente ya no está disponible. Actualice el listado.")
    End Function
    Public Function Guardar(c As Cliente) As Integer Implements IClienteRepositorio.Guardar
        Dim sql = If(c.Id = 0, "INSERT INTO dbo.Clientes (TipoPersona, TipoIdentificacion, Identificacion, Nombres, Apellidos, FechaNacimiento, Sexo, EstadoCivil, Telefono, Correo, Direccion, Provincia, Distrito, Corregimiento, Ocupacion, Ingresos, Referencias) OUTPUT INSERTED.Id VALUES (@TipoPersona, @TipoIdentificacion, @Identificacion, @Nombres, @Apellidos, @FechaNacimiento, @Sexo, @EstadoCivil, @Telefono, @Correo, @Direccion, @Provincia, @Distrito, @Corregimiento, @Ocupacion, @Ingresos, @Referencias)", "UPDATE dbo.Clientes SET TipoPersona=@TipoPersona, TipoIdentificacion=@TipoIdentificacion, Identificacion=@Identificacion, Nombres=@Nombres, Apellidos=@Apellidos, FechaNacimiento=@FechaNacimiento, Sexo=@Sexo, EstadoCivil=@EstadoCivil, Telefono=@Telefono, Correo=@Correo, Direccion=@Direccion, Provincia=@Provincia, Distrito=@Distrito, Corregimiento=@Corregimiento, Ocupacion=@Ocupacion, Ingresos=@Ingresos, Referencias=@Referencias, ModificadoUtc=SYSUTCDATETIME() OUTPUT INSERTED.Id WHERE Id=@Id AND Version=@Version")
        Try
            Using cn As New SqlConnection(conexion), cmd As New SqlCommand(sql, cn)
                Parametros(cmd, c)
                If c.Id <> 0 Then
                    cmd.Parameters.Add("@Id", SqlDbType.Int).Value = c.Id
                    cmd.Parameters.Add("@Version", SqlDbType.Timestamp, 8).Value = If(CType(c.Version, Object), DBNull.Value)
                End If
                cn.Open()
                Dim resultado = cmd.ExecuteScalar()
                If resultado Is Nothing Then Throw New InvalidOperationException("Otro usuario modificó el cliente. Vuelva a abrirlo antes de guardar.")
                Return Convert.ToInt32(resultado)
            End Using
        Catch ex As SqlException When ex.Number = 2601 OrElse ex.Number = 2627
            Throw New ArgumentException("Ya existe un cliente con esa identificación, incluso si está inactivo.", ex)
        End Try
    End Function
    Public Sub CambiarEstado(id As Integer, activo As Boolean, version As Byte()) Implements IClienteRepositorio.CambiarEstado
        Using cn As New SqlConnection(conexion), cmd As New SqlCommand("UPDATE dbo.Clientes SET Activo=@Activo, ModificadoUtc=SYSUTCDATETIME() WHERE Id=@Id AND Version=@Version", cn)
            cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id
            cmd.Parameters.Add("@Activo", SqlDbType.Bit).Value = activo
            cmd.Parameters.Add("@Version", SqlDbType.Timestamp, 8).Value = version
            cn.Open()
            If cmd.ExecuteNonQuery() <> 1 Then Throw New InvalidOperationException("Otro usuario modificó el cliente. Actualice el listado.")
        End Using
    End Sub
    Private Shared Sub Parametros(cmd As SqlCommand, c As Cliente)
        cmd.Parameters.Add("@TipoPersona", SqlDbType.NVarChar, 10).Value = If(c.TipoPersona, "").Trim()
        cmd.Parameters.Add("@TipoIdentificacion", SqlDbType.NVarChar, 10).Value = If(c.TipoIdentificacion, "").Trim()
        cmd.Parameters.Add("@Identificacion", SqlDbType.NVarChar, 30).Value = If(c.Identificacion, "").Trim()
        cmd.Parameters.Add("@Nombres", SqlDbType.NVarChar, 120).Value = If(c.Nombres, "").Trim()
        cmd.Parameters.Add("@Apellidos", SqlDbType.NVarChar, 120).Value = If(c.Apellidos, "").Trim()
        cmd.Parameters.Add("@FechaNacimiento", SqlDbType.Date).Value = If(c.FechaNacimiento.HasValue, CType(c.FechaNacimiento.GetValueOrDefault().Date, Object), DBNull.Value)
        cmd.Parameters.Add("@Sexo", SqlDbType.NVarChar, 10).Value = If(c.Sexo, "").Trim()
        cmd.Parameters.Add("@EstadoCivil", SqlDbType.NVarChar, 15).Value = If(c.EstadoCivil, "").Trim()
        cmd.Parameters.Add("@Telefono", SqlDbType.NVarChar, 8).Value = If(c.Telefono, "").Trim()
        cmd.Parameters.Add("@Correo", SqlDbType.NVarChar, 254).Value = If(c.Correo, "").Trim()
        cmd.Parameters.Add("@Direccion", SqlDbType.NVarChar, 300).Value = If(c.Direccion, "").Trim()
        cmd.Parameters.Add("@Provincia", SqlDbType.NVarChar, 80).Value = If(c.Provincia, "").Trim()
        cmd.Parameters.Add("@Distrito", SqlDbType.NVarChar, 80).Value = If(c.Distrito, "").Trim()
        cmd.Parameters.Add("@Corregimiento", SqlDbType.NVarChar, 80).Value = If(c.Corregimiento, "").Trim()
        cmd.Parameters.Add("@Ocupacion", SqlDbType.NVarChar, 200).Value = If(c.Ocupacion, "").Trim()
        Dim ingresos = cmd.Parameters.Add("@Ingresos", SqlDbType.Decimal)
        ingresos.Precision = 14
        ingresos.Scale = 2
        ingresos.Value = c.Ingresos
        cmd.Parameters.Add("@Referencias", SqlDbType.NVarChar, 1000).Value = If(c.Referencias, "").Trim()
    End Sub
    Private Shared Function Leer(r As SqlDataReader) As Cliente
        Dim c As New Cliente With {.CreadoUtc = CDate(r("CreadoUtc")), .Id = CInt(r("Id")), .Activo = CBool(r("Activo")), .Version = DirectCast(r("Version"), Byte())}
        c.TipoPersona = CStr(r("TipoPersona"))
        c.TipoIdentificacion = CStr(r("TipoIdentificacion"))
        c.Identificacion = CStr(r("Identificacion"))
        c.Nombres = CStr(r("Nombres"))
        c.Apellidos = CStr(r("Apellidos"))
        c.FechaNacimiento = If(r.IsDBNull(r.GetOrdinal("FechaNacimiento")), CType(Nothing, Nullable(Of Date)), CDate(r("FechaNacimiento")))
        c.Sexo = CStr(r("Sexo"))
        c.EstadoCivil = CStr(r("EstadoCivil"))
        c.Telefono = CStr(r("Telefono"))
        c.Correo = CStr(r("Correo"))
        c.Direccion = CStr(r("Direccion"))
        c.Provincia = CStr(r("Provincia"))
        c.Distrito = CStr(r("Distrito"))
        c.Corregimiento = CStr(r("Corregimiento"))
        c.Ocupacion = CStr(r("Ocupacion"))
        c.Ingresos = CDec(r("Ingresos"))
        c.Referencias = CStr(r("Referencias"))
        Return c
    End Function
End Class
